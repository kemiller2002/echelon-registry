/// Pure validation rules for the public portfolio. Each rule maps a portfolio to diagnostics.
module Echelon.Portfolio.Rules

open System
open System.Text.RegularExpressions
open Echelon.Portfolio.Domain

let private duplicates (ids: string list) =
    ids |> List.countBy id |> List.filter (fun (_, n) -> n > 1) |> List.map fst

/// PF001 duplicate repository ids.
let duplicateRepositories (p: Portfolio) =
    p.Repositories
    |> List.map (fun r -> r.Id)
    |> duplicates
    |> List.map (fun d -> diagnostic "PF001" Blocking d "duplicate repository id")

/// PF015 duplicate initiative ids.
let duplicateInitiatives (p: Portfolio) =
    p.Initiatives
    |> List.map (fun i -> i.Id)
    |> duplicates
    |> List.map (fun d -> diagnostic "PF015" Blocking d "duplicate initiative id")

/// PF003 a private repository may be named publicly only with a recorded disclosure decision.
let privateRequiresDisclosure (p: Portfolio) =
    p.Repositories
    |> List.filter (fun r -> r.Visibility = Private && r.DisclosureDecision.IsNone)
    |> List.map (fun r ->
        diagnostic "PF003" Blocking r.Id "private repository named in a public record without a disclosureDecision")

/// PF004 only Public information may appear in a public portfolio record.
let publicClassificationOnly (p: Portfolio) =
    p.Repositories
    |> List.filter (fun r -> r.Classification <> "public")
    |> List.map (fun r ->
        diagnostic "PF004" Blocking r.Id $"classification must be 'public' in a public record (was '{r.Classification}')")

/// PF005 referenced systems exist in registry/systems-v2.json.
let systemsExist (p: Portfolio) =
    let known = p.Systems |> List.map (fun s -> s.Id) |> Set.ofList

    p.Repositories
    |> List.collect (fun r ->
        r.Systems
        |> List.filter (fun s -> not (known.Contains s))
        |> List.map (fun s -> diagnostic "PF005" Blocking r.Id $"system '{s}' is not in registry/systems-v2.json"))

/// PF018 the repository that hosts a referenced system must match systems-v2.json.
let systemRepositoryMatches (p: Portfolio) =
    let hostOf =
        p.Systems |> List.map (fun s -> s.Id, s.Repository.ToLowerInvariant()) |> Map.ofList

    p.Repositories
    |> List.collect (fun r ->
        r.Systems
        |> List.choose (fun s ->
            match hostOf.TryFind s with
            | Some repo when repo <> r.Repository.ToLowerInvariant() ->
                Some(
                    diagnostic
                        "PF018"
                        Blocking
                        r.Id
                        $"system '{s}' is hosted by '{repo}' in systems-v2.json, not '{r.Repository}'"
                )
            | _ -> None))

/// PF006 repository initiative references resolve.
let repositoryInitiativesExist (p: Portfolio) =
    let known = p.Initiatives |> List.map (fun i -> i.Id) |> Set.ofList

    p.Repositories
    |> List.collect (fun r ->
        r.Initiatives
        |> List.filter (fun i -> not (known.Contains i))
        |> List.map (fun i -> diagnostic "PF006" Blocking r.Id $"initiative '{i}' is not in portfolio/initiatives.json"))

/// PF007 initiative repository references resolve to inventoried repositories.
let initiativeRepositoriesExist (p: Portfolio) =
    let known = p.Repositories |> List.map (fun r -> r.Id) |> Set.ofList

    p.Initiatives
    |> List.collect (fun i ->
        i.Repositories
        |> List.filter (fun r -> not (known.Contains r))
        |> List.map (fun r -> diagnostic "PF007" Blocking i.Id $"repository '{r}' is not in portfolio/repositories.json"))

/// PF019 initiative system references resolve.
let initiativeSystemsExist (p: Portfolio) =
    let known = p.Systems |> List.map (fun s -> s.Id) |> Set.ofList

    p.Initiatives
    |> List.collect (fun i ->
        i.Systems
        |> List.filter (fun s -> not (known.Contains s))
        |> List.map (fun s -> diagnostic "PF019" Blocking i.Id $"system '{s}' is not in registry/systems-v2.json"))

/// PF014 related initiatives resolve.
let relatedInitiativesExist (p: Portfolio) =
    let known = p.Initiatives |> List.map (fun i -> i.Id) |> Set.ofList

    p.Initiatives
    |> List.collect (fun i ->
        i.Related
        |> List.filter (fun r -> not (known.Contains r))
        |> List.map (fun r -> diagnostic "PF014" Blocking i.Id $"related initiative '{r}' does not exist"))

/// Supplementary content patterns (PUB-SG-04/07). A pass proves nothing; a hit blocks publication.
let private sensitivePatterns =
    [ "private key block", Regex(@"-----BEGIN [A-Z ]*PRIVATE KEY-----", RegexOptions.Compiled)
      "GitHub token", Regex(@"\bgh[pousr]_[A-Za-z0-9]{20,}\b", RegexOptions.Compiled)
      "AWS access key id", Regex(@"\bAKIA[0-9A-Z]{16}\b", RegexOptions.Compiled)
      "Slack token", Regex(@"\bxox[abprs]-[A-Za-z0-9-]{10,}\b", RegexOptions.Compiled)
      "personal home directory path", Regex(@"(?:/Users/|/home/)[A-Za-z][A-Za-z0-9._-]+/", RegexOptions.Compiled)
      "e-mail address", Regex(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}\b", RegexOptions.Compiled) ]

let sensitiveMatches (text: string) =
    sensitivePatterns
    |> List.filter (fun (_, rx) -> rx.IsMatch text)
    |> List.map fst

/// PF008 supplementary sensitive-content detection over every string value.
let sensitiveContent (p: Portfolio) =
    let check subject texts =
        texts
        |> List.collect sensitiveMatches
        |> List.distinct
        |> List.map (fun kind -> diagnostic "PF008" Blocking subject $"value resembles a {kind}; public records must not contain it")

    (p.Repositories |> List.collect (fun r -> check r.Id r.Text))
    @ (p.Initiatives |> List.collect (fun i -> check i.Id i.Text))

/// PF009 maturity claims follow claim discipline (POL-ORG-TRN-001 §6, REG-REPO-006).
let maturityClaimsHaveEvidence (p: Portfolio) =
    p.Repositories
    |> List.collect (fun r ->
        match r.Maturity with
        | "verified" when List.isEmpty r.Evidence ->
            [ diagnostic "PF009" Blocking r.Id "maturity 'verified' requires at least one evidence reference" ]
        | "released" when r.LatestRelease.IsNone ->
            [ diagnostic "PF009" Blocking r.Id "maturity 'released' requires latestRelease" ]
        | _ -> [])

/// PF016 controlled vocabularies.
let vocabularies (p: Portfolio) =
    let bad subject field value (allowed: Set<string>) =
        if allowed.Contains value then
            []
        else
            [ diagnostic "PF016" Blocking subject $"{field} '{value}' is not in the controlled vocabulary" ]

    (p.Repositories
     |> List.collect (fun r ->
         bad r.Id "developmentStatus" r.DevelopmentStatus Vocabulary.developmentStatuses
         @ bad r.Id "maturity" r.Maturity Vocabulary.maturities
         @ bad r.Id "echelonRelation" r.EchelonRelation Vocabulary.relations))
    @ (p.Initiatives
       |> List.filter (fun i -> not i.Withheld)
       |> List.collect (fun i ->
           match i.Status with
           | Some s -> bad i.Id "status" s Vocabulary.initiativeStatuses
           | None -> [ diagnostic "PF016" Blocking i.Id "status is required" ]))

/// PF012 withheld initiatives carry a legitimate justification and a review date.
let withheldJustified (today: DateOnly) (p: Portfolio) =
    p.Initiatives
    |> List.filter (fun i -> i.Withheld)
    |> List.collect (fun i ->
        let category =
            match i.JustificationCategory with
            | Some c when Vocabulary.justificationCategories.Contains c -> []
            | _ -> [ diagnostic "PF012" Blocking i.Id "withheld initiative needs a valid justificationCategory" ]

        let review =
            match i.ReviewBy with
            | None -> [ diagnostic "PF012" Blocking i.Id "withheld initiative needs reviewBy" ]
            | Some d when d < today -> [ diagnostic "PF012" Advisory i.Id $"restriction review date {d:``yyyy-MM-dd``} has passed" ]
            | Some _ -> []

        category @ review)

/// PF013 initiatives in motion state their next actions.
let activeInitiativesHaveNextActions (p: Portfolio) =
    p.Initiatives
    |> List.filter (fun i -> (not i.Withheld) && List.contains i.Status [ Some "active"; Some "research"; Some "proposed"; Some "blocked" ])
    |> List.filter (fun i -> List.isEmpty i.NextActions)
    |> List.map (fun i -> diagnostic "PF013" Advisory i.Id "initiative in motion has no nextActions")

/// PF010 stale verification (metric M-08 stale registry entries).
let staleEntries (today: DateOnly) (maxAgeDays: int) (p: Portfolio) =
    let stale subject (date: DateOnly option) =
        match date with
        | Some d when today.DayNumber - d.DayNumber > maxAgeDays ->
            [ diagnostic "PF010" Advisory subject $"last verified {d:``yyyy-MM-dd``}, older than {maxAgeDays} days" ]
        | _ -> []

    (p.Repositories |> List.collect (fun r -> stale r.Id r.LastVerified))
    @ (p.Initiatives |> List.filter (fun i -> not i.Withheld) |> List.collect (fun i -> stale i.Id i.LastReview))

/// PF011 every registered system's repository is inventoried (completeness).
let systemsInventoried (p: Portfolio) =
    let inventoried =
        p.Repositories |> List.map (fun r -> r.Repository.ToLowerInvariant()) |> Set.ofList

    p.Systems
    |> List.filter (fun s -> not (inventoried.Contains(s.Repository.ToLowerInvariant())))
    |> List.map (fun s ->
        diagnostic "PF011" Advisory s.Id $"system repository '{s.Repository}' is not in the public repository inventory")

let all (today: DateOnly) (p: Portfolio) : Diagnostic list =
    [ duplicateRepositories
      duplicateInitiatives
      privateRequiresDisclosure
      publicClassificationOnly
      systemsExist
      systemRepositoryMatches
      repositoryInitiativesExist
      initiativeRepositoriesExist
      initiativeSystemsExist
      relatedInitiativesExist
      sensitiveContent
      maturityClaimsHaveEvidence
      vocabularies
      withheldJustified today
      activeInitiativesHaveNextActions
      staleEntries today 180
      systemsInventoried ]
    |> List.collect (fun rule -> rule p)

let hasErrors (ds: Diagnostic list) = ds |> List.exists (fun d -> d.Severity = Blocking)
