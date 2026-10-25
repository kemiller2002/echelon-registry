// EP-A6a-R1: the immutable release-set record of one echelon-current profile version.
//
// Usage:
//   dotnet fsi tools/generate-release-set.fsx -- \
//     --channel channels/echelon-current \
//     --profile profiles/echelon-current.profile.json \
//     --snapshot snapshots/echelon-current.catalog.json \
//     --output sets/echelon-current/<version>.json
//
// The record is derived from the channel's committed files only: profile,
// snapshot and resolved-set digests, the selected releases (union over
// platforms) and the capability states. It is written as `proposed`. Approval
// (`status: approved`, `approval`) is added by the person with release
// authority, or recorded on their documented instruction, after REG-RU-007
// qualification; conformance/validate-release-sets.fsx recomputes everything
// except `status`, `approval` and `assessments` and refuses a difference.

#load "release-set.fsx"

open System
open System.IO
open ReleaseSet

let args =
    fsi.CommandLineArgs
    |> Array.skip 1
    |> Array.filter ((<>) "--")
    |> Array.chunkBySize 2
    |> Array.map (function
        | [| flag; value |] when flag.StartsWith "--" -> flag, value
        | other -> failwith $"invalid arguments: {String.Join(' ', other)}")
    |> Map.ofArray

let arg name =
    args |> Map.tryFind name |> Option.defaultWith (fun () -> failwith $"missing required argument {name}")

let derived = derive (arg "--channel") (arg "--profile") (arg "--snapshot")
let output = arg "--output"

if File.Exists output then
    failwith $"{output} exists; a release-set record is immutable. A change is a new profile version."

Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath output)) |> ignore
File.WriteAllText(output, render (proposed derived))
printfn "Wrote %s (%s@%s, proposed)" output derived.ProfileId derived.ProfileVersion
