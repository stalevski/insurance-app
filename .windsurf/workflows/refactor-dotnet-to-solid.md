---
description: refactor .NET code toward SOLID modular monolith boundaries
---
1. Separate source-specific ingest logic from canonical transformation logic.
2. Preserve modular monolith boundaries between endpoints, mappers, contracts, and flow services.
3. Keep future pluginization possible by minimizing coupling and avoiding source leakage into canonical models.
4. Rebuild after refactoring to verify the vertical slice still compiles cleanly.
