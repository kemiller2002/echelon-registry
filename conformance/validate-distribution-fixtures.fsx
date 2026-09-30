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

let property name (element: JsonElement) =
    element.GetProperty name

let stringProperty name element =
    (property name element).GetString()

let arrayProperty name element =
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

let releasePath = "examples/distribution-proof/ordo.release.v2.json"
let profilePath = "examples/distribution-proof/registry-proof.profile.json"
let snapshotPath = "examples/distribution-proof/catalog-snapshot.json"
let resolvedPath = "examples/distribution-proof/registry-proof.linux-x64.resolved.json"

use releaseDoc = parse releasePath
use profileDoc = parse profilePath
use snapshotDoc = parse snapshotPath
use resolvedDoc = parse resolvedPath
use systemsDoc = parse "registry/systems-v2.json"

let release = releaseDoc.RootElement
let profile = profileDoc.RootElement
let snapshot = snapshotDoc.RootElement
let resolved = resolvedDoc.RootElement

require (stringProperty "schema" release = "echelon.release/v2") "release fixture must use echelon.release/v2"
require (stringProperty "schema" profile = "echelon.profile/v1") "profile fixture must use echelon.profile/v1"
require (stringProperty "schema" snapshot = "echelon.catalog-snapshot/v1") "snapshot fixture must use echelon.catalog-snapshot/v1"
require (stringProperty "schema" resolved = "echelon.resolved-release-set/v1") "resolved fixture must use echelon.resolved-release-set/v1"

let releaseHash = sha256 releasePath
let profileHash = sha256 profilePath
let snapshotHash = sha256 snapshotPath

let snapshotRelease = arrayProperty "releases" snapshot |> Array.exactlyOne
let snapshotProfile = arrayProperty "profiles" snapshot |> Array.exactlyOne

require (stringProperty "sha256" snapshotRelease = releaseHash) "catalog snapshot release digest does not match release manifest bytes"
require (stringProperty "sha256" snapshotProfile = profileHash) "catalog snapshot profile digest does not match profile bytes"

let resolvedProfile = property "profile" resolved
let resolvedSnapshot = property "catalogSnapshot" resolved
require (stringProperty "sha256" resolvedProfile = profileHash) "resolved release set profile digest does not match profile bytes"
require (stringProperty "sha256" resolvedSnapshot = snapshotHash) "resolved release set snapshot digest does not match snapshot bytes"

let profileComponent = arrayProperty "components" profile |> Array.exactlyOne
let resolvedComponent = arrayProperty "components" resolved |> Array.exactlyOne
let versionRule = property "version" profileComponent

require (stringProperty "systemId" profileComponent = stringProperty "systemId" release) "profile system id does not match release"
require (stringProperty "exact" versionRule = stringProperty "version" release) "profile exact version does not match release"
require (stringProperty "systemId" resolvedComponent = stringProperty "systemId" release) "resolved system id does not match release"
require (stringProperty "version" resolvedComponent = stringProperty "version" release) "resolved version does not match release"
require (stringProperty "repository" resolvedComponent = stringProperty "repository" release) "resolved repository does not match release"
require (stringProperty "tag" resolvedComponent = stringProperty "tag" release) "resolved tag does not match release"
require (stringProperty "commit" resolvedComponent = stringProperty "commit" release) "resolved commit does not match release"
require (stringProperty "releaseStage" resolvedComponent = stringProperty "releaseStage" release) "resolved release stage does not match release"
require (stringProperty "lifecycleState" resolvedComponent = "active") "resolved release must be active"
require (stringProperty "distributionClass" resolvedComponent = stringProperty "distributionClass" release) "resolved distribution class does not match release"

let allowedStages =
    arrayProperty "allowedReleaseStages" profile
    |> Array.map (fun item -> item.GetString())

require (allowedStages |> Array.contains (stringProperty "releaseStage" release)) "profile does not permit the selected release stage"

let platform = stringProperty "platform" resolved
let supportedPlatforms =
    arrayProperty "supportedPlatforms" profile
    |> Array.map (fun item -> item.GetString())

require (supportedPlatforms |> Array.contains platform) "resolved platform is not supported by the profile"

let releaseArtifacts = arrayProperty "artifacts" release
let resolvedArtifacts = arrayProperty "artifacts" resolvedComponent

for selected in resolvedArtifacts do
    let name = stringProperty "name" selected
    let digest = stringProperty "sha256" selected
    let matching =
        releaseArtifacts
        |> Array.tryFind (fun artifact ->
            stringProperty "name" artifact = name &&
            stringProperty "sha256" artifact = digest)
    require matching.IsSome (sprintf "resolved artifact %s is not present in the release manifest" name)

let executableForPlatform =
    releaseArtifacts
    |> Array.exists (fun artifact ->
        if stringProperty "purpose" artifact <> "executable" then
            false
        else
            let artifactPlatform = property "platform" artifact
            artifactPlatform.ValueKind = JsonValueKind.String &&
            artifactPlatform.GetString() = platform)

require executableForPlatform (sprintf "release has no executable artifact for %s" platform)

let resolvedReleaseManifest = property "releaseManifest" resolvedComponent
require (stringProperty "sha256" resolvedReleaseManifest = releaseHash) "resolved release manifest digest does not match release bytes"

let systems = arrayProperty "systems" systemsDoc
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

printfn "Distribution conformance PASS"
printfn "  release sha256:  %s" releaseHash
printfn "  profile sha256:  %s" profileHash
printfn "  snapshot sha256: %s" snapshotHash
printfn "  platform:        %s" platform
