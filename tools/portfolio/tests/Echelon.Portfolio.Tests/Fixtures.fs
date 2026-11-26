module Echelon.Portfolio.Tests.Fixtures

open System
open Echelon.Portfolio.Domain

let today = DateOnly(2026, 11, 25)

let repo id =
    { Id = id
      Repository = $"example/{id}"
      Visibility = Public
      Classification = "public"
      EchelonRelation = "core-system"
      Systems = []
      Initiatives = []
      DevelopmentStatus = "active"
      Maturity = "implemented"
      LatestRelease = None
      Evidence = []
      DisclosureDecision = None
      LastVerified = Some today
      Provisional = false
      Text = [ id ] }

let initiative id =
    { Id = id
      Withheld = false
      Status = Some "active"
      Repositories = []
      Systems = []
      Related = []
      NextActions = [ "do the next thing" ]
      LastReview = Some today
      ReviewBy = None
      JustificationCategory = None
      Text = [ id ] }

let portfolio repos inits systems =
    { Repositories = repos
      Initiatives = inits
      Systems = systems
      PrivateAggregateCount = 0 }

let codes (ds: Diagnostic list) = ds |> List.map (fun d -> d.Code) |> Set.ofList

let register =
    """{
  "schema": "echelon.initiative-register/v1",
  "classification": "internal",
  "initiatives": [
    { "id": "INIT-001", "name": "Secure construction", "status": "active",
      "nextActions": ["a"], "lastReview": "2026-11-25",
      "publication": { "state": "public" },
      "internal": { "resourceEstimate": "confidential estimate", "notes": "internal only" },
      "budget": "should never appear" },
    { "id": "INIT-099", "name": "Secret partnership", "status": "proposed",
      "publication": { "state": "withheld", "category": "contractual", "reviewBy": "2027-05-25" },
      "internal": { "counterparty": "Named Counterparty" } }
  ]
}"""
