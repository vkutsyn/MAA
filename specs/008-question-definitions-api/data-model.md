# Data Model: Eligibility Question Definitions API

**Phase 1 Output** | **Date**: 2026-02-10 | **Feature**: 008-question-definitions-api

## Domain Entities

### 1. Question

Represents a single question in an eligibility questionnaire.

**Entity Definition**:

```
Class: Question
Namespace: MAA.Domain

Properties:
├── QuestionId (Guid, PK)
│   └── Generated as new Guid on creation; immutable
│
├── StateCode (string, Required, Index)
│   └── US state code (CA, TX, NY, etc.); part of composite PK for uniqueness
│   └── Validation: Must match StateConfiguration.StateCode
│
├── ProgramCode (string, Required, Index)
│   └── Program identifier (MEDI-CAL, CALFRESH, etc.); part of composite PK
│   └── Validation: Must exist in StateConfiguration.Programs
│
├── DisplayOrder (int, Required)
│   └── Position in questionnaire (1, 2, 3, ...)
│   └── Validation: > 0; unique per (StateCode, ProgramCode)
│
├── QuestionText (string, Required, MaxLength: 1000)
│   └── Text shown to user; plain language, no HTML
│   └── Example: "What is your household size?"
│
├── FieldType (QuestionFieldType enum, Required)
│   └── Values: Text, Select, Checkbox, Radio, Date, Currency
│   └── Determines UI rendering type
│
├── IsRequired (bool, Required, default: false)
│   └── True = user must answer; False = optional answer allowed
│
├── HelpText (string, Nullable, MaxLength: 2000)
│   └── Optional guidance text shown below question
│   └── Example: "Include all people you live with"
│   └── Plain language; supports markdown (frontend renders)
│
├── ValidationRegex (string, Nullable, MaxLength: 500)
│   └── Optional regex pattern for text validation (frontend + backend validation)
│   └── Example: "^[0-9]+$" (digits only)
│   └── Validation: Must be valid regex; backend tests regex on submit
│
├── ConditionalRuleId (Guid, Nullable, FK)
│   └── Reference to ConditionalRule that determines visibility
│   └── Null = always visible
│   └── Navigation Property: ConditionalRule
│
├── CreatedAt (DateTime, Required)
│   └── Timestamp when question was created; immutable
│
├── UpdatedAt (DateTime, Required)
│   └── Timestamp when question metadata was last updated
│   └── Updated when HelpText, FieldType, validation changes
│
└── Options (ICollection<QuestionOption>)
    └── Navigation: Questions with FieldType = Select/Checkbox/Radio have options
    └── Ordered by DisplayOrder
    └── At least 1 option required for select-type questions
```

**Validation Rules**:

- `QuestionId`: Immutable after creation
- `StateCode` + `ProgramCode` + `DisplayOrder`: Unique constraint (no duplicate display order per state/program)
- `FieldType` = Select/Radio/Checkbox → `Options.Count >= 2` (enforced in handler)
- `FieldType` = Text → `ValidationRegex` optional (if provided, must be valid regex)
- `ConditionalRuleId`: If set, must exist in ConditionalRule table
- `QuestionText`: Non-empty, max 1000 characters
- `HelpText`: Optional, max 2000 characters
- Database: Composite index on (state_code, program_code, display_order) for fast query + uniqueness check

**State Diagram**: 
```
[Created] → [Active] → [Archived/Removed] (soft-delete via IsActive flag, optional)
Consider: Do we support soft-delete? Or full removal? → Recommend soft-delete for audit trail
```

---

### 2. ConditionalRule

Represents a visibility condition for questions.

**Entity Definition**:

```
Class: ConditionalRule
Namespace: MAA.Domain

Properties:
├── ConditionalRuleId (Guid, PK)
│   └── Generated as new Guid on creation
│
├── RuleExpression (string, Required, MaxLength: 5000)
│   └── Boolean expression in normalized format
│   └── Format: "{questionId} operator 'value'" with AND/OR/NOT connectors
│   └── Example: "550e8400-e29b-41d4-a716-446655440001 == 'yes' AND 550e8400-e29b-41d4-a716-446655440002 >= 18"
│
├── Description (string, Nullable, MaxLength: 200)
│   └── Human-readable description of rule (for admin portal logging)
│   └── Example: "Show dependents field only if user answered Yes to 'Do you have dependents?'"
│
├── CreatedAt (DateTime, Required)
│   └── When rule was created
│
├── UpdatedAt (DateTime, Required)
│   └── When rule was last modified
│
└── Questions (ICollection<Question>)
    └── Navigation: All questions that reference this rule
    └── Inverse navigation from Question.ConditionalRule
```

**Validation Rules**:

- `RuleExpression`: Non-empty, valid boolean expression
- `RuleExpression` must reference valid QuestionIds (detected via regex parse, enforced when rule is created)
- Circular dependencies prohibited (detected via topological sort on insert)
- Rule expression must be deterministic (no random functions, no side effects)

**Supported Operators**:
- Comparison: `==`, `!=`, `>`, `<`, `>=`, `<=`
- Membership: `IN`, `NOT IN`
- Logic: `AND`, `OR`, `NOT`
- Grouping: Parentheses for precedence

**Example Expressions**:
```
// Simple comparison
question_id_1 == 'yes'

// Numeric comparison
question_id_2 > 3

// Multiple conditions with AND
question_id_1 == 'yes' AND question_id_2 >= 18

// Multiple conditions with OR + grouping
(question_id_1 == 'yes' OR question_id_1 == 'maybe') AND question_id_2 < 65

// Negation
NOT (question_id_1 == 'no')

// Membership test
question_id_3 IN ['option_a', 'option_b', 'option_c']
```

---

### 3. QuestionOption

Represents a selectable option for Select/Checkbox/Radio type questions.

**Entity Definition**:

```
Class: QuestionOption
Namespace: MAA.Domain

Properties:
├── OptionId (Guid, PK)
│   └── Generated on creation
│
├── QuestionId (Guid, Required, FK)
│   └── Reference to parent Question
│   └── Navigation Property: Question
│   └── Constraint: parent must have FieldType ∈ {Select, Checkbox, Radio}
│
├── OptionLabel (string, Required, MaxLength: 200)
│   └── Text displayed to user (renderable)
│   └── Example: "Yes", "No", "Maybe", "California", "Texas"
│
├── OptionValue (string, Required, MaxLength: 100)
│   └── Internal code stored in answer (not user-visible)
│   └── Example: "yes", "no", "maybe", "ca", "tx"
│   └── Constraint: Unique per Question (no duplicates)
│
├── DisplayOrder (int, Required)
│   └── Position in option list (1, 2, 3, ...)
│   └── Constraint: Unique per Question
│
├── CreatedAt (DateTime, Required)
│
└── UpdatedAt (DateTime, Required)
```

**Validation Rules**:

- `OptionLabel`: Non-empty, max 200 characters, no HTML tags
- `OptionValue`: Alphanumeric + underscores, max 100 characters, lowercase recommended
- `QuestionId` + `OptionValue`: Unique constraint (no duplicate values per question)
- `QuestionId` + `DisplayOrder`: Unique constraint (no duplicate order per question)
- If parent Question is Select/Checkbox/Radio: At least 1 option required
- If parent Question is Text/Date/Currency: 0 options expected (not enforced, but expected)

---

## Database Schema

### Tables

#### questions (MAA_Questions)

```sql
CREATE TABLE questions (
    question_id UUID PRIMARY KEY,
    state_code VARCHAR(2) NOT NULL,
    program_code VARCHAR(50) NOT NULL,
    display_order INT NOT NULL,
    question_text VARCHAR(1000) NOT NULL,
    field_type VARCHAR(20) NOT NULL, -- 'Text', 'Select', 'Checkbox', 'Radio', 'Date', 'Currency'
    is_required BOOLEAN NOT NULL DEFAULT FALSE,
    help_text VARCHAR(2000),
    validation_regex VARCHAR(500),
    conditional_rule_id UUID,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    
    -- Composite unique constraint
    UNIQUE (state_code, program_code, display_order),
    
    -- Foreign key (if ConditionalRule deleted, cascade delete or set null?)
    CONSTRAINT fk_conditional_rule
        FOREIGN KEY (conditional_rule_id)
        REFERENCES conditional_rules(conditional_rule_id)
        ON DELETE SET NULL  -- If rule deleted, question becomes always visible
);

-- Indexes for fast lookup
CREATE INDEX idx_questions_state_program 
    ON questions(state_code, program_code);

CREATE INDEX idx_questions_state_program_order 
    ON questions(state_code, program_code, display_order);

CREATE INDEX idx_questions_conditional_rule 
    ON questions(conditional_rule_id);
```

#### conditional_rules (MAA_ConditionalRules)

```sql
CREATE TABLE conditional_rules (
    conditional_rule_id UUID PRIMARY KEY,
    rule_expression VARCHAR(5000) NOT NULL,
    description VARCHAR(200),
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- No special indexes (small table, rarely queried by anything other than FK from questions)
```

#### question_options (MAA_QuestionOptions)

```sql
CREATE TABLE question_options (
    option_id UUID PRIMARY KEY,
    question_id UUID NOT NULL,
    option_label VARCHAR(200) NOT NULL,
    option_value VARCHAR(100) NOT NULL,
    display_order INT NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    
    -- Composite unique constraint
    UNIQUE (question_id, option_value),
    UNIQUE (question_id, display_order),
    
    -- Foreign key
    CONSTRAINT fk_question
        FOREIGN KEY (question_id)
        REFERENCES questions(question_id)
        ON DELETE CASCADE  -- If question deleted, delete all options
);

-- Indexes
CREATE INDEX idx_question_options_question 
    ON question_options(question_id);

CREATE INDEX idx_question_options_order 
    ON question_options(question_id, display_order);
```

---

## Entity Relationships Diagram

```
┌─────────────────┐
│  ConditionalRule│
│                 │
│  - rule_id (PK) │
│  - expression   │
└────────┬────────┘
         │ 0..1
         │ (optional)
         │
         │ 1
┌────────▼──────────────────┐
│      Question             │
│                           │
│  - question_id (PK)       │
│  - state_code (FK)        │
│  - program_code (FK)      │
│  - field_type             │
│  - conditional_rule_id (FK)
└────────┬──────────────────┘
         │ 1
         │
         │ * (0 or more)
┌────────▼──────────────┐
│  QuestionOption       │
│                       │
│  - option_id (PK)     │
│  - question_id (FK)   │
│  - option_label       │
│  - option_value       │
└───────────────────────┘
```

---

## Constraints & Invariants

### Temporal Constraints

- Questions are created once and immutable thereafter (CreatedAt never changes)
- UpdatedAt only changes on metadata updates (field_type, help_text, validation changes)
- No support for question versioning (if definition changes, affects in-progress sessions)

### Uniqueness Constraints

- `(state_code, program_code, display_order)`: One question per display position per state/program
- `(question_id, option_value)`: One option value per question (no duplicate codes)
- `(question_id, display_order)`: Options appear in defined order

### Referential Integrity

- Question → ConditionalRule: ForeignKey with ON DELETE SET NULL (question becomes always visible)
- Question ← QuestionOption: 1:* relationship with ON DELETE CASCADE (options deleted when parent question deleted)
- (Future) Question → StateConfiguration: ForeignKey (validates state/program codes match reference data)

### Business Rules

- If `field_type ∈ {Select, Checkbox, Radio}`: Must have ≥1 option
- If `field_type = Text` AND `validation_regex` provided: Regex must be valid
- No circular conditional dependencies: If Q1 visible when Q2 answered, Q2 cannot be visible when Q1 answered
- All references in `rule_expression` must point to existing questions in same state/program

---

## Migration Path

**EntityFramework Core Migration** (add new tables):

```csharp
// File: src/MAA.Infrastructure/Migrations/[Timestamp]_AddQuestionDefinitions.cs

public partial class AddQuestionDefinitions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Create conditional_rules table first (no dependencies)
        migrationBuilder.CreateTable(
            name: "conditional_rules",
            columns: table => new
            {
                conditional_rule_id = table.Column<Guid>(nullable: false),
                rule_expression = table.Column<string>(maxLength: 5000, nullable: false),
                description = table.Column<string>(maxLength: 200, nullable: true),
                created_at = table.Column<DateTime>(nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                updated_at = table.Column<DateTime>(nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_conditional_rules", x => x.conditional_rule_id);
            });

        // Create questions table (references conditional_rules)
        migrationBuilder.CreateTable(
            name: "questions",
            columns: table => new
            {
                question_id = table.Column<Guid>(nullable: false),
                state_code = table.Column<string>(maxLength: 2, nullable: false),
                program_code = table.Column<string>(maxLength: 50, nullable: false),
                display_order = table.Column<int>(nullable: false),
                question_text = table.Column<string>(maxLength: 1000, nullable: false),
                field_type = table.Column<string>(maxLength: 20, nullable: false),
                is_required = table.Column<bool>(nullable: false, defaultValue: false),
                help_text = table.Column<string>(maxLength: 2000, nullable: true),
                validation_regex = table.Column<string>(maxLength: 500, nullable: true),
                conditional_rule_id = table.Column<Guid>(nullable: true),
                created_at = table.Column<DateTime>(nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                updated_at = table.Column<DateTime>(nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_questions", x => x.question_id);
                table.ForeignKey(
                    name: "fk_questions_conditional_rule",
                    column: x => x.conditional_rule_id,
                    principalTable: "conditional_rules",
                    principalColumn: "conditional_rule_id",
                    onDelete: ReferentialAction.SetNull);
                table.UniqueConstraint(
                    name: "uk_questions_state_program_order",
                    columns: new[] { "state_code", "program_code", "display_order" });
            });

        // Create question_options table (references questions)
        migrationBuilder.CreateTable(
            name: "question_options",
            columns: table => new
            {
                option_id = table.Column<Guid>(nullable: false),
                question_id = table.Column<Guid>(nullable: false),
                option_label = table.Column<string>(maxLength: 200, nullable: false),
                option_value = table.Column<string>(maxLength: 100, nullable: false),
                display_order = table.Column<int>(nullable: false),
                created_at = table.Column<DateTime>(nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                updated_at = table.Column<DateTime>(nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_question_options", x => x.option_id);
                table.ForeignKey(
                    name: "fk_question_options_question",
                    column: x => x.question_id,
                    principalTable: "questions",
                    principalColumn: "question_id",
                    onDelete: ReferentialAction.Cascade);
                table.UniqueConstraint(
                    name: "uk_question_options_value",
                    columns: new[] { "question_id", "option_value" });
                table.UniqueConstraint(
                    name: "uk_question_options_order",
                    columns: new[] { "question_id", "display_order" });
            });

        // Create indexes for query performance
        migrationBuilder.CreateIndex(
            name: "idx_questions_state_program",
            table: "questions",
            columns: new[] { "state_code", "program_code" });

        migrationBuilder.CreateIndex(
            name: "idx_questions_conditional_rule",
            table: "questions",
            column: "conditional_rule_id");

        migrationBuilder.CreateIndex(
            name: "idx_question_options_question",
            table: "question_options",
            column: "question_id");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "question_options");
        migrationBuilder.DropTable(name: "questions");
        migrationBuilder.DropTable(name: "conditional_rules");
    }
}
```

---

## Summary

- **3 Main Entities**: Question, ConditionalRule, QuestionOption
- **3 Database Tables**: questions, conditional_rules, question_options
- **Key Relationships**: Question → ConditionalRule (0..1); Question → QuestionOption (1..*)
- **Unique Constraints**: (state, program, display_order) on questions; (question, option_value) on options
- **Indexes**: Composite index on (state, program) for fast lookup of question sets
- **Validation**: Immutable questions; rule expression validation; circular dependency detection

This model supports fast retrieval (< 200ms p95), client-side rule evaluation, and audit-friendly temporal tracking.
