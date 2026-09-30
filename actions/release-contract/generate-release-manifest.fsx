open System
open System.IO
open System.Security.Cryptography
open System.Text.Json
open System.Text.Json.Nodes
open System.Text.RegularExpressions

let fail message =
    eprintfn "ERROR: %s" message
    Environment.ExitCode <- 2
    failwith message

let arguments =
    fsi.CommandLineArgs
    |> Array.skip 1
    |> Array.filter ((<>) "--")
    |> Array.toList

let rec parseArgs (acc: Map<string, string>) (remaining: string list) =
    match remaining with
    | flag :: value :: tail when flag.StartsWith("--", StringComparison.Ordinal) ->
        parseArgs (Map.add flag value acc) tail
    | [] -> acc
    | flag :: _ -> fail $"argument '{flag}' needs a value"

let args = parseArgs Map.empty arguments

let required name =
    match Map.tryFind name args with
    | Some value when not (String.IsNullOrWhiteSpace value) -> value
    | _ -> fail $"missing required argument {name}"

let inputPath = required "--input" |> Path.GetFullPath
let artifactDir = required "--artifact-dir" |> Path.GetFullPath
let outputPath = required "--output" |> Path.GetFullPath
let repository = required "--repository"
let version = required "--version"
let tag = required "--tag"
let commit = required "--commit"
let releaseStage = required "--stage"

let semver =
    Regex("^(0|[1-9]\\d*)\\.(0|[1-9]\\d*)\\.(0|[1-9]\\d*)(?:-[0-9A-Za-z-]+(?:\\.[0-9A-Za-z-]+)*)?(?:\\+[0-9A-Za-z-]+(?:\\.[0-9A-Za-z-]+)*)?$")

let systemIdPattern = Regex("^[a-z][a-z0-9-]*$")
let repositoryPattern = Regex("^[A-Za-z0-9_.-]+/[A-Za-z0-9._-]+$")
let shaPattern = Regex("^[0-9a-f]{40}$")

if not (File.Exists inputPath) then fail $"release input not found: {inputPath}"
if not (Directory.Exists artifactDir) then fail $"artifact directory not found: {artifactDir}"
if not (semver.IsMatch version) then fail $"version '{version}' is not semantic version syntax"
if not (repositoryPattern.IsMatch repository) then fail $"repository '{repository}' must be owner/name"
if not (shaPattern.IsMatch commit) then fail "commit must be a full lowercase 40-character SHA"
if not ([ "stable"; "preview"; "nightly" ] |> List.contains releaseStage) then fail $"unsupported release stage '{releaseStage}'"

let parse path = JsonDocument.Parse(File.ReadAllBytes path)

let tryProperty (name: string) (element: JsonElement) =
    let mutable value = Unchecked.defaultof<JsonElement>
    if element.ValueKind = JsonValueKind.Object && element.TryGetProperty(name, &value) then Some value else None

let str name element =
    match tryProperty name element with
    | Some value when value.ValueKind = JsonValueKind.String -> value.GetString() |> Option.ofObj
    | _ -> None

let objects name element =
    match tryProperty name element with
    | Some value when value.ValueKind = JsonValueKind.Array -> value.EnumerateArray() |> Seq.toList
    | _ -> []

let sha256File path =
    use stream = File.OpenRead path
    SHA256.HashData stream
    |> Convert.ToHexString
    |> fun value -> value.ToLowerInvariant()

let input = parse inputPath
let root = input.RootElement

if str "schema" root <> Some "echelon.release-input/v1" then
    fail "release input must declare schema echelon.release-input/v1"

let systemId = str "systemId" root |> Option.defaultWith (fun () -> fail "release input needs systemId")
if not (systemIdPattern.IsMatch systemId) then fail $"invalid systemId '{systemId}'"

let distributionClass =
    str "distributionClass" root
    |> Option.defaultWith (fun () -> fail "release input needs distributionClass")

let allowedClasses =
    [ "self-contained-native-cli"
      "self-contained-native-daemon"
      "repository-lifecycle"
      "nuget-library"
      "web-package"
      "contract-bundle"
      "application-artifact" ]

if not (List.contains distributionClass allowedClasses) then fail $"unsupported distributionClass '{distributionClass}'"

let executable =
    match tryProperty "executable" root with
    | Some value when value.ValueKind = JsonValueKind.Null -> None
    | Some value when value.ValueKind = JsonValueKind.String -> value.GetString() |> Option.ofObj
    | _ -> fail "release input needs executable (string or null)"

let distribution =
    tryProperty "distribution" root
    |> Option.defaultWith (fun () -> fail "release input needs distribution")

let mechanism = str "mechanism" distribution |> Option.defaultWith (fun () -> fail "distribution needs mechanism")
let packageName = str "package" distribution
let explicitUrl = str "url" distribution

let distributionUrl =
    match explicitUrl, mechanism with
    | Some url, _ -> Some url
    | None, "github-release" -> Some $"https://github.com/{repository}/releases/tag/{tag}"
    | _ -> None

let artifactSpecs = objects "artifacts" root
if artifactSpecs.IsEmpty then fail "release input needs at least one artifact"

let artifactNames =
    artifactSpecs
    |> List.map (fun artifact ->
        str "name" artifact |> Option.defaultWith (fun () -> fail "artifact needs name"))

if artifactNames.Length <> (artifactNames |> List.distinct |> List.length) then
    fail "release input contains duplicate artifact names"

let resolveArtifact (name: string) =
    if Path.GetFileName name <> name || name.Contains("/") || name.Contains("\\") then
        fail $"artifact '{name}' must be a file name, not a path"

    let path = Path.Combine(artifactDir, name)
    if not (File.Exists path) then fail $"declared artifact is missing: {name}"
    path

type GeneratedArtifact =
    { Name: string
      Purpose: string
      Platform: string option
      MediaType: string option
      Sha256: string }

let generated =
    artifactSpecs
    |> List.map (fun artifact ->
        let name = str "name" artifact |> Option.get
        let purpose = str "purpose" artifact |> Option.defaultWith (fun () -> fail $"artifact '{name}' needs purpose")
        let path = resolveArtifact name

        { Name = name
          Purpose = purpose
          Platform = str "platform" artifact
          MediaType = str "mediaType" artifact
          Sha256 = sha256File path })

let nativeClass =
    distributionClass = "self-contained-native-cli" ||
    distributionClass = "self-contained-native-daemon"

if nativeClass && executable.IsNone then
    fail $"{distributionClass} release requires executable"

if nativeClass && not (generated |> List.exists (fun artifact -> artifact.Purpose = "executable" && artifact.Platform.IsSome)) then
    fail $"{distributionClass} release requires at least one platform executable artifact"

let output = JsonObject()
output["schema"] <- JsonValue.Create "echelon.release/v2"
output["systemId"] <- JsonValue.Create systemId
output["version"] <- JsonValue.Create version
output["repository"] <- JsonValue.Create repository
output["tag"] <- JsonValue.Create tag
output["commit"] <- JsonValue.Create commit
output["releaseStage"] <- JsonValue.Create releaseStage
output["lifecycleState"] <- JsonValue.Create "active"
output["distributionClass"] <- JsonValue.Create distributionClass
output["executable"] <- (match executable with Some value -> JsonValue.Create value :> JsonNode | None -> null)

let aliases = JsonArray()
objects "compatibilityAliases" root |> ignore
match tryProperty "compatibilityAliases" root with
| Some values when values.ValueKind = JsonValueKind.Array ->
    values.EnumerateArray()
    |> Seq.iter (fun value ->
        if value.ValueKind <> JsonValueKind.String then fail "compatibilityAliases must contain strings"
        aliases.Add(JsonValue.Create(value.GetString())))
| _ -> ()
output["compatibilityAliases"] <- aliases

let provides = JsonArray()
for capability in objects "provides" root do
    let id = str "id" capability |> Option.defaultWith (fun () -> fail "capability needs id")
    let versionProperty = tryProperty "contractVersion" capability |> Option.defaultWith (fun () -> fail $"capability '{id}' needs contractVersion")
    if versionProperty.ValueKind <> JsonValueKind.Number then fail $"capability '{id}' contractVersion must be an integer"
    let contractVersion = versionProperty.GetInt32()
    if contractVersion < 1 then fail $"capability '{id}' contractVersion must be positive"
    let node = JsonObject()
    node["id"] <- JsonValue.Create id
    node["contractVersion"] <- JsonValue.Create contractVersion
    provides.Add node
output["provides"] <- provides

let distributions = JsonArray()
let dist = JsonObject()
dist["mechanism"] <- JsonValue.Create mechanism
dist["package"] <- (match packageName with Some value -> JsonValue.Create value :> JsonNode | None -> null)
dist["url"] <- (match distributionUrl with Some value -> JsonValue.Create value :> JsonNode | None -> null)
distributions.Add dist
output["distributions"] <- distributions

let artifacts = JsonArray()
for artifact in generated do
    let node = JsonObject()
    node["name"] <- JsonValue.Create artifact.Name
    node["purpose"] <- JsonValue.Create artifact.Purpose
    node["platform"] <- (match artifact.Platform with Some value -> JsonValue.Create value :> JsonNode | None -> null)
    node["sha256"] <- JsonValue.Create artifact.Sha256
    node["mediaType"] <- (match artifact.MediaType with Some value -> JsonValue.Create value :> JsonNode | None -> null)
    artifacts.Add node
output["artifacts"] <- artifacts

let evidenceRef artifact =
    let node = JsonObject()
    node["artifactName"] <- JsonValue.Create artifact.Name
    node["sha256"] <- JsonValue.Create artifact.Sha256
    node

let evidence = JsonObject()

generated
|> List.tryFind (fun artifact -> artifact.Purpose = "sbom")
|> Option.iter (fun artifact -> evidence["sbom"] <- evidenceRef artifact)

generated
|> List.tryFind (fun artifact -> artifact.Purpose = "licenses")
|> Option.iter (fun artifact -> evidence["licenses"] <- evidenceRef artifact)

let provenance = JsonArray()
generated
|> List.filter (fun artifact -> artifact.Purpose = "provenance")
|> List.iter (fun artifact -> provenance.Add(evidenceRef artifact))
if provenance.Count > 0 then evidence["provenance"] <- provenance

let trust = JsonArray()
generated
|> List.filter (fun artifact -> artifact.Purpose = "signature")
|> List.iter (fun artifact -> trust.Add(evidenceRef artifact))
if trust.Count > 0 then evidence["platformTrust"] <- trust

if evidence.Count > 0 then output["evidence"] <- evidence

let outputDirectory = Path.GetDirectoryName outputPath
if not (String.IsNullOrWhiteSpace outputDirectory) then Directory.CreateDirectory outputDirectory |> ignore

let options = JsonSerializerOptions(WriteIndented = true)
File.WriteAllText(outputPath, output.ToJsonString(options) + "\n")

printfn "Generated %s for %s %s" outputPath systemId version
for artifact in generated do
    printfn "  sha256:%s  %s" artifact.Sha256 artifact.Name
