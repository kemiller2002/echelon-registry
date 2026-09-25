# Echelon Integration Conformance

A component is conformant only if its core behavior survives optional integration loss.

Required scenarios:

| Scenario | Core result | Integration result |
|---|---|---|
| component alone | PASS | unavailable |
| component + empty registry | PASS | unavailable |
| component + compatible provider | PASS | available |
| provider not installed | PASS | unavailable |
| provider declared but inaccessible | PASS | misconfigured |
| provider contract incompatible | PASS | misconfigured |
| full supported ecosystem | PASS | available |

A test suite MUST NOT convert `unavailable` into a failing core-health result.

Financial/time integrations MUST additionally prove idempotency: replaying the same operation ID cannot create a second domain record.
