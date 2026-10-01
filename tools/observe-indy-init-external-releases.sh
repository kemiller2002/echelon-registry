#!/usr/bin/env bash
set -euo pipefail

work="${RUNNER_TEMP:-/tmp}/echelon-indy-release-evidence"
rm -rf "$work"
mkdir -p "$work/npm" "$work/nuget"

observe_npm() {
  local system="$1"
  local package="$2"
  local version="$3"
  local dir="$work/npm/$system"
  mkdir -p "$dir"

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

  if [[ ! "$git_head" =~ ^[0-9a-f]{40}$ ]]; then
    echo "::error::$package@$version did not expose a 40-character gitHead (observed '$git_head')"
    exit 1
  fi

  printf 'EVIDENCE|%s|version=%s|commit=%s|artifact=%s|sha256=%s|url=%s\n'     "$system" "$version" "$git_head" "$filename" "$sha" "$tarball"
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

  curl -fsSL "$url" -o "$path"
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
    echo "::error::$package $version nuspec did not expose a 40-character repository commit (observed '$commit')"
    cat "$nuspec"
    exit 1
  fi

  printf 'EVIDENCE|%s|version=%s|commit=%s|artifact=%s|sha256=%s|url=%s\n'     "$system" "$version" "$commit" "$filename" "$sha" "$url"
}

observe_npm "limen" "@echelon-foundry/typescript-wasm-kernel" "0.6.2"
observe_npm "folio" "@echelon-foundry/print-components" "0.3.0"
observe_nuget "aegis" "EchelonFoundry.Aegis.Core" "1.2.0"
