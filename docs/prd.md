Medicaid Application Assistant (Internal)

1. Purpose & Strategic Context
   Purpose
   Create a consumer-facing, nationwide web application that:

Helps individuals (or caregivers) determine Medicaid eligibility

Guides them through state-specific requirements

Collects required supporting documentation

Produces a ready-to-submit Medicaid application packet

Strategic Goal
Brand awareness and trust-building for AA in healthcare eligibility

Demonstrate deep eligibility, compliance, and automation expertise

Serve as a lead funnel for downstream eligibility/RCM solutions (indirect)

Non-Goals (v1)
Submitting applications directly to state systems

Acting as legal advice (must include disclaimers)

Replacing state Medicaid portals

2. Target Users & Personas
   Primary Users
   Individuals applying for Medicaid

Family members / caregivers

Social workers / case managers assisting patients

Secondary Users (Internal)
Compliance analysts (review rules)

Content/admin team (approve changes)

Product/AI reviewers

3. High-Level User Journey
   Landing Page

What Medicaid is

Who may qualify

“Check your eligibility” CTA

State Identification

Auto-detect by ZIP (override allowed)

Explain state-specific differences early

Eligibility Wizard (Multi-Step)

Household & demographics

Income & assets

Disability / age / pregnancy status

Citizenship & residency

Special programs (MAGI, LTC, waiver, spend-down)

Eligibility Result

Likely eligible / possibly eligible / unlikely eligible

Program(s) matched

Confidence score + explanation

Document Collection

Personalized checklist

Upload evidence

AI-assisted validation

Application Packet Generation

State-specific forms (pre-filled)

Cover letter

Submission instructions

Optional Follow-ups

Save progress

Get notified about missing items

Updates if rules change (opt-in)

4. Functional Requirements
   4.1 State-Aware Eligibility Engine
   Core Capabilities
   Evaluate eligibility based on:

State

Household size

Income (monthly/annual, sources)

Assets (where applicable)

Age / disability

Medicaid category (MAGI vs non-MAGI)

Rule Engine Requirements
Rules must be:

State-specific

Versioned

Date-effective

Support overrides and edge cases

Return:

Eligibility status

Matched Medicaid programs

Explanation in plain English

Example Output
“Based on your income, household size, and age, you are likely eligible for Illinois Aged Medicaid, but additional asset documentation is required.”

4.2 Dynamic Multi-Step Questionnaire
Requirements
Question flow must be:

Conditional (answers unlock next questions)

State-specific

Program-specific

Support:

Progress indicator

Save & resume

Backtracking without data loss

AI Role
Rewrite questions in plain language

Explain “why we ask this”

Detect inconsistent answers and prompt clarification

4.3 Document & Evidence Management
Supported Document Types
Proof of income (pay stubs, SSA letters)

Bank statements

ID / residency proof

Disability documentation

Immigration/citizenship docs

Features
Upload via web/mobile

AI-assisted classification:

Document type detection

Completeness check

Basic OCR & validation (name/date present)

State-specific checklist generation

Output
“You still need: last 2 months of bank statements for account ending in 4321”

4.4 Application Packet Generator
Capabilities
Generate:

State Medicaid application form (PDF)

Pre-filled where possible

Supporting document index

Cover letter with submission instructions

State Variability
Different forms per state

Different submission instructions:

Online

Mail

In-person

Include human-readable checklist for submission

4.5 Self-Improving Regulation Monitoring (Key Innovation)
Objective
Automatically detect Medicaid rule changes by state and flag them for review.

Sources
State Medicaid agency websites

CMS guidance

State plan amendments (SPAs)

Public bulletins / PDFs

AI Workflow
Periodic crawling & document ingestion

Change detection vs prior versions

AI summary:

“Income threshold changed”

“New documentation requirement added”

Confidence scoring

Human approval step

Rules version updated and activated

Auditability
Change history per state

Effective date tracking

Rollback support

4.6 Notifications & Updates (Optional)
Notify users if:

A rule changes while application is in progress

A required document becomes invalid

Notify admins of:

Detected regulation changes

Rule conflicts or ambiguity

5. Admin & Internal Tools
   Admin Portal
   State rule editor (structured + AI-assisted)

Approval workflow for detected changes

Question flow preview

Content overrides

Analytics dashboard

Analytics
Number of eligibility checks by state

Drop-off points in wizard

Common disqualification reasons

Document failure rates

6. Data Model (High-Level)
   Core Entities
   User (anonymous or optional account)

Session / Application

State

Medicaid Program

Eligibility Rule (versioned)

Question

Answer

Document

Application Packet

Regulation Change Log

7. Compliance, Legal & UX Safeguards
   Disclaimers
   “This tool provides guidance, not legal advice”

“Final eligibility determined by the state”

Privacy
Minimal PII retention

Auto-expiration of uploaded docs (e.g., 30–90 days)

Encryption at rest & in transit

Accessibility
WCAG 2.1 compliance

Plain language mode

8. Non-Functional Requirements
   Mobile-first responsive UI

Stateless eligibility evaluation (where possible)

High availability (public-facing)

Explainability required for all eligibility outcomes

Clear error handling for ambiguous rules

9. MVP Scope (Recommended for Competition)
   Must-Have
   3–5 pilot states

Dynamic eligibility wizard

Eligibility result with explanation

Document checklist & uploads

Manual rule config + AI-assisted change detection (read-only)

Nice-to-Have
OCR validation

Full application packet generation

User notifications

Broader state coverage

10. Success Metrics
    Completion rate of eligibility wizard

Avg time to eligibility result

Document completeness rate

Engagement by state

Brand attribution (AA awareness lift)
