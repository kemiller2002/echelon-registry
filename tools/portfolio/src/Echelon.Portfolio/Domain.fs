/// Pure domain model of the public organizational portfolio (GOV-TRN-001).
/// A repository is not a system; an initiative is neither. See spec/repository-manifest.md.
module Echelon.Portfolio.Domain

open System

type Severity =
    | Blocking
    | Advisory

type Diagnostic =
    { Code: string
      Severity: Severity
      Subject: string
      Message: string }

let diagnostic code severity subject message =
    { Code = code
      Severity = severity
      Subject = subject
      Message = message }

type Visibility =
    | Public
    | Private

type RepositoryEntry =
    { Id: string
      Repository: string
      Visibility: Visibility
      Classification: string
      EchelonRelation: string
      Systems: string list
      Initiatives: string list
      DevelopmentStatus: string
      Maturity: string
      LatestRelease: string option
      Evidence: string list
      DisclosureDecision: string option
      LastVerified: DateOnly option
      Provisional: bool
      /// Every string value in the entry, used by supplementary content checks.
      Text: string list }

type InitiativeEntry =
    { Id: string
      Withheld: bool
      Status: string option
      Repositories: string list
      Systems: string list
      Related: string list
      NextActions: string list
      LastReview: DateOnly option
      ReviewBy: DateOnly option
      JustificationCategory: string option
      Text: string list }

type SystemIdentity = { Id: string; Repository: string }

type Portfolio =
    { Repositories: RepositoryEntry list
      Initiatives: InitiativeEntry list
      Systems: SystemIdentity list
      PrivateAggregateCount: int }

module Vocabulary =
    let developmentStatuses =
        set [ "active"; "maintained"; "experimental"; "dormant"; "archived"; "superseded"; "unknown" ]

    let maturities =
        set [ "concept"; "requirements"; "prototype"; "implemented"; "verified"; "released"; "unknown" ]

    let relations =
        set
            [ "core-system"
              "supporting-library"
              "research"
              "product"
              "product-website"
              "governance"
              "test-fixture"
              "experiment"
              "historical" ]

    let initiativeStatuses =
        set [ "proposed"; "active"; "research"; "blocked"; "dormant"; "completed"; "superseded"; "abandoned" ]

    let justificationCategories =
        set
            [ "contractual"
              "personal-data"
              "security-exploitable"
              "credential"
              "legal-privilege"
              "third-party-rights"
              "competitive-specific"
              "incident-evidence"
              "intellectual-property"
              "other" ]
