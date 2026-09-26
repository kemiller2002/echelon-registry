// Reference model of capability resolution with the provenance rules of
// spec/echelon-integration-standard.md §5.8. Pure functions; no I/O.
//
// resolve(request, provider) -> { state, diagnostics }
//   request:  { capability, contractVersion, requiresProvenance }
//   provider: null (nothing installed/discoverable), or
//             { accessible: boolean, manifest: <system manifest v1 or v2, or registry entry> }

const V1_ENVELOPE = "echelon.execution-envelope/v1";
const V2_ENVELOPE = "echelon.execution-envelope/v2";
const SUPPORTED_PROVENANCE_MAJOR = "1";

// A v1 manifest (or a registry entry without `envelopes`) accepts envelope v1
// only and declares no provenance capability.
export const envelopesAccepted = (manifest) => manifest.envelopes?.accepts ?? [V1_ENVELOPE];

export const provenanceProblems = (manifest) => {
  const provenance = manifest.provenance;
  if (!envelopesAccepted(manifest).includes(V2_ENVELOPE))
    return ["provider accepts only execution-envelope v1, which cannot carry actor model/runtime, execution, or provenance"];
  if (provenance === undefined) return ["provider declares no praxis.provenance-record capability"];
  return [
    ...(provenance.versions.accepts.includes(SUPPORTED_PROVENANCE_MAJOR) ? [] : ["provider does not accept praxis.provenance-record major 1"]),
    ...(provenance.preserves ? [] : ["provider does not preserve provenance verbatim"]),
    ...Object.entries(provenance.propagates).filter(([, value]) => !value).map(([name]) => `provider does not propagate ${name}`),
  ];
};

export const resolve = (request, provider) => {
  if (provider === null) return { state: "unavailable", diagnostics: [] };
  if (!provider.accessible) return { state: "misconfigured", diagnostics: ["provider declared but inaccessible"] };
  const offered = provider.manifest.provides.find((capability) => capability.id === request.capability);
  if (offered === undefined) return { state: "unavailable", diagnostics: [] };
  if (offered.contractVersion !== request.contractVersion)
    return { state: "misconfigured", diagnostics: [`incompatible contract version ${offered.contractVersion}`] };
  const problems = request.requiresProvenance ? provenanceProblems(provider.manifest) : [];
  return problems.length > 0 ? { state: "misconfigured", diagnostics: problems } : { state: "available", diagnostics: [] };
};

// Which envelope version a caller sends to an available provider: v2 when the
// provider accepts it, else v1. A caller that requires provenance never reaches
// the v1 branch, because resolve() already reported misconfigured.
export const envelopeFor = (manifest) =>
  envelopesAccepted(manifest).includes(V2_ENVELOPE) ? V2_ENVELOPE : V1_ENVELOPE;

// Core health is independent of any integration outcome (§1.4, §7).
export const coreHealth = () => "PASS";
