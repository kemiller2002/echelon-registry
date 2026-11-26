module Echelon.Portfolio.Tests.RuleTests

open System
open Xunit
open Echelon.Portfolio
open Echelon.Portfolio.Domain
open Echelon.Portfolio.Tests.Fixtures

let private run p = Rules.all today p

[<Fact>]
let ``a consistent portfolio has no errors`` () =
    let p =
        portfolio
            [ { repo "praxis" with Systems = [ "praxis" ]; Repository = "example/praxis"; Initiatives = [ "INIT-001" ] } ]
            [ { initiative "INIT-001" with Repositories = [ "praxis" ]; Systems = [ "praxis" ] } ]
            [ { Id = "praxis"; Repository = "example/praxis" } ]

    Assert.False(Rules.hasErrors (run p), sprintf "%A" (run p))

[<Fact>]
let ``private repository without disclosure decision is an error`` () =
    let p = portfolio [ { repo "secret" with Visibility = Private } ] [] []
    Assert.Contains("PF003", codes (run p))

[<Fact>]
let ``private repository with disclosure decision is allowed`` () =
    let p =
        portfolio [ { repo "known" with Visibility = Private; DisclosureDecision = Some "DD-001" } ] [] []

    Assert.DoesNotContain("PF003", codes (run p))

[<Theory>]
[<InlineData("internal")>]
[<InlineData("confidential")>]
[<InlineData("restricted")>]
let ``non-public classification is rejected`` (c: string) =
    let p = portfolio [ { repo "r" with Classification = c } ] [] []
    Assert.Contains("PF004", codes (run p))

[<Fact>]
let ``unknown system is rejected`` () =
    let p = portfolio [ { repo "r" with Systems = [ "ghost" ] } ] [] []
    Assert.Contains("PF005", codes (run p))

[<Fact>]
let ``system hosted by another repository is rejected`` () =
    let p =
        portfolio [ { repo "r" with Systems = [ "s" ] } ] [] [ { Id = "s"; Repository = "example/other" } ]

    Assert.Contains("PF018", codes (run p))

[<Fact>]
let ``dangling initiative and repository references are rejected`` () =
    let p =
        portfolio
            [ { repo "r" with Initiatives = [ "INIT-404" ] } ]
            [ { initiative "INIT-001" with Repositories = [ "nowhere" ]; Related = [ "INIT-405" ] } ]
            []

    let cs = codes (run p)
    Assert.Contains("PF006", cs)
    Assert.Contains("PF007", cs)
    Assert.Contains("PF014", cs)

[<Theory>]
[<InlineData("contact someone@example.org")>]
[<InlineData("see /Users/someone/work/file.md")>]
[<InlineData("-----BEGIN RSA PRIVATE KEY-----")>]
[<InlineData("ghp_abcdefghijklmnopqrstuvwxyz0123")>]
[<InlineData("AKIAABCDEFGHIJKLMNOP")>]
let ``sensitive-looking values are rejected`` (value: string) =
    let p = portfolio [ { repo "r" with Text = [ value ] } ] [] []
    Assert.Contains("PF008", codes (run p))

[<Fact>]
let ``verified maturity without evidence is rejected`` () =
    let p = portfolio [ { repo "r" with Maturity = "verified" } ] [] []
    Assert.Contains("PF009", codes (run p))

[<Fact>]
let ``released maturity without a release is rejected`` () =
    let p = portfolio [ { repo "r" with Maturity = "released" } ] [] []
    Assert.Contains("PF009", codes (run p))

[<Fact>]
let ``withheld initiative needs category and review date`` () =
    let p =
        portfolio [] [ { initiative "INIT-002" with Withheld = true; Status = None } ] []

    let ds = run p |> List.filter (fun d -> d.Code = "PF012")
    Assert.Equal(2, List.length ds)

[<Fact>]
let ``withheld initiative past its review date warns`` () =
    let i =
        { initiative "INIT-002" with
            Withheld = true
            JustificationCategory = Some "contractual"
            ReviewBy = Some(DateOnly(2026, 1, 1)) }

    let ds = run (portfolio [] [ i ] []) |> List.filter (fun d -> d.Code = "PF012")
    Assert.All(ds, fun d -> Assert.Equal(Advisory, d.Severity))
    Assert.NotEmpty ds

[<Fact>]
let ``stale verification is a warning not an error`` () =
    let p = portfolio [ { repo "r" with LastVerified = Some(DateOnly(2025, 1, 1)) } ] [] []
    let ds = run p |> List.filter (fun d -> d.Code = "PF010")
    Assert.Single ds |> ignore
    Assert.False(Rules.hasErrors (run p))

[<Fact>]
let ``registered system whose repository is not inventoried warns`` () =
    let p = portfolio [] [] [ { Id = "s"; Repository = "example/missing" } ]
    Assert.Contains("PF011", codes (run p))

[<Fact>]
let ``duplicates are detected`` () =
    let p = portfolio [ repo "a"; repo "a" ] [ initiative "INIT-001"; initiative "INIT-001" ] []
    let cs = codes (run p)
    Assert.Contains("PF001", cs)
    Assert.Contains("PF015", cs)

[<Fact>]
let ``controlled vocabularies are enforced`` () =
    let p =
        portfolio
            [ { repo "r" with Maturity = "production-grade"; DevelopmentStatus = "thriving" } ]
            [ { initiative "INIT-001" with Status = Some "winning" } ]
            []

    let ds = run p |> List.filter (fun d -> d.Code = "PF016")
    Assert.Equal(3, List.length ds)

[<Fact>]
let ``unknown fields in a public record are rejected`` () =
    match Json.parseObject """{"id":"r","repository":"a/b","displayName":"R","visibility":"public","classification":"public","purpose":"p","lastVerified":"2026-11-25","salary":"x"}""" with
    | Ok o ->
        let _, ds = Schema.parseRepository o
        Assert.Contains("PF002", codes ds)
    | Error e -> failwith e
