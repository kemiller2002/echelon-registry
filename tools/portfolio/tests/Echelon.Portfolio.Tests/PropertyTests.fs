/// Invariants that must hold for any input.
module Echelon.Portfolio.Tests.PropertyTests

open System.Text.Json.Nodes
open FsCheck
open FsCheck.FSharp
open FsCheck.Xunit
open Echelon.Portfolio
open Echelon.Portfolio.Json

let private keyGen =
    Gen.oneof
        [ Gen.elements Schema.publicInitiativeKeys
          Gen.elements [ "internal"; "budget"; "counterparty"; "salary"; "notes"; "resourceEstimate" ]
          ArbMap.defaults |> ArbMap.generate<NonEmptyString> |> Gen.map (fun s -> s.Get) ]

let private initiativeGen =
    gen {
        let! keys = Gen.listOf keyGen
        let! values = Gen.listOfLength keys.Length (ArbMap.defaults |> ArbMap.generate<string>)
        let! withheld = ArbMap.defaults |> ArbMap.generate<bool>
        let o = JsonObject()

        List.zip keys values
        |> List.distinctBy fst
        |> List.filter (fun (k, _) -> k <> "publication" && k <> "id")
        |> List.iter (fun (k, v) -> o.[k] <- JsonValue.Create(v))

        o.["id"] <- JsonValue.Create "INIT-001"

        o.["publication"] <-
            if withheld then
                JsonObject(
                    dict
                        [ "state", (JsonValue.Create "withheld" :> JsonNode)
                          "category", JsonValue.Create "other"
                          "reviewBy", JsonValue.Create "2027-01-01" ]
                )
            else
                JsonObject(dict [ "state", (JsonValue.Create "public" :> JsonNode) ])

        return o
    }

type Generators =
    static member Initiative() = Arb.fromGen initiativeGen

[<Property(Arbitrary = [| typeof<Generators> |])>]
let ``projection only ever emits allow-listed keys`` (o: JsonObject) =
    match Projection.projectInitiative o with
    | Ok p ->
        let allowed =
            Set.union (set Schema.publicInitiativeKeys) Schema.withheldInitiativeKeys

        p |> Seq.forall (fun kv -> allowed.Contains kv.Key)
    | Error _ -> false

[<Property(Arbitrary = [| typeof<Generators> |])>]
let ``projection never carries the publication decision or internal block`` (o: JsonObject) =
    match Projection.projectInitiative o with
    | Ok p -> not (p.ContainsKey "publication") && not (p.ContainsKey "internal")
    | Error _ -> false

[<Property(Arbitrary = [| typeof<Generators> |])>]
let ``projection is deterministic`` (o: JsonObject) =
    let a = Projection.projectInitiative o |> Result.map (fun p -> p.ToJsonString())
    let b = Projection.projectInitiative o |> Result.map (fun p -> p.ToJsonString())
    a = b

[<Property>]
let ``sensitive matching never throws and a clean word never matches`` (s: string) =
    let s = if isNull s then "" else s
    Rules.sensitiveMatches s |> ignore
    List.isEmpty (Rules.sensitiveMatches "portfolio")

[<Property>]
let ``digest is stable and 64 hex characters`` (s: NonNull<string>) =
    let d = Projection.sha256Hex s.Get
    d = Projection.sha256Hex s.Get && d.Length = 64

[<Property>]
let ``unknown keys are reported exactly once each`` (keys: Set<NonEmptyString>) =
    let o = JsonObject()
    keys |> Set.iter (fun k -> o.[k.Get] <- JsonValue.Create 1)
    let unknown = keys |> Set.map (fun k -> k.Get) |> Set.filter (fun k -> not (Schema.repositoryKeys.Contains k))
    (Schema.unknownKeys Schema.repositoryKeys "s" o).Length = unknown.Count
