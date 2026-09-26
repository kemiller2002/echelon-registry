// Echelon Registry conformance runner (conformance/README.md).
// Usage: npm test   (or: node conformance/run.mjs)
import { createHash } from "node:crypto";
import { readFileSync, readdirSync, statSync } from "node:fs";
import { dirname, join, relative } from "node:path";
import { fileURLToPath } from "node:url";
import Ajv2020 from "ajv/dist/2020.js";
import addFormats from "ajv-formats";
import { envelopeV1ToV2, envelopeV2ToV1 } from "./lib/envelope.mjs";
import { coreHealth, envelopeFor, resolve } from "./lib/resolution.mjs";

const root = join(dirname(fileURLToPath(import.meta.url)), "..");
const at = (...parts) => join(root, ...parts);
const readJson = (path) => JSON.parse(readFileSync(path, "utf8"));
const listFiles = (dir) =>
  readdirSync(dir)
    .sort()
    .flatMap((name) => (statSync(join(dir, name)).isDirectory() ? listFiles(join(dir, name)) : [join(dir, name)]));
const jsonFiles = (dir) => listFiles(dir).filter((path) => path.endsWith(".json"));
const rel = (path) => relative(root, path);

// ---- schemas -------------------------------------------------------------
const ID = {
  envelopeV1: "https://echelonfoundry.com/schemas/echelon.execution-envelope.v1.json",
  envelopeV2: "https://echelonfoundry.com/schemas/echelon.execution-envelope.v2.json",
  systemV1: "https://echelonfoundry.com/schemas/echelon.system.v1.json",
  systemV2: "https://echelonfoundry.com/schemas/echelon.system.v2.json",
  registry: "https://echelonfoundry.com/schemas/echelon.registry.v1.json",
  agentIdentity: "https://echelonfoundry.com/contracts/agent.identity.v1.json",
  followupCreate: "https://echelonfoundry.com/contracts/followup.create.v1.json",
  record: "https://github.com/kemiller2002/praxis/schemas/provenance-record.schema.json",
  artifactProvenance: "https://github.com/kemiller2002/praxis/schemas/artifact-provenance.schema.json",
  actor: "https://github.com/kemiller2002/praxis/schemas/provenance-actor.schema.json",
};
const BY_DISCRIMINATOR = {
  "echelon.execution-envelope/v1": ID.envelopeV1,
  "echelon.execution-envelope/v2": ID.envelopeV2,
  "echelon.system/v1": ID.systemV1,
  "echelon.system/v2": ID.systemV2,
  "echelon.registry/v1": ID.registry,
};

const schemaFiles = [...jsonFiles(at("schemas")), ...jsonFiles(at("contracts"))].filter(
  (path) => !path.endsWith("SOURCE.json"),
);
const schemas = schemaFiles.map((path) => ({ path, schema: readJson(path) }));
const ajv = new Ajv2020({ allErrors: true, strict: true, strictTypes: false, strictRequired: false });
// strictRequired is off only because the vendored Praxis actor schema uses the
// valid 2020-12 idiom `then: { required: [...] }` without redeclaring properties.
addFormats(ajv);
schemas.forEach(({ schema }) => ajv.addSchema(schema));
const validator = (id) => ajv.getSchema(id);
const isValid = (id, value) => validator(id)(value) === true;
const errorsOf = (id, value) => (validator(id)(value) ? "" : ajv.errorsText(validator(id).errors));

// ---- tiny test harness -----------------------------------------------------
const results = [];
const check = (name, predicate, detail = () => "") => {
  const outcome = (() => {
    try {
      return predicate() ? { ok: true } : { ok: false, detail: detail() };
    } catch (error) {
      return { ok: false, detail: String(error?.message ?? error) };
    }
  })();
  results.push({ name, ...outcome });
};

const canonical = (value) =>
  Array.isArray(value)
    ? value.map(canonical)
    : value !== null && typeof value === "object"
      ? Object.fromEntries(Object.keys(value).sort().map((key) => [key, canonical(value[key])]))
      : value;
const deepEqual = (a, b) => JSON.stringify(canonical(a)) === JSON.stringify(canonical(b));

// ---- 1. schemas compile and their own examples validate ---------------------
schemas.forEach(({ path, schema }) =>
  check(`schema compiles: ${rel(path)}`, () => typeof validator(schema.$id) === "function"),
);
schemas.forEach(({ path, schema }) =>
  (schema.examples ?? []).forEach((example, index) =>
    check(`schema example ${index} validates: ${rel(path)}`, () => isValid(schema.$id, example), () =>
      errorsOf(schema.$id, example),
    ),
  ),
);

// ---- 2. vendored Praxis files match SOURCE.json digests ---------------------
const verifyVendored = (dir) => {
  const source = readJson(join(dir, "SOURCE.json"));
  const present = listFiles(dir).map((path) => relative(dir, path)).filter((path) => path !== "SOURCE.json");
  check(`${rel(dir)}: SOURCE.json pins kemiller2002/praxis@${source.commit}`, () =>
    source.repository === "kemiller2002/praxis" && /^[0-9a-f]{7,40}$/.test(source.commit),
  );
  check(`${rel(dir)}: every vendored file is listed and every listed file exists`, () =>
    deepEqual([...present].sort(), Object.keys(source.files).sort()),
  );
  Object.entries(source.files).forEach(([file, digest]) =>
    check(`${rel(dir)}: sha256 ${file}`, () =>
      createHash("sha256").update(readFileSync(join(dir, file))).digest("hex") === digest,
    ),
  );
};
verifyVendored(at("schemas", "vendor", "praxis"));
verifyVendored(at("conformance", "praxis-provenance-record"));

// ---- 3. examples and registry ------------------------------------------------
jsonFiles(at("examples")).forEach((path) => {
  const document = readJson(path);
  const id = BY_DISCRIMINATOR[document.schema];
  check(`example validates as ${document.schema}: ${rel(path)}`, () => id !== undefined && isValid(id, document), () =>
    id === undefined ? "unknown schema discriminator" : errorsOf(id, document),
  );
});
const registry = readJson(at("registry", "systems.json"));
check("registry/systems.json validates as echelon.registry/v1", () => isValid(ID.registry, registry), () =>
  errorsOf(ID.registry, registry),
);
check("registry: every provided capability with a contract file in contracts/ is known", () => {
  const contractIds = schemas.filter(({ path }) => path.includes("/contracts/")).map(({ schema }) => schema.$id);
  return ["agent.identity", "followup.create"].every((capability) =>
    contractIds.some((id) => id.includes(`/contracts/${capability}.v1.json`)),
  );
});

// ---- 4. envelope and contract fixtures ---------------------------------------
const fixtureSets = [
  ["execution-envelope/v1", ID.envelopeV1],
  ["execution-envelope/v2", ID.envelopeV2],
  ["agent-identity", ID.agentIdentity],
];
fixtureSets.forEach(([dir, id]) =>
  ["valid", "invalid"].forEach((expectation) =>
    jsonFiles(at("conformance", "fixtures", dir, expectation)).forEach((path) => {
      const document = readJson(path);
      check(`${expectation === "valid" ? "accepts" : "rejects"} ${rel(path)}`, () =>
        isValid(id, document) === (expectation === "valid"),
      () => (expectation === "valid" ? errorsOf(id, document) : "unexpectedly valid"));
    }),
  ),
);

// ---- 5. vendored Praxis provenance-record conformance cases ------------------
// JSON Schema checks structure only. These invalid cases are semantic and need
// a codec (see conformance/README.md): the schema accepts them, and this runner
// asserts that it does, so the documented split cannot drift silently.
const SEMANTIC_ONLY = new Map([
  ["invalid/agent-without-execution.json", "kind-dependent key prefix (agent must be EXE-)"],
  ["invalid/two-created.json", "at most one created across entries"],
  ["invalid/created-after-modified.json", "temporal order of created"],
  ["invalid/credential-in-actor.json", "credential-shaped value detection"],
  ["invalid/credential-in-reason.json", "credential-shaped value detection"],
  ["invalid/source-not-in-lineage.json", "sources keys must appear in derivedFrom"],
  ["invalid/self-lineage.json", "subject must not appear in derivedFrom"],
]);
const praxisDir = at("conformance", "praxis-provenance-record");
const manifest = readJson(join(praxisDir, "manifest.json"));
const carried = (record) => ({
  schema: "echelon.execution-envelope/v2",
  operationId: "op-conformance",
  correlationId: "corr-conformance",
  timestamp: "2026-09-26T00:00:00Z",
  actor: { kind: "automation", id: "echelon/conformance", provider: "echelon", model: "unknown", runtime: "conformance" },
  execution: "EXE-echelon-registry.conformance",
  provenance: record,
});
check("praxis manifest: every SEMANTIC_ONLY entry names an invalid case", () =>
  [...SEMANTIC_ONLY.keys()].every((file) => manifest.cases.some((item) => item.file === file && item.expect === "invalid")),
);
manifest.cases.forEach(({ file, expect }) => {
  const record = readJson(join(praxisDir, file));
  const label = `praxis case ${file} (${expect})`;
  if (expect === "valid") {
    check(`${label}: record schema accepts`, () => isValid(ID.record, record), () => errorsOf(ID.record, record));
    check(`${label}: carried verbatim in envelope v2`, () => isValid(ID.envelopeV2, carried(record)), () =>
      errorsOf(ID.envelopeV2, carried(record)),
    );
  } else if (expect === "valid-unversioned") {
    check(`${label}: is not a versioned record`, () => !isValid(ID.record, record));
    check(`${label}: artifact-provenance schema accepts (read as v1)`, () => isValid(ID.artifactProvenance, record));
    check(`${label}: carried verbatim in envelope v2`, () => isValid(ID.envelopeV2, carried(record)), () =>
      errorsOf(ID.envelopeV2, carried(record)),
    );
  } else if (expect === "unsupported-version") {
    check(`${label}: not interpreted as major 1`, () => !isValid(ID.record, record));
    check(`${label}: carried verbatim in envelope v2`, () => isValid(ID.envelopeV2, carried(record)), () =>
      errorsOf(ID.envelopeV2, carried(record)),
    );
  } else if (SEMANTIC_ONLY.has(file)) {
    check(`${label}: semantic-only (${SEMANTIC_ONLY.get(file)}); schema accepts structure`, () =>
      isValid(ID.record, record),
    );
  } else {
    check(`${label}: record schema rejects`, () => !isValid(ID.record, record));
    check(`${label}: envelope v2 rejects carrying it`, () => !isValid(ID.envelopeV2, carried(record)));
  }
});
// Successor pairs need a codec's successor check (RQ-ROS-2026-A015); a schema
// can only confirm each side is a record an envelope may carry verbatim.
manifest.successors.forEach(({ before, after }) =>
  [before, after].forEach((file) =>
    check(`praxis successor fixture ${file}: carriable in envelope v2`, () =>
      isValid(ID.envelopeV2, carried(readJson(join(praxisDir, file)))),
    ),
  ),
);
manifest.e2e.steps.forEach(({ file }) =>
  check(`praxis e2e ${file}: record schema accepts`, () => isValid(ID.record, readJson(join(praxisDir, file)))),
);

// ---- 6. v1 <-> v2 envelope mapping -------------------------------------------
jsonFiles(at("conformance", "fixtures", "envelope-mapping")).forEach((path) => {
  const { v1, v2, roundTrip } = readJson(path);
  check(`mapping ${rel(path)}: v1 side is a valid v1 envelope`, () => isValid(ID.envelopeV1, v1), () =>
    errorsOf(ID.envelopeV1, v1),
  );
  check(`mapping ${rel(path)}: v2 side is a valid v2 envelope`, () => isValid(ID.envelopeV2, v2), () =>
    errorsOf(ID.envelopeV2, v2),
  );
  check(`mapping ${rel(path)}: v1 -> v2 is exactly the documented result`, () => deepEqual(envelopeV1ToV2(v1), v2), () =>
    JSON.stringify(envelopeV1ToV2(v1)),
  );
  if (roundTrip)
    check(`mapping ${rel(path)}: v1 -> v2 -> v1 round-trips with nothing lost`, () => {
      const back = envelopeV2ToV1(envelopeV1ToV2(v1));
      return back.lost.length === 0 && deepEqual(back.envelope, v1);
    }, () => JSON.stringify(envelopeV2ToV1(envelopeV1ToV2(v1))));
});
jsonFiles(at("conformance", "fixtures", "execution-envelope", "v1", "valid")).forEach((path) =>
  check(`v1 -> v2 of ${rel(path)} is a valid v2 envelope`, () => isValid(ID.envelopeV2, envelopeV1ToV2(readJson(path))), () =>
    errorsOf(ID.envelopeV2, envelopeV1ToV2(readJson(path))),
  ),
);
jsonFiles(at("conformance", "fixtures", "execution-envelope", "v2", "valid")).forEach((path) => {
  const v2 = readJson(path);
  const { envelope, lost } = envelopeV2ToV1(v2);
  check(`v2 -> v1 of ${rel(path)} reports every lost field and yields a valid v1 envelope or none`, () =>
    (envelope === null ? lost.includes("actor.kind") : isValid(ID.envelopeV1, envelope)) &&
    (v2.execution === undefined || lost.includes("execution")) &&
    (v2.provenance === undefined || lost.includes("provenance")) &&
    (v2.actor.model === undefined || v2.actor.model === "unknown" || lost.includes("actor.model")),
  () => JSON.stringify({ envelope, lost, errors: envelope && errorsOf(ID.envelopeV1, envelope) }));
});

// ---- 7. provenance-aware resolution scenarios --------------------------------
const registryEntry = (id) => registry.systems.find((system) => system.id === id);
const providerOf = (provider) =>
  provider === null
    ? null
    : {
        accessible: provider.accessible,
        manifest:
          provider.registryEntry !== undefined
            ? registryEntry(provider.registryEntry)
            : typeof provider.manifest === "string"
              ? readJson(at(provider.manifest))
              : provider.manifest,
      };
readJson(at("conformance", "fixtures", "resolution", "scenarios.json")).scenarios.forEach(
  ({ name, request, provider, expect }) => {
    const resolvedProvider = providerOf(provider);
    const outcome = resolve(request, resolvedProvider);
    check(`scenario "${name}": integration ${expect.state}, core ${expect.core}`, () =>
      outcome.state === expect.state &&
      coreHealth() === expect.core &&
      (outcome.state !== "misconfigured" || outcome.diagnostics.length > 0) &&
      (expect.envelope === undefined || envelopeFor(resolvedProvider.manifest) === expect.envelope),
    () => JSON.stringify(outcome));
    if (resolvedProvider !== null && typeof provider.manifest === "object")
      check(`scenario "${name}": inline manifest is a valid system manifest`, () =>
        isValid(BY_DISCRIMINATOR[resolvedProvider.manifest.schema], resolvedProvider.manifest),
      );
  },
);

// ---- report ------------------------------------------------------------------
results.forEach(({ ok, name, detail }, index) => {
  console.log(`${ok ? "ok" : "not ok"} ${index + 1} - ${name}`);
  if (!ok && detail) console.log(`  # ${detail.split("\n").join("\n  # ")}`);
});
const failed = results.filter(({ ok }) => !ok).length;
console.log(`1..${results.length}`);
console.log(`# pass ${results.length - failed}`);
console.log(`# fail ${failed}`);
process.exitCode = failed === 0 ? 0 : 1;
