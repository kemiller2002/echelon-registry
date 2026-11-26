/// Data-only JSON access. Content is never evaluated.
module Echelon.Portfolio.Json

open System
open System.Globalization
open System.Text.Json.Nodes

let tryProp (name: string) (o: JsonObject) : JsonNode option =
    let mutable v: JsonNode = null

    if o.TryGetPropertyValue(name, &v) && not (isNull v) then Some v else None

let tryString name o =
    tryProp name o
    |> Option.bind (fun n ->
        match n with
        | :? JsonValue as v ->
            match v.TryGetValue<string>() with
            | true, s -> Some s
            | _ -> None
        | _ -> None)

let tryBool name o =
    tryProp name o
    |> Option.bind (fun n ->
        match n with
        | :? JsonValue as v ->
            match v.TryGetValue<bool>() with
            | true, b -> Some b
            | _ -> None
        | _ -> None)

let tryInt name o =
    tryProp name o
    |> Option.bind (fun n ->
        match n with
        | :? JsonValue as v ->
            match v.TryGetValue<int>() with
            | true, i -> Some i
            | _ -> None
        | _ -> None)

let tryObject name o =
    tryProp name o
    |> Option.bind (fun n ->
        match n with
        | :? JsonObject as x -> Some x
        | _ -> None)

let objects name o : JsonObject list =
    match tryProp name o with
    | Some(:? JsonArray as a) ->
        a
        |> Seq.choose (fun n ->
            match n with
            | :? JsonObject as x -> Some x
            | _ -> None)
        |> List.ofSeq
    | _ -> []

let strings name o : string list =
    match tryProp name o with
    | Some(:? JsonArray as a) ->
        a
        |> Seq.choose (fun n ->
            match n with
            | :? JsonValue as v ->
                match v.TryGetValue<string>() with
                | true, s -> Some s
                | _ -> None
            | _ -> None)
        |> List.ofSeq
    | _ -> []

let tryDate name o =
    tryString name o
    |> Option.bind (fun s ->
        match DateOnly.TryParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None) with
        | true, d -> Some d
        | _ -> None)

/// All string leaves of a node, depth first.
let rec stringLeaves (node: JsonNode) : string list =
    match node with
    | null -> []
    | :? JsonObject as o -> o |> Seq.collect (fun kv -> stringLeaves kv.Value) |> List.ofSeq
    | :? JsonArray as a -> a |> Seq.collect stringLeaves |> List.ofSeq
    | :? JsonValue as v ->
        match v.TryGetValue<string>() with
        | true, s -> [ s ]
        | _ -> []
    | _ -> []

let parseObject (text: string) : Result<JsonObject, string> =
    try
        match JsonNode.Parse text with
        | :? JsonObject as o -> Ok o
        | _ -> Error "document root is not a JSON object"
    with ex ->
        Error $"invalid JSON: {ex.Message}"
