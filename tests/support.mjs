// Shared test support: JSON loading and one Ajv instance holding every registry schema,
// including the unchanged vendored Praxis schemas the v2 envelope references by $id.
import { readFileSync, readdirSync } from "node:fs";
import Ajv2020 from "ajv/dist/2020.js";
import addFormats from "ajv-formats";

export const root = new URL("../", import.meta.url);
export const readJson = (path) => JSON.parse(readFileSync(new URL(path, root), "utf8"));
export const listJson = (dir) => readdirSync(new URL(dir, root)).filter((name) => name.endsWith(".json")).map((name) => `${dir}${name}`);

export const schemaFiles = [
  "schemas/vendor/praxis/provenance-actor.schema.json",
  "schemas/vendor/praxis/provenance-interchange.schema.json",
  "schemas/execution-envelope.schema.json",
  "schemas/execution-envelope.v2.schema.json",
  "schemas/system-manifest.schema.json",
  "schemas/system-manifest.v2.schema.json",
  "schemas/registry.schema.json",
  "schemas/registry.v2.schema.json",
  "contracts/followup-create.v1.schema.json",
];

// strictRequired/strictTypes are relaxed only because the unchanged vendored Praxis actor schema
// names `required` properties inside if/then without redeclaring them; every other strict check is on.
export const createAjv = () => {
  const ajv = new Ajv2020({ allErrors: true, strict: true, strictTypes: false, strictRequired: false });
  addFormats(ajv);
  for (const file of schemaFiles) ajv.addSchema(readJson(file));
  return ajv;
};

/** Schema $id for each document discriminator used in this repository. */
export const schemaIdFor = {
  "echelon.execution-envelope/v1": "https://echelonfoundry.com/schemas/echelon.execution-envelope.v1.json",
  "echelon.execution-envelope/v2": "https://echelonfoundry.com/schemas/echelon.execution-envelope.v2.json",
  "echelon.system/v1": "https://echelonfoundry.com/schemas/echelon.system.v1.json",
  "echelon.system/v2": "https://echelonfoundry.com/schemas/echelon.system.v2.json",
  "echelon.registry/v1": "https://echelonfoundry.com/schemas/echelon.registry.v1.json",
  "echelon.registry/v2": "https://echelonfoundry.com/schemas/echelon.registry.v2.json",
};

export const validator = (ajv, schemaTag) => ajv.getSchema(schemaIdFor[schemaTag]);

export const praxisCases = () => readJson("tests/fixtures/praxis-provenance/cases.json").cases;
export const echelonChain = () => readJson("tests/fixtures/praxis-provenance/echelon-chain.json");
