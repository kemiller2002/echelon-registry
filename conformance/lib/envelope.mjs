// Reference model of the execution-envelope version mappings defined in
// spec/echelon-integration-standard.md §5.6 (v1 -> v2) and §5.7 (v2 -> v1).
// Pure functions over plain JSON values; no I/O, no mutation.

const UNKNOWN = "unknown";

const entriesWhere = (pairs) =>
  Object.fromEntries(pairs.filter(([, value]) => value !== undefined));

const nonEmpty = (object) => (Object.keys(object).length > 0 ? object : undefined);

// v1 knownValue -> v2 string. A known value is carried unchanged; unknown, or
// "known" without a value, is the literal "unknown" (never a guess);
// not-applicable is omitted.
const fromKnownValue = (knownValue) => {
  if (knownValue === undefined) return undefined;
  if (knownValue.state === "not-applicable") return undefined;
  if (knownValue.state === "known" && typeof knownValue.value === "string" && knownValue.value.length > 0)
    return knownValue.value;
  return UNKNOWN;
};

// For a non-human actor provider/model/runtime always apply (Praxis actor), so
// an absent or not-applicable v1 value becomes "unknown".
const applicableValue = (knownValue) => fromKnownValue(knownValue) ?? UNKNOWN;

// A human has no provider/model/runtime in the Praxis actor: only a known
// v1 provider is carried; unknown/not-applicable are omitted.
const knownOnly = (knownValue) => {
  const value = fromKnownValue(knownValue);
  return value === UNKNOWN ? undefined : value;
};

const v1KindToV2 = (kind) => (kind === "system" ? "automation" : kind);

export const actorV1ToV2 = (actor) => {
  const kind = v1KindToV2(actor.kind);
  const id = applicableValue(actor.identity);
  return kind === "human"
    ? entriesWhere([["kind", kind], ["id", id], ["provider", knownOnly(actor.provider)]])
    : { kind, id, provider: applicableValue(actor.provider), model: UNKNOWN, runtime: UNKNOWN };
};

export const envelopeV1ToV2 = (envelope) =>
  entriesWhere([
    ["schema", "echelon.execution-envelope/v2"],
    ["operationId", envelope.operationId],
    ["correlationId", envelope.correlationId],
    ["timestamp", envelope.timestamp],
    ["actor", actorV1ToV2(envelope.actor)],
    [
      "runIdentifiers",
      nonEmpty(entriesWhere([
        ["sessionId", fromKnownValue(envelope.actor.sessionId)],
        ["runId", fromKnownValue(envelope.actor.runId)],
      ])),
    ],
    [
      "source",
      envelope.source === undefined
        ? undefined
        : nonEmpty(entriesWhere(
            ["repository", "branch", "commit", "workItem"].map((name) => [name, fromKnownValue(envelope.source[name])]),
          )),
    ],
  ]);

// v2 string -> v1 knownValue.
const toKnownValue = (value, applicable) => {
  if (value === undefined) return applicable ? { state: UNKNOWN } : { state: "not-applicable" };
  return value === UNKNOWN ? { state: UNKNOWN } : { state: "known", value };
};

const V1_KINDS = new Set(["agent", "human", "automation"]);

// v2 -> v1 is LOSSY. Returns { envelope, lost } where `lost` names every v2
// field whose information v1 cannot carry; `envelope` is null when the actor
// itself cannot be expressed in v1. Callers that require provenance MUST NOT
// use this to feed a v1-only provider (§5.7.3): see resolution.mjs.
export const envelopeV2ToV1 = (envelope) => {
  const actor = envelope.actor;
  const human = actor.kind === "human";
  const knownButDropped = (name) => actor[name] !== undefined && actor[name] !== UNKNOWN;
  const actorExtra = Object.keys(actor).filter((name) => !["kind", "id", "provider", "model", "runtime"].includes(name));
  const envelopeExtra = Object.keys(envelope).filter((name) => name.startsWith("x-"));
  const runIds = envelope.runIdentifiers ?? {};
  const lost = [
    ...(V1_KINDS.has(actor.kind) ? [] : ["actor.kind"]),
    ...(knownButDropped("model") ? ["actor.model"] : []),
    ...(knownButDropped("runtime") ? ["actor.runtime"] : []),
    ...actorExtra.map((name) => `actor.${name}`),
    ...(envelope.execution !== undefined ? ["execution"] : []),
    ...(envelope.provenance !== undefined ? ["provenance"] : []),
    ...(runIds.conversationId !== undefined ? ["runIdentifiers.conversationId"] : []),
    ...Object.keys(runIds).filter((name) => name.startsWith("x-")).map((name) => `runIdentifiers.${name}`),
    ...Object.keys(envelope.source ?? {}).filter((name) => name.startsWith("x-")).map((name) => `source.${name}`),
    ...envelopeExtra,
  ];
  const v1Actor = entriesWhere([
    ["kind", actor.kind],
    ["provider", toKnownValue(actor.provider, !human)],
    ["identity", toKnownValue(actor.id, true)],
    ["runId", runIds.runId === undefined ? undefined : toKnownValue(runIds.runId, true)],
    ["sessionId", runIds.sessionId === undefined ? undefined : toKnownValue(runIds.sessionId, true)],
  ]);
  const source =
    envelope.source === undefined
      ? undefined
      : Object.fromEntries(
          ["repository", "branch", "commit", "workItem"]
            .filter((name) => envelope.source[name] !== undefined)
            .map((name) => [name, toKnownValue(envelope.source[name], true)]),
        );
  return {
    envelope: V1_KINDS.has(actor.kind)
      ? entriesWhere([
          ["schema", "echelon.execution-envelope/v1"],
          ["operationId", envelope.operationId],
          ["correlationId", envelope.correlationId],
          ["timestamp", envelope.timestamp],
          ["actor", v1Actor],
          ["source", source],
        ])
      : null,
    lost,
  };
};
