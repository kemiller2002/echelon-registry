// Echelon Registry reference receiver for provenance carried in execution envelopes.
//
// Normative text: spec/provenance-propagation.md (REG-PROV-001..REG-PROV-017, revision 1.2).
// Identity semantics are NOT defined here: actors, execution keys, contributions,
// verdicts, and appending rules come from the vendored, unchanged Praxis reference
// library (lib/vendor/praxis/provenance-interchange.mjs, RQ-ROS-2026-A013..A019,
// DF-ROS-2026-A037). This module only adds the registry's routing/transport
// concerns: envelope versions, the v1 mapping, idempotent replay by operationId,
// relaying without re-attribution, and reading manifest provenance descriptors. Praxis contract
// revision 1.2 applies throughout: text is read with `classifyText`, "blank" means ASCII whitespace
// only, lineage goes through the checked `addLineage`, key segments use `escapeKeySegment`, and
// timestamps are strict.
//
// Every function is pure: inputs are never mutated and results are new plain objects.

import {
  SCHEMA_TAG, classify, classifyText, appendContribution, actorProblems, actorsAgree, credentialFindings,
  emptyBlock, addLineage, actorFromEnvelopeV1, keyFromEnvelopeV1, foreignExecutionKey, escapeKeySegment,
  parseTimestamp,
} from "./vendor/praxis/provenance-interchange.mjs";

/** The contract-1.2 key segment escaping, re-exported unchanged from the vendored Praxis reference (REG-PROV-008). */
export { escapeKeySegment };

export const ENVELOPE_V1 = "echelon.execution-envelope/v1";
export const ENVELOPE_V2 = "echelon.execution-envelope/v2";

/** Extension property that keeps the original v1 actor verbatim when a v1 envelope is upgraded (REG-PROV-008). */
export const V1_ACTOR_EXTENSION = "x-envelope-v1-actor";

const V1_KINDS = ["agent", "human", "automation", "system"];
const V2_FIELDS = new Set(["schema", "operationId", "correlationId", "timestamp", "actor", "execution", "source", "provenance"]);
const EXTENSION_FIELD = /^x-[a-z0-9][a-z0-9-]*$/;
const EXECUTION = /^(EXE-[A-Za-z0-9._-]+|EXT-[a-z][a-z0-9-]*\.[A-Za-z0-9._-]+)$/;

const isObject = (value) => value !== null && typeof value === "object" && !Array.isArray(value);
/** Contract 1.2: only tab, LF, VT, FF, CR, and space are whitespace; every other character is content. */
const asciiTrim = (value) => value.replace(/^[\t\n\v\f\r ]+|[\t\n\v\f\r ]+$/g, "");
const isNonEmptyString = (value) => typeof value === "string" && asciiTrim(value).length > 0;
const clone = (value) => (value === undefined ? undefined : JSON.parse(JSON.stringify(value)));
const without = (object, field) => Object.fromEntries(Object.entries(object).filter(([key]) => key !== field));
const stable = (value) =>
  Array.isArray(value) ? `[${value.map(stable).join(",")}]`
    : isObject(value) ? `{${Object.keys(value).sort().map((key) => `${JSON.stringify(key)}:${stable(value[key])}`).join(",")}}`
      : JSON.stringify(value);

const RFC3339 = /^([0-9]{4}-[0-9]{2}-[0-9]{2})[Tt]([0-9]{2}:[0-9]{2}:[0-9]{2})(\.[0-9]+)?(?:([Zz])|([+-])([0-9]{2}):([0-9]{2}))$/;

/**
 * RFC 3339 envelope timestamp -> the UTC form Praxis contributions require, or undefined.
 * Strict and host-independent (contract 1.2): a `T` separator, seconds, and a `Z` or `+hh:mm`
 * offset are required, the calendar is checked by the Praxis `parseTimestamp`, and extra fraction
 * digits are truncated to milliseconds. `Date.parse` is never used.
 */
export const contributionTime = (timestamp) => {
  const match = typeof timestamp === "string" ? RFC3339.exec(timestamp) : null;
  if (match === null) return undefined;
  const [, date, time, fraction = "", zulu, sign, hours, minutes] = match;
  const local = parseTimestamp(`${date}T${time}${fraction}Z`);
  if (local === undefined || (zulu === undefined && (Number(hours) > 23 || Number(minutes) > 59))) return undefined;
  const offset = zulu === undefined ? (sign === "-" ? -1 : 1) * (Number(hours) * 60 + Number(minutes)) * 60000 : 0;
  const utc = new Date(local - offset).toISOString();
  return parseTimestamp(utc) === undefined ? undefined : utc;
};

/** Runs a function that throws on refusal (the Praxis key functions) as `{ ok, value }` or `{ ok: false, error }`. */
const attempt = (fn) => {
  try {
    return { ok: true, value: fn() };
  } catch (error) {
    return { ok: false, error: error.message };
  }
};

/** Dotted paths of every key or string that is not well-formed Unicode (an unpaired surrogate; contract 1.2). */
const surrogatePaths = (node, path = "") =>
  typeof node === "string" ? (node.isWellFormed() ? [] : [path])
    : Array.isArray(node) ? node.flatMap((item, index) => surrogatePaths(item, `${path}[${index}]`))
      : isObject(node) ? Object.entries(node).flatMap(([key, value]) => {
        const child = path ? `${path}.${key}` : key;
        return [...(key.isWellFormed() ? [] : [child]), ...surrogatePaths(value, child)];
      })
        : [];

/** Structural problems a receiver can detect without a JSON Schema validator. Empty when acceptable. */
export const envelopeProblems = (envelope) => {
  if (!isObject(envelope)) return ["envelope must be a JSON object"];
  if (envelope.schema !== ENVELOPE_V1 && envelope.schema !== ENVELOPE_V2) {
    return [`envelope schema '${envelope.schema}' is not ${ENVELOPE_V1} or ${ENVELOPE_V2}`];
  }
  const common = [
    ...(!isNonEmptyString(envelope.operationId) ? ["operationId is required"] : []),
    ...(!isNonEmptyString(envelope.correlationId) ? ["correlationId is required"] : []),
    ...(contributionTime(envelope.timestamp) === undefined ? ["timestamp must be an RFC 3339 date-time with a Z or +hh:mm offset"] : []),
    ...surrogatePaths(without(envelope, "provenance")).map((path) => `${path}: unpaired UTF-16 surrogate; an envelope must be well-formed Unicode`),
  ];
  const key = attempt(() => invocationKey(envelope));
  const keyProblems = common.length === 0 && !key.ok ? [`the invocation cannot form a contribution key: ${key.error}`] : [];
  if (envelope.schema === ENVELOPE_V1) {
    return [
      ...common,
      ...(!isObject(envelope.actor) || !V1_KINDS.includes(envelope.actor.kind) ? ["actor.kind must be agent, human, automation, or system"] : []),
      ...(envelope.provenance !== undefined ? ["provenance requires echelon.execution-envelope/v2"] : []),
      ...keyProblems,
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
    ...keyProblems,
  ];
};

const knownValue = (value) => (isObject(value) && value.state === "known" && isNonEmptyString(value.value) ? value.value : undefined);

/**
 * Contribution key for a v1 envelope (REG-PROV-008). v1 run ids are only unique per sender, so when
 * `source.repository` is known a run is namespaced by it: `EXT-run.<seg(repository)>.<seg(runId)>`.
 * Otherwise the Praxis reference key is used unchanged (`EXT-run.<seg(runId)>` or
 * `EXT-op.<seg(operationId)>`). `seg` is the contract-1.2 `escapeKeySegment`, which escapes '.' in
 * every segment, so a namespaced key can never equal an un-namespaced one. Throws, like the Praxis
 * reference, when a segment is empty or not well-formed Unicode; the envelope is then rejected.
 */
export const keyFromEnvelopeV1Namespaced = (envelope) => {
  const reference = keyFromEnvelopeV1(envelope);
  const repository = knownValue(envelope.source?.repository);
  const run = knownValue(envelope.actor?.runId);
  return reference.startsWith("EXT-run.") && repository !== undefined && run !== undefined
    ? `EXT-run.${escapeKeySegment(repository)}.${escapeKeySegment(run)}`
    : reference;
};

/** The invoking actor's contribution key (REG-PROV-006, REG-PROV-008). Throws when no key can be formed. */
const invocationKey = (envelope) =>
  envelope.schema === ENVELOPE_V1 ? keyFromEnvelopeV1Namespaced(envelope) : envelope.execution ?? keyFromEnvelopeV1({ operationId: envelope.operationId });

/**
 * The invocation an acceptable envelope describes: the current actor (a Praxis actor),
 * the contribution key for that actor's work, and the carried provenance block (v2 only).
 * v1 is mapped with the Praxis reference rules (REG-PROV-008). Call it only on an envelope without
 * `envelopeProblems`: an id that cannot form a key throws.
 */
export const invocation = (envelope) =>
  envelope.schema === ENVELOPE_V1
    ? { envelopeSchema: ENVELOPE_V1, actor: actorFromEnvelopeV1(envelope.actor), key: invocationKey(envelope), provenance: undefined }
    : { envelopeSchema: ENVELOPE_V2, actor: clone(envelope.actor), key: invocationKey(envelope), provenance: clone(envelope.provenance) };

/**
 * Upgrades a v1 envelope to v2 without losing anything: the actor is mapped by the Praxis rules,
 * a known runId becomes `execution` (the REG-PROV-008 run key), and the original v1 actor (including
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
 * Capability kind (REG-PROV-006): `create` operations materialise a NEW domain record, so the carried
 * block describes the request's SOURCE; `update` operations transport or change an EXISTING record
 * whose own provenance is carried. A capability contract declares its kind; by default `*.create`
 * and `*.record` are `create`, everything else `update`.
 */
export const capabilityKind = (capability) => (/\.(create|record)$/.test(capability ?? "") ? "create" : "update");

/**
 * The invoker's default operations for an `update` (REG-PROV-006): its role, never authorship.
 * `*.resolve` records `resolved`; every other update records `transformed`. This holds even when the
 * carried block records no contributions: the record's origin then stays unknown, and an updater never
 * becomes the creator of a record it did not create (the rule Vigila's VIG-PROV-008 follows).
 */
export const updateOperations = (capability) => (/\.resolve$/.test(capability ?? "") ? ["resolved"] : ["transformed"]);

const payloadInvalid = (problem) => ({ ok: false, code: "payload-invalid", error: problem });

/**
 * The source record a `followup.create` payload names (REG-PROV-006, section 3): `{ ok: true, reference }`
 * (`reference` undefined when none is named) or `{ ok: false, code, error }`. The canonical shape is
 * Vigila's object `context.source.ref` (a non-blank string such as `aegis:finding/SF-0001`, ASCII-trimmed);
 * a plain string `context.source` is still read for compatibility. An absent or `null` `context` or
 * `source` names nothing; any other shape is malformed and rejects the request, so lineage is never
 * dropped silently. The reference itself is checked by `addLineage` (credentials, surrogates).
 */
export const followupSourceReference = (payload) => {
  const context = isObject(payload) ? payload.context : undefined;
  if (context === undefined || context === null) return { ok: true, reference: undefined };
  if (!isObject(context)) return payloadInvalid("payload.context must be an object or null");
  const { source } = context;
  if (source === undefined || source === null) return { ok: true, reference: undefined };
  const reference = isObject(source) ? source.ref : source;
  if (!isObject(source) && typeof source !== "string") return payloadInvalid("payload.context.source must be an object { ref } (or, for compatibility, a string)");
  return isNonEmptyString(reference)
    ? { ok: true, reference: asciiTrim(reference) }
    : payloadInvalid(`payload.context.source${isObject(source) ? ".ref" : ""} must be a non-empty string such as 'aegis:finding/SF-0001'`);
};

const store = (state, record) => ({ ok: true, state: { ...state, records: { ...state.records, [record.operationId]: record } }, result: { replayed: false, record } });

const transformationKey = (system, operationId) => foreignExecutionKey(system, keyFromEnvelopeV1({ operationId }).slice("EXT-op.".length));

/** Appends one contribution and insists the result is still a well-ordered supported block. */
const appendChecked = (block, key, contribution) => {
  const appended = appendContribution(block, key, contribution);
  if (!appended.ok) return { ok: false, problems: [appended.error] };
  const verdict = classify(appended.block);
  return verdict.verdict === "supported" ? appended : { ok: false, problems: verdict.problems };
};

/**
 * Appends the receiving system's own `transformed` contribution when the binding records one. Either
 * way the block that will be stored is re-classified: nothing unsupported is ever stored.
 */
const withTransformation = (block, binding, operationId, at) => {
  if (!binding.recordsTransformation) {
    const verdict = classify(block);
    return verdict.verdict === "supported" ? { ok: true, block } : { ok: false, problems: verdict.problems };
  }
  const key = attempt(() => transformationKey(binding.system, operationId));
  return key.ok ? appendChecked(block, key.value, { operations: ["transformed"], at, actor: systemActor(binding.system) }) : { ok: false, problems: [key.error] };
};

/** Lineage through the checked Praxis `addLineage` (contract 1.2); a refusal rejects the request. */
const withLineage = (block, references) => {
  if (references.length === 0) return { ok: true, block };
  const added = addLineage(block, references);
  return added.ok ? added : { ok: false, problems: [added.error] };
};

/** The binding's operations for the invoker, or the default for its kind; checked against the kind. */
const invokerOperationsFor = (kind, binding, block, key) => {
  const operations = binding.operations?.(block, key) ?? (kind === "create" ? ["created"] : updateOperations(binding.capability));
  if (kind === "create" && !operations.includes("created")) return { ok: false, problems: [`a create capability records the invoker as created, not ${operations.join(", ")}`] };
  if (kind === "update" && operations.includes("created")) return { ok: false, problems: [`'${binding.capability}' is an update capability; only a create capability records created`] };
  return { ok: true, operations };
};

/**
 * `create`: a NEW praxis.provenance/1 block for the new record. The invoker is its only creator;
 * lineage is the source block's `derivedFrom` (only when that block is supported, since another
 * major is never interpreted) plus the source record the payload names, added through the checked
 * `addLineage` (a refusal rejects the request). The received block is kept verbatim beside it as
 * `receivedProvenance` (`{ verdict: "absent" }` when none was carried) and never appended to or merged.
 */
const receiveCreate = (state, base, call, verdict, binding, operationId, at) => {
  const source = (binding.sourceReference ?? followupSourceReference)(base.payload);
  if (!source.ok) return failure(state, source.code ?? "payload-invalid", [source.error]);
  const operations = invokerOperationsFor("create", binding, emptyBlock(), call.key);
  if (!operations.ok) return failure(state, "binding-invalid", operations.problems);
  const created = appendChecked(emptyBlock(), call.key, { operations: operations.operations, at, actor: call.actor });
  if (!created.ok) return failure(state, "provenance-conflict", created.problems);
  const lineage = withLineage(created.block, [
    ...(verdict?.verdict === "supported" && Array.isArray(call.provenance.derivedFrom) ? call.provenance.derivedFrom : []),
    ...(source.reference !== undefined ? [source.reference] : []),
  ]);
  if (!lineage.ok) return failure(state, "lineage-refused", lineage.problems);
  const transformed = withTransformation(lineage.block, binding, operationId, at);
  if (!transformed.ok) return failure(state, "provenance-conflict", transformed.problems);
  return store(state, {
    ...base,
    kind: "create",
    provenance: { verdict: "created", block: transformed.block, warnings: [] },
    receivedProvenance: call.provenance === undefined ? { verdict: "absent" }
      : { verdict: verdict.verdict, ...(verdict.schema ? { schema: verdict.schema } : {}), block: call.provenance, warnings: verdict.warnings },
    invoker: { recorded: true, key: call.key, operations: operations.operations, changed: true },
  });
};

/**
 * `update`: the carried block is the existing record's own provenance. It is preserved and the
 * invoker appended with its role (`updateOperations`, or the binding's non-authorship operations),
 * never `created`, even when the block records no contributions; an unsupported major is kept
 * verbatim with nothing appended.
 */
const receiveUpdate = (state, base, call, verdict, binding, operationId, at) => {
  if (verdict?.verdict === "unsupported") {
    return store(state, { ...base, kind: "update", provenance: { verdict: "unsupported", schema: verdict.schema, block: call.provenance, warnings: [] }, invoker: { recorded: false, reason: "unsupported-major" } });
  }
  const carried = call.provenance ?? emptyBlock();
  const prior = carried.contributions[call.key];
  if (prior !== undefined && !actorsAgree(prior.actor, call.actor)) {
    return failure(state, "provenance-conflict", [`contribution '${call.key}' is attributed to ${prior.actor.kind}:${prior.actor.id}, not the invoking actor`]);
  }
  const operations = invokerOperationsFor("update", binding, carried, call.key);
  if (!operations.ok) return failure(state, "binding-invalid", operations.problems);
  const appended = appendChecked(carried, call.key, { operations: operations.operations, at, actor: call.actor });
  if (!appended.ok) return failure(state, "provenance-conflict", appended.problems);
  const transformed = withTransformation(appended.block, binding, operationId, at);
  if (!transformed.ok) return failure(state, "provenance-conflict", transformed.problems);
  return store(state, {
    ...base,
    kind: "update",
    provenance: { verdict: verdict?.verdict ?? "absent", block: transformed.block, warnings: verdict?.warnings ?? [] },
    invoker: { recorded: true, key: call.key, operations: operations.operations, changed: appended.changed },
  });
};

/**
 * Reference receiver (REG-PROV-005..REG-PROV-010). `state` is `{ records: { [operationId]: record } }`.
 * `binding` is `{ system, capability, kind?: "create"|"update", operations?: (block, key) => string[],
 * sourceReference?: (payload) => { ok: true, reference?: string } | { ok: false, code?, error },
 * recordsTransformation?: boolean }`; `kind` defaults to `capabilityKind(capability)`. A create's
 * operations must include `created`; an update's must not. The invoking actor is always recorded; an
 * append or lineage the Praxis rules refuse, or one that would leave the history malformed, rejects
 * the request instead.
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

  const at = contributionTime(envelope.timestamp); // defined: envelopeProblems checked it
  const base = { operationId: envelope.operationId, correlationId: envelope.correlationId, capability: binding.capability, envelopeSchema: call.envelopeSchema, payload: clone(payload), invokedBy: { actor: call.actor, key: call.key }, fingerprint };
  const kind = binding.kind ?? capabilityKind(binding.capability);
  return (kind === "create" ? receiveCreate : receiveUpdate)(state, base, call, verdict, binding, envelope.operationId, at);
};

export const emptyState = () => ({ records: {} });

/**
 * Parses JSON text as the Praxis contract reads text (rule 1 of revision 1.2): `{ ok: true, value }`,
 * or `{ ok: false, problems }` when it is not JSON, repeats a member name within any one object, or
 * holds an unpaired UTF-16 surrogate. Duplicates are found by the vendored `classifyText` scanner:
 * without them it returns exactly `classify(JSON.parse(text))`, so any difference names them.
 */
export const parseJsonText = (text) => {
  if (typeof text !== "string") return { ok: false, problems: ["request text must be a string"] };
  const value = attempt(() => JSON.parse(text));
  if (!value.ok) return { ok: false, problems: ["request is not valid JSON"] };
  const asText = classifyText(text);
  if (stable(asText) !== stable(classify(value.value))) return { ok: false, problems: asText.problems };
  const unpaired = surrogatePaths(value.value);
  return unpaired.length === 0 ? value : { ok: false, problems: unpaired.map((path) => `${path}: unpaired UTF-16 surrogate`) };
};

/**
 * `receive` for a request that arrives as JSON text `{"envelope": ..., "payload": ...}`. Text that
 * `parseJsonText` refuses is rejected as `request-malformed` before anything else is read, so a
 * repeated member (for example a second `created` contribution under the same key) can never be
 * resolved differently by different readers.
 */
export const receiveText = (state, text, binding) => {
  const parsed = parseJsonText(text);
  if (!parsed.ok) return failure(state, "request-malformed", parsed.problems);
  if (!isObject(parsed.value)) return failure(state, "request-malformed", ["request must be a JSON object { envelope, payload }"]);
  return receive(state, { envelope: parsed.value.envelope, payload: parsed.value.payload }, binding);
};
