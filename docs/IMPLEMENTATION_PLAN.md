# MAA Implementation Plan & Feature Roadmap

**Project**: Medicaid Application Assistant (MAA)  
**Created**: 2026-02-08  
**Status**: Active Planning  
**Based On**: [PRD](./prd.md) | [Technical Stack](./tech-stack.md) | [Constitution](./.specify/memory/constitution.md)

---

## Table of Contents

1. [Overview](#overview)
2. [Feature Breakdown by Phase](#feature-breakdown-by-phase)
3. [Detailed Epic Specifications](#detailed-epic-specifications)
4. [Feature Dependencies & Sequencing](#feature-dependencies--sequencing)
5. [MVP vs. Future Releases](#mvp-vs-future-releases)
6. [Implementation Roadmap](#implementation-roadmap)

---

## Overview

### Purpose

This document decomposes the MAA Product Requirements Document (PRD) into **5 major epics**, **20+ features**, and supporting user stories. Each epic aligns with the [MAA Constitution](../.specify/memory/constitution.md) principles (Code Quality, Testing, UX Consistency, Performance).

### Key Principles

- ✅ **Constitution-Driven**: Every feature must pass Code Quality, Testing Standards, UX Consistency, and Performance gates
- ✅ **Testability First**: All features designed with unit/integration/contract test scenarios upfront
- ✅ **Accessibility Native**: WCAG 2.1 AA compliance built into every UI feature
- ✅ **Performance Measured**: All latency-sensitive features must meet SLOs from Constitution IV
- ✅ **Independently Deliverable**: Features prioritized to enable incremental releases

### MVP Definition

**MVP Scope** (Phase 1-2, 3-5 pilot states):

- Core eligibility wizard (all question types)
- Eligibility evaluation engine (rules + matching)
- Document upload & basic validation
- Admin rule editor (manual configuration)
- Results display with explanations

**Future Releases** (Phase 3+):

- Application packet PDF generation
- OCR-powered document classification
- Regulation monitoring automation
- User accounts & session save/resume
- Notification system
- Broader state coverage (50 states + DC)

---

## Feature Breakdown by Phase

### Phase 1: Foundation & Core Platform (MVP Prerequisites)

**Focus**: Build infrastructure, data models, and authentication to enable user-facing features
**Duration Target**: 4-6 weeks
**Teams**: Backend, DevOps, Database

| #      | Epic                                    | Features                                               | Status                     |
| ------ | --------------------------------------- | ------------------------------------------------------ | -------------------------- |
| **E1** | **Authentication & Session Management** | User sessions, JWT setup, role-based access            | 📋 Ready for Specification |
| **E2** | **Rules Engine & State Data**           | Rule evaluation logic, state metadata, rule versioning | 📋 Ready for Specification |
| **E3** | **Document Storage Infrastructure**     | Blob storage setup, encryption, access patterns        | 📋 Ready for Specification |

---

### Phase 2: User-Facing Core (MVP Launch)

**Focus**: Public eligibility wizard, results, and document collection
**Duration Target**: 6-8 weeks
**Teams**: Frontend, Backend, UX/Design

| #      | Epic                                 | Features                                                        | Status                     |
| ------ | ------------------------------------ | --------------------------------------------------------------- | -------------------------- |
| **E4** | **Eligibility Wizard UI**            | Landing page, state selection, question flow, progress tracking | 📋 Ready for Specification |
| **E5** | **Eligibility Evaluation & Results** | Result presentation, program matching, explanation generation   | 📋 Ready for Specification |
| **E6** | **Document Management**              | Upload, validation, checklist generation, status display        | 📋 Ready for Specification |

---

### Phase 3: Admin & Internal Tools (MVP+)

**Focus**: Tools for compliance analysts and content team to manage rules and approvals
**Duration Target**: 4-6 weeks
**Teams**: Backend, Frontend, Product

| #      | Epic                                    | Features                                                | Status                     |
| ------ | --------------------------------------- | ------------------------------------------------------- | -------------------------- |
| **E7** | **Admin Portal & Authentication**       | Admin dashboard, rule editor UI, role-based permissions | 📋 Ready for Specification |
| **E8** | **Rule Management & Approval Workflow** | Rule versioning, change detection, approval process     | 📋 Ready for Specification |

---

### Phase 4: Regulation Monitoring & AI (Future Release)

**Focus**: Automatic rule change detection with AI summarization
**Duration Target**: 6-8 weeks
**Teams**: Backend, AI/ML, Platform

| #      | Epic                             | Features                                                  | Status                     |
| ------ | -------------------------------- | --------------------------------------------------------- | -------------------------- |
| **E9** | **Regulation Monitoring System** | Web crawling, document change detection, AI summarization | 📋 Ready for Specification |

---

### Phase 5: Application Packet & Advanced Features (Future Release)

**Focus**: PDF generation, OCR processing, user accounts, notifications
**Duration Target**: 6-8 weeks
**Teams**: Backend, Frontend, Platform

| #       | Epic                                   | Features                                                           | Status                     |
| ------- | -------------------------------------- | ------------------------------------------------------------------ | -------------------------- |
| **E10** | **Application Packet Generation**      | PDF form filling, multi-state templates, cover letters             | 📋 Ready for Specification |
| **E11** | **Document AI Processing**             | OCR extraction, document classification, completeness scoring      | 📋 Ready for Specification |
| **E12** | **User Accounts & Session Management** | Account creation, OAuth, session save/resume                       | 📋 Ready for Specification |
| **E13** | **Notification System**                | Rule change alerts, document missing notifications, email delivery | 📋 Ready for Specification |

---

### Phase 6: Analytics & Reporting (Future Release)

**Focus**: Insights into user behavior, system performance, and compliance metrics
**Duration Target**: 3-4 weeks
**Teams**: Backend, Analytics, Frontend

| #       | Epic                              | Features                                                  | Status                     |
| ------- | --------------------------------- | --------------------------------------------------------- | -------------------------- |
| **E14** | **Analytics Dashboard & Metrics** | Usage by state, drop-off analysis, performance monitoring | 📋 Ready for Specification |

---

## Detailed Epic Specifications

### **EPIC E1: Authentication & Session Management** (Phase 1)

**Goal**: Provide secure, stateless authentication for anonymous and registered users; enable role-based access control for admins.

**Acceptance Criteria**:

- Anonymous users can access public features without login
- Optional user accounts allow session save/resume (Phase 5 feature flag)
- JWT for API authentication with short-lived tokens (1-hour) + refresh tokens (7-day)
- Role-based access control: Admin, Reviewer, Analyst
- Encrypted PII at rest; HTTPS (TLS 1.3) in transit

**Features**:

| Feature                            | Description                                                                       | MVP                     | Effort |
| ---------------------------------- | --------------------------------------------------------------------------------- | ----------------------- | ------ |
| **F1.1: Session Initialization**   | Create anonymous session on app load; store in PostgreSQL with JSONB session data | ✅                      | Medium |
| **F1.2: JWT Token Generation**     | Generate short-lived access token + refresh token for authenticated users         | ⭐ Future               | Medium |
| **F1.3: Role-Based Authorization** | Middleware to enforce Admin/Reviewer/Analyst roles; block unauthorized requests   | ✅ Backend, ⭐ Admin UI | Medium |
| **F1.4: PII Encryption**           | Encrypt sensitive fields (income, assets, SSN) at database level (pgcrypto)       | ✅                      | Small  |
| **F1.5: Secrets Management**       | Store API keys, database passwords in Azure Key Vault (not config files)          | ✅                      | Small  |

**Dependencies**:

- Database setup (E2)
- Infrastructure provisioning (Azure App Service)

**Test Scenarios** (TDD):

- Anonymous user session persists across requests
- JWT token valid for 1 hour; refresh token extends session
- Admin user denied access to non-admin endpoints
- Sensitive fields encrypted in DB; readable in application

**Constitution Alignment**:

- ✅ Code Quality: Authentication middleware testable in isolation
- ✅ Testing: Integration tests with test database
- ✅ UX: Clear error messages when session expires
- ✅ Performance: JWT validation < 50ms

---

### **EPIC E2: Rules Engine & State Data** (Phase 1)

**Goal**: Implement a deterministic, versioned rules engine that evaluates Medicaid eligibility based on state-specific rules and returns program matches with plain-language explanations.

**Acceptance Criteria**:

- Rules evaluate eligibility deterministically (same input → same output)
- Rules support MAGI, non-MAGI, SSI, aged, disability, pregnancy pathways
- Rules versioned with effective dates; support rollback
- Engine returns eligibility status + matched programs + confidence score + explanation
- Rules authored in JSON; evaluated via expressive rule engine (JSONLogic.Net)
- State metadata (FPL thresholds, regulations) versioned separately
- All rules must have test cases covering edge cases

**Features**:

| Feature                                     | Description                                                                                     | MVP        | Effort |
| ------------------------------------------- | ----------------------------------------------------------------------------------------------- | ---------- | ------ |
| **F2.1: Rules Data Model**                  | Define Rule, State, MedicaidProgram, EligibilityResult entities                                 | ✅         | Small  |
| **F2.2: Rules Evaluation Engine**           | Implement JSONLogic evaluator; handle income thresholds, asset limits, categorical eligibility  | ✅         | Large  |
| **F2.3: Program Matching Logic**            | Match user answers to eligible Medicaid programs (Aged, Blind, Disabled, MAGI, etc.)            | ✅         | Large  |
| **F2.4: Plain-Language Explanation**        | Generate human-readable explanation of why user is/isn't eligible ("You exceeded income by $X") | ✅         | Medium |
| **F2.5: Rule Versioning & Effective Dates** | Track rule versions; apply effective dates; support rule rollback                               | ⭐ Phase 2 | Medium |
| **F2.6: FPL Table Management**              | Store federal poverty level tables by year + state; reference in income rules                   | ✅         | Small  |
| **F2.7: Pilot State Rules**                 | Hand-author eligibility rules for 3-5 pilot states (IL, CA, NY, TX, FL)                         | ✅         | Large  |

**Dependencies**:

- Database setup
- Authentication (E1)

**Test Scenarios** (TDD):

- Income at FPL threshold → "likely eligible" (test all 50 income variations)
- Household size change affects threshold → recalculate correctly
- MAGI vs non-MAGI pathways return different programs
- Explanation text references actual income/limits in numbers
- Rule versioning: old rule applies until effective date; new rule applies after
- Asset limits checked when applicable; ignored when not

**Constitution Alignment**:

- ✅ Code Quality: Rules engine testable without database (pure functions)
- ✅ Testing: 80%+ coverage on eligibility logic; unit + integration tests for each state
- ✅ UX: Explanations clear, jargon-free ("Earned income" not "EARNED-INC")
- ✅ Performance: Eligibility evaluation ≤2 seconds (p95)

---

### **EPIC E3: Document Storage Infrastructure** (Phase 1)

**Goal**: Provide secure, scalable blob storage for user-uploaded documents (PDFs, images) with virus scanning, encryption, and audit trails.

**Acceptance Criteria**:

- Documents stored on Azure Blob Storage or AWS S3 (separate CDN domain for security)
- All uploads scanned for viruses before acceptance
- Documents encrypted in transit (HTTPS) and at rest (client-side encryption)
- Supporting documents auto-expire after 90 days (configurable)
- Audit trail logs all uploads/deletes with user + timestamp
- Max file size: 15MB per document

**Features**:

| Feature                        | Description                                                                    | MVP                | Effort |
| ------------------------------ | ------------------------------------------------------------------------------ | ------------------ | ------ |
| **F3.1: Blob Storage Setup**   | Configure Azure Blob Storage containers with encryption, lifecycle policies    | ✅                 | Small  |
| **F3.2: Upload Handler**       | Stream multipart files to blob storage; validate MIME type + magic bytes       | ✅                 | Small  |
| **F3.3: Antivirus Scanning**   | Integrate ClamAV or cloud-based scanner (Azure Defender); block infected files | ⭐ Phase 2         | Medium |
| **F3.4: Document Metadata**    | Store filename, type, upload date, user, session ID in PostgreSQL              | ✅                 | Small  |
| **F3.5: Expiration & Cleanup** | Automatic deletion of documents after 90 days via background job               | ⭐ Phase 2         | Small  |
| **F3.6: Access Control**       | Serve documents only to owner (session/user); never public                     | ✅                 | Small  |
| **F3.7: Audit Trail**          | Log all document operations (upload, view, delete) for compliance              | ⭐ Phase 3 (Admin) | Small  |

**Dependencies**:

- Infrastructure provisioning (Azure subscriptions)
- Authentication (E1)

**Test Scenarios** (TDD):

- Valid PDF uploads successfully; invalid files rejected
- Document accessible to uploader; not accessible to other users
- Expired documents deleted automatically
- Virus-infected file rejected with user-friendly message
- Audit log shows all operations

**Constitution Alignment**:

- ✅ Code Quality: Upload handler testable with mock storage
- ✅ Testing: Integration tests with test blob container
- ✅ UX: Clear upload feedback (progress %, success/error messages)
- ✅ Performance: Large file uploads stream without blocking (≤5 seconds for 15MB)

---

### **EPIC E4: Eligibility Wizard UI** (Phase 2)

**Goal**: Build interactive, accessible, mobile-responsive wizard that guides users through conditional question flow, with progress tracking, save/resume, and plain-language explanations.

**Acceptance Criteria**:

- Mobile-first responsive design (375px - 1920px)
- Conditional question flow (answer determines next questions)
- State-specific question sets (vary by state + eligibility category)
- Progress indicator shows % complete; users can backtrack without data loss
- Save & resume progress (Phase 5 feature flag, requires accounts)
- All questions plain language; "explain why we ask this" tooltips
- WCAG 2.1 AA: keyboard navigation, screen reader support, accessible forms
- Question load: ≤500ms (client-side rendering)

**Features**:

| Feature                                   | Description                                                                     | MVP              | Effort |
| ----------------------------------------- | ------------------------------------------------------------------------------- | ---------------- | ------ |
| **F4.1: Landing Page**                    | Home page explaining Medicaid, eligibility basics, "Check Eligibility" CTA      | ✅               | Medium |
| **F4.2: State Selection**                 | Auto-detect by ZIP; allow override; explain state-specific differences          | ✅               | Medium |
| **F4.3: Question Taxonomy**               | Define question types (text, number, select, multi-select, checkbox, date)      | ✅               | Small  |
| **F4.4: Conditional Question Flow**       | Render questions based on previous answers; support nested conditions           | ✅               | Large  |
| **F4.5: Progress Indicator & Navigation** | Show % complete; enable backtracking; highlight current step                    | ✅               | Medium |
| **F4.6: Question Explanations**           | Tooltips/modals explaining "why we ask this"; examples for confusing fields     | ✅               | Small  |
| **F4.7: Accessibility Compliance**        | Semantic HTML, ARIA labels, keyboard nav, color-blind safe; zero axe violations | ✅               | Medium |
| **F4.8: Mobile Optimization**             | Touch-friendly buttons (≥44×44px), readable text (≥16px), vertical scrolling    | ✅               | Medium |
| **F4.9: Save & Resume**                   | Save answers to session (anonymous) or account (registered); allow resumption   | ⭐ Phase 5 (E12) | Medium |

**Dependencies**:

- Rules Engine (E2) for question taxonomy
- Authentication (E1) for session
- Frontend framework (React 19, TypeScript)

**Test Scenarios** (TDD):

- Questions render in correct conditional order
- Backtracking preserves answers without validation
- Form validation prevents invalid input (e.g., negative income)
- Screen reader announces current question + progress
- Keyboard users navigate all fields and submit without mouse
- Mobile: buttons readable, no horizontal scroll, input fields visible when focused
- Save works correctly; resume loads previous answers

**Constitution Alignment**:

- ✅ Code Quality: React components <300 lines; custom hooks for business logic
- ✅ Testing: Component tests with React Testing Library; axe DevTools on every step
- ✅ UX: WCAG 2.1 AA; plain language verified with user testing
- ✅ Performance: Step load ≤500ms; form validation instant (client-side)

---

### **EPIC E5: Eligibility Evaluation & Results** (Phase 2)

**Goal**: Display eligibility status with matched programs, confidence scoring, and plain-language explanations; guide users toward next steps (documents, application).

**Acceptance Criteria**:

- Results show: eligibility status (likely / possibly / unlikely eligible), matched programs, explanation
- Confidence score (0-100%) for each program
- Explanation includes concrete examples: "Your $2,100/month income is $X below the limit for IL Aged"
- Results actionable: "You need [X documents]" → links to document checklist
- Results exportable/printable for user records
- WCAG 2.1 AA: accessible result presentation
- Results load: ≤2 seconds (p95) after wizard submission

**Features**:

| Feature                              | Description                                                                                                | MVP        | Effort |
| ------------------------------------ | ---------------------------------------------------------------------------------------------------------- | ---------- | ------ |
| **F5.1: Results Page Layout**        | Display eligibility status prominently; list programs in descending likelihood                             | ✅         | Small  |
| **F5.2: Program Cards**              | Show program name, eligibility status, confidence %, key details (income limit, asset limit if applicable) | ✅         | Medium |
| **F5.3: Plain-Language Explanation** | Generate "why you're eligible/ineligible" explanation tied to user's actual data                           | ✅         | Large  |
| **F5.4: Confidence Scoring**         | Calculate confidence based on: data completeness, rule clarity, edge cases                                 | ✅         | Medium |
| **F5.5: Next Steps Guidance**        | Link to document checklist for each matched program                                                        | ✅         | Medium |
| **F5.6: Result Export/Print**        | Generate printable/PDF summary of results                                                                  | ⭐ Phase 3 | Medium |
| **F5.7: Accessibility**              | WCAG 2.1 AA: semantic HTML structure, color + icons for status, screen reader tested                       | ✅         | Small  |
| **F5.8: Error Handling**             | Handle ambiguous rules gracefully; show "contact support" messaging                                        | ✅         | Small  |

**Dependencies**:

- Rules Engine (E2)
- Eligibility Wizard (E4)
- Backend evaluation API

**Test Scenarios** (TDD):

- Same input data → same results every time (deterministic)
- Explanation text references user's specific income ($2,100 not "$X")
- Confidence < 100% when data incomplete or rules ambiguous
- Matched programs listed in order of likelihood
- Screen reader announces: status + program + explanation in logical order
- Print layout renders correctly (no overflow, readability)

**Constitution Alignment**:

- ✅ Code Quality: Results component testable with mock eligibility data
- ✅ Testing: Contract tests validate API output shape; snapshot tests for explanations
- ✅ UX: Plain language verified; no jargon without explanation; accessible to all abilities
- ✅ Performance: Results API ≤2s (cache FPL tables, optimize database queries)

---

### **EPIC E6: Document Management** (Phase 2)

**Goal**: Enable users to upload supporting documents, validate completeness, and generate state-specific checklists showing what's needed for their application.

**Acceptance Criteria**:

- Users see personalized document checklist based on matched programs
- Document types: income (pay stubs, SSA), bank statements, ID, disability, citizenship
- Upload interface: drag-drop or file picker; progress indicator; clear success/error feedback
- Documents validated: file type, size, presence of key fields (OCR in Phase 5)
- Checklist shows: required vs optional items; upload status per item; smart recommendations
- Checklist text plain-language: "Last 2 months of bank statements for account ending in 4321"
- Document upload: ≤5 seconds (p95) for 15MB file
- All documents expire after 90 days

**Features**:

| Feature                                 | Description                                                                         | MVP              | Effort |
| --------------------------------------- | ----------------------------------------------------------------------------------- | ---------------- | ------ |
| **F6.1: Document Checklist Generation** | Generate state-specific required docs based on matched programs                     | ✅               | Medium |
| **F6.2: Checklist Display**             | Show required vs optional, organized by category (income, ID, etc.)                 | ✅               | Small  |
| **F6.3: Upload Interface**              | Drag-drop + file picker; multi-file support; progress bar                           | ✅               | Medium |
| **F6.4: File Validation**               | Check file type (PDF, PNG, JPEG), size (<15MB), magic bytes                         | ✅               | Small  |
| **F6.5: Upload Status Tracking**        | Show which docs uploaded, which missing, which optional                             | ✅               | Medium |
| **F6.6: Basic Validation**              | OCR to detect if name/date present on uploaded doc                                  | ⭐ Phase 5 (E11) | Large  |
| **F6.7: Document Completeness Scoring** | % complete indicator; suggest what's missing before next step                       | ✅               | Small  |
| **F6.8: Smart Recommendations**         | "We suggest uploading a recent pay stub to confirm your income"                     | ⭐ Phase 3       | Small  |
| **F6.9: Accessibility**                 | WCAG 2.1 AA: keyboard-accessible upload, clear error messages, status announcements | ✅               | Small  |

**Dependencies**:

- Document Storage Infrastructure (E3)
- Eligibility Results (E5) to determine required docs
- Backend validation API

**Test Scenarios** (TDD):

- Checklist updates when user matches new program
- Invalid files rejected with clear message (not just "error")
- Upload progress bar appears and completes
- Document marked as "uploaded" immediately; persists on page refresh
- Required vs optional docs clearly distinguished
- User can delete uploaded doc and re-upload
- Expired docs noted in UI

**Constitution Alignment**:

- ✅ Code Quality: Upload component testable with mock API
- ✅ Testing: Integration tests with test blob storage; file validation unit tests
- ✅ UX: WCAG 2.1 AA file input labels, keyboard accessible; error messages actionable
- ✅ Performance: Upload streaming ≤5 seconds; validation instant (client-side + async backend)

---

### **EPIC E7: Admin Portal & Authentication** (Phase 3)

**Goal**: Provide admin dashboard for compliance analysts and content team to manage rules, approve changes, and monitor system health.

**Acceptance Criteria**:

- Admin portal accessible only to Admin/Reviewer/Analyst users
- Dashboard shows system status: active rules, pending approvals, recent changes
- Role-based access: Analysts view rules/data; Reviewers approve changes; Admins manage users
- All admin actions logged for audit compliance
- Performance: Admin pages load ≤3 seconds

**Features**:

| Feature                               | Description                                                      | MVP         | Effort |
| ------------------------------------- | ---------------------------------------------------------------- | ----------- | ------ |
| **F7.1: Admin Login & Authorization** | Separate admin login or role-based access control in main app    | ✅          | Small  |
| **F7.2: Admin Dashboard**             | Overview of system status, pending tasks, recent changes         | ✅          | Medium |
| **F7.3: User Management**             | Create/deactivate admin users; assign roles; audit trail         | ⭐ Phase 3+ | Medium |
| **F7.4: Rule Approval Queue**         | Show pending rule changes; allow approve/reject with comments    | ✅ (E8)     | Medium |
| **F7.5: Compliance Reporting**        | Audit log of all admin actions; exportable for compliance review | ⭐ Phase 3+ | Medium |
| **F7.6: System Health Monitoring**    | Show API uptime, database performance, error rates               | ⭐ Phase 4+ | Medium |

**Dependencies**:

- Authentication with role-based access (E1)
- Rules Engine (E2)
- Rule Management (E8)

**Test Scenarios** (TDD):

- Non-admin users cannot access admin portal
- Admin sees all rules and pending approvals
- Reviewer can approve/reject; changes are logged
- Audit trail shows who changed what when

**Constitution Alignment**:

- ✅ Code Quality: Admin components follow clean architecture
- ✅ Testing: Integration tests with test database; authorization enforcement tested
- ✅ UX: Consistent with public UI; clear call-to-action buttons
- ✅ Performance: Pages load ≤3s; queries use indexed fields (state, status, created_at)

---

### **EPIC E8: Rule Management & Approval Workflow** (Phase 3)

**Goal**: Enable analysts to manually edit rules and admins to approve changes before activation; track all versions and enable rollback.

**Acceptance Criteria**:

- Rule editor: structured UI for income thresholds, asset limits, program eligibility, categoricals
- AI assistance: generate rule from regulation text (Phase 4+)
- Preview: test rule with sample user data before approval
- Versioning: all rule changes tracked; effective date required
- Approval workflow: Analyst proposes → Reviewer approves → Compliance confirms → Rule activated
- Rollback: can revert to previous rule version
- Change detection: flag conflicting rules, data inconsistencies

**Features**:

| Feature                               | Description                                                                | MVP         | Effort |
| ------------------------------------- | -------------------------------------------------------------------------- | ----------- | ------ |
| **F8.1: Rule Editor UI**              | Structured form for income, assets, categoricals; preview rule in JSON     | ✅          | Large  |
| **F8.2: Rule Versioning**             | Track all rule versions; show who changed what when                        | ✅          | Medium |
| **F8.3: Effective Dates**             | Schedule rule activation for future date; current rules remain until then  | ✅          | Medium |
| **F8.4: Rule Preview & Testing**      | Test proposed rule with sample user data before activation                 | ✅          | Medium |
| **F8.5: Approval Workflow**           | Routing to Reviewer; comments; approval/rejection                          | ✅          | Medium |
| **F8.6: Rollback**                    | Revert to previous rule version if new rule causes issues                  | ✅          | Small  |
| **F8.7: Conflict Detection**          | Flag rules that reference non-existent programs or inconsistent thresholds | ⭐ Phase 3+ | Medium |
| **F8.8: AI-Assisted Rule Generation** | Parse regulation PDFs and auto-generate rule suggestions (Phase 4)         | ⭐ Phase 4  | Large  |

**Dependencies**:

- Rules Engine (E2)
- Admin Portal (E7)

**Test Scenarios** (TDD):

- Editor saves valid rule in JSON format
- Preview shows results with sample data
- Rule activation deferred until effective date
- Rollback reverts to previous version; new version becomes alternate draft
- Approval workflow enforces multi-step sign-off
- Conflict detection flags invalid references

**Constitution Alignment**:

- ✅ Code Quality: Rule editor component testable; business logic in handlers (not UI)
- ✅ Testing: Unit tests for rule validation logic; integration tests for workflow
- ✅ UX: Clear form labels, helpful hints, preview shows actual output
- ✅ Performance: Editor loads existing rules quickly; preview evaluates rule ≤2s

---

### **EPIC E9: Regulation Monitoring System** (Phase 4)

**Goal**: Automatically detect Medicaid regulation changes from state agencies and CMS; summarize using AI; flag for admin approval before activation.

**Acceptance Criteria**:

- Periodic crawling of state Medicaid websites and CMS guidance
- Document change detection: compare current vs. previous version
- AI summarization: "Income threshold changed from 130% FPL to 135% FPL"
- Confidence scoring: 0-100% likelihood of material change
- Human approval required before rule update
- Change audit trail: effective dates, rollback history
- Support for multiple sources: state websites, CMS bulletins, SPA documents, PDFs

**Features**:

| Feature                          | Description                                                             | MVP         | Effort |
| -------------------------------- | ----------------------------------------------------------------------- | ----------- | ------ |
| **F9.1: Document Crawler**       | Periodic job to download state PDFs and web content; store versions     | ⭐ Phase 4  | Large  |
| **F9.2: Change Detection**       | Diff previous vs. current version; identify added/removed/modified text | ⭐ Phase 4  | Large  |
| **F9.3: Change Summarization**   | AI (OpenAI/Azure OpenAI) summarizes changes in plain language           | ⭐ Phase 4  | Large  |
| **F9.4: Confidence Scoring**     | Score confidence that change is material (income limit vs. typo fix)    | ⭐ Phase 4  | Medium |
| **F9.5: Alert Dashboard**        | Show detected changes; allow admin to approve/ignore/investigate        | ⭐ Phase 4  | Medium |
| **F9.6: Change History & Audit** | Track all detected changes, approvals, rule updates by change           | ⭐ Phase 4  | Small  |
| **F9.7: Multi-State Coverage**   | Support crawling 50 states + DC with customizable frequency             | ⭐ Phase 4+ | Large  |
| **F9.8: Source Integration**     | Support multiple sources: websites, email alerts, SPA documents, CMS    | ⭐ Phase 4+ | Large  |

**Dependencies**:

- Rule Management & Approval Workflow (E8)
- Admin Portal (E7)
- AI/LLM service (Azure OpenAI Option 1, or future)

**Test Scenarios** (TDD):

- Crawler fetches documents and detects changes vs. baseline
- Change summarization produces expected text (e.g., "threshold changed")
- Approval workflow routes change to correct admin role
- Approved change generates rule update with effective date
- Ignored changes don't create rule changes but are logged

**Constitution Alignment**:

- ✅ Code Quality: Crawler, diff engine, summarization logic separated by responsibility
- ✅ Testing: Unit tests for diff logic; integration tests for crawler + change pipeline
- ✅ UX: Clear change presentation; easy approve/ignore actions
- ✅ Performance: Crawling async (background job); summaries cached for reuse

---

### **EPIC E10: Application Packet Generation** (Phase 5)

**Goal**: Generate state-specific Medicaid application forms pre-filled with user data; include cover letter and submission instructions.

**Acceptance Criteria**:

- PDF forms pre-filled with user's household, income, demographics (where applicable per state)
- Separate forms for each program (Aged, Disabled, MAGI, etc.)
- Cover letter: templated letter explaining application, required documents
- Submission instructions: state-specific (online, mail, in-person, office address)
- User can download or email PDF
- Generation time: ≤10 seconds (server-side)
- Accessible PDFs (tagged, readable by screen readers)

**Features**:

| Feature                             | Description                                                                        | MVP        | Effort |
| ----------------------------------- | ---------------------------------------------------------------------------------- | ---------- | ------ |
| **F10.1: PDF Template Management**  | Store state-specific Medicaid forms (PDF or Handlebars templates)                  | ⭐ Phase 5 | Medium |
| **F10.2: Form Fill & Data Mapping** | Map user answers to form fields; handle state-specific field names                 | ⭐ Phase 5 | Large  |
| **F10.3: Multi-Program Packet**     | Generate separate page for each matched program with relevant fields               | ⭐ Phase 5 | Medium |
| **F10.4: Cover Letter Generation**  | Templated letter including: programs applied for, required docs, submission method | ⭐ Phase 5 | Medium |
| **F10.5: Submission Instructions**  | Customizable per state: mailing address, online portal URL, phones, hours          | ⭐ Phase 5 | Small  |
| **F10.6: Document Index**           | List of uploaded documents referenced in packet                                    | ⭐ Phase 5 | Small  |
| **F10.7: PDF Download & Email**     | User can download packet or email to registered email (Phase 5 accounts)           | ⭐ Phase 5 | Small  |
| **F10.8: Accessibility**            | Accessible PDFs: proper structure, tagged content, alternative text for images     | ⭐ Phase 5 | Medium |

**Dependencies**:

- Document Management (E6)
- PDF generation library (QuestPDF)
- User accounts (E12)

**Test Scenarios** (TDD):

- Form fields pre-filled with user data (no blanks for provided info)
- Missing optional fields left blank (not "[blank]")
- Cover letter includes correct program names, state address, deadline info
- PDF loads in screen readers without errors
- Download/email links work; file size reasonable

**Constitution Alignment**:

- ✅ Code Quality: PDF generation logic testable; template engine separate from business logic
- ✅ Testing: Contract tests for PDF field mapping; visual regression tests for layouts
- ✅ UX: Accessible PDFs; clear download/email buttons; confirmation messages
- ✅ Performance: PDF generation ≤10s; caching templates and pre-compiled forms

---

### **EPIC E11: Document AI Processing** (Phase 5)

**Goal**: Automatically extract information from uploaded documents using OCR; classify document types; score completeness.

**Acceptance Criteria**:

- OCR extraction: detect name, date, account number, income amount from documents
- Document classification: identify pay stub vs. bank statement vs. ID (ML or heuristic)
- Completeness scoring: "50% of required data extracted"; suggestions for missing items
- Validation: flag if extracted data conflicts with user-reported data
- Audit trail: show what was extracted, confidence score, user review option

**Features**:

| Feature                                    | Description                                                                 | MVP        | Effort |
| ------------------------------------------ | --------------------------------------------------------------------------- | ---------- | ------ |
| **F11.1: OCR Engine Setup**                | Integrate Tesseract (or Azure Computer Vision); process images → text       | ⭐ Phase 5 | Medium |
| **F11.2: Text Extraction**                 | Extract key fields: name, date, account number, amounts from extracted text | ⭐ Phase 5 | Large  |
| **F11.3: Document Type Classification**    | Classify document as pay stub, bank statement, ID, etc. (rule-based or ML)  | ⭐ Phase 5 | Large  |
| **F11.4: Data Confidence Scoring**         | Score confidence of extracted data (name 95%, date 87%, amount 72%)         | ⭐ Phase 5 | Medium |
| **F11.5: Validation & Conflict Detection** | Alert if extracted name differs from user-provided name; flag for review    | ⭐ Phase 5 | Medium |
| **F11.6: Completeness Assessment**         | % of required fields extracted per document type                            | ⭐ Phase 5 | Small  |
| **F11.7: User Review Interface**           | Show extracted data; allow user to confirm/correct before saving            | ⭐ Phase 5 | Medium |

**Dependencies**:

- Document Management (E6)
- OCR/CV library (Tesseract, Azure Computer Vision)
- Async background job infrastructure

**Test Scenarios** (TDD):

- OCR extracts name, date, amount correctly from sample documents
- Extracted data confidence scores reasonable (high for clear text, low for blurry)
- Classification identifies document type correctly
- Conflict detection flags missing/conflicting data
- User can review and correct extracted data

**Constitution Alignment**:

- ✅ Code Quality: OCR processing logic separated from business logic; testable with mock OCR
- ✅ Testing: Unit tests with sample document images; integration tests for full pipeline
- ✅ UX: Clear display of extracted data; user control over corrections
- ✅ Performance: OCR async (background job); UI updates via WebSocket/polling

---

### **EPIC E12: User Accounts & Session Management** (Phase 5)

**Goal**: Enable users to create accounts, save progress, and resume applications across sessions.

**Acceptance Criteria**:

- Optional signup (users can apply without account)
- Account creation: email + password (or OAuth identity provider in future)
- Session persistence: save wizard answers, document uploads, results
- Resume: user logs in → auto-populates previous answers
- Account security: encrypted passwords, session timeout, logout

**Features**:

| Feature                             | Description                                                         | MVP         | Effort |
| ----------------------------------- | ------------------------------------------------------------------- | ----------- | ------ |
| **F12.1: Account Registration**     | Email + password signup form; email confirmation                    | ⭐ Phase 5  | Medium |
| **F12.2: Login & Session**          | Login form; session established; token refresh                      | ⭐ Phase 5  | Medium |
| **F12.3: Session Persistence**      | Save wizard answers, documents, results to user account             | ⭐ Phase 5  | Medium |
| **F12.4: Resume Application**       | Login → previous application auto-loaded; can continue or start new | ⭐ Phase 5  | Small  |
| **F12.5: Application History**      | User dashboard showing previous applications, results, documents    | ⭐ Phase 5  | Medium |
| **F12.6: Logout & Session Timeout** | Logout button; auto-logout after 30 min inactivity                  | ⭐ Phase 5  | Small  |
| **F12.7: OAuth Identity Providers** | Sign in with Google/Microsoft (future enhancement)                  | ⭐ Phase 5+ | Medium |

**Dependencies**:

- Authentication (E1) with JWT support
- Database for user accounts
- Frontend user management

**Test Scenarios** (TDD):

- Registration creates user account; email confirmation works
- Login generates valid session token
- Wizard answers saved on submit; resume loads previous answers
- Logout clears session; user must login to see previous app
- Session timeout after 30 min inactivity

**Constitution Alignment**:

- ✅ Code Quality: Authentication handlers testable; token logic separated
- ✅ Testing: Integration tests for signup, login, session persistence
- ✅ UX: Clear signup/login flows; password strength indicators
- ✅ Performance: Session lookup <100ms; resume load instant

---

### **EPIC E13: Notification System** (Phase 5)

**Goal**: Notify users and admins of important events: rule changes, missing documents, application status updates.

**Acceptance Criteria**:

- User notifications: rule changes affecting their application, missing documents
- Admin notifications: regulatory changes detected, pending approvals, error alerts
- Delivery: email + in-app alerts + optional SMS (future)
- Opt-in/opt-out: users control notification preferences
- Audit trail: track notification sends for compliance

**Features**:

| Feature                           | Description                                                                  | MVP         | Effort |
| --------------------------------- | ---------------------------------------------------------------------------- | ----------- | ------ |
| **F13.1: Notification Queue**     | Background job queue for async notification delivery                         | ⭐ Phase 5  | Medium |
| **F13.2: Email Delivery**         | Send emails via SMTP or cloud email service (SendGrid, Azure)                | ⭐ Phase 5  | Small  |
| **F13.3: In-App Alerts**          | Display alerts on user dashboard; mark as read                               | ⭐ Phase 5  | Medium |
| **F13.4: Notification Templates** | Customizable email/alert templates per event type (rule change, missing doc) | ⭐ Phase 5  | Small  |
| **F13.5: User Preferences**       | Allow users to enable/disable notifications for specific events              | ⭐ Phase 5  | Small  |
| **F13.6: Admin Alerts**           | Notify admins of: pending approvals, regulation changes, system errors       | ⭐ Phase 5  | Small  |
| **F13.7: SMS Notifications**      | Send SMS alerts for critical events (future)                                 | ⭐ Phase 5+ | Medium |

**Dependencies**:

- User Accounts (E12)
- Notification delivery service (SendGrid, Azure, AWS SES)
- Background job infrastructure

**Test Scenarios** (TDD):

- Email sent on rule change event
- In-app alert appears and persists until marked read
- User disables notifications → no more emails
- Admin sees pending approval alert

**Constitution Alignment**:

- ✅ Code Quality: Notification handlers testable; queue pattern decouples send
- ✅ Testing: Unit tests for template rendering; integration tests for delivery
- ✅ UX: Clear notification content; actionable (links to relevant pages)
- ✅ Performance: Notifications async; send ≤5 seconds after event

---

### **EPIC E14: Analytics Dashboard & Metrics** (Phase 6)

**Goal**: Track system health and user engagement metrics to inform product decisions and compliance reporting.

**Acceptance Criteria**:

- Dashboards for admins and product team: usage, drop-off, performance
- Metrics: eligibility checks by state, wizard completion rate, avg time to results, document upload rate
- Performance monitoring: response times by endpoint, error rates, database performance
- Compliance reporting: audit events, rule change history, user consent logs

**Features**:

| Feature                               | Description                                                                     | MVP        | Effort |
| ------------------------------------- | ------------------------------------------------------------------------------- | ---------- | ------ |
| **F14.1: Event Logging**              | Log: wizard starts, step completes, eligibility evaluated, documents uploaded   | ⭐ Phase 6 | Small  |
| **F14.2: Metrics Pipeline**           | Aggregate events into metrics (counts, averages, percentiles)                   | ⭐ Phase 6 | Medium |
| **F14.3: Admin Dashboard**            | Visualize key metrics: total checks, checks by state, completion rate, avg time | ⭐ Phase 6 | Medium |
| **F14.4: Performance Dashboard**      | Track p50/p95/p99 latencies by endpoint; error rates; database queries          | ⭐ Phase 6 | Medium |
| **F14.5: Drop-Off Analysis**          | Identify where users abandon the wizard (which step has highest exit rate)      | ⭐ Phase 6 | Medium |
| **F14.6: Program Matching Analytics** | % of users matched to each program; most common ineligibility reasons           | ⭐ Phase 6 | Small  |
| **F14.7: Document Analytics**         | % of users completing document upload; common document types; missing docs      | ⭐ Phase 6 | Small  |
| **F14.8: Compliance Audit Log**       | Export audit events for compliance review                                       | ⭐ Phase 6 | Small  |

**Dependencies**:

- Event logging infrastructure (Serilog, Application Insights)
- Analytics database (PostgreSQL analytics views)
- Visualization (Grafana, Power BI, or custom dashboard)

**Test Scenarios** (TDD):

- Events logged on user actions (start, complete step, etc.)
- Metrics aggregated correctly (counts, averages)
- Dashboard displays correct totals and trends
- Performance metrics reflect actual API latencies

**Constitution Alignment**:

- ✅ Code Quality: Event logging consistent; analytics queries readable
- ✅ Testing: Unit tests for metric aggregation; data integrity tests
- ✅ UX: Clear charts, actionable insights
- ✅ Performance: Analytics queries don't impact production (read replicas or batch jobs)

---

## Feature Dependencies & Sequencing

### Dependency Graph

```
Phase 1 Foundation
├── E1: Authentication & Sessions
│   └─> [provides JWT, roles, session mgmt for all downstream]
├── E2: Rules Engine & State Data
│   └─> [provides eligibility evaluation for E4, E5, E6, E8, E9]
└── E3: Document Storage Infrastructure
    └─> [provides blob storage for E6, E10, E11]

Phase 2 MVP Launch
├── E4: Eligibility Wizard UI
│   ├─> requires: E1, E2
│   └─> enables: E5
├── E5: Eligibility Evaluation & Results
│   ├─> requires: E2, E4
│   └─> enables: E6
└── E6: Document Management
    ├─> requires: E3, E1, E5
    └─> enables: E8, E9, E10

Phase 3 Admin & Compliance
├── E7: Admin Portal & Authentication
│   ├─> requires: E1, E2
│   └─> enables: E8
└── E8: Rule Management & Approval Workflow
    ├─> requires: E2, E7
    └─> enables: E9

Phase 4 Automation
└── E9: Regulation Monitoring System
    ├─> requires: E8, E7
    └─> feeds-into: E8 (proposes rule changes)

Phase 5 Enhanced User Experience
├── E10: Application Packet Generation
│   ├─> requires: E6, E5, E11
│   └─> nice-to-have
├── E11: Document AI Processing
│   ├─> requires: E6
│   └─> improves: E6 validation
├── E12: User Accounts & Session Management
│   ├─> requires: E1
│   └─> enables: E13, E4/E5/E6 (save/resume)
└── E13: Notification System
    ├─> requires: E12, E9 (regulatory changes), E6 (missing docs)
    └─> optional-for-MVP

Phase 6 Analytics & Insights
└── E14: Analytics Dashboard & Metrics
    ├─> requires: All phases (event logging across system)
    └─> informational-only
```

### Critical Path to MVP

**Minimum viable sequence** (3-4 month timeline):

1. **Phase 1 (Weeks 1-6)**: Foundation
   - E1: Authentication & Sessions (can proceed in parallel)
   - E2: Rules Engine & State Data (pilot states: IL, CA, NY, TX, FL)
   - E3: Document Storage Infrastructure

2. **Phase 2 (Weeks 7-14)**: MVP Launch
   - E4: Eligibility Wizard UI (feedback loop with E2 for question taxonomy)
   - E5: Eligibility Evaluation & Results (depends on E2 completion)
   - E6: Document Management (depends on E3 completion)

3. **Phase 3 (Weeks 15-20)**: Admin Tools (allows broader state coverage)
   - E7: Admin Portal & Authentication (parallel with Phase 2)
   - E8: Rule Management & Approval Workflow (parallel with Phase 2)

**Go-Live**: End of Phase 2 with 5 pilot states + basic admin rule management

**Post-Launch** (Phases 4-6): Automation, advanced UX, analytics

---

## MVP vs. Future Releases

### MVP Definition (Weeks 1-14, Target: Early Q2 2026)

**In Scope**:

- ✅ E1: Full authentication & session management (anonymous + optional accounts)
- ✅ E2: Rules engine with 5 pilot states (IL, CA, NY, TX, FL)
- ✅ E3: Document blob storage + basic validation
- ✅ E4: Eligibility wizard (all question types, conditional flow, mobile-responsive, WCAG 2.1 AA)
- ✅ E5: Eligibility results (status, programs, plain-language explanation)
- ✅ E6: Document checklist & upload (validated, basic completeness check)
- ⚠️ E7/E8: Admin portal & rule editor (minimal: manual JSON editing, approval workflow)

**Out of Scope** (Marked ⭐ Phase X):

- E9: Regulation monitoring automation (Phase 4)
- E10: PDF application packet generation (Phase 5)
- E11: OCR document processing (Phase 5)
- E12: User accounts with resume (Phase 5; optional signup in Phase 2 basic form)
- E13: Email notification system (Phase 5)
- E14: Analytics dashboards (Phase 6)

**User-Facing MVP Acceptance**:

- Individuals can determine Medicaid eligibility in <5 minutes
- Eligibility result clear and actionable
- Users can upload required documents
- Mobile app functional on iOS/Android browsers
- System measurably builds trust in AA brand
- 0 accessibility violations (WCAG 2.1 AA)

### Phase 3+ Expansion (Post-MVP)

**Phase 3** (Weeks 21-26):

- Full admin portal with authorization
- Regulation monitoring (manual detection, AI summarization)
- Broader state coverage via admin rule editor

**Phase 4** (Weeks 27-34):

- Automatic regulation crawling & change detection
- OCR document processing
- Application packet PDF generation

**Phase 5** (Weeks 35-42):

- User accounts with full session save/resume
- Email notifications
- Advanced document validation

**Phase 6+**:

- Analytics dashboards for product team
- Compliance reporting tools
- Expansion to all 50 states + territories
- Advanced AI: regulation summarization, rule auto-generation from PDFs

---

## Implementation Roadmap

### Timeline Overview

```
Q1 2026                      Q2 2026                      Q3 2026+
Jan    Feb    Mar    Apr     May    Jun    Jul    Aug    Sep+

E1-E3  E1-E3  E4-E6  E4-E6   E7-E8  E7-E8  E9     E10-E12 E13-E14
Phase1                Phase2         Phase3 Phase4 Phase5   Phase6

🚀MVP Launch Target: Late April 2026
     5 pilot states, core wizard
```

### Phase 1: Foundation (Weeks 1-6)

- **Team**: Backend, DevOps, Database
- **Deliverables**:
  - PostgreSQL database schema + migrations
  - .NET Core API with dependency injection setup
  - Azure Key Vault integration; secrets management
  - React + Vite frontend scaffold
  - Authentication endpoints (session/JWT)
  - Rules engine core logic + sample rules for 1 pilot state

### Phase 2: MVP (Weeks 7-14)

- **Team**: Frontend, Backend, UX/Design
- **Deliverables**:
  - Landing page + state selection (IP geolocation + ZIP override)
  - Eligibility wizard (5+ question types, conditional flow)
  - Results page with program matching + explanation
  - Document upload + validation
  - Mobile-responsive, WCAG 2.1 AA compliant
  - Sample rules for 5 pilot states deployed
  - Beta/staging environment ready

### Phase 3: Admin & Compliance (Weeks 15-20)

- **Team**: Backend, Frontend, Product
- **Deliverables**:
  - Admin portal with rule editor
  - Approval workflow (analyst propose → reviewer approve)
  - Manual regulation monitoring + alerting
  - Expanded state coverage (10+ states)

### Phase 4: Automation (Weeks 21-28)

- **Team**: Backend, AI/ML, Platform
- **Deliverables**:
  - Automated web crawling for state PDFs
  - Change detection system
  - AI-powered summarization (OpenAI integration)
  - PDF application packet generation

### Phase 5: Enhanced UX (Weeks 29-36)

- **Team**: Frontend, Backend, Platform
- **Deliverables**:
  - User registration + accounts
  - Session save/resume
  - OCR document processing
  - Email notifications
  - Broader state coverage (30+ states)

### Phase 6: Analytics & Scale (Weeks 37+)

- **Team**: Analytics, Platform
- **Deliverables**:
  - Analytics dashboards
  - Performance monitoring
  - Scale to all 50 states + territories
  - Compliance reporting exports

---

### Success Criteria by Phase

#### Phase 1 Success

- ✅ Authentication working (anonymous + JWT)
- ✅ Rules engine evaluates eligibility correctly for 1 state
- ✅ CI/CD pipeline green (tests passing, no lint errors)
- ✅ Database migrations tested

#### Phase 2 Success (MVP Launch)

- ✅ Wizard completion rate ≥70% (users finish without quitting)
- ✅ Avg time to eligibility result ≤5 minutes
- ✅ Results understood by users (usability testing feedback positive)
- ✅ 0 accessibility violations (WCAG 2.1 AA)
- ✅ Mobile-responsive (all breakpoints tested)
- ✅ Document upload rate ≥50% (users upload at least 1 doc)
- ✅ System performance: p95 latency ≤2 seconds
- ✅ Marketing: brand attribution lift measurable

#### Phase 3 Success

- ✅ 10+ states live with rules
- ✅ Admin can edit rules without engineering
- ✅ Rule approval workflow used by compliance team
- ✅ No unintended rule changes (approval workflow prevents)

#### Phase 4 Success

- ✅ Regulation monitoring catches 90% of material changes
- ✅ Admin review time reduced (AI summary saves 30 min per change)
- ✅ Zero missed regulation updates that impact eligibility

#### Phase 5 Success

- ✅ User account adoption ≥30% (optional, but used)
- ✅ Session saved/resumed rate ≥50% (users resume interrupted applications)
- ✅ Document completeness score improves 25% (OCR assists validation)
- ✅ Email notification opt-in ≥60%

#### Phase 6 Success

- ✅ All states live (50 + DC + territories)
- ✅ Analytics show trends: drop-off points fixed
- ✅ Compliance audit trail complete (certifiable)
- ✅ Brand awareness lift sustained

---

## How to Use This Roadmap

### For Product Managers

1. **Scope decisions**: Use Phase breakdown to prioritize MVP vs. future work
2. **Stakeholder management**: Reference timeline and success criteria
3. **State selection**: Recommend starting with IL, CA, NY, TX, FL (diverse programs)

### For Engineering

1. **Feature specifications**: Each epic links to specific user stories (stored in `/specs/` as you create them)
2. **Sprint planning**: Dependency graph shows what can run in parallel
3. **Constitution compliance**: Each epic has Constitution Alignment checklist

### For Project Planning

1. **Resource allocation**: Phase team assignments guide hiring/staffing
2. **Risk management**: Critical path (E1→E2→E4→E5→E6) shows where delays cascade
3. **Release planning**: MVP gate defined; post-launch roadmap clear

---

## Next Steps

1. **Validate Epics**: Review with product team; refine epic boundaries if needed
2. **Estimate Efforts**: Assign t-shirt sizes (S/M/L/XL) or story points per feature
3. **Create Feature Specs**: For each epic/feature, run `/speckit.specify` to create detailed spec
4. **Plan Phase 1**: Define sprint schedule; assign teams; kick off E1-E3
5. **Track Progress**: Update this document as features complete

---

**Version**: 1.0  
**Last Updated**: 2026-02-08  
**Status**: Ready for Team Review  
**Next Review**: After Phase 1 (End of Week 6)
