open System
open System.IO
open System.Security.Cryptography
open System.Text.Json

let require condition message =
    if not condition then failwith message

let parse path = JsonDocument.Parse(File.ReadAllBytes path)

let property (name: string) (element: JsonElement) = element.GetProperty name
let str name element = (property name element).GetString()
let array name element = (property name element).EnumerateArray() |> Seq.toArray

let sha256 path =
    use stream = File.OpenRead path
    SHA256.HashData stream
    |> Convert.ToHexString
    |> fun value -> value.ToLowerInvariant()

let profilePath = "profiles/indy-init.profile.json"
let snapshotPath = "snapshots/indy-init-0.1.0.catalog.json"

let profileDoc = parse profilePath
let snapshotDoc = parse snapshotPath

let profile = profileDoc.RootElement
let snapshot = snapshotDoc.RootElement

require (str "schema" profile = "echelon.profile/v1") "Indy Init profile schema mismatch"
require (str "id" profile = "indy-init") "Indy Init profile id mismatch"
require (str "version" profile = "0.1.0") "Indy Init profile version mismatch"
require (str "schema" snapshot = "echelon.catalog-snapshot/v1") "Indy Init snapshot schema mismatch"

let components = array "components" profile
let releases = array "releases" snapshot
let profileRows = array "profiles" snapshot
let systems = array "systems" snapshot

let requiredIds =
    components
    |> Array.filter (fun item -> (property "required" item).GetBoolean())
    |> Array.map (str "systemId")
    |> Set.ofArray

let systemIds = systems |> Array.map (str "id") |> Set.ofArray
require (Set.isSubset requiredIds systemIds) "Indy Init snapshot must have a system row for every required component"

let profileRow =
    profileRows
    |> Array.find (fun row -> str "id" row = "indy-init" && str "version" row = "0.1.0")

require (str "sha256" profileRow = sha256 profilePath) "Indy Init snapshot profile digest mismatch"

let expectedCataloged =
    Map.ofList [
        "praxis", ("3.6.0", "releases/praxis/3.6.0.release.json")
        "ordo", ("1.4.0", "releases/ordo/1.4.0.release.json")
        "percepta", ("0.1.0", "releases/percepta/0.1.0.release.json")
        "forma", ("0.3.0", "releases/forma/0.3.0.release.json")
        "limen", ("0.6.2", "releases/limen/0.6.2.release.json")
        "aegis", ("1.0.0", "releases/aegis/1.0.0.release.json")
        "folio", ("0.3.0", "releases/folio/0.3.0.release.json")
    ]

for KeyValue(systemId, (version, path)) in expectedCataloged do
    let row =
        releases
        |> Array.find (fun item -> str "systemId" item = systemId && str "version" item = version)

    require (str "manifest" row = path) $"Snapshot manifest path mismatch for {systemId}"
    require (str "sha256" row = sha256 path) $"Snapshot release digest mismatch for {systemId}"

let exactVersion systemId =
    components
    |> Array.find (fun item -> str "systemId" item = systemId)
    |> property "version"
    |> property "exact"
    |> fun item -> item.GetString()

require (exactVersion "percepta" = "0.1.0") "Percepta must be pinned to cataloged 0.1.0"
require (exactVersion "forma" = "0.3.0") "Forma must be pinned to cataloged 0.3.0"
require (exactVersion "limen" = "0.6.2") "Limen must be pinned to cataloged 0.6.2"
require (exactVersion "aegis" = "1.0.0") "Aegis must be pinned to cataloged 1.0.0"
require (exactVersion "folio" = "0.3.0") "Folio must be pinned to cataloged 0.3.0"

let catalogedIds = releases |> Array.map (str "systemId") |> Set.ofArray
let unresolved = Set.difference requiredIds catalogedIds
let expectedUnresolved : Set<string> = Set.empty

let unresolvedText = unresolved |> Set.toList |> String.concat ","

require (unresolved = expectedUnresolved)
    $"Unexpected Indy Init unresolved set. Expected none; observed {unresolvedText}"

printfn "Indy Init catalog progress PASS"
printfn "  cataloged: %s" (catalogedIds |> Set.toList |> List.sort |> String.concat ", ")
printfn "  unresolved: %s" (unresolved |> Set.toList |> List.sort |> String.concat ", ")
