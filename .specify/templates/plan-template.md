# Implementation Plan: [FEATURE]

**Branch**: `[###-feature-name]` | **Date**: [DATE] | **Spec**: [link]
**Input**: Feature specification from `/specs/[###-feature-name]/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

[Extract from feature spec: primary requirement + technical approach from research]

## Technical Context

<!--
  ACTION REQUIRED: Replace the content in this section with the technical details
  for the project. The structure here is presented in advisory capacity to guide
  the iteration process.
-->

**Language/Version**: [e.g., Python 3.11, Swift 5.9, Rust 1.75 or NEEDS CLARIFICATION]  
**Primary Dependencies**: [e.g., FastAPI, UIKit, LLVM or NEEDS CLARIFICATION]  
**Storage**: [if applicable, e.g., PostgreSQL, CoreData, files or N/A]  
**Testing**: [e.g., pytest, XCTest, cargo test or NEEDS CLARIFICATION]  
**Target Platform**: [e.g., Linux server, iOS 15+, WASM or NEEDS CLARIFICATION]
**Project Type**: [single/web/mobile - determines source structure]  
**Performance Goals**: [domain-specific, e.g., 1000 req/s, 10k lines/sec, 60 fps or NEEDS CLARIFICATION]  
**Constraints**: [domain-specific, e.g., <200ms p95, <100MB memory, offline-capable or NEEDS CLARIFICATION]  
**Scale/Scope**: [domain-specific, e.g., 10k users, 1M LOC, 50 screens or NEEDS CLARIFICATION]

## Constitution Check _(GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.)_

**Constitution Reference**: See [MAA Constitution](/.specify/memory/constitution.md) for full principle definitions.

### Principle Alignment Assessment

**I. Code Quality & Clean Architecture**

- [ ] Domain logic can be isolated from I/O (testable without DB/HTTP)
- [ ] Dependencies are explicitly injected (no service locator pattern)
- [ ] No classes exceed ~300 lines; single responsibility clear
- [ ] DTO contracts explicitly defined (backend) or type-safe (frontend)

**II. Test-First Development**

- [ ] Test scenarios defined in spec before implementation begins
- [ ] Unit tests target domain/application layers (80%+ coverage planned)
- [ ] Integration tests cover cross-layer flows (handlers + database, components + hooks)
- [ ] All async operations tested for success AND error paths

**III. UX Consistency & Accessibility (if user-facing)**

- [ ] Design meets WCAG 2.1 AA (semantic HTML, keyboard nav, screen reader support)
- [ ] Mobile-first responsive design (375px → 1920px)
- [ ] Plain language (no unexplained jargon; error messages actionable)
- [ ] Consistent component usage (shadcn/ui + Tailwind); no custom component behavior

**IV. Performance & Scalability (if performance-sensitive)**

- [ ] Response time SLOs defined (align with Constitution targets: ≤2s eligibility, ≤500ms interactions)
- [ ] Caching strategy documented (Redis for immutable data, React Query for server state)
- [ ] Database queries indexed on common filters (state_id, eligibility_status, created_at)
- [ ] Load test target defined (1,000 concurrent users or domain-specific metric)

**⚠️ VIOLATIONS FOUND?** Document in "Complexity Tracking" section below with justification.

## Project Structure

### Documentation (this feature)

```text
specs/[###-feature]/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

<!--
  ACTION REQUIRED: Replace the placeholder tree below with the concrete layout
  for this feature. Delete unused options and expand the chosen structure with
  real paths (e.g., apps/admin, packages/something). The delivered plan must
  not include Option labels.
-->

```text
# [REMOVE IF UNUSED] Option 1: Single project (DEFAULT)
src/
├── models/
├── services/
├── cli/
└── lib/

tests/
├── contract/
├── integration/
└── unit/

# [REMOVE IF UNUSED] Option 2: Web application (when "frontend" + "backend" detected)
backend/
├── src/
│   ├── models/
│   ├── services/
│   └── api/
└── tests/

frontend/
├── src/
│   ├── components/
│   ├── pages/
│   └── services/
└── tests/

# [REMOVE IF UNUSED] Option 3: Mobile + API (when "iOS/Android" detected)
api/
└── [same as backend above]

ios/ or android/
└── [platform-specific structure: feature modules, UI flows, platform tests]
```

**Structure Decision**: [Document the selected structure and reference the real
directories captured above]

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

| Violation                  | Why Needed         | Simpler Alternative Rejected Because |
| -------------------------- | ------------------ | ------------------------------------ |
| [e.g., 4th project]        | [current need]     | [why 3 projects insufficient]        |
| [e.g., Repository pattern] | [specific problem] | [why direct DB access insufficient]  |
