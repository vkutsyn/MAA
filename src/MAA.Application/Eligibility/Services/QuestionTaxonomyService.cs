using MAA.Application.Eligibility.DTOs;

namespace MAA.Application.Eligibility.Services;

/// <summary>
/// Question taxonomy service implementation for the eligibility wizard.
/// </summary>
/// <remarks>
/// MVP implementation with hardcoded question sets for pilot states.
/// Future: Integrate with rules engine (specs/002-rules-engine) for dynamic taxonomy.
/// </remarks>
public class QuestionTaxonomyService : IQuestionTaxonomyService
{
    /// <summary>
    /// Gets the question set for a state.
    /// MVP: Returns hardcoded question set for TX and CA.
    /// </summary>
    public Task<QuestionSetDto?> GetQuestionSetByStateAsync(string stateCode)
    {
        if (string.IsNullOrWhiteSpace(stateCode))
        {
            return Task.FromResult<QuestionSetDto?>(null);
        }

        var normalizedCode = stateCode.Trim().ToUpperInvariant();

        // MVP: Return basic question set for pilot states
        if (normalizedCode == "TX" || normalizedCode == "CA")
        {
            var questionSet = new QuestionSetDto
            {
                State = normalizedCode,
                Version = "1.0-mvp",
                Questions = GetDefaultQuestions(normalizedCode)
            };

            return Task.FromResult<QuestionSetDto?>(questionSet);
        }

        return Task.FromResult<QuestionSetDto?>(null);
    }

    /// <summary>
    /// Returns default question list for MVP pilot states.
    /// </summary>
    private static List<QuestionDto> GetDefaultQuestions(string stateCode)
    {
        return new List<QuestionDto>
        {
            // Question 1: Household size
            new QuestionDto
            {
                Key = "household_size",
                Label = "How many people live in your household?",
                Type = "integer",
                Required = true,
                HelpText = "Include yourself, your spouse, and any dependents."
            },

            // Question 2: Annual income
            new QuestionDto
            {
                Key = "annual_income",
                Label = "What is your household's total annual income before taxes?",
                Type = "currency",
                Required = true,
                HelpText = "Include income from all sources: wages, self-employment, benefits, etc."
            },

            // Question 3: Age
            new QuestionDto
            {
                Key = "age",
                Label = "What is your age?",
                Type = "integer",
                Required = true
            },

            // Question 4: Citizenship (conditional based on state)
            new QuestionDto
            {
                Key = "citizenship_status",
                Label = "Are you a U.S. citizen or legal resident?",
                Type = "select",
                Required = true,
                Options = new List<QuestionOption>
                {
                    new QuestionOption { Value = "citizen", Label = "U.S. Citizen" },
                    new QuestionOption { Value = "resident", Label = "Legal Resident" },
                    new QuestionOption { Value = "other", Label = "Other" }
                }
            },

            // Question 5: Pregnancy status (conditional on age/gender)
            new QuestionDto
            {
                Key = "is_pregnant",
                Label = "Are you currently pregnant?",
                Type = "boolean",
                Required = true,
                HelpText = "Pregnant individuals may qualify for additional Medicaid coverage."
            },

            // Question 6: Disability status
            new QuestionDto
            {
                Key = "has_disability",
                Label = "Do you have a disability that affects your ability to work?",
                Type = "boolean",
                Required = true
            }
        };
    }
}
