// Echelon Registry reference receiver for provenance carried in execution envelopes.
//
// Normative text: spec/provenance-propagation.md (REG-PROV-001..REG-PROV-016).
// Identity semantics are NOT defined here: actors, execution keys, contributions,
// verdicts, and appending rules come from the vendored, unchanged Praxis reference
// library (lib/vendor/praxis/provenance-interchange.mjs, RQ-ROS-2026-A013..A019,
// DF-ROS-2026-A037). This module only adds the registry's routing/transport
// concerns: envelope versions, the v1 mapping, idempotent replay by operationId,
// relaying without re-attribution, and reading manifest provenance descriptors.
//
// Every function is pure: inputs are never mutated and results are new plain objects.

import {
  SCHEMA_TAG, classify, appendContribution, actorProblems, actorsAgree, credentialFindings,
  emptyBlock, actorFromEnvelopeV1, keyFromEnvelopeV1, foreignExecutionKey,
} from "./vendor/praxis/provenance-interchange.mjs";

export const ENVELOPE_V1 = "echelon.execution-envelope/v1";
export const ENVELOPE_V2 = "echelon.execution-envelope/v2";

/** Extension property that keeps the original v1 actor verbatim when a v1 envelope is upgraded (REG-PROV-008). */
export const V1_ACTOR_EXTENSION = "x-envelope-v1-actor";

const V1_KINDS = ["agent", "human", "automation", "system"];
const V2_FIELDS = new Set(["schema", "operationId", "correlationId", "timestamp", "actor", "execution", "source", "provenance"]);
const EXTENSION_FIELD = /^x-[a-z0-9][a-z0-9-]*$/;
const EXECUTION = /^(EXE-[A-Za-z0-9._-]+|EXT-[a-z][a-z0-9-]*\.[A-Za-z0-9._-]+)$/;

const isObject = (value) => value !== null && typeof value === "object" && !Array.isArray(value);
const isNonEmptyString = (value) => typeof value === "string" && value.trim().length > 0;
const clone = (value) => (value === undefined ? undefined : JSON.parse(JSON.stringify(value)));
const without = (object, field) => Object.fromEntries(Object.entries(object).filter(([key]) => key !== field));
const stable = (value) =>
  Array.isArray(value) ? `[${value.map(stable).join(",")}]`
    : isObject(value) ? `{${Object.keys(value).sort().map((key) => `${JSON.stringify(key)}:${stable(value[key])}`).join(",")}}`
      : JSON.stringify(value);

/** RFC 3339 envelope timestamp -> the UTC form Praxis contributions require. */
export const contributionTime = (timestamp) => new Date(Date.parse(timestamp)).toISOString();

/** Structural problems a receiver can detect without a JSON Schema validator. Empty when acceptable. */
export const envelopeProblems = (envelope) => {
  if (!isObject(envelope)) return ["envelope must be a JSON object"];
  if (envelope.schema !== ENVELOPE_V1 && envelope.schema !== ENVELOPE_V2) {
    return [`envelope schema '${envelope.schema}' is not ${ENVELOPE_V1} or ${ENVELOPE_V2}`];
  }
  const common = [
    ...(!isNonEmptyString(envelope.operationId) ? ["operationId is required"] : []),
    ...(!isNonEmptyString(envelope.correlationId) ? ["correlationId is required"] : []),
    ...(!isNonEmptyString(envelope.timestamp) || Number.isNaN(Date.parse(envelope.timestamp)) ? ["timestamp must be an RFC 3339 date-time"] : []),
  ];
  if (envelope.schema === ENVELOPE_V1) {
    return [
      ...common,
      ...(!isObject(envelope.actor) || !V1_KINDS.includes(envelope.actor.kind) ? ["actor.kind must be agent, human, automation, or system"] : []),
      ...(envelope.provenance !== undefined ? ["provenance requires echelon.execution-envelope/v2"] : []),
    ];
  }
  return [
    ...common,
    ...actorProblems(envelope.actor),
    ...(envelope.execution !== undefined && (typeof envelope.execution !== "string" || !EXECUTION.test(envelope.execution))
      ? ["execution must be EXE-... or EXT-<system>.<run-id>"] : []),
    ...Object.keys(envelope)
      .filter((field) => !V2_FIELDS.has(field) && !EXTENSION_FIELD.test(field))
      .map((field) => `${field} is not an envelope v2 field; extensions must be named x-...`),
  ];
};

/**
 * The invocation an acceptable envelope describes: the current actor (a Praxis actor),
 * the contribution key for that actor's work, and the carried provenance block (v2 only).
 * v1 is mapped with the Praxis reference rules (REG-PROV-008).
 */
export const invocation = (envelope) =>
  envelope.schema === ENVELOPE_V1
    ? { envelopeSchema: ENVELOPE_V1, actor: actorFromEnvelopeV1(envelope.actor), key: keyFromEnvelopeV1(envelope), provenance: undefined }
    : {
      envelopeSchema: ENVELOPE_V2,
      actor: clone(envelope.actor),
      key: envelope.execution ?? keyFromEnvelopeV1({ operationId: envelope.operationId }),
      provenance: clone(envelope.provenance),
    };

/**
 * Upgrades a v1 envelope to v2 without losing anything: the actor is mapped by the Praxis rules,
 * a known runId becomes `execution: EXT-run.<runId>`, and the original v1 actor (including
 * sessionId, which Praxis actors do not model) is kept verbatim under `x-envelope-v1-actor`.
 */
export const upgradeEnvelopeV1 = (envelope) => {
  const { actor, key } = invocation(envelope);
  return {
    schema: ENVELOPE_V2,
    operationId: envelope.operationId,
    correlationId: envelope.correlationId,
    timestamp: envelope.timestamp,
    actor,
    ...(key.startsWith("EXT-run.") ? { execution: key } : {}),
    ...(envelope.source !== undefined ? { source: clone(envelope.source) } : {}),
    [V1_ACTOR_EXTENSION]: clone(envelope.actor),
  };
};

/**
 * A transport relaying a request it did not originate: the envelope (actor, execution,
 * provenance, extensions) is forwarded unchanged, v1 is upgraded losslessly, and the
 * transport never becomes the actor (REG-PROV-009). A transport that invokes on its own
 * behalf builds a new envelope instead and still carries `provenance` verbatim.
 */
export const relay = (envelope) => (envelope.schema === ENVELOPE_V1 ? upgradeEnvelopeV1(envelope) : clone(envelope));

/** The automation actor a receiving system uses for its own `transformed` contribution. */
export const systemActor = (system) => ({ kind: "automation", id: `echelon/${system}`, provider: "echelon", model: "unknown", runtime: system });

/**
 * What a caller may expect a provider to do with provenance, from its manifest v2 descriptor
 * (REG-PROV-013): `undeclared` (no descriptor; never assume preservation), `lossy`,
 * `preserved` (a supported major), or `carried-verbatim` (another major, carried uninterpreted).
 * Informational only: it never makes an optional provider required (REG-PROV-014).
 */
export const provenanceExpectation = (descriptor, blockSchema = SCHEMA_TAG) => {
  if (!isObject(descriptor)) return "undeclared";
  const p = descriptor.propagation ?? {};
  if (descriptor.unknownFields !== "preserve" || !p.contributions || !p.lineage) return "lossy";
  return descriptor.interchange.includes(blockSchema) ? "preserved" : "carried-verbatim";
};

const failure = (state, code, problems) => ({ ok: false, state, error: { code, problems } });

/**
 * Default invoker operations (REG-PROV-006): `created` only when the receiver materialises a
 * brand-new record (the carried block is absent or records no contributions); otherwise the
 * invoking actor transported or transformed an existing payload and is recorded as `transformed`,
 * never as its author.
 */
export const invokerOperations = (block) => (Object.keys(block.contributions ?? {}).length === 0 ? ["created"] : ["transformed"]);

/** Binding for `followup.create` uses the default rule. */
export const followupCreateOperations = invokerOperations;

/**
 * Reference receiver (REG-PROV-005..REG-PROV-010). `state` is `{ records: { [operationId]: record } }`.
 * `binding` is `{ system, capability, operations?: (block, key) => string[], recordsTransformation?: boolean }`.
 * The invoking actor is always recorded (default operations: `invokerOperations`); an append the
 * Praxis rules refuse, or one that would leave the history malformed, rejects the request instead.
 * Returns `{ ok: true, state, result }` or `{ ok: false, state, error: { code, problems } }`;
 * the input state is never mutated and is returned unchanged on failure.
 */
export const receive = (state, { envelope, payload }, binding) => {
  const structural = envelopeProblems(envelope);
  if (structural.length > 0) return failure(state, "envelope-invalid", structural);

  const call = invocation(envelope);
  const verdict = call.provenance === undefined ? undefined : classify(call.provenance);
  if (verdict?.verdict === "malformed") return failure(state, "provenance-malformed", verdict.problems);

  const secrets = credentialFindings(without(envelope, "provenance"));
  if (secrets.length > 0) return failure(state, "credential-in-envelope", secrets.map((path) => `${path}: credential-like value`));

  const fingerprint = stable({ payload: payload ?? null, actor: call.actor, key: call.key, provenance: call.provenance ?? null });
  const existing = state.records[envelope.operationId];
  if (existing !== undefined) {
    return existing.fingerprint === fingerprint
      ? { ok: true, state, result: { replayed: true, record: existing } }
      : failure(state, "operation-conflict", [`operationId '${envelope.operationId}' was already used for a different request`]);
  }

  const at = contributionTime(envelope.timestamp);
  const base = { operationId: envelope.operationId, correlationId: envelope.correlationId, capability: binding.capability, envelopeSchema: call.envelopeSchema, payload: clone(payload), invokedBy: { actor: call.actor, key: call.key }, fingerprint };

  if (verdict?.verdict === "unsupported") {
    const record = { ...base, provenance: { verdict: "unsupported", schema: verdict.schema, block: call.provenance, warnings: [] }, invoker: { recorded: false, reason: "unsupported-major" } };
    return { ok: true, state: { ...state, records: { ...state.records, [envelope.operationId]: record } }, result: { replayed: false, record } };
  }

  const carried = call.provenance ?? emptyBlock();
  const prior = carried.contributions[call.key];
  if (prior !== undefined && !actorsAgree(prior.actor, call.actor)) {
    return failure(state, "provenance-conflict", [`contribution '${call.key}' is attributed to ${prior.actor.kind}:${prior.actor.id}, not the invoking actor`]);
  }
  const operations = (binding.operations ?? followupCreateOperations)(carried, call.key);
  const appended = appendContribution(carried, call.key, { operations, at, actor: call.actor });
  if (!appended.ok) return failure(state, "provenance-conflict", [appended.error]);
  const ordered = classify(appended.block);
  if (ordered.verdict !== "supported") return failure(state, "provenance-conflict", ordered.problems);
  const withInvoker = appended.block;
  const invoker = { recorded: true, key: call.key, operations, changed: appended.changed };

  const transformed = binding.recordsTransformation
    ? appendContribution(withInvoker, foreignExecutionKey(binding.system, keyFromEnvelopeV1({ operationId: envelope.operationId }).slice("EXT-op.".length)), { operations: ["transformed"], at, actor: systemActor(binding.system) })
    : { ok: true, block: withInvoker };
  if (!transformed.ok) return failure(state, "provenance-conflict", [transformed.error]);
  const final = classify(transformed.block);
  if (final.verdict !== "supported") return failure(state, "provenance-conflict", final.problems);

  const record = { ...base, provenance: { verdict: verdict?.verdict ?? "absent", block: transformed.block, warnings: verdict?.warnings ?? [] }, invoker };
  return { ok: true, state: { ...state, records: { ...state.records, [envelope.operationId]: record } }, result: { replayed: false, record } };
};

export const emptyState = () => ({ records: {} });
