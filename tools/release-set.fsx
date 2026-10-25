// Shared by tools/generate-release-set.fsx and conformance/validate-release-sets.fsx
// (EP-A6a-R1/R2, schemas/release-set.schema.json).
module ReleaseSet

open System
open System.IO
open System.Security.Cryptography
open System.Text.Json
open System.Text.Json.Nodes

let sha256 (path: string) =
    File.ReadAllBytes path |> SHA256.HashData |> Convert.ToHexString |> fun value -> value.ToLowerInvariant()

let private parse (path: string) =
    JsonDocument.Parse(File.ReadAllText path).RootElement.Clone()

let private str (name: string) (element: JsonElement) = element.GetProperty(name).GetString()
let private items (name: string) (element: JsonElement) = element.GetProperty(name).EnumerateArray() |> Seq.toList

type Selection =
    { SystemId: string
      Version: string
      ReleaseManifestSha256: string
      Platforms: string list }

type CapabilityState =
    { Id: string
      Platform: string
      State: string }

type Derived =
    { Channel: string
      ProfileId: string
      ProfileVersion: string
      ProfilePath: string
      ProfileSha256: string
      SnapshotPath: string
      SnapshotSha256: string
      ChannelIndexPath: string
      ChannelIndexSha256: string
      Platforms: (string * string * string) list
      Selections: Selection list
      Capabilities: CapabilityState list }

/// Everything a release-set record states that is computable from the
/// channel's committed files.
let derive (channelDir: string) (profilePath: string) (snapshotPath: string) =
    let indexPath = Path.Combine(channelDir, "channel.json").Replace('\\', '/')
    let index = parse indexPath
    let profile = parse profilePath
    let profileSha = sha256 profilePath
    let snapshotSha = sha256 snapshotPath

    if str "sha256" (index.GetProperty "profile") <> profileSha then
        failwith $"{indexPath} was not generated from {profilePath}"

    if str "sha256" (index.GetProperty "catalogSnapshot") <> snapshotSha then
        failwith $"{indexPath} was not generated from {snapshotPath}"

    let platforms =
        items "platforms" index
        |> List.map (fun entry ->
            let path = str "path" entry
            let actual = sha256 path

            if actual <> str "sha256" entry then
                failwith $"{path} does not match its digest in {indexPath}"

            str "platform" entry, path, actual)

    let sets = platforms |> List.map (fun (platform, path, _) -> platform, parse path)

    let selections =
        sets
        |> List.collect (fun (platform, set) ->
            items "components" set
            |> List.map (fun c ->
                (str "systemId" c, str "version" c, str "sha256" (c.GetProperty "releaseManifest")), platform))
        |> List.groupBy fst
        |> List.map (fun ((systemId, version, manifest), rows) ->
            { SystemId = systemId
              Version = version
              ReleaseManifestSha256 = manifest
              Platforms = rows |> List.map snd })

    let duplicated =
        selections |> List.countBy _.SystemId |> List.filter (fun (_, n) -> n > 1) |> List.map fst

    if not duplicated.IsEmpty then
        failwith $"the channel selects different releases of {duplicated} on different platforms"

    let capabilities =
        sets
        |> List.collect (fun (platform, set) ->
            match set.TryGetProperty "capabilities" with
            | true, value ->
                value.EnumerateArray()
                |> Seq.map (fun c -> { Id = str "id" c; Platform = platform; State = str "state" c })
                |> Seq.toList
            | _ -> [])

    { Channel = str "id" index
      ProfileId = str "id" profile
      ProfileVersion = str "version" profile
      ProfilePath = profilePath.Replace('\\', '/')
      ProfileSha256 = profileSha
      SnapshotPath = snapshotPath.Replace('\\', '/')
      SnapshotSha256 = snapshotSha
      ChannelIndexPath = indexPath
      ChannelIndexSha256 = sha256 indexPath
      Platforms = platforms
      Selections = selections
      Capabilities = capabilities }

let private value (text: string) = JsonValue.Create text :> JsonNode

let private obj (pairs: (string * JsonNode) list) =
    let node = JsonObject()
    pairs |> List.iter (fun (k, v) -> node[k] <- v)
    node :> JsonNode

let private arr (nodes: JsonNode list) =
    let node = JsonArray()
    nodes |> List.iter node.Add
    node :> JsonNode

/// The derived part of a record, as the JSON object written to disk.
let derivedNode (d: Derived) =
    let node =
        obj
            [ "schema", value "echelon.release-set/v1"
              "channel", value d.Channel
              "profile", obj [ "id", value d.ProfileId; "version", value d.ProfileVersion; "path", value d.ProfilePath; "sha256", value d.ProfileSha256 ]
              "catalogSnapshot", obj [ "path", value d.SnapshotPath; "sha256", value d.SnapshotSha256 ]
              "channelIndex", obj [ "path", value d.ChannelIndexPath; "sha256", value d.ChannelIndexSha256 ]
              "platforms",
              arr [ for platform, path, digest in d.Platforms -> obj [ "platform", value platform; "path", value path; "sha256", value digest ] ]
              "components",
              arr
                  [ for s in d.Selections ->
                        obj
                            [ "systemId", value s.SystemId
                              "version", value s.Version
                              "releaseManifestSha256", value s.ReleaseManifestSha256
                              "platforms", arr (s.Platforms |> List.map value) ] ]
              "capabilities",
              arr [ for c in d.Capabilities -> obj [ "id", value c.Id; "platform", value c.Platform; "state", value c.State ] ] ]

    node.AsObject()

let proposed (d: Derived) =
    let node = derivedNode d
    node["status"] <- value "proposed"
    node

let render (node: JsonObject) =
    node.ToJsonString(JsonSerializerOptions(WriteIndented = true)) + "\n"

/// The fields a person adds to a record; everything else is derived.
let decisionFields = set [ "status"; "approval"; "assessments" ]

/// The record without its decision fields, for comparison with `derivedNode`.
let withoutDecision (text: string) =
    let node = JsonNode.Parse(text).AsObject()
    for field in decisionFields do
        node.Remove field |> ignore
    render node
