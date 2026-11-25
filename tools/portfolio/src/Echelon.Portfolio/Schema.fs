/// Closed public schemas. A key that is not allow-listed here fails validation, so a field added
/// upstream cannot reach a public record without a deliberate change (STD-ORG-PUB-001 PUB-SG-10/12).
module Echelon.Portfolio.Schema

open System.Text.Json.Nodes
open Echelon.Portfolio.Domain
open Echelon.Portfolio.Json

[<Literal>]
let RepositoriesSchema = "echelon.portfolio-repositories/v1"

[<Literal>]
let InitiativesSchema = "echelon.portfolio-initiatives/v1"

[<Literal>]
let RegisterSchema = "echelon.initiative-register/v1"

let repositoriesDocumentKeys =
    set [ "schema"; "classification"; "generatedOn"; "notes"; "privateRepositories"; "repositories" ]

let privateAggregateKeys = set [ "count"; "basis" ]

let repositoryKeys =
    set
        [ "id"
          "repository"
          "displayName"
          "visibility"
          "classification"
          "echelonRelation"
          "purpose"
          "businessJustification"
          "responsibilities"
          "nonResponsibilities"
          "intendedConsumers"
          "providedCapabilities"
          "plannedCapabilities"
          "systems"
          "initiatives"
          "dependencies"
          "dependents"
          "developmentStatus"
          "maturity"
          "latestRelease"
          "lastCommit"
          "authoritativeDocuments"
          "outstandingDecisions"
          "knownLimitations"
          "plannedDirection"
          "lineage"
          "evidence"
          "disclosureDecision"
          "provisional"
          "source"
          "lastVerified" ]

/// Public initiative fields. Anything else in the internal register is excluded by construction.
let publicInitiativeKeys =
    [ "id"
      "name"
      "description"
      "origin"
      "rationale"
      "problem"
      "intendedOutcomes"
      "successCriteria"
      "repositories"
      "systems"
      "dependencies"
      "relatedInitiatives"
      "status"
      "evidence"
      "outstandingDecisions"
      "risks"
      "nextActions"
      "completionCriteria"
      "lastReview"
      "provisional" ]

let withheldInitiativeKeys = set [ "id"; "withheld"; "justificationCategory"; "reviewBy" ]

let initiativesDocumentKeys =
    set [ "schema"; "classification"; "sourceRecord"; "sourceDigest"; "initiatives" ]

let unknownKeys (allowed: Set<string>) (subject: string) (o: JsonObject) =
    o
    |> Seq.map (fun kv -> kv.Key)
    |> Seq.filter (fun k -> not (allowed.Contains k))
    |> Seq.sort
    |> Seq.map (fun k ->
        diagnostic "PF002" Blocking subject $"field '{k}' is not allow-listed in the public schema")
    |> List.ofSeq

let private visibilityOf (o: JsonObject) =
    match tryString "visibility" o with
    | Some "public" -> Ok Public
    | Some "private" -> Ok Private
    | other -> Error other

let parseRepository (o: JsonObject) : RepositoryEntry * Diagnostic list =
    let id = tryString "id" o |> Option.defaultValue "<missing id>"

    let visibility, visibilityDiag =
        match visibilityOf o with
        | Ok v -> v, []
        | Error v -> Private, [ diagnostic "PF016" Blocking id $"visibility must be 'public' or 'private' (was %A{v})" ]

    let entry =
        { Id = id
          Repository = tryString "repository" o |> Option.defaultValue ""
          Visibility = visibility
          Classification = tryString "classification" o |> Option.defaultValue ""
          EchelonRelation = tryString "echelonRelation" o |> Option.defaultValue ""
          Systems = strings "systems" o
          Initiatives = strings "initiatives" o
          DevelopmentStatus = tryString "developmentStatus" o |> Option.defaultValue ""
          Maturity = tryString "maturity" o |> Option.defaultValue ""
          LatestRelease = tryString "latestRelease" o
          Evidence = strings "evidence" o
          DisclosureDecision = tryString "disclosureDecision" o
          LastVerified = tryDate "lastVerified" o
          Provisional = tryBool "provisional" o |> Option.defaultValue true
          Text = stringLeaves o }

    let missing =
        [ "id"; "repository"; "displayName"; "visibility"; "classification"; "purpose"; "lastVerified" ]
        |> List.filter (fun k -> (tryProp k o).IsNone)
        |> List.map (fun k -> diagnostic "PF017" Blocking id $"required field '{k}' is missing")

    entry, unknownKeys repositoryKeys id o @ visibilityDiag @ missing

let parseInitiative (o: JsonObject) : InitiativeEntry * Diagnostic list =
    let id = tryString "id" o |> Option.defaultValue "<missing id>"
    let withheld = tryBool "withheld" o |> Option.defaultValue false
    let allowed = if withheld then withheldInitiativeKeys else set publicInitiativeKeys

    let entry =
        { Id = id
          Withheld = withheld
          Status = tryString "status" o
          Repositories = strings "repositories" o
          Systems = strings "systems" o
          Related = strings "relatedInitiatives" o
          NextActions = strings "nextActions" o
          LastReview = tryDate "lastReview" o
          ReviewBy = tryDate "reviewBy" o
          JustificationCategory = tryString "justificationCategory" o
          Text = stringLeaves o }

    entry, unknownKeys allowed id o

let parseSystems (o: JsonObject) : SystemIdentity list =
    objects "systems" o
    |> List.choose (fun s ->
        match tryString "id" s, tryString "repository" s with
        | Some id, Some repo -> Some { Id = id; Repository = repo }
        | _ -> None)
