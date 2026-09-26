// REG-PROV-005..REG-PROV-010, REG-PROV-014, REG-PROV-016: the reference receiver in
// lib/echelon-provenance.mjs classifies carried provenance, appends the invoking actor's
// contribution idempotently, never re-attributes or overwrites, maps v1 losslessly,
// rejects credentials, and treats identity as provenance only.
import test from "node:test";
import assert from "node:assert/strict";
import { createAjv, readJson, validator, praxisCases, echelonChain } from "./support.mjs";
import {
  receive, emptyState, invocation, upgradeEnvelopeV1, relay, envelopeProblems, provenanceExpectation,
  systemActor, V1_ACTOR_EXTENSION, capabilityKind, keyFromEnvelopeV1Namespaced, escapeKeySegment,
} from "../lib/echelon-provenance.mjs";
import {
  actorFromEnvelopeV1, keyFromEnvelopeV1, preservationViolations, IDENTITY_ENVIRONMENT_VARIABLES, identityEnvironment, originator, withRole, addLineage, emptyBlock,
} from "../lib/vendor/praxis/provenance-interchange.mjs";

const ajv = createAjv();
const envelopeV2 = validator(ajv, "echelon.execution-envelope/v2");
const followupContract = ajv.getSchema("https://echelonfoundry.com/contracts/followup.create.v1.json");
const vigila = { system: "vigila", capability: "followup.create" }; // create: a new record from a source
const update = { system: "vigila", capability: "followup.update" }; // update: an existing record's own block
const payload = { title: "Review injection finding SF-0001", reason: "Aegis review found an injection risk", requestedAction: "review", priority: "high" };

const A = { kind: "agent", id: "openai/codex", provider: "openai", model: "gpt-5-codex", runtime: "codex" };
const B = { kind: "agent", id: "anthropic/claude-code", provider: "anthropic", model: "unknown", runtime: "claude-code" };
const CI = { kind: "automation", id: "github/github-actions", provider: "github", model: "unknown", runtime: "github-actions" };
const known = (value) => ({ state: "known", value });

const envelope = (fields = {}) => ({
  schema: "echelon.execution-envelope/v2",
  operationId: "op-1",
  correlationId: "corr-1",
  timestamp: "2026-09-26T09:05:00.000Z",
  actor: B,
  execution: "EXE-20260926T090500000Z-bbbb0005",
  ...fields,
});
const blockBy = (key, actor, at = "2026-09-26T08:00:00.000Z") => ({
  schema: "praxis.provenance/1",
  contributions: { [key]: { operations: ["created"], at, actor } },
  derivedFrom: ["aegis:finding/SF-0001"],
});

test("the follow-up payload used here is a valid followup.create v1 payload", () => {
  assert.ok(followupContract(payload), ajv.errorsText(followupContract.errors));
});

// ---- v1 mapping (REG-PROV-008) --------------------------------------------------------

test("v1 mapping: agent keeps provider and identity; model/runtime are unknown, never invented", () => {
  assert.deepEqual(actorFromEnvelopeV1({ kind: "agent", provider: known("openai"), identity: known("openai/codex") }),
    { kind: "agent", id: "openai/codex", provider: "openai", model: "unknown", runtime: "unknown" });
});

test("v1 mapping: system becomes automation", () => {
  assert.deepEqual(actorFromEnvelopeV1({ kind: "system", provider: { state: "unknown" }, identity: known("echelon/vigila") }),
    { kind: "automation", id: "echelon/vigila", provider: "unknown", model: "unknown", runtime: "unknown" });
});

test("v1 mapping: a human omits not-applicable provider/model/runtime", () => {
  assert.deepEqual(actorFromEnvelopeV1({ kind: "human", provider: { state: "not-applicable" }, identity: known("kevin") }), { kind: "human", id: "kevin" });
});

test("v1 mapping: unknown and not-applicable identity map to the literal unknown", () => {
  assert.deepEqual(actorFromEnvelopeV1({ kind: "agent", provider: { state: "unknown" }, identity: { state: "unknown" } }),
    { kind: "agent", id: "unknown", provider: "unknown", model: "unknown", runtime: "unknown" });
  assert.equal(actorFromEnvelopeV1({ kind: "human", provider: { state: "not-applicable" }, identity: { state: "not-applicable" } }).id, "unknown");
});

test("v1 mapping: runId known keys EXT-run.<runId>; unknown keys EXT-op.<operationId> (reserved pseudo-systems)", () => {
  assert.equal(keyFromEnvelopeV1({ operationId: "op-1", actor: { runId: known("gh/99") } }), "EXT-run.gh_2f99");
  assert.equal(keyFromEnvelopeV1({ operationId: "op 1", actor: { runId: { state: "unknown" } } }), "EXT-op.op_201");
  assert.equal(keyFromEnvelopeV1({ operationId: "op-2", actor: {} }), "EXT-op.op-2");
});

test("v1 envelope upgrade is lossless and yields a valid v2 envelope with the same invocation key", () => {
  for (const file of ["examples/envelopes/v1-agent.envelope.json", "examples/envelopes/v1-human.envelope.json"]) {
    const v1 = readJson(file);
    const before = JSON.stringify(v1);
    const upgraded = upgradeEnvelopeV1(v1);
    assert.equal(JSON.stringify(v1), before, "input not mutated");
    assert.ok(envelopeV2(upgraded), ajv.errorsText(envelopeV2.errors));
    assert.deepEqual(upgraded[V1_ACTOR_EXTENSION], v1.actor, "the original v1 actor (with sessionId) is kept verbatim");
    assert.deepEqual(invocation(upgraded).actor, invocation(v1).actor);
    assert.equal(invocation(upgraded).key, invocation(v1).key);
    assert.deepEqual(upgraded.source, v1.source);
  }
  assert.equal(upgradeEnvelopeV1(readJson("examples/envelopes/v1-agent.envelope.json")).execution, "EXT-run.kemiller2002_2faegis.gh_2f99");
});

test("a v1 envelope is received: the mapped actor originates the follow-up under its repository-namespaced EXT-run key", () => {
  const v1 = readJson("examples/envelopes/v1-agent.envelope.json");
  const received = receive(emptyState(), { envelope: v1, payload }, vigila);
  assert.ok(received.ok, JSON.stringify(received.error));
  const { record } = received.result;
  assert.equal(record.envelopeSchema, "echelon.execution-envelope/v1");
  assert.deepEqual(originator(record.provenance.block).actor, { kind: "agent", id: "openai/codex", provider: "openai", model: "unknown", runtime: "unknown" });
  assert.equal(originator(record.provenance.block).key, "EXT-run.kemiller2002_2faegis.gh_2f99");
  assert.equal(originator(record.provenance.block).at, "2026-09-26T09:05:00.000Z");
});

test("a v1 human envelope with an offset timestamp is normalized to UTC", () => {
  const received = receive(emptyState(), { envelope: readJson("examples/envelopes/v1-human.envelope.json"), payload }, vigila);
  assert.ok(received.ok);
  const origin = originator(received.result.record.provenance.block);
  assert.deepEqual(origin.actor, { kind: "human", id: "kevin" });
  assert.equal(origin.key, "EXT-op.op-followup-0002");
  assert.equal(origin.at, "2026-09-26T09:00:00.000Z");
});

test("a v1 envelope cannot carry provenance (it needs v2)", () => {
  const v1 = { ...readJson("examples/envelopes/v1-agent.envelope.json"), provenance: emptyBlock() };
  assert.equal(receive(emptyState(), { envelope: v1, payload }, vigila).error.code, "envelope-invalid");
});

// ---- classification at the boundary (REG-PROV-005..REG-PROV-007) -------------------

for (const item of praxisCases().filter((c) => c.expect === "supported")) {
  test(`update keeps the record's own supported block '${item.name}' intact (${item.warnings} warnings)`, () => {
    const received = receive(emptyState(), { envelope: envelope({ provenance: item.block }), payload }, update);
    const invokedAt = Date.parse("2026-09-26T09:05:00.000Z");
    if (Object.values(item.block.contributions).some((entry) => Date.parse(entry.at.slice(0, 23) + "Z") > invokedAt && entry.operations.includes("created"))) {
      assert.equal(received.error.code, "provenance-conflict", "an invocation before the recorded creation is refused, never recorded out of order");
      return;
    }
    assert.ok(received.ok, JSON.stringify(received.error));
    const { record } = received.result;
    assert.equal(record.provenance.verdict, "supported");
    assert.equal(record.provenance.warnings.length, item.warnings);
    assert.deepEqual(preservationViolations(item.block, record.provenance.block), []);
  });

  test(`create stores source block '${item.name}' verbatim and gives the new record its own block`, () => {
    const before = JSON.stringify(item.block);
    const received = receive(emptyState(), { envelope: envelope({ provenance: item.block }), payload }, vigila);
    assert.ok(received.ok, JSON.stringify(received.error));
    const { record } = received.result;
    assert.equal(JSON.stringify(record.receivedProvenance.block), before, "source block byte-identical");
    assert.equal(record.receivedProvenance.verdict, "supported");
    assert.equal(record.receivedProvenance.warnings.length, item.warnings);
    assert.equal(record.provenance.block.schema, "praxis.provenance/1");
    assert.deepEqual(Object.keys(record.provenance.block.contributions), ["EXE-20260926T090500000Z-bbbb0005"]);
    assert.deepEqual(withRole(record.provenance.block, "created").map((entry) => entry.actor), [B]);
    assert.deepEqual(record.provenance.block.derivedFrom ?? [], item.block.derivedFrom ?? []);
  });
}

for (const item of praxisCases().filter((c) => c.expect === "malformed")) {
  test(`receiver rejects malformed block '${item.name}' with a structured error and stores nothing`, () => {
    const state = emptyState();
    const received = receive(state, { envelope: envelope({ provenance: item.block }), payload }, vigila);
    assert.equal(received.ok, false);
    assert.equal(received.error.code, "provenance-malformed");
    assert.ok(received.error.problems.length > 0);
    assert.equal(received.state, state);
    assert.deepEqual(received.state.records, {});
  });
}

test("an unsupported major is stored verbatim; no contribution is merged into it (both kinds)", () => {
  for (const block of [readJson("examples/envelopes/v2-unsupported-major.envelope.json").provenance,
    ...praxisCases().filter((c) => c.expect === "unsupported").map((c) => c.block)]) {
    const before = JSON.stringify(block);
    const updated = receive(emptyState(), { envelope: envelope({ provenance: block }), payload }, { ...update, recordsTransformation: true });
    assert.ok(updated.ok, JSON.stringify(updated.error));
    assert.equal(updated.result.record.provenance.verdict, "unsupported");
    assert.equal(JSON.stringify(updated.result.record.provenance.block), before);
    assert.deepEqual(updated.result.record.invoker, { recorded: false, reason: "unsupported-major" });

    const created = receive(emptyState(), { envelope: envelope({ provenance: block }), payload }, { ...vigila, recordsTransformation: true });
    assert.ok(created.ok, JSON.stringify(created.error));
    const { record } = created.result;
    assert.equal(record.receivedProvenance.verdict, "unsupported");
    assert.equal(record.receivedProvenance.schema, block.schema);
    assert.equal(JSON.stringify(record.receivedProvenance.block), before, "carried verbatim beside the new record");
    assert.deepEqual(originator(record.provenance.block).actor, B, "the new record still has its own creator");
    assert.equal(record.provenance.block.derivedFrom, undefined, "another major is never interpreted for lineage");
  }
});

test("no silent stripping: an absent block yields a record whose provenance names the invoker", () => {
  const received = receive(emptyState(), { envelope: envelope(), payload }, vigila);
  assert.equal(received.result.record.provenance.verdict, "created");
  assert.equal(received.result.record.receivedProvenance, null);
  assert.deepEqual(originator(received.result.record.provenance.block).actor, B);
  const updated = receive(emptyState(), { envelope: envelope(), payload }, update);
  assert.equal(updated.result.record.provenance.verdict, "absent");
  assert.deepEqual(originator(updated.result.record.provenance.block).actor, B);
});

// ---- appending, replay, and never overwriting (REG-PROV-009, REG-PROV-010) --------

test("the invoking actor's contribution is appended under envelope.execution and lineage is kept", () => {
  const example = readJson("examples/envelopes/v2-agent-with-provenance.envelope.json");
  const received = receive(emptyState(), { envelope: example, payload }, vigila);
  assert.ok(received.ok, JSON.stringify(received.error));
  const block = received.result.record.provenance.block;
  assert.equal(originator(block).key, "EXE-20260926T090000000Z-c1c1c1c1");
  assert.deepEqual(originator(block).actor, example.actor);
  assert.deepEqual(block.derivedFrom, ["aegis:finding/SF-0001"]);
});

test("replaying the same operationId returns the existing record and adds nothing", () => {
  const request = { envelope: envelope({ provenance: blockBy("EXE-A1", A) }), payload };
  const first = receive(emptyState(), request, { ...vigila, recordsTransformation: true });
  const second = receive(first.state, request, { ...vigila, recordsTransformation: true });
  const third = receive(second.state, JSON.parse(JSON.stringify(request)), { ...vigila, recordsTransformation: true });
  assert.ok(second.ok && third.ok);
  assert.equal(second.result.replayed, true);
  assert.equal(third.result.replayed, true);
  assert.equal(second.state, first.state, "state is unchanged by a replay");
  assert.equal(Object.keys(third.state.records).length, 1);
  assert.deepEqual(third.result.record, first.result.record);
});

test("a retried request with a new timestamp is still the same operation", () => {
  const first = receive(emptyState(), { envelope: envelope(), payload }, vigila);
  const retry = receive(first.state, { envelope: envelope({ timestamp: "2026-09-26T09:06:00.000Z" }), payload }, vigila);
  assert.equal(retry.result.replayed, true);
  assert.equal(Object.keys(retry.state.records).length, 1);
});

test("reusing an operationId for a different request is refused, not merged", () => {
  const first = receive(emptyState(), { envelope: envelope(), payload }, vigila);
  const other = receive(first.state, { envelope: envelope(), payload: { ...payload, title: "Something else" } }, vigila);
  assert.equal(other.ok, false);
  assert.equal(other.error.code, "operation-conflict");
});

test("appending the identical contribution for the invoking execution is a no-op merge", () => {
  const key = "EXE-20260926T090500000Z-bbbb0005";
  const carried = { ...blockBy("EXE-A1", A), contributions: { ...blockBy("EXE-A1", A).contributions, [key]: { operations: ["transformed"], at: "2026-09-26T09:05:00.000Z", actor: B } } };
  const received = receive(emptyState(), { envelope: envelope({ provenance: carried }), payload }, update);
  assert.ok(received.ok, JSON.stringify(received.error));
  assert.deepEqual(received.result.record.invoker, { recorded: true, key, operations: ["transformed"], changed: false });
  assert.deepEqual(received.result.record.provenance.block, carried);
});

test("update of a record without history: the invoker is recorded as created", () => {
  for (const provenance of [undefined, emptyBlock(), { contributions: {} }]) {
    const received = receive(emptyState(), { envelope: envelope(provenance === undefined ? {} : { provenance }), payload }, update);
    assert.ok(received.ok, JSON.stringify(received.error));
    assert.deepEqual(received.result.record.invoker.operations, ["created"]);
    assert.deepEqual(originator(received.result.record.provenance.block).actor, B);
  }
});

test("update of an existing record: when an originator exists the invoker is recorded as transformed, never as author, and the creator is unchanged", () => {
  const carried = blockBy("EXE-A1", A);
  const received = receive(emptyState(), { envelope: envelope({ actor: CI, execution: "EXT-github-actions.run-9", provenance: carried }), payload }, update);
  assert.ok(received.ok, JSON.stringify(received.error));
  const { record } = received.result;
  assert.deepEqual(preservationViolations(carried, record.provenance.block), []);
  assert.deepEqual(originator(record.provenance.block), { key: "EXE-A1", ...carried.contributions["EXE-A1"] });
  assert.deepEqual(record.provenance.block.contributions["EXT-github-actions.run-9"], { operations: ["transformed"], at: "2026-09-26T09:05:00.000Z", actor: CI });
  assert.deepEqual(record.invoker, { recorded: true, key: "EXT-github-actions.run-9", operations: ["transformed"], changed: true });
  assert.deepEqual(record.invokedBy, { actor: CI, key: "EXT-github-actions.run-9" });
  assert.deepEqual(withRole(record.provenance.block, "created").map((entry) => entry.key), ["EXE-A1"]);
});

test("an invoker without an execution transforming an existing payload is keyed EXT-op.<operationId>", () => {
  const carried = blockBy("EXE-A1", A);
  const own = envelope({ actor: CI, provenance: carried });
  delete own.execution;
  const received = receive(emptyState(), { envelope: own, payload }, update);
  assert.deepEqual(received.result.record.provenance.block.contributions["EXT-op.op-1"].operations, ["transformed"]);
  assert.deepEqual(preservationViolations(carried, received.result.record.provenance.block), []);
});

test("earlier non-authorship contributions without an originator: the invoker is transformed, not a late creator", () => {
  const carried = { schema: "praxis.provenance/1", contributions: { "EXE-A1": { operations: ["discovered"], at: "2026-09-26T08:00:00.000Z", actor: A } } };
  const received = receive(emptyState(), { envelope: envelope({ provenance: carried }), payload }, update);
  assert.ok(received.ok, JSON.stringify(received.error));
  assert.deepEqual(received.result.record.invoker.operations, ["transformed"]);
  assert.equal(originator(received.result.record.provenance.block), undefined, "no authorship is invented");
});

test("replay stays idempotent when the invoker is recorded as transformed", () => {
  const request = { envelope: envelope({ actor: CI, execution: "EXT-github-actions.run-9", provenance: blockBy("EXE-A1", A) }), payload };
  const first = receive(emptyState(), request, { ...update, recordsTransformation: true });
  const second = receive(first.state, request, { ...update, recordsTransformation: true });
  const retried = receive(second.state, { ...request, envelope: { ...request.envelope, timestamp: "2026-09-26T09:07:00.000Z" } }, { ...update, recordsTransformation: true });
  assert.ok(first.ok && second.ok && retried.ok);
  assert.equal(second.result.replayed, true);
  assert.equal(retried.result.replayed, true);
  assert.equal(retried.state, first.state);
  assert.equal(Object.keys(first.result.record.provenance.block.contributions).length, 3, "creator, invoker, receiving system");
});

test("an invocation that would precede the recorded creation is rejected, never recorded out of order", () => {
  const carried = blockBy("EXE-A1", A, "2026-09-26T10:00:00.000Z");
  const received = receive(emptyState(), { envelope: envelope({ provenance: carried }), payload }, update);
  assert.equal(received.ok, false);
  assert.equal(received.error.code, "provenance-conflict");
  assert.deepEqual(received.state.records, {});
});

test("an execution key already attributed to another actor is a conflict, never re-attributed", () => {
  const carried = blockBy("EXE-20260926T090500000Z-bbbb0005", A);
  const received = receive(emptyState(), { envelope: envelope({ provenance: carried }), payload }, update);
  assert.equal(received.ok, false);
  assert.equal(received.error.code, "provenance-conflict");
});

test("an optional transformed contribution is the receiving system's automation actor, keyed EXT-<system>.<operationId>", () => {
  const received = receive(emptyState(), { envelope: envelope({ operationId: "op/7", provenance: blockBy("EXE-A1", A) }), payload }, { ...update, recordsTransformation: true });
  const block = received.result.record.provenance.block;
  assert.deepEqual(block.contributions["EXT-vigila.op_2f7"], { operations: ["transformed"], at: "2026-09-26T09:05:00.000Z", actor: systemActor("vigila") });
  assert.deepEqual(originator(block).actor, A);
  assert.deepEqual(preservationViolations(blockBy("EXE-A1", A), block), []);
});

test("an unknown actor without an execution is recorded as unknown under EXT-op.<operationId>, never guessed", () => {
  const example = readJson("examples/envelopes/v2-unknown-actor.envelope.json");
  const received = receive(emptyState(), { envelope: example, payload }, vigila);
  const origin = originator(received.result.record.provenance.block);
  assert.equal(origin.key, "EXT-op.op-followup-0004");
  assert.deepEqual(origin.actor, example.actor);
});

test("update: a relaying transport never becomes the actor and carries provenance verbatim", () => {
  const original = envelope({ provenance: blockBy("EXE-A1", A), "x-hop": 1 });
  const forwarded = relay(relay(original));
  assert.deepEqual(forwarded, original);
  assert.ok(envelopeV2(forwarded));
  const received = receive(emptyState(), { envelope: forwarded, payload }, update);
  assert.deepEqual(received.result.record.invokedBy.actor, B);
  assert.deepEqual(originator(received.result.record.provenance.block).actor, A);
});

test("a transport invoking on its own behalf still carries the upstream block unmodified", () => {
  const upstream = blockBy("EXE-A1", A);
  const own = { ...envelope({ actor: CI, execution: "EXT-github-actions.run-10" }), provenance: relay(envelope({ provenance: upstream })).provenance };
  const received = receive(emptyState(), { envelope: own, payload }, update);
  assert.deepEqual(preservationViolations(upstream, received.result.record.provenance.block), []);
  assert.deepEqual(originator(received.result.record.provenance.block).actor, A);
});

// ---- create vs update (REG-PROV-006) -------------------------------------------------

test("capability kind: *.create and *.record create new records; others update existing ones", () => {
  for (const capability of ["followup.create", "time.record", "billing.record", "invoice.create", "payment.record"]) assert.equal(capabilityKind(capability), "create", capability);
  for (const capability of ["followup.update", "followup.resolve", "time.approve", "followup.query"]) assert.equal(capabilityKind(capability), "update", capability);
});

const sourceBlock = () => ({
  schema: "praxis.provenance/1",
  contributions: {
    "EXE-20260926T090000000Z-c1c1c1c1": { operations: ["created", "discovered"], at: "2026-09-26T09:00:00.000Z", actor: { kind: "agent", id: "google/gemini-cli", provider: "google", model: "unknown", runtime: "gemini-cli" }, evidence: ["aegis:evidence/EVD-0001"] },
    "EXT-github-actions.run-777-1": { operations: ["validated"], at: "2026-09-26T09:02:00.000Z", actor: CI },
  },
  derivedFrom: ["git:commit/5e1f0c2", "dokimos:observation/OBS-2026-0001"],
  "x-aegis": { severity: "high" },
});
const sourcedPayload = { ...payload, context: { source: "aegis:finding/SF-0001" } };

test("create: the source block is stored byte-identical and never merged into the new record", () => {
  const source = sourceBlock();
  const before = JSON.stringify(source);
  const received = receive(emptyState(), { envelope: envelope({ provenance: source }), payload: sourcedPayload }, { ...vigila, recordsTransformation: true });
  assert.ok(received.ok, JSON.stringify(received.error));
  const { record } = received.result;
  assert.equal(JSON.stringify(source), before, "input not mutated");
  assert.equal(JSON.stringify(record.receivedProvenance.block), before, "source block byte-identical");
  assert.equal(record.kind, "create");
  assert.deepEqual(Object.keys(record.provenance.block.contributions).sort(), ["EXE-20260926T090500000Z-bbbb0005", "EXT-vigila.op-1"]);
  assert.equal(record.provenance.block.contributions["EXE-20260926T090000000Z-c1c1c1c1"], undefined, "the source's creator is not the new record's contributor");
});

test("create: the new record has exactly one creator, the invoker, and the receiver's own transformed", () => {
  const received = receive(emptyState(), { envelope: envelope({ provenance: sourceBlock() }), payload: sourcedPayload }, { ...vigila, recordsTransformation: true });
  const block = received.result.record.provenance.block;
  assert.deepEqual(withRole(block, "created").map((entry) => [entry.key, entry.actor]), [["EXE-20260926T090500000Z-bbbb0005", B]]);
  assert.deepEqual(withRole(block, "transformed").map((entry) => [entry.key, entry.actor]), [["EXT-vigila.op-1", systemActor("vigila")]]);
  assert.deepEqual(received.result.record.invoker, { recorded: true, key: "EXE-20260926T090500000Z-bbbb0005", operations: ["created"], changed: true });
});

test("create: lineage is the source's derivedFrom plus the source record the payload names", () => {
  const received = receive(emptyState(), { envelope: envelope({ provenance: sourceBlock() }), payload: sourcedPayload }, vigila);
  assert.deepEqual(received.result.record.provenance.block.derivedFrom, ["git:commit/5e1f0c2", "dokimos:observation/OBS-2026-0001", "aegis:finding/SF-0001"]);
  const unnamed = receive(emptyState(), { envelope: envelope({ provenance: sourceBlock() }), payload }, vigila);
  assert.deepEqual(unnamed.result.record.provenance.block.derivedFrom, ["git:commit/5e1f0c2", "dokimos:observation/OBS-2026-0001"]);
  const noBlock = receive(emptyState(), { envelope: envelope(), payload: sourcedPayload }, vigila);
  assert.deepEqual(noBlock.result.record.provenance.block.derivedFrom, ["aegis:finding/SF-0001"]);
});

test("create: a key in the source attributed to another actor is not a conflict for the new record", () => {
  const source = blockBy("EXE-20260926T090500000Z-bbbb0005", A);
  const received = receive(emptyState(), { envelope: envelope({ provenance: source }), payload }, vigila);
  assert.ok(received.ok, JSON.stringify(received.error));
  assert.deepEqual(originator(received.result.record.provenance.block).actor, B);
  assert.deepEqual(received.result.record.receivedProvenance.block, source);
});

test("create: replay is idempotent (no second record, block unchanged)", () => {
  const request = { envelope: envelope({ provenance: sourceBlock() }), payload: sourcedPayload };
  const first = receive(emptyState(), request, { ...vigila, recordsTransformation: true });
  const second = receive(first.state, request, { ...vigila, recordsTransformation: true });
  const retried = receive(second.state, { ...request, envelope: { ...request.envelope, timestamp: "2026-09-26T09:09:00.000Z" } }, { ...vigila, recordsTransformation: true });
  assert.equal(second.result.replayed, true);
  assert.equal(retried.result.replayed, true);
  assert.equal(retried.state, first.state);
  assert.equal(Object.keys(retried.state.records).length, 1);
  assert.deepEqual(retried.result.record, first.result.record);
});

test("a capability contract can declare its kind explicitly", () => {
  const asUpdate = receive(emptyState(), { envelope: envelope({ provenance: sourceBlock() }), payload }, { ...vigila, kind: "update" });
  assert.deepEqual(asUpdate.result.record.invoker.operations, ["transformed"]);
  assert.equal(asUpdate.result.record.receivedProvenance, undefined);
  const asCreate = receive(emptyState(), { envelope: envelope({ provenance: sourceBlock() }), payload }, { ...update, kind: "create" });
  assert.deepEqual(asCreate.result.record.invoker.operations, ["created"]);
});

test("the Chrona and Summa create capabilities follow the same rule", () => {
  for (const binding of [{ system: "chrona", capability: "time.record" }, { system: "summa", capability: "billing.record" }, { system: "summa", capability: "invoice.create" }]) {
    const received = receive(emptyState(), { envelope: envelope({ provenance: sourceBlock() }), payload: { hours: 1 } }, { ...binding, recordsTransformation: true });
    assert.ok(received.ok, JSON.stringify(received.error));
    const { record } = received.result;
    assert.deepEqual(withRole(record.provenance.block, "created").map((entry) => entry.actor), [B]);
    assert.ok(record.provenance.block.contributions[`EXT-${binding.system}.op-1`]);
    assert.deepEqual(record.receivedProvenance.block, sourceBlock());
  }
});

// ---- secrets and envelope structure (REG-PROV-004, REG-PROV-016) -------------------

const TOKEN = `ghp_${"a1B2c3D4e5".repeat(4)}`;

test("credentials anywhere in an envelope are rejected and nothing is stored", () => {
  const cases = [
    envelope({ actor: { ...B, id: TOKEN } }),
    envelope({ "x-auth": `Bearer ${"abcdefghijklmnopqrstuvwxyz"}` }),
    envelope({ source: { repository: { state: "known", value: TOKEN } } }),
    { ...readJson("examples/envelopes/v1-agent.envelope.json"), actor: { kind: "agent", provider: known("openai"), identity: known(TOKEN) } },
  ];
  for (const candidate of cases) {
    const received = receive(emptyState(), { envelope: candidate, payload }, vigila);
    assert.equal(received.ok, false);
    assert.equal(received.error.code, "credential-in-envelope");
    assert.deepEqual(received.state.records, {});
  }
  const inBlock = receive(emptyState(), { envelope: envelope({ provenance: { ...blockBy("EXE-A1", A), note: TOKEN } }), payload }, vigila);
  assert.equal(inBlock.error.code, "provenance-malformed");
});

test("envelope structural problems are reported by the dependency-free receiver", () => {
  assert.deepEqual(envelopeProblems(envelope()), []);
  assert.match(envelopeProblems(envelope({ invokedBy: "x" }))[0], /not an envelope v2 field/);
  assert.ok(envelopeProblems(envelope({ execution: "CTB-1" })).length > 0);
  assert.ok(envelopeProblems(envelope({ actor: { kind: "agent", id: "a" } })).length > 0);
  assert.ok(envelopeProblems({ ...envelope(), schema: "echelon.execution-envelope/v3" }).length > 0);
  assert.ok(envelopeProblems(envelope({ timestamp: "yesterday" })).length > 0);
  assert.equal(receive(emptyState(), { envelope: envelope({ invokedBy: "x" }), payload }, vigila).error.code, "envelope-invalid");
});

// ---- identity is not authority (REG-PROV-003) --------------------------------------

test("the receiver's outcome does not depend on who the actor claims to be", () => {
  const actors = [A, B, CI, { kind: "human", id: "kevin" }, { kind: "unknown", id: "unknown", provider: "unknown", model: "unknown", runtime: "unknown" }, { kind: "x-bot", id: "x" }];
  for (const actor of actors) {
    const received = receive(emptyState(), { envelope: envelope({ actor, execution: "EXT-ci.run-1" }), payload }, vigila);
    assert.ok(received.ok, JSON.stringify(received.error));
    assert.deepEqual(Object.keys(received.result.record).sort(), ["capability", "correlationId", "envelopeSchema", "fingerprint", "invokedBy", "invoker", "kind", "operationId", "payload", "provenance", "receivedProvenance"]);
  }
});

// ---- contract revision 1.1 (REG-PROV-006, REG-PROV-008, REG-PROV-017) --------------

test("finding: an unknown invoker cannot extend a carried entry held by a known actor (provenance-conflict)", () => {
  const claude = { kind: "agent", id: "anthropic/claude-code", provider: "anthropic", model: "unknown", runtime: "claude-code" };
  const unknownAgent = { kind: "agent", id: "unknown", provider: "unknown", model: "unknown", runtime: "unknown" };
  const carried = { schema: "praxis.provenance/1", contributions: { "EXT-run.7": { operations: ["created"], at: "2026-09-26T08:00:00.000Z", actor: claude } } };
  const before = JSON.stringify(carried);
  const state = emptyState();
  const received = receive(state, { envelope: envelope({ actor: unknownAgent, execution: "EXT-run.7", provenance: carried }), payload }, update);
  assert.equal(received.ok, false);
  assert.equal(received.error.code, "provenance-conflict");
  assert.match(received.error.problems.join(" "), /unknown identity/);
  assert.equal(received.state, state);
  assert.equal(JSON.stringify(carried), before);
  const unknownKind = receive(state, { envelope: envelope({ actor: { kind: "unknown", id: "anthropic/claude-code" }, execution: "EXT-run.7", provenance: carried }), payload }, update);
  assert.equal(unknownKind.error.code, "provenance-conflict");
});

test("v1 keys use the injective escaping: distinct ids never share a key", () => {
  const ids = ["op 1", "op-1", "op_1", "op_201", "op/1", "op.1", "\u00e9"];
  const keys = ids.map((operationId) => keyFromEnvelopeV1({ operationId, actor: {} }));
  assert.equal(new Set(keys).size, ids.length, keys.join(" "));
  for (const key of keys) assert.match(key, /^EXT-op\.[A-Za-z0-9._-]+$/);
  for (const id of ids) assert.equal(keyFromEnvelopeV1({ operationId: id, actor: {} }), `EXT-op.${escapeKeySegment(id)}`, "local escaping mirrors the Praxis reference");
});

const v1With = (repository, runId, operationId = "op-1") => ({
  schema: "echelon.execution-envelope/v1", operationId, correlationId: "c", timestamp: "2026-09-26T09:00:00Z",
  actor: { kind: "agent", provider: known("openai"), identity: known("openai/codex"), ...(runId === undefined ? {} : { runId: runId === null ? { state: "unknown" } : known(runId) }) },
  ...(repository === undefined ? {} : { source: { repository: repository === null ? { state: "unknown" } : known(repository) } }),
});

test("v1 runs are namespaced by a known source.repository", () => {
  assert.equal(keyFromEnvelopeV1Namespaced(v1With("kemiller2002/aegis", "7")), "EXT-run.kemiller2002_2faegis.7");
  assert.equal(keyFromEnvelopeV1Namespaced(v1With("kemiller2002/vigila", "7")), "EXT-run.kemiller2002_2fvigila.7");
  assert.notEqual(keyFromEnvelopeV1Namespaced(v1With("kemiller2002/aegis", "7")), keyFromEnvelopeV1Namespaced(v1With("kemiller2002/vigila", "7")), "same run id, different senders");
  assert.equal(keyFromEnvelopeV1Namespaced(v1With("octo/repo.js", "gh/99")), "EXT-run.octo_2frepo_2ejs.gh_2f99");
});

test("v1 runs without a known repository keep the Praxis key; unknown runs stay EXT-op", () => {
  for (const repository of [undefined, null]) {
    assert.equal(keyFromEnvelopeV1Namespaced(v1With(repository, "gh/99")), "EXT-run.gh_2f99");
    assert.equal(keyFromEnvelopeV1Namespaced(v1With(repository, "gh/99")), keyFromEnvelopeV1(v1With(repository, "gh/99")));
  }
  assert.equal(keyFromEnvelopeV1Namespaced(v1With("kemiller2002/aegis", null, "op 1")), "EXT-op.op_201");
  assert.equal(keyFromEnvelopeV1Namespaced(v1With("kemiller2002/aegis", undefined)), "EXT-op.op-1");
});

test("namespaced v1 run keys are injective across repositories containing dots", () => {
  const pairs = [["a/b.c", "5"], ["a/b", "c.5"], ["a.b/c", "5"], ["a/b_2ec", "5"]];
  const keys = pairs.map(([repository, run]) => keyFromEnvelopeV1Namespaced(v1With(repository, run)));
  assert.equal(new Set(keys).size, pairs.length, keys.join(" "));
  for (const key of keys) assert.match(key, /^EXT-run\.[A-Za-z0-9._-]+$/);
});

test("received v1 runs from two repositories with the same run id stay separate executions", () => {
  const first = receive(emptyState(), { envelope: v1With("kemiller2002/aegis", "7", "op-a"), payload }, update);
  const second = receive(first.state, { envelope: v1With("kemiller2002/vigila", "7", "op-b"), payload }, update);
  assert.equal(first.result.record.invokedBy.key, "EXT-run.kemiller2002_2faegis.7");
  assert.equal(second.result.record.invokedBy.key, "EXT-run.kemiller2002_2fvigila.7");
  const upgraded = upgradeEnvelopeV1(v1With("kemiller2002/aegis", "7"));
  assert.equal(upgraded.execution, "EXT-run.kemiller2002_2faegis.7");
  assert.ok(envelopeV2(upgraded), ajv.errorsText(envelopeV2.errors));
});

test("exact matching and null: an execution with a trailing newline or null is rejected", () => {
  assert.equal(receive(emptyState(), { envelope: envelope({ execution: "EXE-1\n" }), payload }, update).error.code, "envelope-invalid");
  assert.equal(receive(emptyState(), { envelope: envelope({ execution: null }), payload }, update).error.code, "envelope-invalid");
  assert.equal(envelopeV2(envelope({ execution: "EXE-1\n" })), false);
  assert.equal(receive(emptyState(), { envelope: envelope({ provenance: { schema: null, contributions: {} } }), payload }, vigila).error.code, "provenance-malformed");
});

test("identity environment: the vendored list is the one identityEnvironment strips", () => {
  const pinned = readJson("tests/fixtures/praxis-provenance/identity-environment.json").variables;
  assert.deepEqual([...IDENTITY_ENVIRONMENT_VARIABLES].sort(), [...pinned].sort());
  for (const name of ["ROS_ACTOR", "ROS_ACTOR_KIND", "ROS_EXECUTION_ID", "CLAUDE_CODE_SESSION_ID", "GITHUB_RUN_ID", "OLLAMA_HOST"]) assert.ok(pinned.includes(name), name);
});

test("identity environment: dispatching for another actor removes every inherited identity variable", () => {
  const inherited = { PATH: "/usr/bin", HOME: "/home/x", ...Object.fromEntries(readJson("tests/fixtures/praxis-provenance/identity-environment.json").variables.map((name) => [name, `parent-${name}`])) };
  const child = identityEnvironment(inherited, { ROS_ACTOR_KIND: "agent", ROS_ACTOR: "openai/codex", ROS_TELEMETRY_PROVIDER: "openai", ROS_TELEMETRY_MODEL: undefined, ROS_EXECUTION_ID: "" });
  assert.deepEqual(child, { PATH: "/usr/bin", HOME: "/home/x", ROS_ACTOR_KIND: "agent", ROS_ACTOR: "openai/codex", ROS_TELEMETRY_PROVIDER: "openai" });
  assert.ok(!Object.values(child).some((value) => String(value).startsWith("parent-")), "no parent identity, session, or run leaks");
  assert.equal(inherited.ROS_ACTOR, "parent-ROS_ACTOR", "input not mutated");
});

// ---- the Praxis end-to-end chain carried through registry envelopes (REG-PROV-015) --

const replayChain = () => {
  const chain = echelonChain();
  return chain.steps.reduce((acc, step, index) => {
    const current = acc.blocks[step.record] ?? emptyBlock();
    if (step.lineage) return { ...acc, blocks: { ...acc.blocks, [step.record]: addLineage(current, step.lineage) } };
    const { key, contribution } = step.append;
    const carried = relay({
      schema: "echelon.execution-envelope/v2",
      operationId: `chain-${index}`,
      correlationId: "chain",
      timestamp: contribution.at,
      actor: contribution.actor,
      execution: key.startsWith("CTB-") ? undefined : key,
      provenance: current,
    });
    const env = Object.fromEntries(Object.entries(carried).filter(([, value]) => value !== undefined));
    const received = receive(acc.state, { envelope: env, payload: { record: step.record } }, { system: "vigila", capability: "followup.update", operations: () => contribution.operations });
    assert.ok(received.ok, JSON.stringify(received.error));
    const block = received.result.record.provenance.block;
    const expectedKey = key.startsWith("CTB-") ? `EXT-op.chain-${index}` : key;
    assert.ok(block.contributions[expectedKey], `step ${index} recorded under ${expectedKey}`);
    return { state: received.state, blocks: { ...acc.blocks, [step.record]: block } };
  }, { state: emptyState(), blocks: {} });
};

test("echelon-chain: every record keeps its own originator when carried through v2 envelopes", () => {
  const chain = echelonChain();
  const { blocks } = replayChain();
  for (const [record, expected] of Object.entries(chain.expect.originators)) {
    const origin = originator(blocks[record]);
    assert.equal(origin.actor.id, expected.actorId, record);
    if (!expected.key.startsWith("CTB-")) assert.equal(origin.key, expected.key, record);
  }
  const originators = new Set(Object.values(blocks).map((block) => originator(block)?.key).filter(Boolean));
  assert.equal(originators.size, chain.expect.chainOriginatorCount);
});

test("echelon-chain: roles and lineage survive transport", () => {
  const chain = echelonChain();
  const { blocks } = replayChain();
  for (const [record, roles] of Object.entries(chain.expect.roles)) {
    for (const [operation, keys] of Object.entries(roles)) {
      const actual = withRole(blocks[record], operation).map((entry) => entry.key);
      assert.deepEqual(actual, keys.map((key) => (key.startsWith("CTB-") ? actual.find((k) => k.startsWith("EXT-op.")) : key)), `${record} ${operation}`);
    }
  }
  const reach = (record, seen = new Set()) => (blocks[record]?.derivedFrom ?? []).reduce((acc, ref) => (acc.has(ref) ? acc : reach(ref, new Set([...acc, ref]))), seen);
  assert.deepEqual([...reach(chain.expect.lineageFrom)].sort(), [...chain.expect.lineageReaches].sort());
});

test("echelon-chain: replaying the whole chain twice is idempotent", () => {
  assert.deepEqual(replayChain(), replayChain());
});

// ---- optional providers stay optional (REG-PROV-014, conformance/README.md) --------

const resolve = (registry, capability, contractVersion, reachable) => {
  if (registry === undefined) return { state: "unavailable" };
  const entry = registry.systems.find((system) => system.provides.some((cap) => cap.id === capability));
  if (entry === undefined) return { state: "unavailable" };
  if (!entry.provides.some((cap) => cap.id === capability && cap.contractVersion === contractVersion)) return { state: "misconfigured", diagnostic: "incompatible contract" };
  if (!reachable.has(entry.id)) return { state: "misconfigured", diagnostic: "declared but inaccessible" };
  return { state: "available", provider: entry.id, provenance: provenanceExpectation(entry.provenance) };
};

/** A caller's core work plus optional follow-up capture; core never depends on the integration. */
const runCaller = (registry, reachable) => {
  const core = "PASS";
  const resolution = resolve(registry, "followup.create", 1, reachable);
  if (resolution.state !== "available") return { core, integration: resolution.state, provenance: "not-sent" };
  const received = receive(emptyState(), { envelope: envelope({ provenance: blockBy("EXE-A1", A) }), payload }, { system: resolution.provider, capability: "followup.create" });
  return { core, integration: received.ok ? "available" : "misconfigured", provenance: resolution.provenance };
};

const v1Registry = readJson("registry/systems.json");
const v2Registry = { schema: "echelon.registry/v2", systems: [readJson("examples/vigila.v2.system.json")].map(({ schema, ...entry }) => entry) };

test("the v2 registry index used by the scenarios is valid", () => {
  const validate = validator(ajv, "echelon.registry/v2");
  assert.ok(validate(v2Registry), ajv.errorsText(validate.errors));
});

const scenarios = [
  ["component alone", undefined, [], "unavailable", "not-sent"],
  ["component + empty registry", { schema: "echelon.registry/v1", systems: [] }, [], "unavailable", "not-sent"],
  ["provider not installed", { schema: "echelon.registry/v1", systems: v1Registry.systems.filter((s) => s.id !== "vigila") }, ["praxis"], "unavailable", "not-sent"],
  ["provider declared but inaccessible", v1Registry, [], "misconfigured", "not-sent"],
  ["provider contract incompatible", { schema: "echelon.registry/v1", systems: [{ id: "vigila", repository: "kemiller2002/vigila", provides: [{ id: "followup.create", contractVersion: 2 }] }] }, ["vigila"], "misconfigured", "not-sent"],
  ["compatible provider, provenance undeclared (v1 registry)", v1Registry, ["vigila"], "available", "undeclared"],
  ["compatible provider declaring praxis.provenance/1 (v2 registry)", v2Registry, ["vigila"], "available", "preserved"],
  ["full supported ecosystem", v1Registry, ["praxis", "vigila", "chrona", "summa"], "available", "undeclared"],
];

for (const [name, registry, reachable, integration, provenance] of scenarios) {
  test(`conformance: ${name} -> core PASS, integration ${integration}, provenance ${provenance}`, () => {
    const result = runCaller(registry, new Set(reachable));
    assert.deepEqual(result, { core: "PASS", integration, provenance });
  });
}

test("provenance expectations from descriptors", () => {
  const d = readJson("examples/vigila.v2.system.json").provenance;
  assert.equal(provenanceExpectation(undefined), "undeclared");
  assert.equal(provenanceExpectation(d), "preserved");
  assert.equal(provenanceExpectation(d, "praxis.provenance/2"), "carried-verbatim");
  assert.equal(provenanceExpectation({ ...d, unknownFields: "discard" }), "lossy");
  assert.equal(provenanceExpectation({ ...d, propagation: { ...d.propagation, lineage: false } }), "lossy");
});
