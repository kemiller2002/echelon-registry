open System
open System.IO
open System.Security.Cryptography
open System.Text.Json

let fail message =
    eprintfn "ERROR: %s" message
    Environment.ExitCode <- 2
    failwith message

let require condition message =
    if not condition then fail message

let sha256 path =
    use stream = File.OpenRead path
    SHA256.HashData stream
    |> Convert.ToHexString
    |> fun value -> value.ToLowerInvariant()

let parse path = JsonDocument.Parse(File.ReadAllBytes path)

let property name (element: JsonElement) = element.GetProperty name
let str name element = (property name element).GetString()
let array name element = (property name element).EnumerateArray() |> Seq.toArray

let receiptPath = "freezes/indy-init-0.1.0.freeze.json"
use receiptDoc = parse receiptPath
let receipt = receiptDoc.RootElement

require (str "schema" receipt = "echelon.environment-freeze/v1") "freeze schema mismatch"
require (str "id" receipt = "indy-init") "freeze id mismatch"
require (str "version" receipt = "0.1.0") "freeze version mismatch"

let profile = property "profile" receipt
let profilePath = str "path" profile
require (sha256 profilePath = str "sha256" profile) "frozen profile digest mismatch"

let catalog = property "catalogSnapshot" receipt
let catalogPath = str "path" catalog
require (sha256 catalogPath = str "sha256" catalog) "frozen catalog snapshot digest mismatch"

for component in array "components" receipt do
    let systemId = str "systemId" component
    let version = str "version" component
    let releasePath = $"releases/{systemId}/{version}.release.json"
    require (File.Exists releasePath) $"frozen release manifest missing: {releasePath}"
    require
        (sha256 releasePath = str "releaseManifestSha256" component)
        $"frozen release manifest digest mismatch: {systemId}@{version}"

match Environment.GetEnvironmentVariable "INDY_INIT_RESOLVED_DIR" |> Option.ofObj with
| None -> ()
| Some resolvedDir ->
    for expected in array "resolvedSets" receipt do
        let platform = str "platform" expected
        let path = Path.Combine(resolvedDir, platform + ".json")
        require (File.Exists path) $"resolved set missing for {platform}: {path}"
        require (sha256 path = str "sha256" expected) $"resolved set digest mismatch for {platform}"

printfn "Indy Init freeze PASS"
printfn "  profile: %s" (str "sha256" profile)
printfn "  catalog: %s" (str "sha256" catalog)
printfn "  components: %d" ((array "components" receipt).Length)
printfn "  resolved sets: %d" ((array "resolvedSets" receipt).Length)
