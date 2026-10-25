// REG-PAIR-001-R3: no Registry record names a release manifest that does not exist.
//
// Usage: dotnet fsi conformance/validate-manifest-references.fsx
//
// Checks, from the repository root:
// 1. every `releases/<system>/<version>.release.json` path named in a
//    checked-in JSON, Markdown, F# script or workflow file exists;
// 2. every release row of every catalog snapshot names an existing manifest
//    whose SHA-256 matches the row, and whose systemId and version match it;
// 3. every manifest under releases/ declares the systemId and version its
//    path names.
// It first proves that it fails on a synthetic tree with a dangling reference.

open System
open System.IO
open System.Security.Cryptography
open System.Text.Json
open System.Text.RegularExpressions

let referencePattern =
    Regex(@"releases/([a-z0-9-]+)/([0-9]+\.[0-9]+\.[0-9]+[0-9A-Za-z.+-]*?)\.release\.json", RegexOptions.Compiled)

let scannedExtensions = set [ ".json"; ".md"; ".fsx"; ".yml"; ".yaml" ]

/// Directories whose content is execution history or third-party state, not records.
let skippedDirectories = set [ ".git"; ".ros"; "node_modules"; "bin"; "obj" ]

let rec files (directory: string) =
    seq {
        for file in Directory.EnumerateFiles directory do
            if scannedExtensions.Contains(Path.GetExtension file) then yield file

        for child in Directory.EnumerateDirectories directory do
            if not (skippedDirectories.Contains(Path.GetFileName child)) then yield! files child
    }

let sha256 (path: string) =
    File.ReadAllBytes path |> SHA256.HashData |> Convert.ToHexString |> fun value -> value.ToLowerInvariant()

let relative (root: string) (path: string) = Path.GetRelativePath(root, path).Replace('\\', '/')

let tryString (name: string) (element: JsonElement) =
    if element.ValueKind <> JsonValueKind.Object then
        None
    else
        match element.TryGetProperty name with
        | true, value when value.ValueKind = JsonValueKind.String -> Some(value.GetString())
        | _ -> None

let danglingReferences (root: string) =
    [ for file in files root do
          let text = File.ReadAllText file

          for m in referencePattern.Matches text do
              let path = Path.Combine(root, "releases", m.Groups[1].Value, $"{m.Groups[2].Value}.release.json")

              if not (File.Exists path) then
                  yield $"{relative root file} names {m.Value}, which does not exist" ]
    |> List.distinct

let isSnapshot (file: string) =
    file.EndsWith(".json", StringComparison.Ordinal)
    && (try
            use doc = JsonDocument.Parse(File.ReadAllText file)
            tryString "schema" doc.RootElement = Some "echelon.catalog-snapshot/v1"
        with :? JsonException -> false)

let snapshotRowErrors (root: string) =
    [ for file in files root |> Seq.filter isSnapshot do
          use doc = JsonDocument.Parse(File.ReadAllText file)
          // The resolver reads snapshot manifest paths relative to the
          // snapshot's parent directory; the oldest fixtures are relative to
          // the repository root. Either names the manifest.
          let candidates manifest =
              [ Path.Combine(Path.GetDirectoryName file, "..", manifest); Path.Combine(root, manifest) ]
              |> List.map Path.GetFullPath

          for row in doc.RootElement.GetProperty("releases").EnumerateArray() do
              let systemId = tryString "systemId" row |> Option.defaultValue "<none>"
              let version = tryString "version" row |> Option.defaultValue "<none>"
              let manifest = tryString "manifest" row |> Option.defaultValue ""
              let label = $"{relative root file}: {systemId} {version}"

              match candidates manifest |> List.tryFind File.Exists with
              | None -> yield $"{label} names manifest {manifest}, which does not exist"
              | Some path ->
                  if tryString "sha256" row <> Some(sha256 path) then
                      yield $"{label} digest does not match {manifest}"

                  use release = JsonDocument.Parse(File.ReadAllText path)

                  if tryString "systemId" release.RootElement <> Some systemId
                     || tryString "version" release.RootElement <> Some version then
                      yield $"{label} names {manifest}, which records a different system or version" ]

let manifestPathErrors (root: string) =
    let releases = Path.Combine(root, "releases")

    if not (Directory.Exists releases) then
        []
    else
        [ for file in Directory.EnumerateFiles(releases, "*.release.json", SearchOption.AllDirectories) do
              let systemId = Path.GetFileName(Path.GetDirectoryName file)
              let version = Path.GetFileName(file).Replace(".release.json", "")
              use doc = JsonDocument.Parse(File.ReadAllText file)

              if tryString "systemId" doc.RootElement <> Some systemId || tryString "version" doc.RootElement <> Some version then
                  yield $"{relative root file} does not record systemId {systemId} version {version}" ]

let check root =
    danglingReferences root @ snapshotRowErrors root @ manifestPathErrors root

// Self-test: a tree whose snapshot and prose name a missing manifest must fail.
// The paths are assembled so that this file itself names no manifest.
let missing version = String.concat "/" [ "releases"; "praxis"; $"{version}.release.json" ]
let synthetic = Path.Combine(Path.GetTempPath(), $"manifest-references-{Guid.NewGuid():N}")
Directory.CreateDirectory(Path.Combine(synthetic, "snapshots")) |> ignore
Directory.CreateDirectory(Path.Combine(synthetic, "spec")) |> ignore

File.WriteAllText(
    Path.Combine(synthetic, "snapshots", "dangling.catalog.json"),
    """{ "schema": "echelon.catalog-snapshot/v1", "systems": [], "profiles": [],
         "releases": [ { "systemId": "praxis", "version": "3.18.0", "manifest": "MISSING_ROW",
                         "sha256": "0000000000000000000000000000000000000000000000000000000000000000" } ] }"""
        .Replace("MISSING_ROW", missing "3.18.0")
)

let missingProse = missing "3.17.0"
File.WriteAllText(Path.Combine(synthetic, "spec", "claims.md"), $"Recorded in `{missingProse}`.\n")

let syntheticErrors = check synthetic
Directory.Delete(synthetic, true)

if syntheticErrors.Length <> 3 then
    eprintfn "Self-test: expected 3 findings on the synthetic tree (two dangling prose/JSON paths and one snapshot row), got %d:" syntheticErrors.Length
    syntheticErrors |> List.iter (eprintfn "  %s")
    exit 1

match check (Directory.GetCurrentDirectory()) with
| [] ->
    printfn "Release manifest references (REG-PAIR-001-R3) PASS"
    printfn "  self-test: %d findings on a tree with dangling references, as expected" syntheticErrors.Length
| errors ->
    errors |> List.iter (eprintfn "REG-PAIR-001-R3: %s")
    exit 1
