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

let profilePath = requiredArg "--profile" |> Path.GetFullPath
let snapshotPath = requiredArg "--snapshot" |> Path.GetFullPath
let platform = requiredArg "--platform"
let outputPath = requiredArg "--output" |> Path.GetFullPath

let resolverName = "echelon-registry-resolver"
let resolverVersion = "0.1.0"

let parse path =
    if not (File.Exists path) then fail $"file not found: {path}"
    JsonDocument.Parse(File.ReadAllBytes path)

let sha256 path =
    use stream = File.OpenRead path
    SHA256.HashData stream
    |> Convert.ToHexString
    |> fun value -> value.ToLowerInvariant()

let tryProperty (name: string) (element: JsonElement) =
    let mutable value = Unchecked.defaultof<JsonElement>
    if element.ValueKind = JsonValueKind.Object && element.TryGetProperty(name, &value) then Some value else None

let property name element =
    tryProperty name element |> Option.defaultWith (fun () -> fail $"missing property '{name}'")

let str name element =
    match tryProperty name element with
    | Some value when value.ValueKind = JsonValueKind.String ->
        value.GetString() |> Option.ofObj |> Option.defaultWith (fun () -> fail $"property '{name}' is null")
    | _ -> fail $"property '{name}' must be a string"

let optStr name element =
    match tryProperty name element with
    | None -> None
    | Some value when value.ValueKind = JsonValueKind.Null -> None
    | Some value when value.ValueKind = JsonValueKind.String -> value.GetString() |> Option.ofObj
    | _ -> fail $"property '{name}' must be a string or null"

let boolValue name element =
    match tryProperty name element with
    | Some value when value.ValueKind = JsonValueKind.True -> true
    | Some value when value.ValueKind = JsonValueKind.False -> false
    | _ -> fail $"property '{name}' must be a boolean"

let objects name element =
    match tryProperty name element with
    | Some value when value.ValueKind = JsonValueKind.Array -> value.EnumerateArray() |> Seq.toList
    | _ -> fail $"property '{name}' must be an array"

let strings name element =
    objects name element
    |> List.map (fun value ->
        if value.ValueKind <> JsonValueKind.String then fail $"property '{name}' must contain only strings"
        value.GetString() |> Option.ofObj |> Option.defaultWith (fun () -> fail $"property '{name}' contains null"))

type SemVer =
    { Major: int
      Minor: int
      Patch: int
      PreRelease: string list
      Original: string }

let semverPattern =
    Regex("^(0|[1-9]\\d*)\\.(0|[1-9]\\d*)\\.(0|[1-9]\\d*)(?:-([0-9A-Za-z-]+(?:\\.[0-9A-Za-z-]+)*))?(?:\\+[0-9A-Za-z-]+(?:\\.[0-9A-Za-z-]+)*)?$", RegexOptions.Compiled)

let parseSemVer raw =
    let m = semverPattern.Match raw
    if not m.Success then fail $"unsupported semantic version '{raw}'"
    { Major = int m.Groups[1].Value
      Minor = int m.Groups[2].Value
      Patch = int m.Groups[3].Value
      PreRelease =
        if m.Groups[4].Success then m.Groups[4].Value.Split('.') |> Array.toList
        else []
      Original = raw }

let compareIdentifier (left: string) (right: string) =
    let numeric (value: string) =
        match Int32.TryParse value with
        | true, n -> Some n
        | _ -> None

    match numeric left, numeric right with
    | Some a, Some b -> compare a b
    | Some _, None -> -1
    | None, Some _ -> 1
    | None, None -> StringComparer.Ordinal.Compare(left, right)

let rec comparePrerelease left right =
    match left, right with
    | [], [] -> 0
    | [], _ -> 1
    | _, [] -> -1
    | lh :: lt, rh :: rt ->
        let c = compareIdentifier lh rh
        if c <> 0 then c else comparePrerelease lt rt

let compareSemVer left right =
    let core = compare (left.Major, left.Minor, left.Patch) (right.Major, right.Minor, right.Patch)
    if core <> 0 then core
    else comparePrerelease left.PreRelease right.PreRelease

type Comparator =
    | GreaterThan of SemVer
    | GreaterThanOrEqual of SemVer
    | LessThan of SemVer
    | LessThanOrEqual of SemVer
    | EqualTo of SemVer

let comparatorPattern = Regex("^(>=|<=|>|<|=)?(.+)$", RegexOptions.Compiled)

let parseComparator token =
    let m = comparatorPattern.Match token
    if not m.Success then fail $"invalid version comparator '{token}'"
    let op = if m.Groups[1].Success then m.Groups[1].Value else "="
    let version = parseSemVer m.Groups[2].Value
    match op with
    | ">" -> GreaterThan version
    | ">=" -> GreaterThanOrEqual version
    | "<" -> LessThan version
    | "<=" -> LessThanOrEqual version
    | "=" -> EqualTo version
    | _ -> fail $"unsupported version comparator '{op}'"

let satisfiesComparator candidate comparator =
    let c =
        match comparator with
        | GreaterThan value
        | GreaterThanOrEqual value
        | LessThan value
        | LessThanOrEqual value
        | EqualTo value -> compareSemVer candidate value

    match comparator with
    | GreaterThan _ -> c > 0
    | GreaterThanOrEqual _ -> c >= 0
    | LessThan _ -> c < 0
    | LessThanOrEqual _ -> c <= 0
    | EqualTo _ -> c = 0

let versionMatches (rule: JsonElement) rawVersion =
    let candidate = parseSemVer rawVersion
    match tryProperty "exact" rule, tryProperty "range" rule with
    | Some exact, None when exact.ValueKind = JsonValueKind.String ->
        compareSemVer candidate (parseSemVer (exact.GetString())) = 0
    | None, Some range when range.ValueKind = JsonValueKind.String ->
        let raw = range.GetString()
        let tokens = raw.Split([|' '; '\t'|], StringSplitOptions.RemoveEmptyEntries) |> Array.toList
        require (not tokens.IsEmpty) "version range is empty"
        tokens |> List.map parseComparator |> List.forall (satisfiesComparator candidate)
    | _ ->
        fail "version rule must contain exactly one of exact or range"

let allowedClasses role =
    match role with
    | "host-tool" -> Set.ofList [ "self-contained-native-cli" ]
    | "host-daemon" -> Set.ofList [ "self-contained-native-daemon" ]
    | "repository-lifecycle" -> Set.ofList [ "repository-lifecycle"; "self-contained-native-cli"; "web-package"; "nuget-library" ]
    | "project-binding" -> Set.ofList [ "nuget-library"; "web-package" ]
    | "contract-bundle" -> Set.ofList [ "contract-bundle" ]
    | "application" -> Set.ofList [ "application-artifact" ]
    | other -> fail $"unsupported profile role '{other}'"

let distributionPriority distributionClass =
    match distributionClass with
    | "self-contained-native-cli"
    | "self-contained-native-daemon" -> [ "github-release" ]
    | "repository-lifecycle" -> [ "npm"; "nuget"; "github-release"; "static-bundle" ]
    | "nuget-library" -> [ "nuget"; "github-release" ]
    | "web-package" -> [ "npm"; "github-release"; "static-bundle" ]
    | "contract-bundle" -> [ "github-release"; "static-bundle" ]
    | "application-artifact" -> [ "github-release"; "container"; "static-bundle" ]
    | other -> fail $"unsupported distribution class '{other}'"

let chooseDistribution distributionClass release =
    let available = objects "distributions" release
    distributionPriority distributionClass
    |> List.tryPick (fun mechanism ->
        available |> List.tryFind (fun distribution -> str "mechanism" distribution = mechanism))
    |> Option.defaultWith (fun () ->
        let found = available |> List.map (str "mechanism") |> String.concat ", "
        let systemId = str "systemId" release
        let version = str "version" release
        fail $"release {systemId} {version} has no supported distribution for {distributionClass}; found [{found}]")

let isSupportArtifact purpose =
    Set.ofList [ "checksums"; "sbom"; "licenses"; "provenance"; "signature" ]
    |> Set.contains purpose

/// The artifacts that install the release on this platform: one, for a
/// release that supports the platform; none, for one that does not.
let primaryArtifacts distributionClass selectedDistribution release =
    let artifacts = objects "artifacts" release

    let primary artifact =
        let purpose = str "purpose" artifact
        let artifactPlatform = optStr "platform" artifact

        match distributionClass with
        | "self-contained-native-cli"
        | "self-contained-native-daemon" ->
            purpose = "executable" && artifactPlatform = Some platform
        | "repository-lifecycle"
        | "web-package" ->
            purpose = "package" && artifactPlatform.IsNone
        | "nuget-library" ->
            if purpose <> "package" || artifactPlatform.IsSome then
                false
            else
                match optStr "package" selectedDistribution with
                | None -> true
                | Some packageId ->
                    let artifactName = str "name" artifact
                    artifactName.StartsWith(packageId + ".", StringComparison.OrdinalIgnoreCase)
                    && artifactName.EndsWith(".nupkg", StringComparison.OrdinalIgnoreCase)
                    && not (artifactName.EndsWith(".snupkg", StringComparison.OrdinalIgnoreCase))
        | "contract-bundle" ->
            purpose = "bundle" && artifactPlatform.IsNone
        | "application-artifact" ->
            (purpose = "application" || purpose = "bundle") && artifactPlatform.IsNone
        | _ -> false

    artifacts |> List.filter primary

let selectArtifacts distributionClass selectedDistribution release =
    let mechanism = str "mechanism" selectedDistribution
    let artifacts = objects "artifacts" release
    let primaries = primaryArtifacts distributionClass selectedDistribution release
    let releaseSystemId = str "systemId" release
    let releaseVersion = str "version" release
    // A NuGet library that names no single distribution package ships a
    // package family from one release (for example a core package and its
    // adapter): every platform-neutral .nupkg it publishes installs it, so all
    // are primary. Every other release installs from exactly one artifact.
    let family =
        distributionClass = "nuget-library" && (optStr "package" selectedDistribution).IsNone

    if family then
        require (not primaries.IsEmpty) $"release {releaseSystemId} {releaseVersion} must expose at least one package artifact for {distributionClass}/{mechanism}/{platform}"

        primaries
        |> List.iter (fun artifact ->
            let name = str "name" artifact
            require (name.EndsWith(".nupkg", StringComparison.OrdinalIgnoreCase) && not (name.EndsWith(".snupkg", StringComparison.OrdinalIgnoreCase))) $"release {releaseSystemId} {releaseVersion} package artifact {name} is not a .nupkg")
    else
        require (primaries.Length = 1) $"release {releaseSystemId} {releaseVersion} must expose exactly one primary artifact for {distributionClass}/{mechanism}/{platform}; found {primaries.Length}"

    let support =
        artifacts
        |> List.filter (fun artifact ->
            let purpose = str "purpose" artifact
            isSupportArtifact purpose && (optStr "platform" artifact |> Option.forall ((=) platform)))

    primaries @ support

let copyArtifact (artifact: JsonElement) =
    let node = JsonObject()
    node["name"] <- JsonValue.Create(str "name" artifact)
    node["purpose"] <- JsonValue.Create(str "purpose" artifact)
    node["platform"] <-
        match optStr "platform" artifact with
        | Some value -> JsonValue.Create(value) :> JsonNode
        | None -> null
    node["sha256"] <- JsonValue.Create(str "sha256" artifact)
    node

let copyDistribution (distribution: JsonElement) =
    let node = JsonObject()
    node["mechanism"] <- JsonValue.Create(str "mechanism" distribution)
    node["package"] <-
        match optStr "package" distribution with
        | Some value -> JsonValue.Create(value) :> JsonNode
        | None -> null
    node["url"] <-
        match optStr "url" distribution with
        | Some value -> JsonValue.Create(value) :> JsonNode
        | None -> null
    node

let repositoryLifecycleCapability = "echelon.repository-lifecycle"

/// The declared repository lifecycle contract version, if the release
/// provides the capability.
let repositoryLifecycle (release: JsonElement) =
    objects "provides" release
    |> List.filter (fun capability -> str "id" capability = repositoryLifecycleCapability)
    |> function
        | [] -> None
        | [ capability ] ->
            match tryProperty "contractVersion" capability with
            | Some value when value.ValueKind = JsonValueKind.Number ->
                match value.TryGetInt32() with
                | true, version when version >= 1 -> Some version
                | _ -> fail $"{repositoryLifecycleCapability} contractVersion must be a positive integer"
            | _ -> fail $"{repositoryLifecycleCapability} needs an integer contractVersion"
        | _ -> fail $"release declares {repositoryLifecycleCapability} more than once"

type Candidate =
    { ReleasePath: string
      ReleaseHash: string
      Release: JsonElement
      Version: SemVer }

let profileDoc = parse profilePath
let snapshotDoc = parse snapshotPath
let profile = profileDoc.RootElement
let snapshot = snapshotDoc.RootElement

require (str "schema" profile = "echelon.profile/v1") "profile must use echelon.profile/v1"
require (str "schema" snapshot = "echelon.catalog-snapshot/v1") "snapshot must use echelon.catalog-snapshot/v1"

let profileId = str "id" profile
let profileVersion = str "version" profile
let profileHash = sha256 profilePath
let snapshotHash = sha256 snapshotPath

let supportedPlatforms = strings "supportedPlatforms" profile
require (supportedPlatforms |> List.contains platform) $"profile {profileId}@{profileVersion} does not support {platform}"

let snapshotProfiles = objects "profiles" snapshot
let snapshotProfile =
    snapshotProfiles
    |> List.tryFind (fun item -> str "id" item = profileId && str "version" item = profileVersion)
    |> Option.defaultWith (fun () -> fail $"snapshot does not contain profile {profileId}@{profileVersion}")

require (str "sha256" snapshotProfile = profileHash) $"snapshot digest for profile {profileId}@{profileVersion} does not match the supplied profile"

let releaseRows = objects "releases" snapshot
let systemRows = objects "systems" snapshot
let profileAllowedStages = strings "allowedReleaseStages" profile |> Set.ofList

let resolveComponent (profileComponent: JsonElement) =
    let systemId = str "systemId" profileComponent
    let role = str "role" profileComponent
    let required = boolValue "required" profileComponent
    let rule = property "version" profileComponent

    let allowedStages =
        match tryProperty "allowedReleaseStages" profileComponent with
        | Some value when value.ValueKind = JsonValueKind.Array ->
            value.EnumerateArray()
            |> Seq.map (fun item -> item.GetString())
            |> Seq.choose Option.ofObj
            |> Set.ofSeq
        | None -> profileAllowedStages
        | _ -> fail $"allowedReleaseStages for {systemId} must be an array"

    require (not allowedStages.IsEmpty) $"component {systemId} has no allowed release stages"

    let systemRow =
        systemRows
        |> List.tryFind (fun row -> str "id" row = systemId)
        |> Option.defaultWith (fun () -> fail $"snapshot has no system record for {systemId}")

    let rows =
        releaseRows
        |> List.filter (fun row -> str "systemId" row = systemId)

    require (not rows.IsEmpty) $"snapshot has no releases for required profile component {systemId}"

    let candidates =
        rows
        |> List.choose (fun row ->
            let releaseRel = str "manifest" row
            let releasePath =
                if Path.IsPathRooted releaseRel then releaseRel
                else Path.Combine(Path.GetDirectoryName(snapshotPath), "..", releaseRel) |> Path.GetFullPath

            if not (File.Exists releasePath) then
                fail $"snapshot release manifest for {systemId} not found: {releasePath}"

            let releaseHash = sha256 releasePath
            let rowVersion = str "version" row
            require (releaseHash = str "sha256" row) $"snapshot release digest mismatch for {systemId} {rowVersion}"

            use releaseDoc = parse releasePath
            let release = releaseDoc.RootElement.Clone()

            require (str "schema" release = "echelon.release/v2") $"release {systemId} must use echelon.release/v2"
            require (str "systemId" release = systemId) $"release manifest system id mismatch for {systemId}"
            require (str "version" release = str "version" row) $"snapshot/release version mismatch for {systemId}"
            require (str "repository" release = str "repository" systemRow) $"release repository mismatch for {systemId}"

            let lifecycle = str "lifecycleState" release
            let stage = str "releaseStage" release
            let distributionClass = str "distributionClass" release
            let compatibleClass = allowedClasses role |> Set.contains distributionClass

            if lifecycle = "active"
               && allowedStages.Contains stage
               && compatibleClass
               && versionMatches rule (str "version" release) then
                Some
                    { ReleasePath = releasePath
                      ReleaseHash = releaseHash
                      Release = release
                      Version = parseSemVer (str "version" release) }
            else
                None)

    let selected =
        candidates
        |> List.sortWith (fun a b -> compareSemVer b.Version a.Version)
        |> List.tryHead
        |> Option.defaultWith (fun () ->
            fail $"no active release satisfies {profileId}@{profileVersion} component {systemId} for role {role}")

    let release = selected.Release
    let distributionClass = str "distributionClass" release
    let distribution = chooseDistribution distributionClass release

    // REG-REL-031: every REQUIRED component resolves on every platform the
    // profile supports. An OPTIONAL component whose selected release ships
    // nothing for this platform is omitted from this platform's set rather
    // than failing it — a component that cannot run here is not offered here.
    // A required one still fails below, as before.
    if not required && List.isEmpty (primaryArtifacts distributionClass distribution release) then
        None
    else

    let selectedArtifacts = selectArtifacts distributionClass distribution release

    let node = JsonObject()
    node["systemId"] <- JsonValue.Create systemId
    node["role"] <- JsonValue.Create role
    node["required"] <- JsonValue.Create required
    node["version"] <- JsonValue.Create(str "version" release)
    node["repository"] <- JsonValue.Create(str "repository" release)
    node["tag"] <- JsonValue.Create(str "tag" release)
    node["commit"] <- JsonValue.Create(str "commit" release)
    node["releaseStage"] <- JsonValue.Create(str "releaseStage" release)
    node["lifecycleState"] <- JsonValue.Create(str "lifecycleState" release)
    node["distributionClass"] <- JsonValue.Create distributionClass
    node["executable"] <-
        match optStr "executable" release with
        | Some value -> JsonValue.Create(value) :> JsonNode
        | None -> null

    // spec/repository-lifecycle-contract.md: the lifecycle contract is a
    // declared release capability, copied only when declared so resolved sets
    // of non-declaring releases stay byte-identical.
    match repositoryLifecycle release with
    | None -> ()
    | Some contractVersion ->
        require (distributionClass = "self-contained-native-cli") $"release {systemId} declares {repositoryLifecycleCapability} but has distribution class {distributionClass}"
        require ((optStr "executable" release).IsSome) $"release {systemId} declares {repositoryLifecycleCapability} without an executable"
        let lifecycle = JsonObject()
        lifecycle["contract"] <- JsonValue.Create repositoryLifecycleCapability
        lifecycle["contractVersion"] <- JsonValue.Create contractVersion
        node["repositoryLifecycle"] <- lifecycle

    let releaseRef = JsonObject()
    releaseRef["schema"] <- JsonValue.Create(str "schema" release)
    releaseRef["sha256"] <- JsonValue.Create selected.ReleaseHash
    node["releaseManifest"] <- releaseRef
    node["distribution"] <- copyDistribution distribution

    let artifacts = JsonArray()
    selectedArtifacts |> List.iter (copyArtifact >> artifacts.Add)
    node["artifacts"] <- artifacts
    Some node

let resolvedComponents =
    objects "components" profile
    |> List.choose resolveComponent

let output = JsonObject()
output["schema"] <- JsonValue.Create "echelon.resolved-release-set/v1"

let profileRef = JsonObject()
profileRef["id"] <- JsonValue.Create profileId
profileRef["version"] <- JsonValue.Create profileVersion
profileRef["sha256"] <- JsonValue.Create profileHash
output["profile"] <- profileRef
output["platform"] <- JsonValue.Create platform

let resolver = JsonObject()
resolver["name"] <- JsonValue.Create resolverName
resolver["version"] <- JsonValue.Create resolverVersion
output["resolver"] <- resolver

let catalog = JsonObject()
catalog["sha256"] <- JsonValue.Create snapshotHash
output["catalogSnapshot"] <- catalog

let components = JsonArray()
resolvedComponents |> List.iter components.Add
output["components"] <- components

let outputDirectory = Path.GetDirectoryName outputPath
if not (String.IsNullOrWhiteSpace outputDirectory) then Directory.CreateDirectory outputDirectory |> ignore

File.WriteAllText(outputPath, output.ToJsonString(JsonSerializerOptions(WriteIndented = true)) + "\n")

printfn "Resolved %s@%s for %s" profileId profileVersion platform
printfn "profileSha256=%s" profileHash
printfn "catalogSha256=%s" snapshotHash
printfn "components=%d" resolvedComponents.Length
