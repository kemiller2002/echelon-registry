#!/usr/bin/env bash
set -euo pipefail

work="${RUNNER_TEMP:-/tmp}/echelon-indy-release-evidence"
rm -rf "$work"
mkdir -p "$work/npm" "$work/nuget"

decode_attestation_commit() {
  local json_path="$1"
  python3 - "$json_path" <<'PY'
import base64, json, re, sys

with open(sys.argv[1], "r", encoding="utf-8") as f:
    root = json.load(f)

candidates = []

def visit(value, path="$"):
    if isinstance(value, dict):
        for k, v in value.items():
            visit(v, f"{path}.{k}")
    elif isinstance(value, list):
        for i, v in enumerate(value):
            visit(v, f"{path}[{i}]")
    elif isinstance(value, str):
        if re.fullmatch(r"[0-9a-f]{40}", value):
            candidates.append((path, value))
        if path.endswith(".payload"):
            try:
                decoded = base64.b64decode(value + "===")
                nested = json.loads(decoded)
                visit(nested, path + "<decoded>")
            except Exception:
                pass

visit(root)

preferred = [
    (path, value)
    for path, value in candidates
    if any(term in path.lower() for term in ("gitcommit", "commit", "resolveddependencies", "source"))
]

chosen = preferred[0] if preferred else (candidates[0] if candidates else None)
if chosen:
    print(chosen[1])
else:
    print("")
PY
}

observe_npm() {
  local system="$1"
  local package="$2"
  local version="$3"
  local dir="$work/npm/$system"
  mkdir -p "$dir"

  if ! npm view "$package@$version" version >/dev/null 2>&1; then
    printf 'EVIDENCE|%s|status=unpublished|package=%s|version=%s\n' "$system" "$package" "$version"
    return 0
  fi

  local packed_json
  packed_json="$(npm pack "$package@$version" --pack-destination "$dir" --json)"
  local filename
  filename="$(printf '%s' "$packed_json" | node -e 'let s="";process.stdin.on("data",d=>s+=d).on("end",()=>process.stdout.write(JSON.parse(s)[0].filename))')"
  local path="$dir/$filename"
  local sha
  sha="$(sha256sum "$path" | awk '{print $1}')"
  local git_head
  git_head="$(npm view "$package@$version" gitHead --json 2>/dev/null | tr -d '"' || true)"
  local tarball
  tarball="$(npm view "$package@$version" dist.tarball --json | tr -d '"')"
  local attestation_url
  attestation_url="$(npm view "$package@$version" dist.attestations.url --json 2>/dev/null | tr -d '"' || true)"
  local integrity
  integrity="$(npm view "$package@$version" dist.integrity --json 2>/dev/null | tr -d '"' || true)"

  local provenance_commit=""
  if [[ -n "$attestation_url" && "$attestation_url" != "null" ]]; then
    local attestation_path="$dir/attestations.json"
    curl -fsSL "$attestation_url" -o "$attestation_path"
    provenance_commit="$(decode_attestation_commit "$attestation_path")"
  fi

  local source_commit="$git_head"
  if [[ ! "$source_commit" =~ ^[0-9a-f]{40}$ && "$provenance_commit" =~ ^[0-9a-f]{40}$ ]]; then
    source_commit="$provenance_commit"
  fi
  if [[ ! "$source_commit" =~ ^[0-9a-f]{40}$ ]]; then
    source_commit="unavailable"
  fi

  printf 'EVIDENCE|%s|version=%s|commit=%s|artifact=%s|sha256=%s|url=%s|integrity=%s|attestation=%s\n' \
    "$system" "$version" "$source_commit" "$filename" "$sha" "$tarball" "$integrity" "${attestation_url:-unavailable}"
}

observe_nuget() {
  local system="$1"
  local package="$2"
  local version="$3"
  local lower
  lower="$(printf '%s' "$package" | tr '[:upper:]' '[:lower:]')"
  local dir="$work/nuget/$system"
  mkdir -p "$dir"
  local filename="$package.$version.nupkg"
  local path="$dir/$filename"
  local url="https://api.nuget.org/v3-flatcontainer/$lower/$version/$lower.$version.nupkg"

  if ! curl -fsSL "$url" -o "$path"; then
    printf 'EVIDENCE|%s|status=unpublished|package=%s|version=%s\n' "$system" "$package" "$version"
    return 0
  fi

  local sha
  sha="$(sha256sum "$path" | awk '{print $1}')"

  local nuspec="$dir/package.nuspec"
  unzip -p "$path" '*.nuspec' > "$nuspec"

  local commit
  commit="$(python3 - "$nuspec" <<'PY'
import sys, xml.etree.ElementTree as ET
root = ET.parse(sys.argv[1]).getroot()
repo = next((e for e in root.iter() if e.tag.endswith("repository")), None)
print("" if repo is None else repo.attrib.get("commit", ""))
PY
)"

  if [[ ! "$commit" =~ ^[0-9a-f]{40}$ ]]; then
    commit="unavailable"
  fi

  printf 'EVIDENCE|%s|version=%s|commit=%s|artifact=%s|sha256=%s|url=%s\n' \
    "$system" "$version" "$commit" "$filename" "$sha" "$url"
}

observe_npm "limen" "@echelon-foundry/typescript-wasm-kernel" "0.6.2"
observe_npm "folio" "@echelon-foundry/print-components" "0.3.0"
observe_nuget "aegis" "EchelonFoundry.Aegis.Core" "1.0.0"


echo "NUGET_VERSIONS|aegis|$(curl -fsSL https://api.nuget.org/v3-flatcontainer/echelonfoundry.aegis.core/index.json | tr -d '\n')"
