/// Public projection of the internal initiative register (POL-ORG-TRN-001 §8, STD-ORG-PUB-001 §4).
/// Restricted fields are excluded by construction: only allow-listed keys are ever copied.
module Echelon.Portfolio.Projection

open System
open System.Security.Cryptography
open System.Text
open System.Text.Json
open System.Text.Json.Nodes
open Echelon.Portfolio.Json
open Echelon.Portfolio.Schema

let sha256Hex (text: string) =
    SHA256.HashData(Encoding.UTF8.GetBytes text)
    |> Array.map (fun b -> b.ToString("x2"))
    |> String.concat ""

type Publication =
    | Publish
    | Withhold of category: string * reviewBy: string

let publicationOf (o: JsonObject) : Result<Publication, string> =
    let id = tryString "id" o |> Option.defaultValue "<missing id>"

    match tryObject "publication" o with
    | None -> Error $"{id}: publication is required"
    | Some p ->
        match tryString "state" p with
        | Some "public" -> Ok Publish
        | Some "withheld" ->
            match tryString "category" p, tryString "reviewBy" p with
            | Some c, Some r -> Ok(Withhold(c, r))
            | _ -> Error $"{id}: withheld publication needs category and reviewBy"
        | other -> Error $"{id}: publication.state must be 'public' or 'withheld' (was %A{other})"

/// Copy only allow-listed keys, in allow-list order, deep-cloning values.
let allowListed (keys: string list) (source: JsonObject) : JsonObject =
    keys
    |> List.choose (fun k -> tryProp k source |> Option.map (fun v -> k, v.DeepClone()))
    |> List.fold
        (fun (acc: JsonObject) (k, v) ->
            acc.Add(k, v)
            acc)
        (JsonObject())

let projectInitiative (o: JsonObject) : Result<JsonObject, string> =
    publicationOf o
    |> Result.map (fun publication ->
        match publication with
        | Publish -> allowListed publicInitiativeKeys o
        | Withhold(category, reviewBy) ->
            let w = JsonObject()
            w.Add("id", JsonValue.Create(tryString "id" o |> Option.defaultValue ""))
            w.Add("withheld", JsonValue.Create true)
            w.Add("justificationCategory", JsonValue.Create category)
            w.Add("reviewBy", JsonValue.Create reviewBy)
            w)

let private sequenceResults (rs: Result<'a, string> list) : Result<'a list, string list> =
    let errors = rs |> List.choose (function Error e -> Some e | Ok _ -> None)

    if List.isEmpty errors then
        Ok(rs |> List.choose (function Ok v -> Some v | Error _ -> None))
    else
        Error errors

let serializerOptions = JsonSerializerOptions(WriteIndented = true)

/// Project the full register text into the public document text (deterministic, trailing newline).
let projectRegister (sourceRecord: string) (registerText: string) : Result<string, string list> =
    match parseObject registerText with
    | Error e -> Error [ e ]
    | Ok register when tryString "schema" register <> Some RegisterSchema ->
        Error [ $"source schema must be '{RegisterSchema}'" ]
    | Ok register ->
        objects "initiatives" register
        |> List.map projectInitiative
        |> sequenceResults
        |> Result.map (fun projected ->
            let doc = JsonObject()
            doc.Add("schema", JsonValue.Create InitiativesSchema)
            doc.Add("classification", JsonValue.Create "public")
            doc.Add("sourceRecord", JsonValue.Create sourceRecord)
            doc.Add("sourceDigest", JsonValue.Create("sha256:" + sha256Hex registerText))
            let arr = JsonArray()
            projected |> List.iter (fun p -> arr.Add p)
            doc.Add("initiatives", arr)
            doc.ToJsonString(serializerOptions).Replace("\r\n", "\n") + "\n")
