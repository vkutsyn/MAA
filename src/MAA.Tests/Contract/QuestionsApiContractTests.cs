using FluentAssertions;
using MAA.Domain;
using MAA.Infrastructure.Data;
using MAA.Tests.Integration;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text.Json;
using Xunit;

namespace MAA.Tests.Contract;

/// <summary>
/// Contract tests validating question definitions endpoints match expected behavior.
/// </summary>
public class QuestionsApiContractTests : IAsyncLifetime
{
    private HttpClient? _httpClient;
    private TestWebApplicationFactory? _factory;

    private const string StateCode = "CA";
    private const string ProgramCode = "MEDI-CAL";

    public async Task InitializeAsync()
    {
        _factory = new TestWebApplicationFactory();
        _httpClient = _factory.CreateClient();

        await SeedQuestionDefinitionsAsync();
    }

    public async Task DisposeAsync()
    {
        _httpClient?.Dispose();
        if (_factory != null)
            await _factory.DisposeAsync();
    }

    [Fact]
    public async Task GetQuestions_InvalidState_ReturnsBadRequest()
    {
        // Act
        var response = await _httpClient!.GetAsync($"/api/questions/XX/{ProgramCode}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "GET /api/questions/{stateCode}/{programCode} should validate state code format");
    }

    [Fact]
    public async Task GetQuestions_ValidStateProgram_ReturnsQuestionDefinitions()
    {
        // Act
        var response = await _httpClient!.GetAsync($"/api/questions/{StateCode}/{ProgramCode}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "GET /api/questions/{stateCode}/{programCode} should return definitions when available");

        var payload = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(payload);

        var root = doc.RootElement;
        root.GetProperty("stateCode").GetString().Should().Be(StateCode);
        root.GetProperty("programCode").GetString().Should().Be(ProgramCode);

        var questions = root.GetProperty("questions");
        questions.ValueKind.Should().Be(JsonValueKind.Array);
        questions.GetArrayLength().Should().Be(2);

        var question = questions[0];
        question.GetProperty("questionText").GetString().Should().Be("Do you have dependents?");
        question.GetProperty("fieldType").GetString().Should().Be("select");

        var options = question.GetProperty("options");
        options.ValueKind.Should().Be(JsonValueKind.Array);
        options.GetArrayLength().Should().Be(2);

        var rules = root.GetProperty("conditionalRules");
        rules.ValueKind.Should().Be(JsonValueKind.Array);
        rules.GetArrayLength().Should().Be(1);
    }

    private async Task SeedQuestionDefinitionsAsync()
    {
        if (_factory == null)
            return;

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SessionContext>();

        var now = DateTime.UtcNow;
        var ruleId = Guid.NewGuid();
        var triggerQuestionId = Guid.NewGuid();
        var followUpQuestionId = Guid.NewGuid();

        context.MedicaidPrograms.Add(new MAA.Domain.Rules.MedicaidProgram
        {
            ProgramId = Guid.NewGuid(),
            StateCode = StateCode,
            ProgramName = "Medi-Cal",
            ProgramCode = ProgramCode,
            EligibilityPathway = MAA.Domain.Rules.EligibilityPathway.MAGI,
            CreatedAt = now,
            UpdatedAt = now
        });

        context.ConditionalRules.Add(new ConditionalRule
        {
            ConditionalRuleId = ruleId,
            RuleExpression = $"{triggerQuestionId} == 'yes'",
            Description = "Show follow-up if yes",
            CreatedAt = now,
            UpdatedAt = now
        });

        context.Questions.AddRange(
            new Question
            {
                QuestionId = triggerQuestionId,
                StateCode = StateCode,
                ProgramCode = ProgramCode,
                DisplayOrder = 1,
                QuestionText = "Do you have dependents?",
                FieldType = QuestionFieldType.Select,
                IsRequired = false,
                CreatedAt = now,
                UpdatedAt = now,
                Options = new List<QuestionOption>
                {
                    new()
                    {
                        OptionId = Guid.NewGuid(),
                        QuestionId = triggerQuestionId,
                        OptionLabel = "Yes",
                        OptionValue = "yes",
                        DisplayOrder = 1,
                        CreatedAt = now,
                        UpdatedAt = now
                    },
                    new()
                    {
                        OptionId = Guid.NewGuid(),
                        QuestionId = triggerQuestionId,
                        OptionLabel = "No",
                        OptionValue = "no",
                        DisplayOrder = 2,
                        CreatedAt = now,
                        UpdatedAt = now
                    }
                }
            },
            new Question
            {
                QuestionId = followUpQuestionId,
                StateCode = StateCode,
                ProgramCode = ProgramCode,
                DisplayOrder = 2,
                QuestionText = "How many dependents?",
                FieldType = QuestionFieldType.Text,
                IsRequired = false,
                ConditionalRuleId = ruleId,
                CreatedAt = now,
                UpdatedAt = now
            }
        );

        await context.SaveChangesAsync();
    }
}
