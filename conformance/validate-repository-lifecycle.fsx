// Conformance for spec/repository-lifecycle-contract.md.
//
// Proves, with synthetic fixtures only, that:
// - a release declaring echelon.repository-lifecycle resolves to a component
//   carrying repositoryLifecycle with the declared contract version;
// - a non-declaring release resolves without the field;
// - the resolved-release-set schema admits exactly that optional shape; and
// - the checked-in resolved fixture equals the resolver's output.
//
// Usage: dotnet fsi conformance/validate-repository-lifecycle.fsx [GENERATED-RESOLVED-SET]

open System
open System.IO
open System.Text.Json

let require condition message =
    if not condition then failwith message

let fixtureDir = "examples/repository-lifecycle-proof"
let checkedIn = Path.Combine(fixtureDir, "repository-lifecycle-proof.linux-x64.resolved.json")

let generated =
    fsi.CommandLineArgs
    |> Array.skip 1
    |> Array.filter ((<>) "--")
    |> Array.tryHead
    |> Option.defaultValue checkedIn

require (File.Exists "spec/repository-lifecycle-contract.md") "repository lifecycle contract specification is missing"
require (File.ReadAllBytes generated = File.ReadAllBytes checkedIn) $"{generated} differs from the checked-in resolver output {checkedIn}"

let tryProperty (name: string) (element: JsonElement) =
    let mutable value = Unchecked.defaultof<JsonElement>
    if element.ValueKind = JsonValueKind.Object && element.TryGetProperty(name, &value) then Some value else None

let text name element =
    tryProperty name element |> Option.map (fun v -> v.GetString()) |> Option.bind Option.ofObj

let schemaDoc = JsonDocument.Parse(File.ReadAllText "schemas/resolved-release-set.schema.json")
let lifecycleSchema =
    schemaDoc.RootElement
    |> tryProperty "$defs"
    |> Option.bind (tryProperty "component")
    |> Option.bind (tryProperty "properties")
    |> Option.bind (tryProperty "repositoryLifecycle")

require lifecycleSchema.IsSome "resolved-release-set schema does not define component.repositoryLifecycle"

let required =
    schemaDoc.RootElement
    |> tryProperty "$defs"
    |> Option.bind (tryProperty "component")
    |> Option.bind (tryProperty "required")
    |> Option.map (fun r -> r.EnumerateArray() |> Seq.choose (fun v -> v.GetString() |> Option.ofObj) |> Set.ofSeq)
    |> Option.defaultValue Set.empty

require (not (required.Contains "repositoryLifecycle")) "repositoryLifecycle must stay optional so existing resolved sets remain valid"

let contractConst =
    lifecycleSchema
    |> Option.bind (tryProperty "properties")
    |> Option.bind (tryProperty "contract")
    |> Option.bind (text "const")

require (contractConst = Some "echelon.repository-lifecycle") "repositoryLifecycle.contract must be the echelon.repository-lifecycle constant"

let resolvedDoc = JsonDocument.Parse(File.ReadAllText generated)
let components =
    resolvedDoc.RootElement
    |> tryProperty "components"
    |> Option.map (fun c -> c.EnumerateArray() |> Seq.toList)
    |> Option.defaultValue []

let byId id =
    components
    |> List.tryFind (fun c -> text "systemId" c = Some id)
    |> Option.defaultWith (fun () -> failwith $"resolved set has no component {id}")

let lifecycle = byId "lifecycle-fixture"
let declared = tryProperty "repositoryLifecycle" lifecycle
require declared.IsSome "declaring release did not resolve with repositoryLifecycle"
require (declared |> Option.bind (text "contract") = Some "echelon.repository-lifecycle") "declared lifecycle contract id mismatch"
require (declared |> Option.bind (tryProperty "contractVersion") |> Option.map (fun v -> v.GetInt32()) = Some 1) "declared lifecycle contract version mismatch"
require (text "executable" lifecycle = Some "lifecycle-fixture") "lifecycle component executable must come from the release"
require (text "distributionClass" lifecycle = Some "self-contained-native-cli") "lifecycle component must be a self-contained native CLI"

let legacy = byId "legacy-fixture"
require (tryProperty "repositoryLifecycle" legacy).IsNone "non-declaring release must not gain repositoryLifecycle"

printfn "Repository lifecycle contract PASS"
printfn "  declaring component:     lifecycle-fixture (echelon.repository-lifecycle v1)"
printfn "  non-declaring component: legacy-fixture (no repositoryLifecycle)"
