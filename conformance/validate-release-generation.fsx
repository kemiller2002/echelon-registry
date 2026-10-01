open System
open System.IO
open System.Security.Cryptography
open System.Text.Json

let fail message = failwith message
let require condition message = if not condition then fail message

let parse path = JsonDocument.Parse(File.ReadAllBytes path)

let property (name: string) (element: JsonElement) = element.GetProperty name
let str name element = (property name element).GetString()
let array name element = (property name element).EnumerateArray() |> Seq.toArray

let sha256 path =
    use stream = File.OpenRead path
    SHA256.HashData stream
    |> Convert.ToHexString
    |> fun value -> value.ToLowerInvariant()

let outputPath =
    if fsi.CommandLineArgs.Length > 1 then fsi.CommandLineArgs[1]
    else "examples/release-contract/generated.release.json"

use doc = parse outputPath
let root = doc.RootElement

require (str "schema" root = "echelon.release/v2") "generated schema mismatch"
require (str "systemId" root = "release-contract-fixture") "generated systemId mismatch"
require (str "version" root = "1.2.3") "generated version mismatch"
require (str "repository" root = "kemiller2002/echelon-registry") "generated repository mismatch"
require (str "tag" root = "v1.2.3") "generated tag mismatch"
require (str "commit" root = "1111111111111111111111111111111111111111") "generated commit mismatch"
require (str "releaseStage" root = "preview") "generated stage mismatch"
require (str "lifecycleState" root = "active") "generated lifecycle state mismatch"
require (str "distributionClass" root = "self-contained-native-cli") "generated distribution class mismatch"
require (str "executable" root = "fixture") "generated executable mismatch"

let artifacts = array "artifacts" root
require (artifacts.Length = 3) "generated artifact count mismatch"

let expected =
    [ "fixture-1.2.3-linux-x64.bin", sha256 "examples/release-contract/artifacts/fixture-1.2.3-linux-x64.bin"
      "fixture.spdx.json", sha256 "examples/release-contract/artifacts/fixture.spdx.json"
      "THIRD-PARTY-NOTICES.txt", sha256 "examples/release-contract/artifacts/THIRD-PARTY-NOTICES.txt" ]
    |> Map.ofList

for artifact in artifacts do
    let name = str "name" artifact
    let digest = str "sha256" artifact
    require (Map.tryFind name expected = Some digest) $"generated digest mismatch for {name}"

let evidence = property "evidence" root
let sbom = property "sbom" evidence
let licenses = property "licenses" evidence
require (str "artifactName" sbom = "fixture.spdx.json") "SBOM evidence reference missing"
require (str "sha256" sbom = expected["fixture.spdx.json"]) "SBOM digest mismatch"
require (str "artifactName" licenses = "THIRD-PARTY-NOTICES.txt") "license evidence reference missing"
require (str "sha256" licenses = expected["THIRD-PARTY-NOTICES.txt"]) "license digest mismatch"

let distribution = array "distributions" root |> Array.exactlyOne
require (str "mechanism" distribution = "github-release") "distribution mechanism mismatch"
require (str "url" distribution = "https://github.com/kemiller2002/echelon-registry/releases/tag/v1.2.3") "generated release URL mismatch"

printfn "Shared release contract PASS"
