// EP-A6a-R1/R2: every echelon-current profile version from 1.27.0 on is kept as an
// immutable release-set record (sets/echelon-current/<version>.json,
// schemas/release-set.schema.json), and the current channel's set is approved.
//
// Usage: dotnet fsi conformance/validate-release-sets.fsx
//   RELEASE_SET_BASE_REF  optional git revision; records that exist there must be
//                         unchanged (approved) or change only by being approved.
//
// Fails when:
// - the current profile version has no record, or the record's derived fields
//   differ from what the channel's committed files produce;
// - the current set is not `approved` (a proposed set is never current);
// - an approved record lacks who, when, the owner's statement, the decision it
//   implements, or REG-RU-007 and Conditor qualification evidence;
// - a published record was edited, other than proposed -> approved.
// It first proves each refusal on synthetic records.

#load "../tools/release-set.fsx"

open System
open System.Diagnostics
open System.IO
open System.Text.Json.Nodes
open ReleaseSet

let setsDir = "sets/echelon-current"

let requiredEvidenceKinds = set [ "reg-ru-007"; "conditor-qualification" ]

let text (node: JsonNode) (name: string) =
    match node[name] with
    | null -> None
    | :? JsonValue as v ->
        match v.TryGetValue<string>() with
        | true, s when not (String.IsNullOrWhiteSpace s) -> Some s
        | _ -> None
    | _ -> None

let approvalErrors (label: string) (record: JsonObject) =
    match text record "status" with
    | Some "proposed" -> if isNull record["approval"] then [] else [ $"{label}: a proposed set carries an approval" ]
    | Some "approved" ->
        match record["approval"] with
        | :? JsonObject as approval ->
            let evidence =
                match approval["evidence"] with
                | :? JsonArray as a -> a |> Seq.choose (function :? JsonObject as e -> Some e | _ -> None) |> Seq.toList
                | _ -> []

            let kinds = evidence |> List.choose (fun e -> text e "kind") |> Set.ofList
            let statements =
                match approval["statement"] with
                | :? JsonArray as a -> a |> Seq.filter (fun s -> not (isNull s) && not (String.IsNullOrWhiteSpace(s.ToString()))) |> Seq.length
                | _ -> 0

            [ if (text approval "by").IsNone then $"{label}: approval names no person"
              match text approval "at" with
              | Some at when fst (DateTimeOffset.TryParse at) -> ()
              | _ -> $"{label}: approval has no valid time"
              if statements = 0 then $"{label}: approval quotes no owner statement"
              if (text approval "decision").IsNone then $"{label}: approval cites no decision"
              for kind in Set.difference requiredEvidenceKinds kinds do
                  $"{label}: approval has no {kind} evidence"
              for e in evidence do
                  match text e "url" with
                  | Some url when url.StartsWith("https://", StringComparison.Ordinal) -> ()
                  | _ -> $"{label}: an evidence entry has no https url" ]
        | _ -> [ $"{label}: an approved set carries no approval" ]
    | other -> [ $"{label}: status must be proposed or approved, not {other}" ]

/// A published record never changes, except once from proposed to approved.
let immutabilityErrors (label: string) (baseText: string) (headText: string) =
    let baseNode = JsonNode.Parse(baseText).AsObject()

    match text baseNode "status" with
    | Some "approved" when baseText <> headText -> [ $"{label}: an approved release set was edited; a change is a new profile version" ]
    | Some "approved" -> []
    | _ when withoutDecision baseText <> withoutDecision headText -> [ $"{label}: a proposed release set's derived fields changed" ]
    | _ -> []

// Self-tests on synthetic records.
let synthetic status (approval: string option) =
    let a = approval |> Option.map (fun body -> $", \"approval\": {body}") |> Option.defaultValue ""
    JsonNode.Parse($"""{{ "schema": "echelon.release-set/v1", "components": [], "status": "{status}"{a} }}""").AsObject()

let goodApproval =
    """{ "by": "owner", "at": "2026-10-24T21:09:00-04:00", "statement": ["Release it"], "decision": "OD-X",
         "evidence": [ { "kind": "reg-ru-007", "url": "https://example.invalid/1" },
                       { "kind": "conditor-qualification", "url": "https://example.invalid/2" } ] }"""

let selfTests =
    [ "a complete approval passes", (approvalErrors "t" (synthetic "approved" (Some goodApproval))).IsEmpty
      "an approval without evidence fails",
      not (approvalErrors "t" (synthetic "approved" (Some """{ "by": "o", "at": "2026-10-24T00:00:00Z", "statement": ["s"], "decision": "d", "evidence": [] }"""))).IsEmpty
      "an approved set without approval fails", not (approvalErrors "t" (synthetic "approved" None)).IsEmpty
      "an edited approved set fails",
      not (immutabilityErrors "t" (render (synthetic "approved" (Some goodApproval))) (render (synthetic "approved" (Some (goodApproval.Replace("OD-X", "OD-Y")))))).IsEmpty
      "approving a proposed set passes",
      (immutabilityErrors "t" (render (synthetic "proposed" None)) (render (synthetic "approved" (Some goodApproval)))).IsEmpty ]

for name, passed in selfTests do
    if not passed then
        eprintfn "Self-test failed: %s" name
        exit 1

let gitShow (revision: string) (path: string) =
    let info = ProcessStartInfo("git", RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false)
    [ "show"; $"{revision}:{path}" ] |> List.iter info.ArgumentList.Add
    use proc = Process.Start info
    let output = proc.StandardOutput.ReadToEnd()
    proc.WaitForExit()
    if proc.ExitCode = 0 then Some output else None

let current = derive "channels/echelon-current" "profiles/echelon-current.profile.json" "snapshots/echelon-current.catalog.json"
let currentPath = Path.Combine(setsDir, $"{current.ProfileVersion}.json").Replace('\\', '/')

let records =
    if Directory.Exists setsDir then
        Directory.GetFiles(setsDir, "*.json") |> Array.sort |> Array.map (fun p -> p.Replace('\\', '/')) |> Array.toList
    else
        []

let errors =
    [ if not (File.Exists currentPath) then
          yield $"echelon-current {current.ProfileVersion} has no release-set record {currentPath} (generate it with tools/generate-release-set.fsx)"
      else
          let recordText = File.ReadAllText currentPath

          if withoutDecision recordText <> render (derivedNode current) then
              yield $"{currentPath} differs from the set the committed channel files produce"

          let record = JsonNode.Parse(recordText).AsObject()

          if text record "status" <> Some "approved" then
              yield $"{currentPath}: echelon-current {current.ProfileVersion} is selected as current but is not approved (EP-A6a-R2)"

      for path in records do
          let record = JsonNode.Parse(File.ReadAllText path).AsObject()
          yield! approvalErrors path record

          match Environment.GetEnvironmentVariable "RELEASE_SET_BASE_REF" |> Option.ofObj |> Option.filter (String.IsNullOrWhiteSpace >> not) with
          | None -> ()
          | Some baseRef ->
              match gitShow baseRef path with
              | None -> ()
              | Some baseText -> yield! immutabilityErrors path baseText (File.ReadAllText path) ]

match errors with
| [] ->
    printfn "Release sets (EP-A6a-R1/R2) PASS"
    printfn "  current: %s@%s approved, %d record(s), %d self-tests" current.ProfileId current.ProfileVersion records.Length selfTests.Length
| _ ->
    errors |> List.iter (eprintfn "EP-A6a: %s")
    exit 1
