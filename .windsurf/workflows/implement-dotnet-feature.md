---
description: implement a .NET feature in the insurance integration platform
---
1. Identify whether the feature belongs to a source-specific contract, a canonical internal contract, or a final outbound message.
2. Keep source mapping separate from canonical processing logic.
3. Organize the implementation by lifecycle flow so the orchestration remains modular and plugin-ready later.
4. Validate request binding, mapping, service orchestration, and response shape before considering the feature complete.
