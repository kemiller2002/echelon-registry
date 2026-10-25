// REG-PAIR-001-R1 (spec/capability-pairing.md): capability pairing constraints.
//
// Usage: dotnet fsi conformance/validate-capability-pairing.fsx
//
// Runs tools/resolve-profile.fsx over the fixtures in
// examples/capability-pairing/ and checks each outcome, then checks the
// capability states the echelon-current channel records. Fixture releases are
// the real Registry records (Praxis 3.16.0 and 3.20.0, Dokimos 0.6.0 and 0.7.0).

open System
open System.Diagnostics
open System.IO
open System.Text.Json

let require condition message =
    if not condition then failwith message

let fixtureDir = "examples/capability-pairing"
let profile = Path.Combine(fixtureDir, "capability-pairing-proof.profile.json")
let pairingId = "verified-completion-attested"

type Outcome =
    | Resolved of JsonElement
    | Refused of string

let resolve (snapshot: string) =
    let output = Path.Combine(Path.GetTempPath(), $"capability-pairing-{Guid.NewGuid():N}.json")
    let info = ProcessStartInfo("dotnet", RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false)

    [ "fsi"; "tools/resolve-profile.fsx"; "--"; "--profile"; profile; "--snapshot"; Path.Combine(fixtureDir, snapshot)
      "--platform"; "linux-x64"; "--output"; output ]
    |> List.iter info.ArgumentList.Add

    use proc = Process.Start info
    let stdout = proc.StandardOutput.ReadToEndAsync()
    let stderr = proc.StandardError.ReadToEndAsync()
    proc.WaitForExit()

    if proc.ExitCode = 0 then
        require (File.Exists output) $"{snapshot}: resolver exited 0 without writing a set"
        let root = JsonDocument.Parse(File.ReadAllText output).RootElement.Clone()
        File.Delete output
        Resolved root
    else
        require (not (File.Exists output)) $"{snapshot}: a refused resolution still wrote a set"
        Refused(stderr.Result + stdout.Result)

let capability (set: JsonElement) =
    set.GetProperty("capabilities").EnumerateArray()
    |> Seq.filter (fun c -> c.GetProperty("id").GetString() = pairingId)
    |> Seq.exactlyOne

let selected (side: string) (c: JsonElement) =
    let value = c.GetProperty(side).GetProperty("selected")
    if value.ValueKind = JsonValueKind.Null then None else Some(value.GetString())

let expectState snapshot state (host: string) (paired: string) =
    match resolve snapshot with
    | Refused message -> failwith $"{snapshot}: expected {state}, resolver refused: {message}"
    | Resolved set ->
        let c = capability set
        require (c.GetProperty("state").GetString() = state) $"{snapshot}: expected state {state}"
        require (selected "host" c = Some host) $"{snapshot}: expected host praxis {host}"
        require (selected "requires" c = Some paired) $"{snapshot}: expected paired dokimos {paired}"
        let hasReason = (let mutable r = Unchecked.defaultof<JsonElement> in c.TryGetProperty("reason", &r))
        require (hasReason = (state = "disabled")) $"{snapshot}: a reason must be present exactly when the capability is disabled"
        printfn "  %-28s %s (praxis %s, dokimos %s)" snapshot state host paired

let expectRefusal snapshot (fragment: string) =
    match resolve snapshot with
    | Resolved _ -> failwith $"{snapshot}: expected the resolver to refuse, but it produced a set"
    | Refused message ->
        require (message.Contains fragment) $"{snapshot}: refusal does not name '{fragment}': {message}"
        printfn "  %-28s refused (%s)" snapshot fragment

printfn "Capability pairing fixtures (REG-PAIR-001-R1)"
// Acceptance: Praxis 3.20.0 + Dokimos 0.6.0 with verified completion enabled fails ...
expectRefusal "violation.catalog.json" "REG-PAIR-001: capability verified-completion-attested"
// ... and passes with the capability recorded as disabled.
expectState "disabled.catalog.json" "disabled" "3.20.0" "0.6.0"
expectState "paired.catalog.json" "enabled" "3.20.0" "0.7.0"
expectState "inactive.catalog.json" "inactive" "3.16.0" "0.6.0"
expectRefusal "undeclared-disabled.catalog.json" "does not declare"

// The channel: every set of a profile that declares pairings carries one state
// per pairing, and an enabled state is consistent with the selections.
let parse (path: string) = JsonDocument.Parse(File.ReadAllText path).RootElement

let currentProfile = parse "profiles/echelon-current.profile.json"
let pairings =
    match currentProfile.TryGetProperty "capabilityPairings" with
    | true, value -> value.EnumerateArray() |> Seq.map (fun p -> p.GetProperty("id").GetString()) |> Seq.toList
    | _ -> []

let channel = parse "channels/echelon-current/channel.json"

for entry in channel.GetProperty("platforms").EnumerateArray() do
    let platform = entry.GetProperty("platform").GetString()
    let set = parse (entry.GetProperty("path").GetString())

    match set.TryGetProperty "capabilities" with
    | false, _ -> require pairings.IsEmpty $"echelon-current {platform} records no capability states, but the profile declares {pairings}"
    | true, states ->
        let ids = states.EnumerateArray() |> Seq.map (fun c -> c.GetProperty("id").GetString()) |> Seq.toList
        require (ids = pairings) $"echelon-current {platform} capability states {ids} differ from the profile's pairings {pairings}"

        for c in states.EnumerateArray() do
            let id = c.GetProperty("id").GetString()
            let state = c.GetProperty("state").GetString()
            require (List.contains state [ "enabled"; "inactive"; "disabled" ]) $"echelon-current {platform}: {id} has unknown state {state}"
            printfn "  echelon-current %-12s %s %s" platform id state

printfn "Capability pairing PASS"
