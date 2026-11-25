module Echelon.Portfolio.Tests.ProjectionTests

open Xunit
open Echelon.Portfolio
open Echelon.Portfolio.Json
open Echelon.Portfolio.Tests.Fixtures

let private projected () =
    match Projection.projectRegister "org-admin:portfolio/initiatives.json" register with
    | Ok text -> text
    | Error es -> failwithf "%A" es

[<Fact>]
let ``internal fields never reach the projection`` () =
    let text = projected ()
    Assert.DoesNotContain("internal only", text)
    Assert.DoesNotContain("confidential estimate", text)
    Assert.DoesNotContain("should never appear", text)
    Assert.DoesNotContain("Named Counterparty", text)
    Assert.DoesNotContain("Secret partnership", text)
    Assert.DoesNotContain("publication", text)

[<Fact>]
let ``withheld initiatives are visible only as justified stubs`` () =
    match parseObject (projected ()) with
    | Ok doc ->
        let withheld = objects "initiatives" doc |> List.find (fun i -> tryString "id" i = Some "INIT-099")
        Assert.Equal(Some true, tryBool "withheld" withheld)
        Assert.Equal(Some "contractual", tryString "justificationCategory" withheld)
        Assert.Equal(4, withheld.Count)
    | Error e -> failwith e

[<Fact>]
let ``projection cites its source and digest`` () =
    match parseObject (projected ()) with
    | Ok doc ->
        Assert.Equal(Some "echelon.portfolio-initiatives/v1", tryString "schema" doc)
        Assert.Equal(Some "public", tryString "classification" doc)
        Assert.StartsWith("sha256:", tryString "sourceDigest" doc |> Option.defaultValue "")
    | Error e -> failwith e

[<Fact>]
let ``projected initiatives pass the closed public schema`` () =
    match parseObject (projected ()) with
    | Ok doc ->
        let diags = objects "initiatives" doc |> List.collect (Schema.parseInitiative >> snd)
        Assert.Empty diags
    | Error e -> failwith e

[<Fact>]
let ``missing publication decision fails the projection`` () =
    let bad =
        """{"schema":"echelon.initiative-register/v1","initiatives":[{"id":"INIT-001","name":"x"}]}"""

    match Projection.projectRegister "src" bad with
    | Ok _ -> failwith "expected failure"
    | Error es -> Assert.NotEmpty es

[<Fact>]
let ``wrong source schema fails`` () =
    match Projection.projectRegister "src" """{"schema":"something-else","initiatives":[]}""" with
    | Ok _ -> failwith "expected failure"
    | Error es -> Assert.NotEmpty es
