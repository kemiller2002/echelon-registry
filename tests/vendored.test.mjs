// REG-PROV-015: vendored Praxis files are byte-identical to the recorded contract commit,
// and the vendored reference classifier reaches every pinned verdict.
import test from "node:test";
import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { createHash } from "node:crypto";
import { root, readJson, praxisCases } from "./support.mjs";
import { classify } from "../lib/vendor/praxis/provenance-interchange.mjs";

const CONTRACT_COMMIT = "a42c44e8ae0e6e16fdd513141460b700e5fa6648";
const vendorDirs = ["schemas/vendor/praxis/", "lib/vendor/praxis/", "tests/fixtures/praxis-provenance/"];

for (const dir of vendorDirs) {
  test(`vendored ${dir} matches SOURCE.json (sha256, contract commit)`, () => {
    const source = readJson(`${dir}SOURCE.json`);
    assert.equal(source.repository, "kemiller2002/praxis");
    assert.equal(source.commit, CONTRACT_COMMIT);
    assert.ok(Object.keys(source.files).length > 0);
    for (const [name, expected] of Object.entries(source.files)) {
      const actual = createHash("sha256").update(readFileSync(new URL(`${dir}${name}`, root))).digest("hex");
      assert.equal(actual, expected, `${dir}${name} was edited locally; re-vendor it from Praxis instead`);
    }
  });
}

test("all 40 Praxis conformance cases are present", () => {
  assert.equal(praxisCases().length, 40);
});

for (const item of praxisCases()) {
  test(`praxis conformance: ${item.name} is ${item.expect} (${item.warnings} warnings)`, () => {
    const result = classify(item.block);
    assert.equal(result.verdict, item.expect, JSON.stringify(result.problems));
    assert.equal(result.warnings.length, item.warnings);
  });
}
