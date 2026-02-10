# MAA Feature Catalog & Specification Index

**Purpose**: Central catalog of all MAA features, epics, and user stories. Use this to track specification creation and cross-reference features.

**How to Use**:

1. **Find an Epic**: Locate in table below
2. **View Features**: Expand epic to see features
3. **Create Spec**: Features marked `📋 Ready` can be specified via `/speckit.specify "feature description"`
4. **Track Progress**: Update status as specs are created and features shipped

---

## Feature Status Legend

| Symbol | Meaning                                           |
| ------ | ------------------------------------------------- |
| 📋     | Ready to Specify (use `/speckit.specify` command) |
| 📝     | Spec Created (link to spec file)                  |
| ✅     | Shipped (deployed to production)                  |
| ⭐     | Phase marker (indicates which future phase)       |
| 🔴     | Blocked (dependency not met)                      |

---

## Phase 1: Foundation & Infrastructure (MVP Prerequisites)

### **E1: Authentication & Session Management** 📋 Ready

**Status**: Not Started  
**Effort**: Medium  
**Team**: Backend  
**Duration**: 2-3 weeks

**Goal**: Provide secure, stateless authentication for anonymous and registered users

**Features**:

- [ ] F1.1: Session Initialization — Create anonymous session on app load; store in PostgreSQL
- [ ] F1.2: JWT Token Generation ⭐ Phase 5 — Generate short-lived access token + refresh token
- [ ] F1.3: Role-Based Authorization — Middleware to enforce Admin/Reviewer/Analyst roles
- [ ] F1.4: PII Encryption — Encrypt sensitive fields (income, assets, SSN) at database level
- [ ] F1.5: Secrets Management — Store API keys in Azure Key Vault

**Dependencies**: Database setup (E3)

**Success Criteria**:

- Anonymous session persists across requests
- JWT token valid for 1 hour; refresh extends session
- Sensitive fields encrypted in DB
- Constitution II: Auth logic unit-tested with 80%+ coverage

---

### **E2: Rules Engine & State Data** 📋 Ready

**Status**: Not Started  
**Effort**: Large  
**Team**: Backend + Rules Analyst  
**Duration**: 4-5 weeks

**Goal**: Implement deterministic, versioned rules engine evaluating Medicaid eligibility by state

**Features**:

- [ ] F2.1: Rules Data Model — Define Rule, State, MedicaidProgram, EligibilityResult entities
- [ ] F2.2: Rules Evaluation Engine — Implement JSONLogic evaluator; handle income/asset thresholds
- [ ] F2.3: Program Matching Logic — Match user answers to eligible programs
- [ ] F2.4: Plain-Language Explanation — Generate human-readable eligibility explanation
- [ ] F2.5: Rule Versioning & Effective Dates ⭐ Phase 2 — Track versions; effective dates; rollback
- [ ] F2.6: FPL Table Management — Store federal poverty level tables by year + state
- [ ] F2.7: Pilot State Rules — Hand-author rules for 5 pilot states (IL, CA, NY, TX, AZ)

**Dependencies**: Database setup

**Success Criteria**:

- Same input → same output (deterministic)
- Income at FPL threshold → correct eligibility status (test all 50 income variations)
- Rule versioning: old rule until effective date; new rule after
- Constitution III: All explanations jargon-free (e.g., not "EARNED-INC" but "Earned income")
- Constitution IV: Evaluation ≤2 seconds (p95)

---

### **E3: Document Storage Infrastructure** 📋 Ready

**Status**: Not Started  
**Effort**: Small  
**Team**: DevOps + Backend  
**Duration**: 1-2 weeks

**Goal**: Provide secure, scalable blob storage for user-uploaded documents

**Features**:

- [ ] F3.1: Blob Storage Setup — Configure Azure Blob Storage with encryption, lifecycle
- [ ] F3.2: Upload Handler — Stream multipart files to blob storage; validate MIME type
- [ ] F3.3: Antivirus Scanning ⭐ Phase 2 — Integrate ClamAV or cloud scanner; block infected files
- [ ] F3.4: Document Metadata — Store filename, type, upload date, user in PostgreSQL
- [ ] F3.5: Expiration & Cleanup ⭐ Phase 2 — Auto-delete documents after 90 days
- [ ] F3.6: Access Control — Serve documents only to owner (session/user)
- [ ] F3.7: Audit Trail ⭐ Phase 3 — Log all document operations for compliance

**Dependencies**: Infrastructure provisioning (Azure subscriptions)

**Success Criteria**:

- Valid PDF uploads succeed; invalid files rejected
- Document accessible to uploader only
- Constitution IV: Upload streaming ≤5 seconds for 15MB

---

## Phase 2: MVP Launch - User-Facing Core

### **E4: Eligibility Wizard UI** � [Spec](../specs/004-ui-implementation/spec.md)

**Status**: In Progress (Phases 1-5 Complete)  
**Effort**: Large  
**Team**: Frontend + UX/Design  
**Duration**: 4-5 weeks

**Goal**: Build interactive, accessible, mobile-responsive wizard guiding users through eligibility questions

**Specification**: [specs/004-ui-implementation/spec.md](../specs/004-ui-implementation/spec.md)

**Implementation Progress**:
- ✅ Phase 1: Setup (T001-T003) — Vite, Tailwind, Router
- ✅ Phase 2: Foundational (T004-T014) — API contracts, state/question services, frontend infrastructure
- ✅ Phase 3: User Story 1 (T015-T020) — Landing page, state selector, start wizard
- ✅ Phase 4: User Story 2 (T021-T024) — Multi-step flow, answer persistence, conditional logic
- ✅ Phase 5: User Story 3 (T025-T028) — WCAG 2.1 AA accessibility, mobile-first responsive design
- ✅ Phase 6: Polish (T029-T030) — Performance monitoring, documentation

**Features**:

- [X] F4.1: Landing Page — Hero, value props, "Check Eligibility" CTA
- [X] F4.2: State Selection — Auto-detect by ZIP; override allowed; state info explained
- [X] F4.3: Question Taxonomy — Define question types (text, number, select, multi-select, checkbox, date)
- [X] F4.4: Conditional Question Flow — Render questions based on previous answers
- [X] F4.5: Progress Indicator & Navigation — Show % complete; enable backtracking
- [X] F4.6: Question Explanations — Tooltips explaining "why we ask this"
- [X] F4.7: Accessibility Compliance — WCAG 2.1 AA: semantic HTML, ARIA labels, keyboard nav
- [X] F4.8: Mobile Optimization — Touch-friendly buttons, readable text, vertical scrolling
- [ ] F4.9: Save & Resume ⭐ Phase 7 (Future) — Save answers to account; allow resumption beyond session

**Dependencies**: E1 (authentication/sessions) ✅, E2 (rules engine for question taxonomy) ✅

**Success Criteria**:

- ✅ Questions render in correct conditional order
- ✅ Backtracking preserves answers
- ✅ WCAG 2.1 AA: Ready for axe DevTools validation
- ✅ Constitution IV: Step load target ≤500ms (perf monitoring implemented)
- ⏳ Wizard completion rate ≥70% (pending user testing)

---

### **E5: Eligibility Evaluation & Results** 📋 Ready

**Status**: Depends on E2, E4  
**Effort**: Medium-Large  
**Team**: Backend + Frontend  
**Duration**: 3-4 weeks

**Goal**: Display eligibility status with matched programs, confidence scoring, plain-language explanations

**Features**:

- [ ] F5.1: Results Page Layout — Display status prominently; list programs by likelihood
- [ ] F5.2: Program Cards — Show program name, status, confidence %, key details
- [ ] F5.3: Plain-Language Explanation — Generate "why eligible/ineligible" tied to user data
- [ ] F5.4: Confidence Scoring — Calculate confidence (0-100%) based on data completeness
- [ ] F5.5: Next Steps Guidance — Link to document checklist for matched programs
- [ ] F5.6: Result Export/Print ⭐ Phase 3 — Generate printable/PDF summary
- [ ] F5.7: Accessibility — WCAG 2.1 AA: semantic HTML, status indicated by icon+text
- [ ] F5.8: Error Handling — Handle ambiguous rules gracefully; show "contact support"

**Dependencies**: E2 (rules engine), E4 (wizard completion)

**Success Criteria**:

- Same input data → same results (deterministic)
- Explanation references user's specific data ($2,100 not "$X")
- Constitution II: Results API contract-tested; snapshot tests for explanations
- Constitution IV: Results API ≤2 seconds (p95)

---

### **E6: Document Management** 📋 Ready

**Status**: Depends on E3, E1, E5  
**Effort**: Medium  
**Team**: Frontend + Backend  
**Duration**: 3-4 weeks

**Goal**: Enable users to upload supporting docs, validate completeness, generate state-specific checklists

**Features**:

- [ ] F6.1: Document Checklist Generation — Generate state-specific required docs
- [ ] F6.2: Checklist Display — Show required vs optional, organized by category
- [ ] F6.3: Upload Interface — Drag-drop + file picker; multi-file support; progress
- [ ] F6.4: File Validation — Check file type, size (<15MB), magic bytes
- [ ] F6.5: Upload Status Tracking — Show which docs uploaded, which missing, which optional
- [ ] F6.6: Basic Validation ⭐ Phase 5 — OCR to detect name/date on document
- [ ] F6.7: Document Completeness Scoring — % complete indicator; suggest missing items
- [ ] F6.8: Smart Recommendations ⭐ Phase 3 — "We suggest uploading a recent pay stub"
- [ ] F6.9: Accessibility — WCAG 2.1 AA: keyboard-accessible upload, clear error messages

**Dependencies**: E3 (blob storage), E1 (authentication), E5 (eligibility results)

**Success Criteria**:

- Checklist updates when user matches new program
- Invalid files rejected with clear message
- Constitution I: Upload component testable with mock API
- Constitution IV: Upload streaming ≤5 seconds; validation instant

---

## Phase 3: Admin & Compliance Tools

### **E7: Admin Portal & Authentication** 📋 Ready

**Status**: Depends on E1, E2  
**Effort**: Medium  
**Team**: Frontend + Backend  
**Duration**: 2-3 weeks

**Goal**: Provide admin dashboard for compliance analysts and content team

**Features**:

- [ ] F7.1: Admin Login & Authorization — Separate login or role-based access control
- [ ] F7.2: Admin Dashboard — Overview of system status, pending tasks, recent changes
- [ ] F7.3: User Management ⭐ Phase 3+ — Create/deactivate admin users; assign roles
- [ ] F7.4: Rule Approval Queue — Show pending rule changes; allow approve/reject
- [ ] F7.5: Compliance Reporting ⭐ Phase 3+ — Audit log of admin actions; exportable
- [ ] F7.6: System Health Monitoring ⭐ Phase 4+ — Show API uptime, database performance

**Dependencies**: E1 (role-based access), E2 (rules)

**Success Criteria**:

- Non-admin users cannot access admin portal
- Constitution II: Authorization enforcement unit-tested; integration tests verify roles
- Constitution IV: Admin pages load ≤3 seconds

---

### **E8: Rule Management & Approval Workflow** 📋 Ready

**Status**: Depends on E2, E7  
**Effort**: Large  
**Team**: Backend + Frontend  
**Duration**: 3-4 weeks

**Goal**: Enable analysts to edit rules; admins to approve changes before activation

**Features**:

- [ ] F8.1: Rule Editor UI — Structured form for income, assets, categoricals; JSON preview
- [ ] F8.2: Rule Versioning — Track all rule versions; show who changed what when
- [ ] F8.3: Effective Dates — Schedule rule activation for future date
- [ ] F8.4: Rule Preview & Testing — Test proposed rule with sample user data
- [ ] F8.5: Approval Workflow — Routing to Reviewer; comments; approval/rejection
- [ ] F8.6: Rollback — Revert to previous rule version
- [ ] F8.7: Conflict Detection ⭐ Phase 3+ — Flag rules referencing non-existent programs
- [ ] F8.8: AI-Assisted Rule Generation ⭐ Phase 4 — Parse PDFs; auto-generate rule suggestions

**Dependencies**: E2 (rules engine), E7 (admin portal)

**Success Criteria**:

- Editor saves valid rule in JSON format
- Preview shows results with sample data
- Constitution II: Rule validation logic unit-tested
- Constitution I: Rule editor component <300 lines; business logic in handlers

---

## Phase 4: Automation & Intelligence

### **E9: Regulation Monitoring System** 📋 Ready

**Status**: Depends on E8, E7  
**Effort**: Large  
**Team**: Backend + AI/ML  
**Duration**: 4-5 weeks

**Goal**: Auto-detect Medicaid regulation changes; summarize using AI; flag for approval

**Features**:

- [ ] F9.1: Document Crawler — Periodic job to download state PDFs/web content
- [ ] F9.2: Change Detection — Diff previous vs. current version; identify changes
- [ ] F9.3: Change Summarization — AI summarizes changes in plain language
- [ ] F9.4: Confidence Scoring — Score likelihood of material change (0-100%)
- [ ] F9.5: Alert Dashboard — Show detected changes; approve/ignore/investigate
- [ ] F9.6: Change History & Audit — Track all changes, approvals, rule updates
- [ ] F9.7: Multi-State Coverage ⭐ Phase 4+ — Support crawling 50 states + DC
- [ ] F9.8: Source Integration ⭐ Phase 4+ — Support multiple sources (websites, email, PDFs)

**Dependencies**: E8 (rule management), E7 (admin portal), AI/LLM service

**Success Criteria**:

- Crawler fetches documents; detects changes vs. baseline
- AI summaries produce expected text ("threshold changed from 130% to 135% FPL")
- Constitution II: Diff logic unit-tested; integration tests for crawler pipeline
- Constitution IV: Crawling async (background job); summaries cached

---

## Phase 5: Enhanced User Experience

### **E10: Application Packet Generation** 📋 Ready

**Status**: Depends on E6, E5  
**Effort**: Large  
**Team**: Backend + Frontend  
**Duration**: 3-4 weeks

**Goal**: Generate state-specific Medicaid application forms pre-filled with user data

**Features**:

- [ ] F10.1: PDF Template Management — Store state-specific Medicaid forms
- [ ] F10.2: Form Fill & Data Mapping — Map user answers to form fields
- [ ] F10.3: Multi-Program Packet — Generate separate page per matched program
- [ ] F10.4: Cover Letter Generation — Templated letter with programs, required docs, submission method
- [ ] F10.5: Submission Instructions — Customizable per state (mailing address, portal URL, phone)
- [ ] F10.6: Document Index — List of uploaded documents referenced in packet
- [ ] F10.7: PDF Download & Email — User can download or email packet
- [ ] F10.8: Accessibility — Accessible PDFs: proper structure, tagged content, alt text

**Dependencies**: E6 (documents), E5 (results), PDF generation library (QuestPDF)

**Success Criteria**:

- Form fields pre-filled with user data (no blanks for provided info)
- Cover letter includes correct program names, state address, deadline
- Constitution I: PDF generation logic testable; template engine separate
- Constitution IV: PDF generation ≤10 seconds

---

### **E11: Document AI Processing** 📋 Ready

**Status**: Depends on E6  
**Effort**: Large  
**Team**: Backend + AI/ML  
**Duration**: 4-5 weeks

**Goal**: Auto-extract information from uploaded docs using OCR; classify types; score completeness

**Features**:

- [ ] F11.1: OCR Engine Setup — Integrate Tesseract or Azure Computer Vision
- [ ] F11.2: Text Extraction — Extract key fields (name, date, account #, amounts)
- [ ] F11.3: Document Type Classification — Classify as pay stub/bank statement/ID/etc.
- [ ] F11.4: Data Confidence Scoring — Score confidence of extracted data (95%, 87%, etc.)
- [ ] F11.5: Validation & Conflict Detection — Alert if extracted name differs from user-provided
- [ ] F11.6: Completeness Assessment — % of required fields extracted per doc type
- [ ] F11.7: User Review Interface — Show extracted data; allow user to confirm/correct

**Dependencies**: E6 (documents), OCR library (Tesseract), Async job infrastructure

**Success Criteria**:

- OCR extracts name, date, amount correctly from sample documents
- Confidence scores reasonable (high for clear text, low for blurry)
- Constitution II: OCR logic unit-tested with sample document images
- Constitution IV: OCR async (background job); UI updates via polling/WebSocket

---

### **E12: User Accounts & Session Management** 📋 Ready

**Status**: Depends on E1  
**Effort**: Medium  
**Team**: Frontend + Backend  
**Duration**: 2-3 weeks

**Goal**: Enable users to create accounts, save progress, resume applications

**Features**:

- [ ] F12.1: Account Registration — Email + password signup form; email confirmation
- [ ] F12.2: Login & Session — Login form; session established; token refresh
- [ ] F12.3: Session Persistence — Save wizard answers, documents, results to account
- [ ] F12.4: Resume Application — Login → previous application auto-loaded
- [ ] F12.5: Application History — User dashboard showing previous applications, results, docs
- [ ] F12.6: Logout & Session Timeout — Logout button; auto-logout after 30 min inactivity
- [ ] F12.7: OAuth Identity Providers ⭐ Phase 5+ — Sign in with Google/Microsoft

**Dependencies**: E1 (authentication with JWT support), Database for user accounts

**Success Criteria**:

- Registration creates user account; email confirmation works
- Wizard answers saved on submit; resume loads previous answers
- Constitution II: Session persistence logic tested; resume functionality verified
- Session timeout after 30 min inactivity

---

### **E13: Notification System** 📋 Ready

**Status**: Depends on E12, E9, E6  
**Effort**: Medium  
**Team**: Backend + Frontend  
**Duration**: 2-3 weeks

**Goal**: Notify users and admins of important events (rule changes, missing docs, approvals)

**Features**:

- [ ] F13.1: Notification Queue — Background job queue for async delivery
- [ ] F13.2: Email Delivery — Send emails via SMTP or cloud service (SendGrid, Azure)
- [ ] F13.3: In-App Alerts — Display alerts on user dashboard; mark as read
- [ ] F13.4: Notification Templates — Customizable templates per event type
- [ ] F13.5: User Preferences — Enable/disable notifications for specific events
- [ ] F13.6: Admin Alerts — Notify admins of pending approvals, rule changes, errors
- [ ] F13.7: SMS Notifications ⭐ Phase 5+ — Send SMS alerts for critical events

**Dependencies**: E12 (user accounts), E9 (regulation monitoring), E6 (documents), Email service

**Success Criteria**:

- Email sent on rule change event
- In-app alert appears and persists until marked read
- User disables notifications → no more emails
- Constitution IV: Notifications async; send ≤5 seconds after event

---

## Phase 6: Analytics & Reporting

### **E14: Analytics Dashboard & Metrics** 📋 Ready

**Status**: Depends on all previous phases  
**Effort**: Medium  
**Team**: Backend + Analytics  
**Duration**: 3-4 weeks

**Goal**: Track system health and user engagement metrics for product decisions

**Features**:

- [ ] F14.1: Event Logging — Log wizard starts, steps, eligibility eval, doc uploads
- [ ] F14.2: Metrics Pipeline — Aggregate events into metrics (counts, averages, percentiles)
- [ ] F14.3: Admin Dashboard — Visualize key metrics (total checks, checks by state, completion rate)
- [ ] F14.4: Performance Dashboard — Track p50/p95/p99 latencies by endpoint; error rates
- [ ] F14.5: Drop-Off Analysis — Identify where users abandon wizard (exit rate by step)
- [ ] F14.6: Program Matching Analytics — % of users matched to each program
- [ ] F14.7: Document Analytics — % completing upload; common doc types; missing docs
- [ ] F14.8: Compliance Audit Log — Export audit events for compliance review

**Dependencies**: All phases (event logging across system), Analytics database, Visualization tool

**Success Criteria**:

- Events logged on user actions (start, complete step, etc.)
- Metrics aggregated correctly (counts, averages)
- Dashboard displays correct totals and trends
- Constitution IV: Analytics queries don't impact production (read replicas / batch jobs)

---

## How to Specify a Feature

**To create detailed spec for any feature**:

1. Find the feature above (e.g., "F4.4: Conditional Question Flow")
2. Copy the feature name and goal
3. Run:
   ```bash
   /speckit.specify "Implement conditional question flow in wizard - answers unlock next questions based on state-specific rules; support nested conditions"
   ```
4. Update the feature status to 📝 (Spec Created)
5. Add link to created spec file

**Spec Creation Checklist**:

- ✅ Run `/speckit.specify` and provide feature description
- ✅ Spec template auto-generates (see: `.specify/templates/spec-template.md`)
- ✅ Add user stories with P1/P2/P3 priorities
- ✅ Define acceptance scenarios (Given/When/Then)
- ✅ List edge cases to test
- ✅ Reference Constitution principles in requirements
- ✅ Update status in this catalog: 📝 + link to spec

---

## Specification Status Tracker

**Specs Created** (Update as you go):

- [ ] E1.F1.1 - Session Initialization
- [ ] E2.F2.1 - Rules Data Model
- [ ] E4.F4.1 - Landing Page
- [ ] (Add more as created)

**Specs In Progress**:

- (None yet)

**Specs Ready for Planning**:

- (None yet)

**Shipped Features**:

- (None yet - MVP in progress)

---

## Quick Reference: Most Used Features

**Top 5 Highest-Value Features for MVP**:

1. **F2.2**: Rules Evaluation Engine (enables all eligibility logic)
2. **F4.4**: Conditional Question Flow (core user experience)
3. **F5.3**: Plain-Language Explanation (builds trust)
4. **F6.1**: Document Checklist Generation (guides users to submission)
5. **F1.1**: Session Initialization (enables back-end to track users)

**Top 5 Risk Features** (complex, critical path):

1. **F2.7**: Pilot State Rules (regulatory accuracy critical)
2. **F11.2**: OCR Text Extraction (accuracy affects user experience)
3. **F9.3**: AI Change Summarization (must summarize correctly)
4. **F10.2**: PDF Form Fill (state-specific complexity)
5. **F8.1**: Rule Editor UI (admin workflow complexity)

---

**Last Updated**: 2026-02-08  
**Status**: Phase 1 Planning  
**Total Features**: 50+  
**MVP Features**: 25 (E1-E6)  
**Future Features**: 25+ (E7-E14)
