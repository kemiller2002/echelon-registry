open System
open System.IO
open System.Security.Cryptography
open System.Text.Json

let require condition message =
    if not condition then failwith message

let parse path =
    JsonDocument.Parse(File.ReadAllText path)

let sha256 path =
    let bytes = File.ReadAllBytes path
    let hash = SHA256.HashData bytes
    Convert.ToHexString(hash).ToLowerInvariant()

let property (name: string) (element: JsonElement) =
    element.GetProperty name

let stringProperty (name: string) (element: JsonElement) =
    (property name element).GetString()

let arrayProperty (name: string) (element: JsonElement) =
    (property name element).EnumerateArray() |> Seq.toArray

let schemaFiles =
    [|
        "schemas/system-manifest.schema.json"
        "schemas/system-manifest-v2.schema.json"
        "schemas/release-manifest.schema.json"
        "schemas/release-manifest-v2.schema.json"
        "schemas/profile.schema.json"
        "schemas/resolved-release-set.schema.json"
        "schemas/catalog-snapshot.schema.json"
        "schemas/readiness.schema.json"
    |]

for path in schemaFiles do
    use parsed = parse path
    require (parsed.RootElement.ValueKind = JsonValueKind.Object) (sprintf "%s is not a JSON object" path)

let systemsDoc = parse "registry/systems-v2.json"
let systems = arrayProperty "systems" systemsDoc.RootElement
let canonicalIds = systems |> Array.map (stringProperty "id")

require (canonicalIds |> Array.distinct |> Array.length = canonicalIds.Length) "canonical system ids must be unique"

let aliases =
    systems
    |> Array.collect (fun system ->
        arrayProperty "aliases" system
        |> Array.map (fun alias -> alias.GetString()))

require (aliases |> Array.distinct |> Array.length = aliases.Length) "system aliases must be globally unique"

for alias in aliases do
    require (not (canonicalIds |> Array.contains alias)) (sprintf "alias %s collides with a canonical system id" alias)

let validateResolution
    (name: string)
    (profilePath: string)
    (snapshotPath: string)
    (resolvedPath: string)
    (releasePaths: Map<string, string>) =

    use profileDoc = parse profilePath
    use snapshotDoc = parse snapshotPath
    use resolvedDoc = parse resolvedPath

    let profile = profileDoc.RootElement
    let snapshot = snapshotDoc.RootElement
    let resolved = resolvedDoc.RootElement

    require (stringProperty "schema" profile = "echelon.profile/v1") (sprintf "%s profile schema mismatch" name)
    require (stringProperty "schema" snapshot = "echelon.catalog-snapshot/v1") (sprintf "%s snapshot schema mismatch" name)
    require (stringProperty "schema" resolved = "echelon.resolved-release-set/v1") (sprintf "%s resolved schema mismatch" name)

    let profileHash = sha256 profilePath
    let snapshotHash = sha256 snapshotPath
    let snapshotReleases = arrayProperty "releases" snapshot
    let snapshotProfiles = arrayProperty "profiles" snapshot

    let profileId = stringProperty "id" profile
    let profileVersion = stringProperty "version" profile

    let snapshotProfile =
        snapshotProfiles
        |> Array.find (fun item ->
            stringProperty "id" item = profileId &&
            stringProperty "version" item = profileVersion)

    require (stringProperty "sha256" snapshotProfile = profileHash) (sprintf "%s snapshot profile digest mismatch" name)

    let resolvedProfile = property "profile" resolved
    require (stringProperty "id" resolvedProfile = profileId) (sprintf "%s resolved profile id mismatch" name)
    require (stringProperty "version" resolvedProfile = profileVersion) (sprintf "%s resolved profile version mismatch" name)
    require (stringProperty "sha256" resolvedProfile = profileHash) (sprintf "%s resolved profile digest mismatch" name)

    let resolvedSnapshot = property "catalogSnapshot" resolved
    require (stringProperty "sha256" resolvedSnapshot = snapshotHash) (sprintf "%s resolved snapshot digest mismatch" name)

    let platform = stringProperty "platform" resolved
    let supportedPlatforms =
        arrayProperty "supportedPlatforms" profile
        |> Array.map (fun item -> item.GetString())

    require (supportedPlatforms |> Array.contains platform) (sprintf "%s does not support resolved platform %s" name platform)

    let allowedStages =
        arrayProperty "allowedReleaseStages" profile
        |> Array.map (fun item -> item.GetString())

    let profileComponents = arrayProperty "components" profile
    let resolvedComponents = arrayProperty "components" resolved

    require (profileComponents.Length = resolvedComponents.Length) (sprintf "%s resolved component count differs from profile" name)
    require (snapshotReleases.Length = releasePaths.Count) (sprintf "%s snapshot release count differs from supplied release manifests" name)

    for profileComponent in profileComponents do
        let systemId = stringProperty "systemId" profileComponent
        require (releasePaths.ContainsKey systemId) (sprintf "%s has no release manifest for %s" name systemId)

        let releasePath = releasePaths.[systemId]
        use releaseDoc = parse releasePath
        let release = releaseDoc.RootElement
        let releaseHash = sha256 releasePath

        require (stringProperty "schema" release = "echelon.release/v2") (sprintf "%s release %s must use echelon.release/v2" name systemId)
        require (stringProperty "systemId" release = systemId) (sprintf "%s release system id mismatch for %s" name systemId)

        let snapshotRelease =
            snapshotReleases
            |> Array.find (fun item -> stringProperty "systemId" item = systemId)

        require (stringProperty "version" snapshotRelease = stringProperty "version" release) (sprintf "%s snapshot version mismatch for %s" name systemId)
        require (stringProperty "sha256" snapshotRelease = releaseHash) (sprintf "%s snapshot release digest mismatch for %s" name systemId)

        let versionRule = property "version" profileComponent
        require (stringProperty "exact" versionRule = stringProperty "version" release) (sprintf "%s exact version does not match release for %s" name systemId)

        let releaseStage = stringProperty "releaseStage" release
        require (allowedStages |> Array.contains releaseStage) (sprintf "%s release stage is not allowed for %s" name systemId)
        require (stringProperty "lifecycleState" release = "active") (sprintf "%s selected release is not active for %s" name systemId)

        let resolvedComponent =
            resolvedComponents
            |> Array.find (fun item -> stringProperty "systemId" item = systemId)

        require (stringProperty "role" resolvedComponent = stringProperty "role" profileComponent) (sprintf "%s resolved role mismatch for %s" name systemId)
        require ((property "required" resolvedComponent).GetBoolean() = (property "required" profileComponent).GetBoolean()) (sprintf "%s required policy mismatch for %s" name systemId)
        require (stringProperty "version" resolvedComponent = stringProperty "version" release) (sprintf "%s resolved version mismatch for %s" name systemId)
        require (stringProperty "repository" resolvedComponent = stringProperty "repository" release) (sprintf "%s repository mismatch for %s" name systemId)
        require (stringProperty "tag" resolvedComponent = stringProperty "tag" release) (sprintf "%s tag mismatch for %s" name systemId)
        require (stringProperty "commit" resolvedComponent = stringProperty "commit" release) (sprintf "%s commit mismatch for %s" name systemId)
        require (stringProperty "releaseStage" resolvedComponent = releaseStage) (sprintf "%s stage mismatch for %s" name systemId)
        require (stringProperty "lifecycleState" resolvedComponent = "active") (sprintf "%s resolved release must be active for %s" name systemId)
        require (stringProperty "distributionClass" resolvedComponent = stringProperty "distributionClass" release) (sprintf "%s distribution class mismatch for %s" name systemId)

        let releaseExecutable = property "executable" release
        let resolvedExecutable = property "executable" resolvedComponent
        require (releaseExecutable.ValueKind = resolvedExecutable.ValueKind) (sprintf "%s executable shape mismatch for %s" name systemId)
        if releaseExecutable.ValueKind = JsonValueKind.String then
            require (releaseExecutable.GetString() = resolvedExecutable.GetString()) (sprintf "%s executable mismatch for %s" name systemId)

        let resolvedReleaseManifest = property "releaseManifest" resolvedComponent
        require (stringProperty "sha256" resolvedReleaseManifest = releaseHash) (sprintf "%s release manifest digest mismatch for %s" name systemId)

        let releaseArtifacts = arrayProperty "artifacts" release
        let resolvedArtifacts = arrayProperty "artifacts" resolvedComponent

        for selected in resolvedArtifacts do
            let artifactName = stringProperty "name" selected
            let digest = stringProperty "sha256" selected
            let matching =
                releaseArtifacts
                |> Array.tryFind (fun artifact ->
                    stringProperty "name" artifact = artifactName &&
                    stringProperty "sha256" artifact = digest)
            require matching.IsSome (sprintf "%s resolved artifact %s is not in release %s" name artifactName systemId)

        let distributionClass = stringProperty "distributionClass" release
        let requiresPlatformExecutable =
            distributionClass = "self-contained-native-cli" ||
            distributionClass = "self-contained-native-daemon"

        if requiresPlatformExecutable then
            let executableForPlatform =
                releaseArtifacts
                |> Array.exists (fun artifact ->
                    if stringProperty "purpose" artifact <> "executable" then
                        false
                    else
                        let artifactPlatform = property "platform" artifact
                        artifactPlatform.ValueKind = JsonValueKind.String &&
                        artifactPlatform.GetString() = platform)

            require executableForPlatform (sprintf "%s release %s has no executable for %s" name systemId platform)

    printfn "%s PASS" name
    printfn "  profile sha256:  %s" profileHash
    printfn "  snapshot sha256: %s" snapshotHash
    printfn "  platform:        %s" platform

validateResolution
    "Registry proof"
    "examples/distribution-proof/registry-proof.profile.json"
    "examples/distribution-proof/catalog-snapshot.json"
    "examples/distribution-proof/registry-proof.linux-x64.resolved.json"
    (Map.ofList [
        "ordo", "examples/distribution-proof/ordo.release.v2.json"
    ])

validateResolution
    "Echelon engineering"
    "profiles/echelon-engineering.profile.json"
    "snapshots/echelon-engineering-0.1.0.catalog.json"
    "resolved/echelon-engineering/0.1.0/linux-x64.json"
    (Map.ofList [
        "praxis", "releases/praxis/3.6.0.release.json"
        "ordo", "releases/ordo/1.4.0.release.json"
    ])

printfn "Distribution conformance PASS"
