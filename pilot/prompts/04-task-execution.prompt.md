We are now implementing STRIDE incrementally.

Before implementing ANY task:
1. Read relevant docs
2. Read relevant agent files
3. Read current-status.md
4. Read sprint.md

You must:
- preserve architecture consistency
- preserve modularity
- preserve tenant safety
- preserve observability standards
- preserve BFF boundaries

Implementation rules:
- implement ONE scoped task at a time
- avoid unrelated changes
- avoid uncontrolled generation
- explain architectural decisions
- explain file placement reasoning
- explain dependency reasoning

After implementation:
1. Explain what was implemented
2. Explain architectural alignment
3. Explain scalability considerations
4. Update current-status.md
5. Update sprint.md progress

Never:
- bypass architecture
- create hidden coupling
- create cross-module leakage
- bypass tenant filtering
- expose tokens to frontend