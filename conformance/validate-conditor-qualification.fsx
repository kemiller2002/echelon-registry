// REG-RU-007: echelon-current must not select a version of a consumer-declared
// component that the latest released Conditor refuses.
//
// Usage: dotnet fsi conformance/validate-conditor-qualification.fsx <compatibility.json>
// where <compatibility.json> is `conditor compatibility --json` from the latest
// released Conditor binary. A NuGet library that the compatibility graph does
// not list is accepted at the selected version (Conditor takes it on Registry
// authority); any other unlisted declared component is refused.

open System.IO
open System.Text.Json

let parse (path: string) = JsonDocument.Parse(File.ReadAllText path).RootElement
let str (name: string) (e: JsonElement) = e.GetProperty(name).GetString()
let items (name: string) (e: JsonElement) = e.GetProperty(name).EnumerateArray() |> Seq.toList

let compatibilityPath =
    match fsi.CommandLineArgs |> Array.toList |> List.tail with
    | [ path ] -> path
    | _ -> failwith "usage: validate-conditor-qualification.fsx <conditor-compatibility.json>"

let qualified: Map<string, Set<string>> =
    parse compatibilityPath
    |> items "components"
    |> List.map (fun c -> str "id" c, c |> items "versions" |> List.map (fun v -> v.GetString()) |> Set.ofList)
    |> Map.ofList

let declared =
    parse "conformance/conditor-declared-components.json"
    |> items "components"
    |> List.map (fun c -> c.GetString())
    |> Set.ofList

let resolvedSets =
    parse "channels/echelon-current/channel.json"
    |> items "platforms"
    |> List.map (fun p -> str "platform" p, parse (str "path" p))

let refusal platform (c: JsonElement) =
    let id, version, cls = str "systemId" c, str "version" c, str "distributionClass" c
    match Map.tryFind id qualified with
    | Some versions when Set.contains version versions -> None
    | Some versions ->
        let known = String.concat ", " versions
        Some $"{platform}: {id} {version} is not qualified by the released Conditor (qualified: {known})"
    | None when cls = "nuget-library" -> None
    | None -> Some $"{platform}: {id} {version} ({cls}) has no Conditor compatibility entry"

let selected =
    resolvedSets
    |> List.collect (fun (platform, set) ->
        set |> items "components" |> List.filter (fun c -> declared.Contains(str "systemId" c)) |> List.map (fun c -> platform, c))

let refusals = selected |> List.choose (fun (platform, c) -> refusal platform c)

match refusals with
| [] ->
    let checkedIds = selected |> List.map (snd >> str "systemId") |> List.distinct |> List.sort
    printfn "Conditor qualification (REG-RU-007) PASS"
    printfn "  platforms: %d" resolvedSets.Length
    printfn "  declared components checked: %s" (String.concat ", " checkedIds)
| _ ->
    refusals |> List.iter (eprintfn "REG-RU-007: %s")
    eprintfn "Release a Conditor that qualifies these versions before selecting them in echelon-current."
    exit 1
