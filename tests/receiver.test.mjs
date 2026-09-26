// REG-PROV-005..REG-PROV-010, REG-PROV-014, REG-PROV-016: the reference receiver in
// lib/echelon-provenance.mjs classifies carried provenance, appends the invoking actor's
// contribution idempotently, never re-attributes or overwrites, maps v1 losslessly,
// rejects credentials, and treats identity as provenance only.
import test from "node:test";
import assert from "node:assert/strict";
import { createAjv, readJson, validator, praxisCases, echelonChain } from "./support.mjs";
import {
  receive, emptyState, invocation, upgradeEnvelopeV1, relay, envelopeProblems, provenanceExpectation,
  systemActor, V1_ACTOR_EXTENSION,
} from "../lib/echelon-provenance.mjs";
import {
  actorFromEnvelopeV1, keyFromEnvelopeV1, preservationViolations, originator, withRole, addLineage, emptyBlock,
} from "../lib/vendor/praxis/provenance-interchange.mjs";

const ajv = createAjv();
const envelopeV2 = validator(ajv, "echelon.execution-envelope/v2");
const followupContract = ajv.getSchema("https://echelonfoundry.com/contracts/followup.create.v1.json");
const vigila = { system: "vigila", capability: "followup.create" };
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
  assert.equal(keyFromEnvelopeV1({ operationId: "op-1", actor: { runId: known("gh/99") } }), "EXT-run.gh-99");
  assert.equal(keyFromEnvelopeV1({ operationId: "op 1", actor: { runId: { state: "unknown" } } }), "EXT-op.op-1");
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
  assert.equal(upgradeEnvelopeV1(readJson("examples/envelopes/v1-agent.envelope.json")).execution, "EXT-run.gh-99");
});

test("a v1 envelope is received: the mapped actor originates the follow-up under its EXT-run key", () => {
  const v1 = readJson("examples/envelopes/v1-agent.envelope.json");
  const received = receive(emptyState(), { envelope: v1, payload }, vigila);
  assert.ok(received.ok, JSON.stringify(received.error));
  const { record } = received.result;
  assert.equal(record.envelopeSchema, "echelon.execution-envelope/v1");
  assert.deepEqual(originator(record.provenance.block).actor, { kind: "agent", id: "openai/codex", provider: "openai", model: "unknown", runtime: "unknown" });
  assert.equal(originator(record.provenance.block).key, "EXT-run.gh-99");
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
  test(`receiver keeps supported block '${item.name}' intact (${item.warnings} warnings)`, () => {
    const received = receive(emptyState(), { envelope: envelope({ provenance: item.block }), payload }, vigila);
    assert.ok(received.ok, JSON.stringify(received.error));
    const { record } = received.result;
    assert.equal(record.provenance.verdict, "supported");
    assert.equal(record.provenance.warnings.length, item.warnings);
    assert.deepEqual(preservationViolations(item.block, record.provenance.block), []);
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

test("an unsupported major is stored verbatim; no contribution is merged into it", () => {
  for (const block of [readJson("examples/envelopes/v2-unsupported-major.envelope.json").provenance,
    ...praxisCases().filter((c) => c.expect === "unsupported").map((c) => c.block)]) {
    const received = receive(emptyState(), { envelope: envelope({ provenance: block }), payload }, { ...vigila, recordsTransformation: true });
    assert.ok(received.ok, JSON.stringify(received.error));
    const { record } = received.result;
    assert.equal(record.provenance.verdict, "unsupported");
    assert.deepEqual(record.provenance.block, block);
    assert.deepEqual(record.invoker, { recorded: false, reason: "unsupported-major" });
    assert.deepEqual(preservationViolations(block, record.provenance.block), []);
  }
});

test("no silent stripping: an absent block yields a record whose provenance names the invoker", () => {
  const received = receive(emptyState(), { envelope: envelope(), payload }, vigila);
  assert.equal(received.result.record.provenance.verdict, "absent");
  assert.deepEqual(originator(received.result.record.provenance.block).actor, B);
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

test("appending the identical contribution for the invoking execution is idempotent", () => {
  const key = "EXE-20260926T090500000Z-bbbb0005";
  const carried = blockBy(key, B, "2026-09-26T09:05:00.000Z");
  const received = receive(emptyState(), { envelope: envelope({ provenance: carried }), payload }, vigila);
  assert.deepEqual(received.result.record.invoker, { recorded: true, key, changed: false });
  assert.deepEqual(received.result.record.provenance.block, carried);
});

test("the original actor is never overwritten by a different invoking actor", () => {
  const carried = blockBy("EXE-A1", A);
  const received = receive(emptyState(), { envelope: envelope({ actor: CI, execution: "EXT-github-actions.run-9", provenance: carried }), payload }, vigila);
  assert.ok(received.ok, JSON.stringify(received.error));
  const { record } = received.result;
  assert.deepEqual(preservationViolations(carried, record.provenance.block), []);
  assert.deepEqual(originator(record.provenance.block).actor, A);
  assert.equal(record.invoker.recorded, false, "a second 'created' is never recorded");
  assert.match(record.invoker.reason, /originator/);
  assert.deepEqual(record.invokedBy, { actor: CI, key: "EXT-github-actions.run-9" }, "the invoker is still reported, separately");
});

test("an execution key already attributed to another actor is a conflict, never re-attributed", () => {
  const carried = blockBy("EXE-20260926T090500000Z-bbbb0005", A);
  const received = receive(emptyState(), { envelope: envelope({ provenance: carried }), payload }, vigila);
  assert.equal(received.ok, false);
  assert.equal(received.error.code, "provenance-conflict");
});

test("an optional transformed contribution is the receiving system's automation actor, keyed EXT-<system>.<operationId>", () => {
  const received = receive(emptyState(), { envelope: envelope({ operationId: "op/7", provenance: blockBy("EXE-A1", A) }), payload }, { ...vigila, recordsTransformation: true });
  const block = received.result.record.provenance.block;
  assert.deepEqual(block.contributions["EXT-vigila.op-7"], { operations: ["transformed"], at: "2026-09-26T09:05:00.000Z", actor: systemActor("vigila") });
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

test("a relaying transport never becomes the actor and carries provenance verbatim", () => {
  const original = envelope({ provenance: blockBy("EXE-A1", A), "x-hop": 1 });
  const forwarded = relay(relay(original));
  assert.deepEqual(forwarded, original);
  assert.ok(envelopeV2(forwarded));
  const received = receive(emptyState(), { envelope: forwarded, payload }, vigila);
  assert.deepEqual(received.result.record.invokedBy.actor, B);
  assert.deepEqual(originator(received.result.record.provenance.block).actor, A);
});

test("a transport invoking on its own behalf still carries the upstream block unmodified", () => {
  const upstream = blockBy("EXE-A1", A);
  const own = { ...envelope({ actor: CI, execution: "EXT-github-actions.run-10" }), provenance: relay(envelope({ provenance: upstream })).provenance };
  const received = receive(emptyState(), { envelope: own, payload }, vigila);
  assert.deepEqual(preservationViolations(upstream, received.result.record.provenance.block), []);
  assert.deepEqual(originator(received.result.record.provenance.block).actor, A);
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
    assert.deepEqual(Object.keys(received.result.record).sort(), ["capability", "correlationId", "envelopeSchema", "fingerprint", "invokedBy", "invoker", "operationId", "payload", "provenance"]);
  }
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
    const received = receive(acc.state, { envelope: env, payload: { record: step.record } }, { system: "vigila", capability: "followup.create", operations: () => contribution.operations });
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
