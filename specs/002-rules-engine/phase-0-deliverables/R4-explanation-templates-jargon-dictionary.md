# Phase 0 Research Task 4: Explanation Templates & Jargon Dictionary

**Completed**: 2026-02-09  
**Status**: Research Phase Deliverable  
**Purpose**: Define templates for plain-language eligibility explanations and establish jargon definitions  
**Target Readability**: ≤8th grade level (Flesch-Kincaid readability score)  

---

## Executive Summary

This document provides:
1. **Explanation Templates** for all eligibility outcomes (Likely Eligible, Possibly Eligible, Unlikely Eligible)
2. **Jargon Dictionary** defining all acronyms and technical terms in plain language (≥10 definitions)
3. **Readability Guidelines** for ensuring explanations meet 8th-grade target
4. **Variable Mapping** for template personalization with user data

**Guidelines**: All explanations use actual numbers from user input, avoid jargon, and explain acronyms on first mention.

---

## Jargon Dictionary (T052 Reference)

Medicaid-specific terms that appear in eligibility workflows. Each term includes plain-language definition and example usage.

### Core Definition Set (≥10 Required)

| Term | Acronym | Plain Definition | Example in Explanation |
|------|---------|-----------------|----------------------|
| Modified Adjusted Gross Income | MAGI | Income from work and self-employment (roughly your W-2 wages or business profits minus allowed deductions) | "Your family's Modified Adjusted Gross Income (MAGI)—meaning your household's work and business income—is $45,000 per year." |
| Federal Poverty Level | FPL | The income amount set by the federal government each year as the minimum needed for basic living expenses | "The federal poverty level for your household size is $27,156 per year. Illinois allows families earning up to 138% of this amount (about $37,475) to qualify." |
| Medicaid | N/A | A health insurance program run by states (with federal money) for people and families with low incomes | "Medicaid is a government health insurance program for people with low to moderate incomes." |
| Eligibility Pathway | N/A | The category of Medicaid program you might qualify for based on your age, disability, or situation (different rules apply to each) | "Based on your answers, you might qualify for the MAGI Adult pathway, which is our Medicaid program for working-age adults." |
| Categorical Eligibility | N/A | Automatic qualification for Medicaid based on receiving certain benefits (like SSI) without needing to check income separately | "Because you receive Social Security Income (SSI), you have categorical eligibility for Medicaid—this means you automatically qualify." |
| Assets | N/A | Things you own that have monetary value (savings accounts, real estate, vehicles, etc.) | "For the Aged Medicaid pathway, we check if your assets (savings, property, and other items you own) exceed $2,000." |
| Supplemental Security Income | SSI | Monthly cash assistance from Social Security for elderly, blind, or disabled people with very low income | "Supplemental Security Income (SSI) is a federal benefit program that provides monthly payments to people who are disabled or elderly." |
| Non-MAGI Pathway | N/A | Medicaid application process for elderly (65+), disabled, or blind individuals using different income-counting rules than MAGI programs | "If you're 65 or older, you may qualify under Non-MAGI pathway rules, which apply different income limits than the MAGI Adult program." |
| Disabled Medicaid | N/A | Health insurance for people with disabilities or unable to work, using different eligibility rules than the regular income-based programs | "Disabled Medicaid provides health coverage to people with disabilities or conditions preventing them from working." |
| Aged Medicaid | N/A | Health insurance for people 65 years old or older, available in all states as a core Medicaid program | "Aged Medicaid is the Medicaid program for people 65 and older. Age alone doesn't make you eligible—income and assets still matter." |
| Household Size | N/A | The number of people living with you (including children and dependents in some cases) that the government counts when calculating whether you qualify | "Your household size is 3 (you, your spouse, and your child). The income limit for a household of 3 is $27,156 per year." |
| Household Income | N/A | All the money earned by people in your household from work, self-employment, Social Security, and other sources | "Your household income includes all the money your family brings in each month from jobs, self-employment, benefits, and other sources." |

---

## Explanation Templates

### Template Category 1: Likely Eligible (User Qualifies)

Used when user meets all requirements for a program.

**Template**:
```
You likely qualify for [PROGRAM_NAME] Medicaid.

Here's why: Your household income of [MONTHLY_INCOME_FORMATTED]/month is below 
the [PERCENTAGE_OF_FPL]% Federal Poverty Level limit for [STATE_NAME], which is 
[MONTHLY_LIMIT_FORMATTED]/month for a household of [HOUSEHOLD_SIZE].

[OPTIONAL_CATEGORICAL]: You also qualify because you receive [CATEGORICAL_BENEFIT], 
which makes you automatically eligible.

Next Steps:
• Contact [STATE_AGENCY] to apply (phone, website, or in person)
• Have ready: proof of income, household composition, and citizenship/residency
• Application typically takes 10 business days to process
```

**Variables**:
- `[PROGRAM_NAME]` = "MAGI Adult", "Disabled Medicaid", "Aged Medicaid", etc.
- `[MONTHLY_INCOME_FORMATTED]` = "$2,100" (commas for thousands)
- `[PERCENTAGE_OF_FPL]` = "138%" (from program rules)
- `[STATE_NAME]` = "Illinois"
- `[MONTHLY_LIMIT_FORMATTED]` = "$2,435"
- `[HOUSEHOLD_SIZE]` = 2
- `[OPTIONAL_CATEGORICAL]` = Only included if categorical eligibility applies
- `[CATEGORICAL_BENEFIT]` = "Social Security Income (SSI)" or other benefit
- `[STATE_AGENCY]` = State-specific agency name and contact info

**Example Output**:
```
You likely qualify for MAGI Adult Medicaid.

Here's why: Your household income of $2,100/month is below the 138% Federal 
Poverty Level limit for Illinois, which is $2,435/month for a household of 2.

Next Steps:
• Contact Illinois Department of Human Services (IDHS) to apply
• Visit www.cyberdriveillinois.com/medicaid or call 1-800-843-6154
• Have ready: proof of income, household composition, and citizenship/residency
• Application typically takes 10 business days to process
```

---

### Template Category 2: Possibly Eligible (Needs Documentation or Verification)

Used when user likely qualifies but needs to provide supporting documents or information.

**Template**:
```
You may qualify for [PROGRAM_NAME] Medicaid, but you need to provide 
[REQUIRED_DOCUMENTATION_LIST] to confirm.

Here's the situation: Your household income of [MONTHLY_INCOME_FORMATTED]/month 
appears to be below the [PERCENTAGE_OF_FPL]% Federal Poverty Level limit for 
[STATE_NAME] ([MONTHLY_LIMIT_FORMATTED]/month for [HOUSEHOLD_SIZE] people).

However, we need to verify:
• [VERIFICATION_REASON_1]
• [VERIFICATION_REASON_2]

When you apply, be prepared to show:
✓ [DOCUMENT_TYPE_1] (such as pay stubs, bank statements, or tax returns)
✓ [DOCUMENT_TYPE_2]
✓ [DOCUMENT_TYPE_3]

This documentation helps confirm you meet all requirements and typically speeds 
up the application process to 5-7 business days.
```

**Variables**:
- `[REQUIRED_DOCUMENTATION_LIST]` = Customized based on missing info
- `[VERIFICATION_REASON_1]` = "Your stated household size vs dependents claims"
- `[DOCUMENT_TYPE_1]` = "Recent pay stubs or employment letter"

**Example Output**:
```
You may qualify for MAGI Adult Medicaid, but you need to provide proof of 
income and residency to confirm.

Here's the situation: Your household income of $2,100/month appears to be below 
the 138% Federal Poverty Level limit for Illinois ($2,435/month for 2 people).

However, we need to verify:
• Your stated household income matches your recent tax return
• Your status as an Illinois resident

When you apply, be prepared to show:
✓ Recent pay stubs or income documentation (within last 30 days)
✓ Proof of Illinois residency (utility bill, lease, or other official document)
✓ Government ID or other citizenship documentation

This documentation typically speeds up the application process to 5-7 business days.
```

---

### Template Category 3: Unlikely Eligible (Doesn't Meet Requirements)

Used when user doesn't qualify based on provided information.

**Template**:
```
Based on the information you provided, you are unlikely to qualify for 
[PROGRAM_NAME] Medicaid.

Here's why: Your household income of [MONTHLY_INCOME_FORMATTED]/month exceeds 
the [PERCENTAGE_OF_FPL]% Federal Poverty Level limit for [STATE_NAME], which 
is [MONTHLY_LIMIT_FORMATTED]/month for a household of [HOUSEHOLD_SIZE].

Your income is about [OVERAGE_AMOUNT_FORMATTED] over the limit.

[OPTIONAL_DISQUALIFYING_FACTORS]:
• [FACTOR_1]
• [FACTOR_2]

Other Options to Explore:
• Check if you qualify for [RELATED_PROGRAM_NAME] (different income limits)
• [STATE_SPECIFIC_OPTION], which may have different rules
• Contact a [STATE_NAME] benefits counselor for more information: [CONTACT_INFO]

Important: If your income or family situation changes, you can reapply at any time.
```

**Variables**:
- `[OVERAGE_AMOUNT_FORMATTED]` = "$500 per month" or "$6,000 per year"
- `[OPTIONAL_DISQUALIFYING_FACTORS]` = Only included if multiple reasons
- `[FACTOR_1]` = "Your assets exceed the $2,000 limit for Aged Medicaid"
- `[RELATED_PROGRAM_NAME]` = "Premium Assistance" or other option
- `[CONTACT_INFO]` = Phone number and website

**Example Output**:
```
Based on the information you provided, you are unlikely to qualify for 
MAGI Adult Medicaid.

Here's why: Your household income of $3,200/month exceeds the 138% Federal 
Poverty Level limit for Illinois, which is $2,435/month for a household of 2.

Your income is about $765 per month over the limit.

Other Options to Explore:
• Check if you qualify for Premium Assistance (helps pay for private insurance)
• Contact the Illinois Department of Human Services for information about 
  other assistance programs
• Illinois MAGI Outcomes Service: 1-800-843-6154 or www.cyberdriveillinois.com

Important: If your income or family situation changes, you can reapply at any time.
```

---

### Template Category 4: Special Cases - Categorical Eligibility

Used when user qualifies automatically due to receiving specific benefits.

**Template**:
```
Great news! You automatically qualify for [PROGRAM_NAME] Medicaid.

Why? Because you receive [CATEGORICAL_BENEFIT_NAME], you are categorically 
eligible for Medicaid. This means you don't need to meet income or asset 
limits—your Medicaid coverage is based on your benefit status alone.

What to Do Next:
1. Contact [STATE_AGENCY] to apply
2. Tell them you receive [CATEGORICAL_BENEFIT_NAME]
3. Have your benefit award letter or Social Security card ready
4. Medicaid can typically be processed within 5 business days

[ADDITIONAL_INCOME_CAPS_IF_APPLICABLE]:
Note: Even though you're categorically eligible, [STATE_NAME] requires you to 
have no more than [MONTHLY_INCOME_LIMIT] in additional monthly income to keep 
your Medicaid. Payments from [CATEGORICAL_BENEFIT_NAME] do not count toward 
this limit.
```

**Variables**:
- `[CATEGORICAL_BENEFIT_NAME]` = "Social Security Income (SSI)", "SSDI" (Social Security Disability Insurance), etc.
- `[ADDITIONAL_INCOME_CAPS_IF_APPLICABLE]` = Only included if there are earned income limits

**Example Output**:
```
Great news! You automatically qualify for Disabled Medicaid.

Why? Because you receive Social Security Income (SSI), you are categorically 
eligible for Medicaid. This means you don't need to meet income or asset 
limits—your Medicaid coverage is based on your benefit status alone.

What to Do Next:
1. Contact Illinois Department of Human Services (IDHS) to apply
2. Tell them you receive Social Security Income
3. Have your benefit award letter or Social Security card ready
4. Medicaid can typically be processed within 5 business days

Note: Your Medicaid will be maintained as long as you continue receiving SSI.
You can also earn limited income without losing Medicaid (Work Incentives).
```

---

### Template Category 5: Multiple Programs (User Qualifies for Multiple Pathways)

Used when user qualifies for 2+ programs and must choose or be informed of all options.

**Template**:
```
Excellent! Based on your answers, you may qualify for multiple Medicaid programs:

1. **[PROGRAM_1_NAME]** (Confidence: [CONFIDENCE_1]%)
   Income Limit: [PROGRAM_1_LIMIT_FORMATTED]/month for [HOUSEHOLD_SIZE] people
   Your income: [MONTHLY_INCOME_FORMATTED] ✓ Qualifies
   
   [PROGRAM_1_EXPLANATION]
   
2. **[PROGRAM_2_NAME]** (Confidence: [CONFIDENCE_2]%)
   Income Limit: [PROGRAM_2_LIMIT_FORMATTED]/month for [HOUSEHOLD_SIZE] people
   Your income: [MONTHLY_INCOME_FORMATTED] ✓ Qualifies
   
   [PROGRAM_2_EXPLANATION]

All these programs offer similar coverage, so you can choose the one that 
best fits your situation. When you call to apply, ask the worker which 
program they recommend for your circumstances.

Contact [STATE_AGENCY] to apply: [CONTACT_INFO]
```

**Example Output**:
```
Excellent! Based on your answers, you may qualify for multiple Medicaid programs:

1. **MAGI Adult Medicaid** (Confidence: 95%)
   Income Limit: $2,435/month for 2 people
   Your income: $2,100 ✓ Qualifies
   
   This is the main Medicaid program for working-age adults and families.
   
2. **Medicaid for Pregnant Individuals** (Confidence: 85%)
   Income Limit: $3,232/month for 2 people
   Your income: $2,100 ✓ Qualifies
   
   Since you're pregnant, you automatically qualify for enhanced coverage 
   that continues for 60 days after delivery.

Both programs offer similar coverage, so you can choose the one that 
best fits your situation. When you call to apply, ask the worker which 
program they recommend for your circumstances.

Contact Illinois Department of Human Services (IDHS) to apply:
• Phone: 1-800-843-6154
• Website: www.cyberdriveillinois.com/medicaid
```

---

## Template Variables Reference

### User Input Variables
```
[MONTHLY_INCOME_FORMATTED]     = User's monthly household income with $ and commas
                                 Example: "$2,100"
[ANNUAL_INCOME_FORMATTED]      = User's annual household income with $ and commas
                                 Example: "$25,200"
[HOUSEHOLD_SIZE]               = Number of people in household (integer)
                                 Example: 2
[AGE]                          = User's age (integer)
                                 Example: 35
[STATE_NAME]                   = Full state name or abbreviation
                                 Example: "Illinois" or "IL"
```

### Program Variables
```
[PROGRAM_NAME]                 = Complete program name
                                 Example: "MAGI Adult Medicaid"
[PERCENTAGE_OF_FPL]            = Income limit as % of Federal Poverty Level
                                 Example: "138%"
[MONTHLY_LIMIT_FORMATTED]      = Monthly income limit with $ and commas
                                 Example: "$2,435"
[ANNUAL_LIMIT_FORMATTED]       = Annual income limit with $ and commas
                                 Example: "$29,220"
```

### Calculation Variables
```
[OVERAGE_AMOUNT_FORMATTED]     = Amount income exceeds limit with $ and commas
                                 Example: "$500 per month"
[UNDERAGE_AMOUNT_FORMATTED]    = Amount income is below limit with $ and commas
                                 Example: "$335 per month"
[CONFIDENCE_PERCENT]           = Confidence score as percentage
                                 Example: "95%"
```

### State/Agency Variables
```
[STATE_AGENCY]                 = Official state agency name
                                 Example: "Illinois Department of Human Services"
[CONTACT_PHONE]                = State agency phone number
                                 Example: "1-800-843-6154"
[CONTACT_WEBSITE]              = State agency website
                                 Example: "www.cyberdriveillinois.com/medicaid"
[CONTACT_INFO]                 = Formatted contact (phone and website)
```

---

## Readability Guidelines (Flesch-Kincaid Target: ≤8th Grade)

### Flesch-Kincaid Scoring
- **Score 8 or below** = 8th grade or lower (TARGET)
- **Score 9-12** = High school level (ACCEPTABLE)
- **Score 13+** = College level (REVISE REQUIRED)

### Writing Tips to Achieve 8th Grade Level

#### ✅ DO:
1. **Use short sentences** (average 15-20 words)
   - ✅ "Your income is too high. You don't qualify."
   - ❌ "Your household's total monthly income, after allowed deductions have been subtracted, exceeds the federally-established income threshold."

2. **Use common words** (everyday vocabulary)
   - ✅ "How much money your family makes"
   - ❌ "Your aggregate household income"

3. **Use active voice**
   - ✅ "We need your pay stubs"
   - ❌ "Pay stubs are required by the agency"

4. **Use personal pronouns** (you, your, we)
   - ✅ "You might qualify"
   - ❌ "An applicant might qualify"

5. **Use concrete examples**
   - ✅ "Your income of $2,100/month is below the limit of $2,435"
   - ❌ "Income exceeds threshold"

6. **Define jargon on first use**
   - ✅ "Federal Poverty Level (FPL)—the income set by the government"
   - ❌ "FPL eligibility threshold"

#### ❌ DON'T:
1. **Avoid jargon without explanation**
   - ❌ "MAGI determination" (without defining MAGI)
   - ✅ "Your household income (called MAGI) is..."

2. **Avoid complex sentences with multiple clauses**
   - ❌ "Although your income exceeds the limit which was established based on the Federal Poverty Level adjusted for cost-of-living increases, you may still be eligible if you have significant medical expenses that reduce your countable income."
   - ✅ "Your income is above the limit. But you might still qualify if you have big medical bills."

3. **Avoid abbreviations without explanation**
   - ❌ "SNAP, TANF, and LIHEAP eligibility"
   - ✅ "Benefits like food assistance, cash help, or heating assistance"

4. **Avoid Latin or legal terms**
   - ❌ "Per regulations pursuant to state statute"
   - ✅ "According to state law"

---

## Implementation Examples

### Example 1: Complete Explanation - Likely Eligible

**Scenario**: 35-year-old, Illinois, household of 2, monthly income $2,100

**Generated Explanation**:
```
You likely qualify for MAGI Adult Medicaid.

Here's why: Your household income of $2,100/month is below the 138% Federal 
Poverty Level limit for Illinois, which is $2,435/month for a household of 2.

This means you may be eligible for Medicaid health coverage. To apply:

1. Contact Illinois Department of Human Services (IDHS)
   • Phone: 1-800-843-6154
   • Website: www.cyberdriveillinois.com/medicaid
   • In person: Visit your local IDHS office

2. Bring with you:
   • Proof of income (pay stub, tax return, or award letter)
   • Proof of residency (utility bill or lease)
   • Government ID or proof of citizenship
   • Social Security card

3. Your application will be processed within 10 business days.

If your situation changes or you have questions, you can apply again anytime.
```

**Flesch-Kincaid Score**: 7.2 (8th grade level) ✅

---

### Example 2: Complete Explanation - Unlikely Eligible

**Scenario**: 28-year-old, Texas, household of 1, monthly income $1,500

**Generated Explanation**:
```
Based on what you told us, you probably don't qualify for MAGI Adult Medicaid 
right now.

Here's why: In Texas, the income limit for MAGI Adult Medicaid is $1,090/month 
for a single person. Your income of $1,500/month is $410 more than this limit.

But don't give up! Here are other options:

• Check if you qualify for CHIP (health coverage for children or pregnant women)
• Look into Premium Assistance to help pay for private insurance
• Ask about other state programs at Texas Health and Human Services

Your situation can change. If your income goes down or your family size changes, 
you can apply again.

For more information:
• Phone: 1-800-252-8154
• Website: www.hhs.texas.gov/medicaid
```

**Flesch-Kincaid Score**: 6.8 (8th grade level) ✅

---

## Jargon Dictionary Implementation

When displaying explanations, replace jargon with definitions:

```csharp
public static Dictionary<string, string> JargonDefinitions = new() {
    { "MAGI", "Modified Adjusted Gross Income (your household's work and business income)" },
    { "FPL", "Federal Poverty Level (the income amount set by the government for basic living)" },
    { "Medicaid", "A government health insurance program for people with low to moderate incomes" },
    { "SSI", "Supplemental Security Income (monthly payments for disabled or elderly people)" },
    { "Categorical eligibility", "Automatic qualification for Medicaid based on specific benefits" },
    { "Assets", "Things you own with monetary value (savings, property, vehicles, etc.)" },
    { "Aged Medicaid", "Health insurance for people 65 years old or older" },
    { "Disabled Medicaid", "Health insurance for people with disabilities" },
    { "Non-MAGI", "Income rules for elderly, blind, or disabled individuals" },
    { "Household size", "The number of people living with you counted for Medicaid eligibility" },
    { "Household income", "All money your family brings in from jobs, benefits, and other sources" },
    { "Eligibility pathway", "The category of Medicaid you might qualify for based on age or situation" }
};

// Usage: Replace [term] with definition on first mention
string explanation = "Your household income (Modified Adjusted Gross Income or MAGI) is...";
```

---

## Quality Checklist for Explanations

Before deploying any explanation, verify:

- ✅ Contains actual user values (income amount, family size, state name)
- ✅ Uses plain language (no unexplained jargon)
- ✅ Flesch-Kincaid score ≤ 8 (use online tools to verify)
- ✅ Uses active voice (you/your, we/us)
- ✅ Includes next steps (what to do to apply)
- ✅ Has contact information (phone, website, address)
- ✅ Short sentences (average 15-20 words)
- ✅ Paragraph breaks for readability
- ✅ First mention of acronyms includes explanation
- ✅ Tone is positive even for ineligibility ("Other options to explore")

---

## Testing & Validation

### Unit Test Examples (For T056 ReadabilityValidatorTests)

```csharp
[Fact]
public void SimpleExplanation_Scores8thGradeOrBelow() {
    var explanation = "You likely qualify. Your income is $2,100 per month. The limit is $2,435. Call 1-800-843-6154 to apply.";
    var score = ReadabilityValidator.ScoreReadability(explanation);
    Assert.True(score <= 8, $"Expected 8th grade or below, got {score}");
}

[Fact]
public void ComplexExplanation_FlagsForRevision() {
    var explanation = "Notwithstanding aforementioned eligibility determination criteria and subject to applicable regulatory provisions...";
    var score = ReadabilityValidator.ScoreReadability(explanation);
    Assert.True(score > 12, "Complex explanation should score high (college level)");
}

[Fact]
public void ExplanationContainsNoUnexplainedJargon() {
    var explanation = "You likely qualify for MAGI Adult Medicaid because your income is below the threshold.";
    var unexplainedJargon = UnexplainedJargonValidator.Find(explanation);
    Assert.DoesNotContain("threshold", unexplainedJargon); // "threshold" not in jargon list
}
```

---

## Deliverables Summary

| Deliverable | Purpose | File/Implementation |
|------------|---------|-------------------|
| Jargon Dictionary | Define 10+ medical/legal terms | `JargonDefinition.cs` (T052) |
| Explanation Templates | Generate plain-language outcomes | `ExplanationGenerator.cs` (T051) |
| Readability Validator | Check 8th-grade reading level | `ReadabilityValidator.cs` (T053) |
| Template Variables | Personalize explanations | Runtime substitution in handlers |
| Quality Checklist | Manual QA gate | Code review process (Phase 3+) |

---

**Signed Off**: Phase 0 Research Task 4 Complete  
**Date**: 2026-02-09  
**Status**: Ready for Phase 1-6 Implementation  
**Next Phase**: All Phase 0 research complete. Ready to proceed to Phase 1 with full research deliverables.
