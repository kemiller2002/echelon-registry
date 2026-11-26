/// `echelon-portfolio` — the only module with effects (file I/O, console, exit codes).
module Echelon.Portfolio.Cli.Program

open System
open System.IO
open Echelon.Portfolio
open Echelon.Portfolio.Domain
open Echelon.Portfolio.Json

let private read (path: string) =
    if File.Exists path then Ok(File.ReadAllText path) else Error $"file not found: {path}"

let private loadPortfolio (root: string) : Result<Portfolio * Diagnostic list, string> =
    let p rel = Path.Combine(root, rel)

    match read (p "portfolio/repositories.json"), read (p "portfolio/initiatives.json"), read (p "registry/systems-v2.json") with
    | Ok r, Ok i, Ok s ->
        match parseObject r, parseObject i, parseObject s with
        | Ok rdoc, Ok idoc, Ok sdoc ->
            let repos = objects "repositories" rdoc |> List.map Schema.parseRepository
            let inits = objects "initiatives" idoc |> List.map Schema.parseInitiative

            let docDiags =
                Schema.unknownKeys Schema.repositoriesDocumentKeys "portfolio/repositories.json" rdoc
                @ Schema.unknownKeys Schema.initiativesDocumentKeys "portfolio/initiatives.json" idoc
                @ (match tryObject "privateRepositories" rdoc with
                   | Some agg -> Schema.unknownKeys Schema.privateAggregateKeys "privateRepositories" agg
                   | None -> [])
                @ (if tryString "schema" rdoc <> Some Schema.RepositoriesSchema then
                       [ diagnostic "PF020" Blocking "portfolio/repositories.json" "wrong or missing schema" ]
                   else
                       [])
                @ (if tryString "schema" idoc <> Some Schema.InitiativesSchema then
                       [ diagnostic "PF020" Blocking "portfolio/initiatives.json" "wrong or missing schema" ]
                   else
                       [])
                @ ([ "portfolio/repositories.json", rdoc; "portfolio/initiatives.json", idoc ]
                   |> List.filter (fun (_, d) -> tryString "classification" d <> Some "public")
                   |> List.map (fun (n, _) -> diagnostic "PF004" Blocking n "document classification must be 'public'"))

            let portfolio =
                { Repositories = repos |> List.map fst
                  Initiatives = inits |> List.map fst
                  Systems = Schema.parseSystems sdoc
                  PrivateAggregateCount =
                    tryObject "privateRepositories" rdoc
                    |> Option.bind (tryInt "count")
                    |> Option.defaultValue 0 }

            Ok(portfolio, docDiags @ (repos |> List.collect snd) @ (inits |> List.collect snd))
        | Error e, _, _
        | _, Error e, _
        | _, _, Error e -> Error e
    | Error e, _, _
    | _, Error e, _
    | _, _, Error e -> Error e

let private print (d: Diagnostic) =
    let sev =
        match d.Severity with
        | Blocking -> "error"
        | Advisory -> "warning"

    printfn "%s %s [%s] %s" sev d.Code d.Subject d.Message

let private metrics (p: Portfolio) =
    let n = List.length p.Repositories
    let provisional = p.Repositories |> List.filter (fun r -> r.Provisional) |> List.length
    let withNon = p.Initiatives |> List.filter (fun i -> not i.Withheld)

    printfn "metrics: repositories=%d provisional=%d verified=%d private-aggregate=%d initiatives=%d withheld=%d"
        n provisional (n - provisional) p.PrivateAggregateCount (List.length p.Initiatives)
        (List.length p.Initiatives - List.length withNon)

let private validate root today =
    match loadPortfolio root with
    | Error e ->
        eprintfn "error: %s" e
        2
    | Ok(portfolio, parseDiags) ->
        let diags = parseDiags @ Rules.all today portfolio
        diags |> List.iter print
        metrics portfolio
        if Rules.hasErrors diags then 1 else 0

let private renderAll root check =
    let p rel = Path.Combine(root, rel)

    let outputs =
        [ "portfolio/repositories.json", "portfolio/generated/repositories.md", Render.repositoriesMarkdown
          "portfolio/initiatives.json", "portfolio/generated/initiatives.md", Render.initiativesMarkdown ]

    let results =
        outputs
        |> List.map (fun (src, dst, render) ->
            read (p src)
            |> Result.bind parseObject
            |> Result.map (fun doc ->
                let expected = render doc
                let current = if File.Exists(p dst) then File.ReadAllText(p dst) else ""

                if check then
                    if current = expected then None else Some $"{dst} is stale; run render"
                else
                    Directory.CreateDirectory(Path.GetDirectoryName(p dst)) |> ignore
                    File.WriteAllText(p dst, expected)
                    None))

    results
    |> List.iter (function
        | Error e -> eprintfn "error: %s" e
        | Ok(Some msg) -> eprintfn "error: %s" msg
        | Ok None -> ())

    if results |> List.forall (fun r -> r = Ok None) then 0 else 1

let private project source sourceRecord output =
    match read source with
    | Error e ->
        eprintfn "error: %s" e
        2
    | Ok text ->
        match Projection.projectRegister sourceRecord text with
        | Ok json ->
            File.WriteAllText(output, json)
            printfn "projected %s -> %s" sourceRecord output
            0
        | Error es ->
            es |> List.iter (eprintfn "error: %s")
            1

let usage () =
    eprintfn "usage: echelon-portfolio validate [--root DIR] [--today YYYY-MM-DD]"
    eprintfn "       echelon-portfolio render [--root DIR] [--check]"
    eprintfn "       echelon-portfolio project-initiatives --source FILE --source-record ID --out FILE"
    2

let rec private option name (args: string list) =
    match args with
    | n :: v :: _ when n = name -> Some v
    | _ :: rest -> option name rest
    | [] -> None

[<EntryPoint>]
let main argv =
    let args = List.ofArray argv
    let root = option "--root" args |> Option.defaultValue "."

    let today =
        option "--today" args
        |> Option.map DateOnly.Parse
        |> Option.defaultValue (DateOnly.FromDateTime DateTime.UtcNow)

    match args with
    | "validate" :: _ -> validate root today
    | "render" :: rest -> renderAll root (List.contains "--check" rest)
    | "project-initiatives" :: _ ->
        match option "--source" args, option "--source-record" args, option "--out" args with
        | Some s, Some r, Some o -> project s r o
        | _ -> usage ()
    | _ -> usage ()
