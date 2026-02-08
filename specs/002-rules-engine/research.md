# Phase 0 Research: Rules Engine & State Data

**Date**: 2026-02-09  
**Status**: Planning Document (Research Tasks TBD - Execution by Team)  
**Purpose**: Identify and resolve unknowns before design phase begins

---

## Research Tasks

### Task 1: Pilot State Rules Analysis

**Objective**: Gather official Medicaid eligibility documentation for 5 pilot states

**States to Research**: Illinois (IL), California (CA), New York (NY), Texas (TX), Florida (FL)

**Information to Collect**:

For each state, identify and document:

1. **MAGI vs Non-MAGI Pathway Split**
   - Which populations use MAGI (Modified Adjusted Gross Income)?
   - Which use non-MAGI (Aged, Blind, Disabled)?
   - How do states allocate applicants?

2. **Income Thresholds** (document as % of FPL)
   - MAGI Adult threshold
   - Aged Medicaid threshold
   - Disabled Medicaid threshold
   - Pregnancy-Related Medicaid threshold
   - Any state-specific programs and their thresholds

3. **Categorical Eligibility Rules**
   - SSI (Supplemental Security Income) recipients: automatic categorical eligibility?
   - Age cutoffs (65+ typically = Aged pathway)
   - Disability determination method
   - Pregnancy assumptions and duration

4. **Asset Tests** (if applicable)
   - Non-MAGI pathways (Aged, Disabled) often have asset limits
   - Exact limits and what counts as "asset"
   - Married couple rules

5. **State-Specific Programs**
   - Any unique programs not in other states
   - E.g., "Texas Health Steps" (children pathway)
   - Emergency Medicaid, Refugee Medicaid

**Information Sources**:
- CMS.gov State Medicaid Manuals (SMM)
- State agency websites (DHHS, Department of Human Services)
- Medicaid Income Limit Charts (published annually)
- State Plan Amendments (SPA)

**Outcome**: Document comparing 5 states with unified structure (template below)

**Template**:
```
## [State] Medicaid Eligibility Rules (2026)

### MAGI Pathways
- MAGI Adult: XXX% FPL = $XXX/month (household of 1)
- Pregnant: XXX% FPL = $XXX/month
- Parent/Caregiver: XXX% FPL (if applicable)

### Non-MAGI Pathways
- Aged (65+): $XXX/month, assets: $XXX
- Disabled: $XXX/month, assets: $XXX
- Blind (if separate): ...

### Categorical Eligibility
- SSI recipients: Automatic (Y/N)
- Pregnancy assumption: Duration

### State Programs
- [Program Name]: [eligibility description]

### Data Completeness
- Household size considerations (1-8+)
- Spousal rules (married couples)
- Child-only households
```

---

### Task 2: Federal Poverty Level (FPL) Tables - 2026

**Objective**: Obtain and validate 2026 FPL values for income threshold calculations

**Data Requirements**:

1. **Baseline FPL (48 Contiguous States + DC)**
   - Household sizes 1 through 8+
   - Annual amounts (will convert to monthly in code)
   - Source: HHS Poverty Guidelines (typically published in January)

2. **State Adjustments** (for future expansion)
   - Alaska: multiplier ~1.25
   - Hawaii: multiplier ~1.15
   - Other territories (if applicable)

3. **Effective Dates**
   - 2026 FPL effective date
   - When old FPL is no longer used
   - Transition period handling

**Outcome**: JSON file with 2026 data properly structured for database seeding

**Example Structure**:
```json
{
  "year": 2026,
  "effective_date": "2026-01-01",
  "baseline": [
    { "household_size": 1, "annual_income": 14580 },
    { "household_size": 2, "annual_income": 19720 },
    { "household_size": 3, "annual_income": 24860 },
    { "household_size": 4, "annual_income": 30000 },
    ...
    { "household_size": 8, "annual_income": 60660 }
  ],
  "perPersonIncrement": 6140,
  "stateAdjustments": {
    "AK": { "multiplier": 1.25 },
    "HI": { "multiplier": 1.15 }
  }
}
```

---

### Task 3: Evaluate Rule Engine Libraries

**Objective**: Compare options for evaluating Medicaid eligibility rules

**Candidates**:

#### Option A: JSONLogic.Net
- **Pros**: 
  - JSON-based rules are human-readable
  - No compilation required for rule changes
  - Good for admin-editable rules (Phase 3 feature)
  - Mature library with active maintenance
- **Cons**:
  - Learning curve for rules authors
  - Limited expressiveness (no loops, limited functions)
  - Performance overhead for complex logic
- **Best For**: MVP where rules are relatively simple and admin editability is planned

#### Option B: Custom C# Expression Evaluator
- **Pros**:
  - Full language expressiveness (loops, custom functions)
  - Maximum performance (no serialization overhead)
  - Easy to write and debug
- **Cons**:
  - Admin editability requires custom UI (much harder)
  - Code compilation needed for rule changes
  - Requires more developer time
- **Best For**: Rigid rules that rarely change (not ideal for MVP with admin editor planned)

#### Option C: Hybrid Approach
- **Pros**:
  - Simple rules via JSONLogic (inline income checks)
  - Complex rules via C# code (specialized pathways)
  - Admin can edit simple rules, developers handle complex
- **Cons**:
  - Maintenance burden of two systems
  - Complexity increases testing surface area
- **Best For**: Future consideration if JSONLogic becomes bottleneck

**Decision Framework**:
- MVP Requirements: Admin-editable rules → **JSONLogic.Net preferred**
- Performance target: ≤2s evaluation (test both libraries with full rule set)
- Testing ease: Which is easier to unit test in isolation?

**Outcome**: Recommendation document with code examples and performance benchmarks side-by-side

---

### Task 4: Explanation Template Design

**Objective**: Define template structure for generating plain-language eligibility explanations

**Template Categories**:

1. **Likely Eligible** (user qualifies for program)
   - Template: "You likely qualify for [program_name] because your monthly income ($X) is below the limit of $Y for a household of [size]."
   - Variables: {income}, {threshold}, {program_name}, {household_size}, {percentage_of_fpl}

2. **Possibly Eligible** (qualifies but needs documentation verification)
   - Template: "You may qualify for [program_name] if you can provide proof of [requirement]. Your income ($X) would be below the limit."
   - Variables: {program_name}, {income}, {threshold}, {required_documentation}

3. **Unlikely Eligible** (doesn't meet criteria)
   - Template: "You are unlikely to qualify for [program_name] because your income ($X) exceeds the limit by ($Y) for a household of [size]."
   - Variables: {program_name}, {income}, {thresholdAmount}, {excess_amount}, {household_size}

4. **Special Cases**
   - Categorical eligibility (SSI): "You automatically qualify for Disabled Medicaid because you receive Social Security Income"
   - Multiple disqualifications: "You don't qualify because: (1) income too high, (2) not a state resident"

**Jargon Dictionary** (plain language replacements):
- MAGI → "Modified Adjusted Gross Income" (or just use "income")
- FPL → "Federal Poverty Level" (with explanation on first use)
- SSI → "Social Security Income benefits"
- Asset test → "having too much in savings or property"
- Categorically eligible → "automatically qualifies"
- Medicaid expansion → "expanded Medicaid eligibility"

**Readability Target**: Flesch-Kincaid Grade 8 or lower
- Automated check: [Flesch-Kincaid Calculator](https://www.readabilityformulas.com/flesch-kincaid-grade-level.php)
- Manual review by compliance analyst

**Outcome**: Template library with 20+ concrete examples covering all pathways

---

## Pre-Design Checklist

Before proceeding to Phase 1 (Design), verify:

- [ ] All 5 pilot states documented (IL, CA, NY, TX, FL) with income thresholds, programs, categoricals
- [ ] 2026 FPL tables obtained and validated
- [ ] Rule engine library recommended (JSONLogic.Net preferred per MVP requirements)
- [ ] Explanation template library created with jargon dictionary
- [ ] Sample rules written for 1 test state (IL) to validate engine choice
- [ ] UI mockup for rule editor reviewed (if planning admin interface)

---

## Timeline Estimate

**Research Phase (Week 1-2)**:
- Day 1-2: Gather state documentation online
- Day 3-4: Consolidate rules into unified format
- Day 5: Obtain FPL tables
- Day 6-7: Library evaluation and benchmarking
- Day 8-10: Template design and readability validation

**Blockers**: 
- State documentations may be outdated or unclear → Escalate to compliance analyst for interpretation
- FPL tables not published yet → Use 2025 as baseline, update once 2026 available

---

## Next: Phase 1 (Design)

Once all research tasks complete, proceed to:
1. `[data-model.md](./data-model.md)` - Database schema with pilot state rules embedded
2. `[contracts/rules-api.openapi.yaml](./contracts/rules-api.openapi.yaml)` - API endpoint contracts
3. `[quickstart.md](./quickstart.md)` - Developer setup guide

