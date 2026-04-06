---
description: design a .NET API contract with clear model boundaries
---
1. Decide whether the contract is source-specific ingest, canonical internal, or final outbound message.
2. Isolate source field names from canonical platform models.
3. Separate DTO contracts from domain or persistence concerns.
4. Confirm the contract shape supports thin endpoints and flow-service orchestration.
