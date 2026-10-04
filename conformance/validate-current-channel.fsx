open System
open System.IO
open System.Security.Cryptography
open System.Text.Json

let require condition message =
    if not condition then failwith message

let parse path =
    JsonDocument.Parse(File.ReadAllText path)

let sha256 path =
    File.ReadAllBytes path
    |> SHA256.HashData
    |> Convert.ToHexString
    |> fun value -> value.ToLowerInvariant()

let property name (element: JsonElement) =
    element.GetProperty name

let stringProperty name element =
    (property name element).GetString()

let arrayProperty name element =
    (property name element).EnumerateArray() |> Seq.toArray

let channelPath = "channels/echelon-current/channel.json"
use channelDoc = parse channelPath
let channel = channelDoc.RootElement

require (stringProperty "schema" channel = "echelon.current-channel/v1") "current channel schema mismatch"
require (stringProperty "id" channel = "echelon-current") "current channel id mismatch"

let profileRef = property "profile" channel
let profilePath = "profiles/echelon-current.profile.json"
require (File.Exists profilePath) "current profile is missing"
require (stringProperty "id" profileRef = "echelon-current") "current channel profile id mismatch"
require (stringProperty "sha256" profileRef = sha256 profilePath) "current channel profile digest mismatch"

use profileDoc = parse profilePath
let profile = profileDoc.RootElement
require (stringProperty "version" profileRef = stringProperty "version" profile) "current channel profile version mismatch"

let snapshotRef = property "catalogSnapshot" channel
let snapshotPath = stringProperty "path" snapshotRef
require (snapshotPath = "snapshots/echelon-current.catalog.json") "current channel snapshot path drifted"
require (File.Exists snapshotPath) "current channel snapshot is missing"
require (stringProperty "sha256" snapshotRef = sha256 snapshotPath) "current channel snapshot digest mismatch"

let profileIds =
    arrayProperty "components" profile
    |> Array.map (stringProperty "systemId")
    |> Set.ofArray

let supported =
    arrayProperty "supportedPlatforms" profile
    |> Array.map (fun item -> item.GetString())
    |> Set.ofArray

let entries = arrayProperty "platforms" channel
let platforms = entries |> Array.map (stringProperty "platform")
require (platforms |> Array.distinct |> Array.length = platforms.Length) "current channel repeats a platform"
require (Set.ofArray platforms = supported) "current channel platforms differ from the profile"

for entry in entries do
    let platform = stringProperty "platform" entry
    let path = stringProperty "path" entry
    let expected = stringProperty "sha256" entry

    require (path = $"channels/echelon-current/{platform}.json") $"current channel path mismatch for {platform}"
    require (File.Exists path) $"current resolved set is missing for {platform}"
    require (sha256 path = expected) $"current resolved-set digest mismatch for {platform}"

    use resolvedDoc = parse path
    let resolved = resolvedDoc.RootElement
    require (stringProperty "schema" resolved = "echelon.resolved-release-set/v1") $"resolved schema mismatch for {platform}"
    require (stringProperty "platform" resolved = platform) $"resolved platform mismatch for {platform}"

    let resolvedProfile = property "profile" resolved
    require (stringProperty "id" resolvedProfile = stringProperty "id" profileRef) $"resolved profile id mismatch for {platform}"
    require (stringProperty "version" resolvedProfile = stringProperty "version" profileRef) $"resolved profile version mismatch for {platform}"
    require (stringProperty "sha256" resolvedProfile = stringProperty "sha256" profileRef) $"resolved profile digest mismatch for {platform}"

    let resolvedSnapshot = property "catalogSnapshot" resolved
    require (stringProperty "sha256" resolvedSnapshot = stringProperty "sha256" snapshotRef) $"resolved snapshot digest mismatch for {platform}"

    let resolvedIds =
        arrayProperty "components" resolved
        |> Array.map (stringProperty "systemId")
        |> Set.ofArray

    require (resolvedIds = profileIds) $"resolved component set mismatch for {platform}"

printfn "Echelon current channel PASS"
printfn "  profile: %s@%s" (stringProperty "id" profileRef) (stringProperty "version" profileRef)
printfn "  platforms: %s" (platforms |> Array.sort |> String.concat ", ")
