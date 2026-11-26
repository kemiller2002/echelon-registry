/// Acceptance test: the committed portfolio records in this repository validate and their
/// generated Markdown views are current.
module Echelon.Portfolio.Tests.RepositoryDataTests

open System
open System.IO
open Xunit
open Echelon.Portfolio
open Echelon.Portfolio.Json

let rec private findRoot (dir: DirectoryInfo) =
    if isNull dir then None
    elif File.Exists(Path.Combine(dir.FullName, "registry", "systems-v2.json")) then Some dir.FullName
    else findRoot dir.Parent

let private root () =
    findRoot (DirectoryInfo AppContext.BaseDirectory)
    |> Option.defaultWith (fun () -> failwith "registry root not found")

let private load rel =
    match parseObject (File.ReadAllText(Path.Combine(root (), rel))) with
    | Ok o -> o
    | Error e -> failwith e

let private portfolio () =
    let repos = objects "repositories" (load "portfolio/repositories.json") |> List.map Schema.parseRepository
    let inits = objects "initiatives" (load "portfolio/initiatives.json") |> List.map Schema.parseInitiative

    let p: Domain.Portfolio =
        { Repositories = repos |> List.map fst
          Initiatives = inits |> List.map fst
          Systems = Schema.parseSystems (load "registry/systems-v2.json")
          PrivateAggregateCount = 0 }

    p, (repos |> List.collect snd) @ (inits |> List.collect snd)

[<Fact>]
let ``committed portfolio records have no errors`` () =
    let p, parse = portfolio ()
    let errors = parse @ Rules.all (DateOnly(2026, 11, 25)) p |> List.filter (fun d -> d.Severity = Domain.Blocking)
    Assert.True(List.isEmpty errors, sprintf "%A" errors)

[<Fact>]
let ``every registered system's repository is inventoried`` () =
    let p, _ = portfolio ()
    Assert.Empty(Rules.systemsInventoried p)

[<Fact>]
let ``generated markdown views are current`` () =
    let check src dst render =
        let expected = render (load src)
        let actual = File.ReadAllText(Path.Combine(root (), dst))
        Assert.True((expected = actual), $"{dst} is stale; run the render command")

    check "portfolio/repositories.json" "portfolio/generated/repositories.md" Render.repositoriesMarkdown
    check "portfolio/initiatives.json" "portfolio/generated/initiatives.md" Render.initiativesMarkdown
