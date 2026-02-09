# Data Model: Rules Engine & State Data (E2)

**Created**: 2026-02-09  
**Status**: Phase 1 Design  
**Purpose**: Define database schema for eligibility rules, FPL tables, and evaluation results

---

## Domain Entities

### 1. MedicaidProgram

**Purpose**: Represents a Medicaid program offered by a state (e.g., "MAGI Adult", "Aged Medicaid")

**Attributes**:
- `program_id` (UUID, primary key)
- `state_code` (VARCHAR(2), NOT NULL, FK: State)
- `program_name` (VARCHAR(100), NOT NULL) - e.g., "MAGI Adult", "Aged Medicaid"
- `program_code` (VARCHAR(20), UNIQUE) - e.g., "IL_MAGI_ADULT"
- `eligibility_pathway` (ENUM: MAGI, NonMAGI_Aged, NonMAGI_Disabled, SSI_Linked, Pregnancy, Other)
- `description` (TEXT) - Plain-language program description
- `created_at` (TIMESTAMP, NOT NULL)
- `updated_at` (TIMESTAMP, NOT NULL)

**Indexes**:
- UNIQUE(state_code, program_name)
- INDEX(state_code, eligibility_pathway)

**Example Data**:
```
| program_id | state_code | program_name | eligibility_pathway | description |
|------------|------------|--------------|-------------------|-------------|
| uuid-1     | IL         | MAGI Adult   | MAGI              | Income-based program for non-elderly adults |
| uuid-2     | IL         | Aged Medicaid| NonMAGI_Aged      | For individuals 65 and older |
| uuid-3     | CA         | MAGI Adult   | MAGI              | California's MAGI expansion program |
```

---

### 2. EligibilityRule

**Purpose**: Stores versioned rules for a program's eligibility determination

**Attributes**:
- `rule_id` (UUID, primary key)
- `program_id` (UUID, NOT NULL, FK: MedicaidProgram)
- `state_code` (VARCHAR(2), NOT NULL) - Denormalized for query efficiency
- `rule_name` (VARCHAR(100), NOT NULL) - e.g., "IL MAGI Adult Income Threshold 2026"
- `version` (DECIMAL(4,2), NOT NULL) - e.g., 1.0, 1.1, 2.0
- `rule_logic` (JSON, NOT NULL) - Rule evaluation logic (JSONLogic format or custom DSL)
- `effective_date` (DATE, NOT NULL) - When rule becomes active
- `end_date` (DATE, NULL) - When rule is superseded (NULL = still active)
- `created_by` (UUID, FK: User) - Admin who created the rule  
- `created_at` (TIMESTAMP, NOT NULL)
- `updated_at` (TIMESTAMP, NOT NULL)
- `description` (TEXT) - Plain-language summary of the rule

**Indexes**:
- INDEX(program_id, effective_date, end_date) - For finding active rules
- INDEX(state_code, effective_date)
- UNIQUE(program_id, version) - Ensure unique versions per program

**Rule Logic Example** (JSONLogic format):
```json
{
  "rule_id": "uuid-123",
  "program_id": "uuid-1",
  "rule_logic": {
    "if": [
      { "and": [
        { "<=": [ { "var": "monthly_income" }, 2500 ] },
        { ">=": [ { "var": "age" }, 18 ] },
        { "<=": [ { "var": "age" }, 64 ] },
        { "==": [ { "var": "is_citizen" }, true ] }
      ]},
      { "confidence_score": 95, "status": "Likely Eligible" },
      { "confidence_score": 0, "status": "Unlikely Eligible" }
    ]
  },
  "effective_date": "2026-01-01",
  "version": 1.0
}
```

**Rationale**: 
- Version field enables rule versioning for audit trail
- effective_date allows scheduling future rules
- JSON storage enables flexible rule logic without schema changes
- Denormalized state_code for query efficiency (state-specific dashboards)

---

### 3. FederalPovertyLevel

**Purpose**: Stores FPL thresholds for income calculations (updated annually)

**Attributes**:
- `fpl_id` (UUID, primary key)
- `year` (INTEGER, NOT NULL) - e.g., 2026
- `household_size` (INTEGER, NOT NULL) - 1 to 8+ (use 8 for "8 or more")
- `annual_income_cents` (BIGINT, NOT NULL) - Annual FPL * 100 for precision (avoids decimal rounding)
- `state_code` (VARCHAR(2), NULL) - NULL for baseline states, filled for AK/HI adjustments
- `adjustment_multiplier` (DECIMAL(3,2), NULL) - e.g., 1.25 for Alaska
- `created_at` (TIMESTAMP, NOT NULL)
- `updated_at` (TIMESTAMP, NOT NULL)

**Indexes**:
- UNIQUE(year, household_size, state_code)
- INDEX(year, household_size) - For baseline lookups

**Example Data** (2026 FPL):
```
| year | household_size | annual_income_cents | state_code | adjustment_multiplier |
|------|---|---|---|---|
| 2026 | 1 | 1458000 | NULL | NULL |
| 2026 | 2 | 1972000 | NULL | NULL |
| 2026 | 8 | 6066000 | NULL | NULL |
| 2026 | 8 | 7582500 | AK   | 1.25 |
| 2026 | 8 | 6975900 | HI   | 1.15 |
```

**Rationale**:
- annual_income_cents avoids floating-point precision issues
- Explicit effective dates enable FPL updates without breaking historical data
- State code allows Alaska/Hawaii adjustments without separate tables

---

### 4. UserEligibilityInput (DTO/Value Object)

**Purpose**: Represents user-submitted data for eligibility evaluation (not persisted; input validation model)

**Attributes**:
- `state_code` (VARCHAR(2), NOT NULL)
- `household_size` (INT, NOT NULL) - 1 to 8+
- `monthly_income_cents` (BIGINT, NOT NULL) - Monthly income * 100
- `age` (INT, NULL)
- `has_disability` (BOOLEAN, NULL)
- `is_pregnant` (BOOLEAN, NULL)
- `receives_ssi` (BOOLEAN, NULL)
- `is_citizen` (BOOLEAN, NOT NULL)
- `assets_cents` (BIGINT, NULL) - For non-MAGI asset tests

**Validation Rules**:
- state_code: 2-character code, must be one of 5 pilot states (IL, CA, NY, TX, FL)
- household_size: 1-8+
- monthly_income_cents: >= 0
- age: 0-130 (if provided)
- is_citizen: required

**Citizenship & Residency Interpretation** (CLARIFICATION FOR A14):
- `is_citizen` field: Boolean representing user's self-reported citizenship status (TRUE = U.S. citizen or qualified non-citizen; FALSE = not citizen/non-citizen). This is a data collection marker only; no document proof of citizenship is collected during eligibility evaluation.
  - Frontend displays citizenship question; user self-reports
  - Backend validation: TRUE required for most Medicaid programs (some states allow qualified non-citizens for specific programs)
  - Note: Full citizenship verification (I-131, passport, etc.) occurs during enrollment confirmation layer (out of scope for E2)
- `is_state_resident`: Implicitly determined by state_code selection. User selects one state (IL, CA, NY, TX, or FL); residency requirements are state-specific and are embedded in rule_logic JSON. If user moves between states during application, they must re-select state and re-evaluate (new Session).
  - Rationale: Medicaid is state-administered; eligibility determined by state of current residence
  - No explicit residency proof collected during evaluation (assumed via state selection)
  - Verification occurs during enrollment enrollment confirmation (out of scope for E2)

**Rationale**:
- Frontend normalizes income to monthly format; backend validates
- NULL fields for optional characteristics (age, disability)
- Cents representation for precise dollar calculations

---

### 4b. Non-MAGI Asset Limits (Reference Table by State)

**Purpose**: Defines maximum asset limits for non-MAGI Aged and Disabled pathways by state

**State-Specific Asset Limits** (2026 pilot states):
```
| State | Pathway | Asset Limit | Notes |
|-------|---------|-------------|-------|
| IL | Aged | $2,000 | Individual; higher for married couples (per HFS rules) |
| IL | Disabled | $2,000 | Same as Aged for SSDI/SSI recipients |
| CA | Aged | $3,000 | California uses higher limit for aged population |
| CA | Disabled | $3,000 | Aligned with aged pathway |
| NY | Aged | $4,500 | New York uses highest limit among pilot states |
| NY | Disabled | $4,500 | Equal treatment across pathways |
| TX | Aged | $2,000 | Texas follows federal baseline (lower limit) |
| TX | Disabled | $2,000 | Same as aged pathway |
| FL | Aged | $2,500 | Florida hybrid approach (between IL/TX and CA/NY) |
| FL | Disabled | $2,500 | No differentiation by pathway |
```

**Integration**: Used by AssetEvaluator.cs (T031a) to determine non-MAGI eligibility. Exceeding asset limit = disqualifying factor; explanation includes: "Your assets ($X) exceed the limit ($Y) for [Pathway] Medicaid in [State]."

**Rationale**: Asset tests are state-specific and vary by eligibility pathway. This reference table enables consistent evaluation rules across all 5 pilot states.

---

### 5. EligibilityResult (DTO/Value Object)

**Purpose**: Represents the output of eligibility evaluation

**Attributes**:
- `evaluation_date` (TIMESTAMP, NOT NULL)
- `status` (ENUM: Likely Eligible, Possibly Eligible, Unlikely Eligible)
- `matched_programs` (ARRAY of ProgramMatch objects)
- `explanation` (TEXT, NOT NULL) - Plain-language summary
- `rule_version_used` (DECIMAL(4,2), NOT NULL) - For audit trail
- `confidence_score` (INT, 0-100) - Overall confidence across all programs
- `disqualifying_factors` (ARRAY of strings, NULL) - If ineligible

**Example**:
```json
{
  "evaluation_date": "2026-02-09T14:30:00Z",
  "status": "Likely Eligible",
  "matched_programs": [
    {
      "program_id": "uuid-1",
      "program_name": "MAGI Adult",
      "confidence_score": 95,
      "explanation": "Your income ($2,100/month) is below the eligibility limit ($2,500) for a household of 2."
    },
    {
      "program_id": "uuid-4",
      "program_name": "Pregnancy-Related",
      "confidence_score": 85,
      "explanation": "You may qualify because you reported pregnancy."
    }
  ],
  "explanation": "Based on your household size of 2 and monthly income of $2,100, you likely qualify for MAGI Adult and Pregnancy-Related Medicaid programs.",
  "rule_version_used": 1.0,
  "confidence_score": 90,
  "disqualifying_factors": null
}
```

---

### 6. ProgramMatch (Nested in EligibilityResult)

**Purpose**: Represents individual program match within evaluation results

**Attributes**:
- `program_id` (UUID, FK: MedicaidProgram)
- `program_name` (VARCHAR, denormalized)
- `confidence_score` (INT, 0-100)
- `explanation` (TEXT) - Why user matches this specific program
- `disqualifying_factors` (ARRAY of strings, NULL) - Reasons for reduced confidence

---

### 7. SessionData Extension (E1 Existing)

**Purpose**: Sessions store eligibility evaluation history (extends E1 Session entity)

**Add to existing Session entity**:
- `last_evaluated_result` (JSON, NULL) - Latest eligibility evaluation result
- `evaluation_history` (JSONB ARRAY, NULL) - Historical evaluations with timestamps
- `matched_programs` (JSONB ARRAY, NULL) - Programs user qualifies for (denormalized for quick display)

**Rationale**: One query to get session + eligibility history; avoids separate lookup

---

## Database Migrations Strategy

### Migration 1: InitializeRulesEngine

**Purpose**: Create tables for rules engine

```sql
CREATE TABLE medicaid_programs (
  program_id UUID PRIMARY KEY,
  state_code VARCHAR(2) NOT NULL,
  program_name VARCHAR(100) NOT NULL,
  program_code VARCHAR(20) UNIQUE,
  eligibility_pathway VARCHAR(50) NOT NULL,
  description TEXT,
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  UNIQUE(state_code, program_name),
  INDEX(state_code, eligibility_pathway)
);

CREATE TABLE eligibility_rules (
  rule_id UUID PRIMARY KEY,
  program_id UUID NOT NULL REFERENCES medicaid_programs(program_id),
  state_code VARCHAR(2) NOT NULL,
  rule_name VARCHAR(100) NOT NULL,
  version DECIMAL(4,2) NOT NULL,
  rule_logic JSONB NOT NULL,
  effective_date DATE NOT NULL,
  end_date DATE,
  created_by UUID,
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  description TEXT,
  INDEX(program_id, effective_date, end_date),
  INDEX(state_code, effective_date),
  UNIQUE(program_id, version)
);

CREATE TABLE federal_poverty_levels (
  fpl_id UUID PRIMARY KEY,
  year INTEGER NOT NULL,
  household_size INTEGER NOT NULL,
  annual_income_cents BIGINT NOT NULL,
  state_code VARCHAR(2),
  adjustment_multiplier DECIMAL(3,2),
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  UNIQUE(year, household_size, state_code)
);
```

### Migration 2: SeedPilotStateRules

**Purpose**: Insert rules for 5 pilot states (IL, CA, NY, TX, FL)

- Create 6 MedicaidProgram records per state (MAGI Adult, Aged, Disabled, Pregnancy, SSI, State-Specific)
- Create 1 EligibilityRule per program (v1.0, effective 2026-01-01)
- Populate rule_logic with JSONLogic for each program

### Migration 3: SeedFPLTables

**Purpose**: Insert 2026 FPL baseline and state adjustments

- Insert 8 FPL records (household sizes 1-8+) for baseline
- Insert 8 FPL records for AK (1.25x multiplier)
- Insert 8 FPL records for HI (1.15x multiplier)
- effective_date: depends on when official FPL published

---

## Query Patterns & Performance

### Pattern 1: Get Active Rules for State/Program

**Query**:
```sql
SELECT * FROM eligibility_rules
WHERE program_id = $1
  AND effective_date <= CURRENT_DATE
  AND (end_date IS NULL OR end_date > CURRENT_DATE)
LIMIT 1;
```

**Index**: (program_id, effective_date, end_date)  
**Expected**: <10ms (with in-memory cache)

### Pattern 2: Calculate FPL Threshold

**Query**:
```sql
SELECT annual_income_cents FROM federal_poverty_levels
WHERE year = $1
  AND household_size = $2
  AND state_code IS NULL
LIMIT 1;
```

**Index**: (year, household_size)  
**Expected**: <5ms (cached in application memory for entire year)

### Pattern 3: Evaluate All Programs for State

**Query**:
```sql
SELECT mp.program_id, mp.program_name, mp.eligibility_pathway, er.rule_logic
FROM medicaid_programs mp
LEFT JOIN eligibility_rules er ON mp.program_id = er.program_id
WHERE mp.state_code = $1
  AND er.effective_date <= CURRENT_DATE
  AND (er.end_date IS NULL OR er.end_date > CURRENT_DATE);
```

**Index**: (state_code, eligibility_pathway)  
**Expected**: <50ms for 6-8 programs per state (full result set cached by state)

---

## Caching Strategy

### In-Memory Cache Configuration

**Cache Layer**: Application memory (IMemoryCache in MAA.API)

**Cached Data**:
1. **FPL Tables** (entire year)
   - Key: `fpl:2026`
   - Size: ~8 rows * 52 states = ~400 bytes
   - TTL: 1 year (manually invalidated on FPL updates)

2. **Rules by State** (active rules)
   - Key: `rules:IL` (per state)
   - Size: ~6 programs * rule_logic JSON = ~10KB per state
   - TTL: 1 hour (refreshed on rule updates in admin portal)

3. **Programs by State** (static metadata)
   - Key: `programs:CA` (per state)
   - Size: ~1KB per state
   - TTL: 24 hours

**Cache Invalidation**:
- Manual: Admin updates rule → invalidate `rules:*`
- Time-based: FPL cache expires after 1 year
- Metrics: Track cache hit rate; target 95%+ for rules

---

## Data Quality Considerations

### Validation Rules

**At Insert Time**:
1. Effective dates must be in future or present
2. End date must be >= effective date
3. Rule version must be unique per program
4. Rule logic must be valid JSON
5. FPL household_size 1-8 (8 represents 8+)

### Audit Trail

**Tracking**:
- `created_by` field on rules → Admin attribution
- `created_at` / `updated_at` → Timestamps for traceability
- Version field → Complete rule history available

**Queries for Compliance**:
```sql
-- Show all rule versions for auditing
SELECT rule_name, version, effective_date, end_date, created_by, created_at
FROM eligibility_rules
WHERE program_id = $1
ORDER BY version DESC;

-- Show which rules were active on a specific date
SELECT * FROM eligibility_rules
WHERE program_id = $1
  AND effective_date <= $2
  AND (end_date IS NULL OR end_date > $2);
```

---

## Schema Diagram

```
medicaid_programs (reference data)
├─ state_code (IL, CA, NY, TX, FL)
├─ program_name (MAGI Adult, Aged, etc.)
└─ eligibility_pathway (MAGI, NonMAGI_Aged, etc.)

eligibility_rules (versioned, effective-dated)
├─ program_id → medicaid_programs
├─ rule_logic (JSON - core evaluation logic)
├─ version (v1.0, v1.1, v2.0, etc.)
└─ effective_date (when rule takes effect)

federal_poverty_levels (annual lookup table)
├─ year (2026)
├─ household_size (1-8+)
└─ annual_income_cents (FPL value)

sessions (E1 extension)
└─ last_evaluated_result (JSONB - latest evaluation)
    └─ matched_programs (IL MAGI, IL Pregnancy, etc.)
    └─ explanation (plain-language)
```

---

## Constitutional Compliance

- ✅ **Code Quality (CONST-I)**: Schema enables pure domain logic testable without business logic changes
- ✅ **Testing (CONST-II)**: Indexes and query patterns support fast test execution
- ✅ **UX (CONST-III)**: Explanation field stores plain-language output
- ✅ **Performance (CONST-IV)**: Indexes and caching support ≤2s evaluation, <10ms FPL lookups

