// REG-PROV-004, REG-PROV-007, REG-PROV-008, REG-PROV-011..REG-PROV-013, REG-PROV-016:
// every schema compiles, every example validates against the schema version it declares,
// v1 stays valid, and v2 envelopes carry Praxis blocks structurally.
import test from "node:test";
import assert from "node:assert/strict";
import { createAjv, schemaFiles, readJson, listJson, validator, praxisCases } from "./support.mjs";

const ajv = createAjv();
const envelopeV1 = validator(ajv, "echelon.execution-envelope/v1");
const envelopeV2 = validator(ajv, "echelon.execution-envelope/v2");
const manifestV1 = validator(ajv, "echelon.system/v1");
const manifestV2 = validator(ajv, "echelon.system/v2");
const registryV1 = validator(ajv, "echelon.registry/v1");
const registryV2 = validator(ajv, "echelon.registry/v2");
const errors = (validate) => ajv.errorsText(validate.errors);

for (const file of schemaFiles) {
  test(`schema compiles: ${file}`, () => {
    const schema = readJson(file);
    assert.equal(typeof ajv.getSchema(schema.$id), "function");
  });
}

test("the v1 schemas are unchanged in identity and remain closed", () => {
  assert.equal(readJson("schemas/execution-envelope.schema.json").$id, "https://echelonfoundry.com/schemas/echelon.execution-envelope.v1.json");
  assert.equal(readJson("schemas/system-manifest.schema.json").$id, "https://echelonfoundry.com/schemas/echelon.system.v1.json");
  assert.equal(readJson("contracts/followup-create.v1.schema.json").additionalProperties, false);
});

const examples = [...listJson("examples/"), ...listJson("examples/envelopes/"), "registry/systems.json"];
for (const file of examples) {
  test(`example validates against its declared schema: ${file}`, () => {
    const document = readJson(file);
    const validate = validator(ajv, document.schema);
    assert.equal(typeof validate, "function", `no schema for ${document.schema}`);
    assert.ok(validate(document), errors(validate));
  });
}

test("v1 manifest examples still validate against manifest v1, and v2 ones are rejected by v1", () => {
  for (const name of ["chrona", "praxis", "summa", "vigila"]) {
    const document = readJson(`examples/${name}.system.json`);
    assert.equal(document.schema, "echelon.system/v1");
    assert.ok(manifestV1(document), errors(manifestV1));
  }
  assert.equal(manifestV1(readJson("examples/praxis.v2.system.json")), false);
});

test("v1 envelope examples validate against v1 and are not v2 envelopes", () => {
  for (const file of ["examples/envelopes/v1-agent.envelope.json", "examples/envelopes/v1-human.envelope.json"]) {
    const document = readJson(file);
    assert.ok(envelopeV1(document), errors(envelopeV1));
    assert.equal(envelopeV2(document), false);
  }
});

const v2 = (provenance, extra = {}) => ({
  schema: "echelon.execution-envelope/v2",
  operationId: "op-1",
  correlationId: "corr-1",
  timestamp: "2026-09-26T09:00:00.000Z",
  actor: { kind: "agent", id: "anthropic/claude-code", provider: "anthropic", model: "unknown", runtime: "claude-code" },
  execution: "EXE-20260926T090000000Z-dddd0001",
  ...(provenance === undefined ? {} : { provenance }),
  ...extra,
});

for (const item of praxisCases().filter((c) => c.expect === "supported")) {
  test(`v2 envelope carrying supported block '${item.name}' validates`, () => {
    assert.ok(envelopeV2(v2(item.block)), errors(envelopeV2));
  });
}

test("v2 envelope carrying an unsupported major validates structurally (carried verbatim)", () => {
  for (const item of praxisCases().filter((c) => c.expect === "unsupported")) {
    assert.ok(envelopeV2(v2(item.block)), errors(envelopeV2));
  }
});

test("a provenance value that is not a Praxis block of any major is rejected by the envelope schema", () => {
  assert.equal(envelopeV2(v2({ schema: "echelon.execution-envelope/v1", contributions: {} })), false);
  assert.equal(envelopeV2(v2({ schema: "praxis.provenance/1.1", contributions: {} })), false);
  assert.equal(envelopeV2(v2(["not", "an", "object"])), false);
  assert.equal(envelopeV2(v2({ schema: "praxis.provenance/1", contributions: [] })), false);
});

test("envelope v2 rejects extra non-x- properties and accepts x- extensions", () => {
  assert.equal(envelopeV2(v2(undefined, { invokedBy: "someone" })), false);
  assert.equal(envelopeV2(v2(undefined, { "X-Upper": 1 })), false);
  assert.ok(envelopeV2(v2(undefined, { "x-trace": { span: "1" } })), errors(envelopeV2));
});

test("envelope v2 requires a Praxis actor, not a v1 knownValue actor", () => {
  const v1Actor = { kind: "agent", provider: { state: "known", value: "openai" }, identity: { state: "known", value: "openai/codex" } };
  assert.equal(envelopeV2({ ...v2(), actor: v1Actor }), false);
  assert.equal(envelopeV2({ ...v2(), actor: { kind: "agent", id: "openai/codex" } }), false, "agents need provider/model/runtime");
  assert.ok(envelopeV2({ ...v2(), actor: { kind: "human", id: "kevin" } }), errors(envelopeV2));
});

test("envelope v2 execution must be an EXE- or EXT- execution key, never CTB-", () => {
  assert.equal(envelopeV2({ ...v2(), execution: "CTB-20260926-5f2e19aa" }), false);
  assert.equal(envelopeV2({ ...v2(), execution: "EXT-Dokimos.run" }), false);
  assert.ok(envelopeV2({ ...v2(), execution: "EXT-dokimos.snapshot-1" }), errors(envelopeV2));
  const withoutExecution = v2();
  delete withoutExecution.execution;
  assert.ok(envelopeV2(withoutExecution), "execution is optional (unknown)");
});

const descriptor = {
  interchange: ["praxis.provenance/1"],
  roles: ["consumer"],
  propagation: { actor: true, execution: true, contributions: true, lineage: true },
  unknownFields: "preserve",
};
const manifest = (extra) => ({ ...readJson("examples/vigila.v2.system.json"), ...extra });

test("manifest v2 provenance descriptor: valid forms", () => {
  assert.ok(manifestV2(manifest({ provenance: descriptor })), errors(manifestV2));
  assert.ok(manifestV2(manifest({ provenance: { ...descriptor, roles: ["producer", "consumer", "transport"], recordsTransformation: true, envelopes: ["echelon.execution-envelope/v1", "echelon.execution-envelope/v2"], "x-note": "ok" } })), errors(manifestV2));
  const undeclared = manifest({});
  delete undeclared.provenance;
  assert.ok(manifestV2(undeclared), "the descriptor is optional (undeclared)");
});

test("manifest v2 provenance descriptor: invalid forms", () => {
  const bad = [
    { ...descriptor, interchange: [] },
    { ...descriptor, interchange: ["praxis.provenance/1.2"] },
    { ...descriptor, roles: ["router"] },
    { ...descriptor, roles: [] },
    { ...descriptor, unknownFields: "drop" },
    { ...descriptor, propagation: { actor: true, execution: true, contributions: true } },
    { ...descriptor, propagation: { ...descriptor.propagation, lineage: "yes" } },
    { ...descriptor, envelopes: ["echelon.execution-envelope/v3"] },
    { ...descriptor, identityModel: "local" },
    (({ unknownFields, ...rest }) => rest)(descriptor),
  ];
  for (const provenance of bad) assert.equal(manifestV2(manifest({ provenance })), false, JSON.stringify(provenance));
});

test("the reserved pseudo-systems run and op can never be registered", () => {
  for (const id of ["run", "op"]) {
    assert.equal(manifestV2(manifest({ id })), false, id);
    assert.equal(registryV2({ schema: "echelon.registry/v2", systems: [{ id, repository: "a/b", provides: [] }] }), false, id);
  }
  assert.equal(manifestV2(manifest({ id: "vigila.core" })), false, "registry ids carry no dots (EXT-<system>.<run-id>)");
});

test("registry index v1 (published file) and v2 validate; v1 entries cannot declare provenance", () => {
  const published = readJson("registry/systems.json");
  assert.ok(registryV1(published), errors(registryV1));
  const entry = { id: "vigila", repository: "kemiller2002/vigila", provides: [{ id: "followup.create", contractVersion: 1 }] };
  assert.equal(registryV1({ schema: "echelon.registry/v1", systems: [{ ...entry, provenance: descriptor }] }), false);
  assert.ok(registryV2({ schema: "echelon.registry/v2", systems: [{ ...entry, provenance: descriptor }, { ...entry, id: "chrona" }] }), errors(registryV2));
});
