// Generates a channel index (echelon.current-channel/v1) from a profile, its
// catalog snapshot and the per-platform resolved sets that
// tools/resolve-profile.fsx wrote into the channel directory.
//
//   dotnet fsi tools/generate-channel-index.fsx -- \
//     --channel channels/echelon-current \
//     --profile profiles/echelon-current.profile.json \
//     --snapshot snapshots/echelon-current.catalog.json \
//     [--output channels/echelon-current/channel.json]
//
// Paths recorded in the index are relative to the working directory (run it
// from the repository root). Platforms follow the profile's
// supportedPlatforms order. The output is a pure function of those inputs.
open System
open System.IO
open System.Security.Cryptography
open System.Text.Json
open System.Text.Json.Nodes

let fail message =
    eprintfn "ERROR: %s" message
    Environment.ExitCode <- 2
    failwith message

let require condition message =
    if not condition then fail message

let args =
    let values =
        fsi.CommandLineArgs
        |> Array.skip 1
        |> Array.filter ((<>) "--")
        |> Array.toList

    let rec loop (state: Map<string,string>) (remaining: string list) =
        match remaining with
        | flag :: value :: tail when flag.StartsWith("--", StringComparison.Ordinal) ->
            loop (state.Add(flag, value)) tail
        | [] -> state
        | flag :: _ -> fail $"argument '{flag}' needs a value"

    loop Map.empty values

let requiredArg name =
    match args.TryFind name with
    | Some value when not (String.IsNullOrWhiteSpace value) -> value
    | _ -> fail $"missing required argument {name}"

let root = Directory.GetCurrentDirectory()

/// Repository-relative path with forward slashes, as recorded in the index.
let relative (path: string) =
    Path.GetRelativePath(root, Path.GetFullPath path).Replace('\\', '/')

let channelDir = requiredArg "--channel" |> Path.GetFullPath |> fun p -> p.TrimEnd(Path.DirectorySeparatorChar)
let profilePath = requiredArg "--profile" |> Path.GetFullPath
let snapshotPath = requiredArg "--snapshot" |> Path.GetFullPath
let outputPath =
    args.TryFind "--output"
    |> Option.defaultValue (Path.Combine(channelDir, "channel.json"))
    |> Path.GetFullPath

let parse path =
    if not (File.Exists path) then fail $"file not found: {relative path}"
    JsonDocument.Parse(File.ReadAllBytes path)

let sha256 (path: string) =
    use stream = File.OpenRead path
    SHA256.HashData stream
    |> Convert.ToHexString
    |> fun value -> value.ToLowerInvariant()

let str (name: string) (element: JsonElement) =
    let mutable value = Unchecked.defaultof<JsonElement>
    if element.ValueKind = JsonValueKind.Object
       && element.TryGetProperty(name, &value)
       && value.ValueKind = JsonValueKind.String then
        value.GetString()
    else
        fail $"property '{name}' must be a string"

let nested (name: string) (element: JsonElement) =
    let mutable value = Unchecked.defaultof<JsonElement>
    if element.TryGetProperty(name, &value) && value.ValueKind = JsonValueKind.Object then value
    else fail $"property '{name}' must be an object"

let strings (name: string) (element: JsonElement) =
    let mutable value = Unchecked.defaultof<JsonElement>
    if element.TryGetProperty(name, &value) && value.ValueKind = JsonValueKind.Array then
        value.EnumerateArray() |> Seq.map (fun item -> item.GetString()) |> Seq.toList
    else
        fail $"property '{name}' must be an array"

let profile = (parse profilePath).RootElement
require (str "schema" profile = "echelon.profile/v1") "profile must use echelon.profile/v1"

let channelId = Path.GetFileName channelDir
let profileId = str "id" profile
let profileVersion = str "version" profile
let profileHash = sha256 profilePath
let snapshotHash = sha256 snapshotPath
require (channelId = profileId) $"channel directory '{channelId}' does not match profile id '{profileId}'"

/// One platform entry; the resolved set must have been produced from exactly
/// this profile and snapshot, so a stale resolve fails here.
let platformEntry platform =
    let path = Path.Combine(channelDir, $"{platform}.json")
    let resolved = (parse path).RootElement
    require (str "schema" resolved = "echelon.resolved-release-set/v1") $"{relative path} is not a resolved release set"
    require (str "platform" resolved = platform) $"{relative path} platform mismatch"
    require (str "sha256" (nested "profile" resolved) = profileHash) $"{relative path} was not resolved from {relative profilePath}; rerun tools/resolve-profile.fsx"
    require (str "sha256" (nested "catalogSnapshot" resolved) = snapshotHash) $"{relative path} was not resolved from {relative snapshotPath}; rerun tools/resolve-profile.fsx"

    let node = JsonObject()
    node["platform"] <- JsonValue.Create platform
    node["path"] <- JsonValue.Create(relative path)
    node["sha256"] <- JsonValue.Create(sha256 path)
    node

let index = JsonObject()
index["schema"] <- JsonValue.Create "echelon.current-channel/v1"
index["id"] <- JsonValue.Create channelId

let profileRef = JsonObject()
profileRef["id"] <- JsonValue.Create profileId
profileRef["version"] <- JsonValue.Create profileVersion
profileRef["sha256"] <- JsonValue.Create profileHash
index["profile"] <- profileRef

let snapshotRef = JsonObject()
snapshotRef["path"] <- JsonValue.Create(relative snapshotPath)
snapshotRef["sha256"] <- JsonValue.Create snapshotHash
index["catalogSnapshot"] <- snapshotRef

let platforms = JsonArray()
strings "supportedPlatforms" profile |> List.map platformEntry |> List.iter platforms.Add
index["platforms"] <- platforms

Directory.CreateDirectory(Path.GetDirectoryName outputPath) |> ignore
File.WriteAllText(outputPath, index.ToJsonString(JsonSerializerOptions(WriteIndented = true)) + "\n")

printfn "Generated %s for %s@%s" outputPath profileId profileVersion
printfn "catalogSha256=%s" snapshotHash
printfn "platforms=%d" platforms.Count
