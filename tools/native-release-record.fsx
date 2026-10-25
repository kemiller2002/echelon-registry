// Build an echelon.release/v2 record for a self-contained native CLI release
// from its downloaded GitHub release assets.
//
// Usage:
//   dotnet fsi tools/native-release-record.fsx -- \
//     --assets DIR --system ID --version X.Y.Z --repository OWNER/REPO \
//     --tag TAG --commit SHA40 --executable NAME --checksums checksums.txt \
//     --artifact linux-x64=NAME --artifact win-x64=NAME ... \
//     --output releases/<system>/<version>.release.json
//
// Every artifact digest is computed from the downloaded file and must equal
// the release's own checksum file entry; the checksum file's digest is
// recorded too. Nothing is taken from the release on trust.

open System
open System.IO
open System.Security.Cryptography
open System.Text.Json
open System.Text.Json.Nodes

let pairs =
    fsi.CommandLineArgs
    |> Array.skip 1
    |> Array.filter ((<>) "--")
    |> Array.chunkBySize 2
    |> Array.map (function
        | [| flag; value |] when flag.StartsWith "--" -> flag, value
        | other -> failwith $"invalid arguments: {String.Join(' ', other)}")
    |> Array.toList

let arg name =
    pairs |> List.tryFind (fst >> (=) name) |> Option.map snd |> Option.defaultWith (fun () -> failwith $"missing {name}")

let assets = arg "--assets"
let version = arg "--version"
let commit = arg "--commit"
let output = arg "--output"

if commit.Length <> 40 || not (commit |> Seq.forall Uri.IsHexDigit) then failwith "--commit must be a 40-character commit"
if File.Exists output then failwith $"{output} exists; release records are immutable"

let sha256 name =
    File.ReadAllBytes(Path.Combine(assets, name)) |> SHA256.HashData |> Convert.ToHexString |> fun v -> v.ToLowerInvariant()

let checksumsName = arg "--checksums"

let published =
    File.ReadAllLines(Path.Combine(assets, checksumsName))
    |> Array.filter (String.IsNullOrWhiteSpace >> not)
    |> Array.map (fun line ->
        match line.Split([| ' '; '\t' |], StringSplitOptions.RemoveEmptyEntries) with
        | [| digest; name |] -> name.TrimStart('*'), digest.ToLowerInvariant()
        | _ -> failwith $"unreadable checksum line: {line}")
    |> Map.ofArray

let artifact (platform: string, name: string) =
    let digest = sha256 name

    match published |> Map.tryFind name with
    | Some expected when expected = digest -> ()
    | Some expected -> failwith $"{name}: computed {digest}, {checksumsName} says {expected}"
    | None -> failwith $"{name} is not listed in {checksumsName}"

    let node = JsonObject()
    node["name"] <- JsonValue.Create name
    node["purpose"] <- JsonValue.Create "executable"
    node["platform"] <- JsonValue.Create platform
    node["sha256"] <- JsonValue.Create digest
    node["mediaType"] <- JsonValue.Create(if name.EndsWith ".zip" then "application/zip" elif name.EndsWith ".tar.gz" then "application/gzip" else "application/octet-stream")
    node :> JsonNode

let artifacts =
    pairs
    |> List.filter (fst >> (=) "--artifact")
    |> List.map (fun (_, value) ->
        match value.Split('=', 2) with
        | [| platform; name |] -> platform, name
        | _ -> failwith $"--artifact needs PLATFORM=NAME, got {value}")

if artifacts.IsEmpty then failwith "at least one --artifact is required"

let checksums = JsonObject()
checksums["name"] <- JsonValue.Create checksumsName
checksums["purpose"] <- JsonValue.Create "checksums"
checksums["platform"] <- null
checksums["sha256"] <- JsonValue.Create(sha256 checksumsName)
checksums["mediaType"] <- JsonValue.Create "text/plain"

let tag = arg "--tag"
let repository = arg "--repository"
let distribution = JsonObject()
distribution["mechanism"] <- JsonValue.Create "github-release"
distribution["package"] <- null
distribution["url"] <- JsonValue.Create $"https://github.com/{repository}/releases/tag/{tag}"

let record = JsonObject()
record["schema"] <- JsonValue.Create "echelon.release/v2"
record["systemId"] <- JsonValue.Create(arg "--system")
record["version"] <- JsonValue.Create version
record["repository"] <- JsonValue.Create repository
record["tag"] <- JsonValue.Create tag
record["commit"] <- JsonValue.Create commit
record["releaseStage"] <- JsonValue.Create "stable"
record["lifecycleState"] <- JsonValue.Create "active"
record["distributionClass"] <- JsonValue.Create "self-contained-native-cli"
record["executable"] <- JsonValue.Create(arg "--executable")
record["compatibilityAliases"] <- JsonArray()
record["provides"] <- JsonArray()
let distributions = JsonArray()
distributions.Add distribution
record["distributions"] <- distributions
let items = JsonArray()
artifacts |> List.iter (artifact >> items.Add)
items.Add checksums
record["artifacts"] <- items

Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath output)) |> ignore
File.WriteAllText(output, record.ToJsonString(JsonSerializerOptions(WriteIndented = true)) + "\n")
printfn "Wrote %s (%d artifacts verified against %s)" output artifacts.Length checksumsName
