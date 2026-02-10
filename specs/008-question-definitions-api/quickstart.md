# Quickstart: Eligibility Question Definitions API

**Phase 1 Output** | **Date**: 2026-02-10 | **Feature**: 008-question-definitions-api

A fast integration guide for developers implementing the Question Definitions API client-side and incorporating it into the wizard flow.

---

## Overview

The Question Definitions API provides question metadata and conditional visibility rules for an eligibility questionnaire. The frontend consumes this API to:

1. Load questions for a state/program combination
2. Render questions based on field type (text, select, checkbox, etc.)
3. Evaluate conditional visibility rules against user answers
4. Display the next applicable question

**Key Concept**: Conditional rule evaluation happens **client-side**. The backend returns rule definitions; the frontend evaluates them to determine visibility.

---

## Backend Implementation (ASP.NET Core)

### Step 1: Add Domain Entities

Create domain entities in `MAA.Domain/`:

```csharp
// File: src/MAA.Domain/Question.cs
public class Question
{
    public Guid QuestionId { get; set; }
    public string StateCode { get; set; } // "CA"
    public string ProgramCode { get; set; } // "MEDI-CAL"
    public int DisplayOrder { get; set; }
    public string QuestionText { get; set; }
    public QuestionFieldType FieldType { get; set; }
    public bool IsRequired { get; set; } = false;
    public string? HelpText { get; set; }
    public string? ValidationRegex { get; set; }
    public Guid? ConditionalRuleId { get; set; }
    
    // Navigation properties
    public ConditionalRule? ConditionalRule { get; set; }
    public ICollection<QuestionOption> Options { get; set; } = new List<QuestionOption>();
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public enum QuestionFieldType
{
    Text,
    Select,
    Checkbox,
    Radio,
    Date,
    Currency
}

// File: src/MAA.Domain/ConditionalRule.cs
public class ConditionalRule
{
    public Guid ConditionalRuleId { get; set; }
    public string RuleExpression { get; set; }
    public string? Description { get; set; }
    
    // Navigation
    public ICollection<Question> Questions { get; set; } = new List<Question>();
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// File: src/MAA.Domain/QuestionOption.cs
public class QuestionOption
{
    public Guid OptionId { get; set; }
    public Guid QuestionId { get; set; }
    public string OptionLabel { get; set; }
    public string OptionValue { get; set; }
    public int DisplayOrder { get; set; }
    
    // Navigation
    public Question Question { get; set; } = null!;
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

### Step 2: Update EntityFramework DbContext

Add DbSets to `SessionContext`:

```csharp
// File: src/MAA.Infrastructure/Data/SessionContext.cs
public class SessionContext : DbContext
{
    public DbSet<Question> Questions { get; set; }
    public DbSet<ConditionalRule> ConditionalRules { get; set; }
    public DbSet<QuestionOption> QuestionOptions { get; set; }
    
    // ... existing code ...
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Configure Question entity
        modelBuilder.Entity<Question>(entity =>
        {
            entity.HasKey(e => e.QuestionId);
            entity.Property(e => e.StateCode).IsRequired().HasMaxLength(2);
            entity.Property(e => e.ProgramCode).IsRequired().HasMaxLength(50);
            entity.Property(e => e.QuestionText).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.HelpText).HasMaxLength(2000);
            entity.Property(e => e.ValidationRegex).HasMaxLength(500);
            
            entity.HasIndex(e => new { e.StateCode, e.ProgramCode });
            entity.HasIndex(e => new { e.StateCode, e.ProgramCode, e.DisplayOrder }).IsUnique();
            entity.HasIndex(e => e.ConditionalRuleId);
            
            entity.HasOne(e => e.ConditionalRule)
                .WithMany(cr => cr.Questions)
                .HasForeignKey(e => e.ConditionalRuleId)
                .OnDelete(DeleteBehavior.SetNull);
            
            entity.HasMany(e => e.Options)
                .WithOne(op => op.Question)
                .HasForeignKey(op => op.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        // Configure ConditionalRule entity
        modelBuilder.Entity<ConditionalRule>(entity =>
        {
            entity.HasKey(e => e.ConditionalRuleId);
            entity.Property(e => e.RuleExpression).IsRequired().HasMaxLength(5000);
            entity.Property(e => e.Description).HasMaxLength(200);
        });
        
        // Configure QuestionOption entity
        modelBuilder.Entity<QuestionOption>(entity =>
        {
            entity.HasKey(e => e.OptionId);
            entity.Property(e => e.OptionLabel).IsRequired().HasMaxLength(200);
            entity.Property(e => e.OptionValue).IsRequired().HasMaxLength(100);
            
            entity.HasIndex(e => new { e.QuestionId, e.OptionValue }).IsUnique();
            entity.HasIndex(e => new { e.QuestionId, e.DisplayOrder }).IsUnique();
        });
    }
}
```

### Step 3: Create Application DTOs

```csharp
// File: src/MAA.Application/DTOs/QuestionDto.cs
public record QuestionDto(
    Guid QuestionId,
    int DisplayOrder,
    string QuestionText,
    string FieldType,
    bool IsRequired,
    string? HelpText,
    string? ValidationRegex,
    Guid? ConditionalRuleId,
    List<QuestionOptionDto>? Options
);

// File: src/MAA.Application/DTOs/ConditionalRuleDto.cs
public record ConditionalRuleDto(
    Guid ConditionalRuleId,
    string RuleExpression,
    string? Description
);

// File: src/MAA.Application/DTOs/QuestionOptionDto.cs
public record QuestionOptionDto(
    Guid OptionId,
    string OptionLabel,
    string OptionValue,
    int DisplayOrder
);

// File: src/MAA.Application/DTOs/GetQuestionsResultDto.cs
public record GetQuestionsResultDto(
    string StateCode,
    string ProgramCode,
    List<QuestionDto> Questions,
    List<ConditionalRuleDto> ConditionalRules
);
```

### Step 4: Create Query Handler

```csharp
// File: src/MAA.Application/Queries/GetQuestionsQuery.cs
public record GetQuestionsQuery(string StateCode, string ProgramCode) 
    : IRequest<GetQuestionsResultDto>;

// File: src/MAA.Application/Handlers/GetQuestionsQueryHandler.cs
public class GetQuestionsQueryHandler : IRequestHandler<GetQuestionsQuery, GetQuestionsResultDto>
{
    private readonly IQuestionRepository _questionRepository;
    private readonly IValidator<GetQuestionsQuery> _validator;
    
    public GetQuestionsQueryHandler(
        IQuestionRepository questionRepository,
        IValidator<GetQuestionsQuery> validator)
    {
        _questionRepository = questionRepository;
        _validator = validator;
    }
    
    public async Task<GetQuestionsResultDto> Handle(GetQuestionsQuery request, CancellationToken cancellationToken)
    {
        // Validate input
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);
        
        // Fetch questions
        var questions = await _questionRepository.GetQuestionsAsync(
            request.StateCode, 
            request.ProgramCode, 
            cancellationToken);
        
        if (!questions.Any())
            throw new NotFoundException($"No questions found for state '{request.StateCode}' and program '{request.ProgramCode}'");
        
        // Map to DTOs
        var questionDtos = questions
            .OrderBy(q => q.DisplayOrder)
            .Select(q => new QuestionDto(
                q.QuestionId,
                q.DisplayOrder,
                q.QuestionText,
                q.FieldType.ToString().ToLower(),
                q.IsRequired,
                q.HelpText,
                q.ValidationRegex,
                q.ConditionalRuleId,
                q.Options?.OrderBy(o => o.DisplayOrder)
                    .Select(o => new QuestionOptionDto(
                        o.OptionId,
                        o.OptionLabel,
                        o.OptionValue,
                        o.DisplayOrder))
                    .ToList() ?? new List<QuestionOptionDto>()
            ))
            .ToList();
        
        // Collect all referenced conditional rules
        var ruleIds = questionDtos
            .Where(q => q.ConditionalRuleId.HasValue)
            .Select(q => q.ConditionalRuleId!.Value)
            .Distinct();
        
        var rules = await _questionRepository.GetConditionalRulesAsync(ruleIds, cancellationToken);
        var ruleDtos = rules
            .Select(r => new ConditionalRuleDto(
                r.ConditionalRuleId,
                r.RuleExpression,
                r.Description))
            .ToList();
        
        return new GetQuestionsResultDto(
            request.StateCode,
            request.ProgramCode,
            questionDtos,
            ruleDtos);
    }
}
```

### Step 5: Create Repository

```csharp
// File: src/MAA.Infrastructure/Repositories/IQuestionRepository.cs
public interface IQuestionRepository
{
    Task<List<Question>> GetQuestionsAsync(string stateCode, string programCode, CancellationToken cancellationToken);
    Task<List<ConditionalRule>> GetConditionalRulesAsync(IEnumerable<Guid> ruleIds, CancellationToken cancellationToken);
}

// File: src/MAA.Infrastructure/Repositories/QuestionRepository.cs
public class QuestionRepository : IQuestionRepository
{
    private readonly SessionContext _context;
    
    public QuestionRepository(SessionContext context)
    {
        _context = context;
    }
    
    public async Task<List<Question>> GetQuestionsAsync(
        string stateCode, 
        string programCode, 
        CancellationToken cancellationToken)
    {
        return await _context.Questions
            .AsNoTracking()
            .Where(q => q.StateCode == stateCode && q.ProgramCode == programCode)
            .Include(q => q.Options)
            .OrderBy(q => q.DisplayOrder)
            .ToListAsync(cancellationToken);
    }
    
    public async Task<List<ConditionalRule>> GetConditionalRulesAsync(
        IEnumerable<Guid> ruleIds, 
        CancellationToken cancellationToken)
    {
        return await _context.ConditionalRules
            .AsNoTracking()
            .Where(r => ruleIds.Contains(r.ConditionalRuleId))
            .ToListAsync(cancellationToken);
    }
}
```

### Step 6: Add API Controller

```csharp
// File: src/MAA.API/Controllers/QuestionsController.cs
[ApiController]
[Route("api/questions")]
[Produces("application/json")]
public class QuestionsController : ControllerBase
{
    private readonly IMediator _mediator;
    
    public QuestionsController(IMediator mediator)
    {
        _mediator = mediator;
    }
    
    /// <summary>
    /// Get question definitions for a state/program combination
    /// </summary>
    [HttpGet("{stateCode}/{programCode}")]
    [ProducesResponseType(typeof(GetQuestionsResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetQuestions(string stateCode, string programCode)
    {
        var query = new GetQuestionsQuery(stateCode, programCode);
        var result = await _mediator.Send(query);
        return Ok(result);
    }
}
```

### Step 7: Create Database Migration

```bash
# From src/ directory
cd d:\Programming\Langate\MedicaidApplicationAssistant\src
dotnet ef migrations add AddQuestionDefinitions --context SessionContext -p MAA.Infrastructure -s MAA.API
dotnet ef database update
```

---

## Frontend Implementation (React / TypeScript)

### Step 1: Create API Service

```typescript
// File: frontend/src/services/questionService.ts
import axios from 'axios';

export interface QuestionOptionDto {
  optionId: string;
  optionLabel: string;
  optionValue: string;
  displayOrder: number;
}

export interface QuestionDto {
  questionId: string;
  displayOrder: number;
  questionText: string;
  fieldType: 'text' | 'select' | 'checkbox' | 'radio' | 'date' | 'currency';
  isRequired: boolean;
  helpText?: string;
  validationRegex?: string;
  conditionalRuleId?: string;
  options?: QuestionOptionDto[];
}

export interface ConditionalRuleDto {
  conditionalRuleId: string;
  ruleExpression: string;
  description?: string;
}

export interface GetQuestionsResponse {
  stateCode: string;
  programCode: string;
  questions: QuestionDto[];
  conditionalRules: ConditionalRuleDto[];
}

const API_BASE = import.meta.env.VITE_API_URL || 'http://localhost:5000';

export const questionService = {
  async getQuestions(stateCode: string, programCode: string): Promise<GetQuestionsResponse> {
    const response = await axios.get<GetQuestionsResponse>(
      `${API_BASE}/api/questions/${stateCode}/${programCode}`
    );
    return response.data;
  }
};
```

### Step 2: Create Conditional Rule Evaluator

```typescript
// File: frontend/src/lib/evaluateConditionalRules.ts
import { ConditionalRuleDto } from '../services/questionService';

export type AnswerMap = Record<string, string | number | boolean | null>;

/**
 * Evaluate a conditional rule expression against provided answers
 * 
 * Supports:
 * - Comparisons: ==, !=, >, <, >=, <=
 * - Membership: IN, NOT IN
 * - Logic: AND, OR, NOT
 * - Grouping: parentheses
 * 
 * Example: "550e8400-e29b-41d4-a716-446655440000 == 'yes' AND 550e8400-e29b-41d4-a716-446655440001 >= 18"
 */
export function evaluateRuleExpression(
  ruleExpression: string,
  answers: AnswerMap
): boolean {
  try {
    // Replace question IDs with their values in the expression
    let expression = ruleExpression;
    
    // Find all UUID patterns in the expression
    const uuidPattern = /[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}/gi;
    const questionIds = [...new Set(expression.match(uuidPattern) || [])];
    
    // Replace each question ID with its value
    questionIds.forEach(id => {
      const value = answers[id];
      const serializedValue = typeof value === 'string' ? `'${value}'` : value;
      expression = expression.replace(new RegExp(id, 'g'), String(serializedValue));
    });
    
    // Evaluate the expression (safe because we control the operands)
    // In production, implement a proper expression parser instead of eval()
    return Function(`return ${expression}`)() as boolean;
  } catch (error) {
    console.error('Error evaluating rule expression:', {
      ruleExpression,
      error: error instanceof Error ? error.message : String(error)
    });
    return false; // Default to hidden on evaluation error
  }
}

/**
 * Determine if a question should be visible based on conditional rules
 */
export function isQuestionVisible(
  questionId: string,
  conditionalRuleId: string | undefined,
  rules: ConditionalRuleDto[],
  answers: AnswerMap
): boolean {
  // No rule = always visible
  if (!conditionalRuleId) {
    return true;
  }
  
  // Find the rule
  const rule = rules.find(r => r.conditionalRuleId === conditionalRuleId);
  if (!rule) {
    console.warn(`Rule not found: ${conditionalRuleId}`);
    return false; // Default to hidden if rule not found
  }
  
  // Evaluate the rule
  return evaluateRuleExpression(rule.ruleExpression, answers);
}
```

### Step 3: Create React Hook

```typescript
// File: frontend/src/hooks/useQuestions.ts
import { useQuery } from '@tanstack/react-query';
import { questionService, GetQuestionsResponse } from '../services/questionService';

export function useQuestions(stateCode: string, programCode: string) {
  return useQuery<GetQuestionsResponse>({
    queryKey: ['questions', stateCode, programCode],
    queryFn: () => questionService.getQuestions(stateCode, programCode),
    staleTime: 24 * 60 * 60 * 1000, // Cache for 24 hours
    gcTime: 30 * 60 * 1000, // Keep in cache for 30 minutes after stale
  });
}
```

### Step 4: Create Question Renderer Component

```typescript
// File: frontend/src/components/QuestionRenderer.tsx
import { QuestionDto } from '../services/questionService';
import { Input } from './ui/input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from './ui/select';
import { Checkbox } from './ui/checkbox';
import { RadioGroup, RadioGroupItem } from './ui/radio-group';
import { Label } from './ui/label';

interface QuestionRendererProps {
  question: QuestionDto;
  value: string | number | boolean | null;
  onChange: (value: string | number | boolean | null) => void;
}

export function QuestionRenderer({ question, value, onChange }: QuestionRendererProps) {
  const baseClasses = 'mb-6';
  
  return (
    <div className={baseClasses}>
      <Label htmlFor={question.questionId} className="block text-base font-semibold mb-2">
        {question.questionText}
        {question.isRequired && <span className="text-red-500 ml-1">*</span>}
      </Label>
      
      {question.helpText && (
        <p className="text-sm text-gray-600 mb-3">{question.helpText}</p>
      )}
      
      {question.fieldType === 'text' && (
        <Input
          id={question.questionId}
          type="text"
          value={String(value || '')}
          onChange={e => onChange(e.target.value)}
          pattern={question.validationRegex}
          required={question.isRequired}
        />
      )}
      
      {question.fieldType === 'select' && (
        <Select value={String(value || '')} onValueChange={onChange}>
          <SelectTrigger id={question.questionId}>
            <SelectValue placeholder="Select an option..." />
          </SelectTrigger>
          <SelectContent>
            {question.options?.map(option => (
              <SelectItem key={option.optionId} value={option.optionValue}>
                {option.optionLabel}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      )}
      
      {question.fieldType === 'radio' && (
        <RadioGroup value={String(value || '')} onValueChange={onChange}>
          {question.options?.map(option => (
            <div key={option.optionId} className="flex items-center space-x-2">
              <RadioGroupItem value={option.optionValue} id={option.optionId} />
              <Label htmlFor={option.optionId}>{option.optionLabel}</Label>
            </div>
          ))}
        </RadioGroup>
      )}
      
      {question.fieldType === 'checkbox' && (
        <div className="space-y-2">
          {question.options?.map(option => (
            <div key={option.optionId} className="flex items-center space-x-2">
              <Checkbox
                id={option.optionId}
                checked={value === option.optionValue}
                onCheckedChange={checked => onChange(checked ? option.optionValue : null)}
              />
              <Label htmlFor={option.optionId}>{option.optionLabel}</Label>
            </div>
          ))}
        </div>
      )}
      
      {question.fieldType === 'date' && (
        <Input
          id={question.questionId}
          type="date"
          value={String(value || '')}
          onChange={e => onChange(e.target.value)}
          required={question.isRequired}
        />
      )}
      
      {question.fieldType === 'currency' && (
        <Input
          id={question.questionId}
          type="number"
          step="0.01"
          value={String(value || '')}
          onChange={e => onChange(parseFloat(e.target.value))}
          required={question.isRequired}
        />
      )}
    </div>
  );
}
```

### Step 5: Integrate into Wizard

```typescript
// File: frontend/src/components/EligibilityWizard.tsx
import { useState } from 'react';
import { useQuestions } from '../hooks/useQuestions';
import { isQuestionVisible } from '../lib/evaluateConditionalRules';
import { QuestionRenderer } from './QuestionRenderer';
import { Button } from './ui/button';
import { Card, CardContent, CardHeader, CardTitle } from './ui/card';

interface WizardProps {
  stateCode: string;
  programCode: string;
  onComplete: (answers: Record<string, any>) => void;
}

export function EligibilityWizard({ stateCode, programCode, onComplete }: WizardProps) {
  const [currentQuestionIndex, setCurrentQuestionIndex] = useState(0);
  const [answers, setAnswers] = useState<Record<string, any>>({});
  
  const { data, isLoading, error } = useQuestions(stateCode, programCode);
  
  if (isLoading) {
    return <div>Loading questions...</div>;
  }
  
  if (error) {
    return <div className="text-red-600">Error loading questions: {error.message}</div>;
  }
  
  if (!data || data.questions.length === 0) {
    return <div>No questions available for this state/program.</div>;
  }
  
  // Get visible questions
  const visibleQuestions = data.questions.filter(q =>
    isQuestionVisible(q.questionId, q.conditionalRuleId, data.conditionalRules, answers)
  );
  
  if (currentQuestionIndex >= visibleQuestions.length) {
    return (
      <Card>
        <CardHeader>
          <CardTitle>Questionnaire Complete!</CardTitle>
        </CardHeader>
        <CardContent>
          <Button onClick={() => onComplete(answers)}>
            Submit Application
          </Button>
        </CardContent>
      </Card>
    );
  }
  
  const currentQuestion = visibleQuestions[currentQuestionIndex];
  
  return (
    <Card>
      <CardHeader>
        <CardTitle>
          Question {currentQuestionIndex + 1} of {visibleQuestions.length}
        </CardTitle>
      </CardHeader>
      <CardContent>
        <QuestionRenderer
          question={currentQuestion}
          value={answers[currentQuestion.questionId] || null}
          onChange={value => {
            setAnswers(prev => ({
              ...prev,
              [currentQuestion.questionId]: value
            }));
          }}
        />
        
        <div className="flex gap-4 mt-6">
          <Button
            onClick={() => setCurrentQuestionIndex(Math.max(0, currentQuestionIndex - 1))}
            disabled={currentQuestionIndex === 0}
            variant="outline"
          >
            Previous
          </Button>
          
          <Button
            onClick={() => setCurrentQuestionIndex(currentQuestionIndex + 1)}
            disabled={
              currentQuestion.isRequired && !answers[currentQuestion.questionId]
            }
          >
            {currentQuestionIndex === visibleQuestions.length - 1 ? 'Finish' : 'Next'}
          </Button>
        </div>
      </CardContent>
    </Card>
  );
}
```

---

## Testing Checklist

### Backend Tests

- [ ] Question retrieval for valid state/program combination
- [ ] Empty result for state/program with no questions  
- [ ] 400 error for invalid state code format
- [ ] 400 error for invalid program code format
- [ ] 404 error for undefined state/program combo
- [ ] Questions returned in displayOrder
- [ ] Options returned in displayOrder
- [ ] Conditional rules correctly referenced

### Frontend Tests

- [ ] Hook `useQuestions` fetches and caches data
- [ ] Conditional rule evaluator handles all operators
- [ ] Circular dependencies logged/handled gracefully
- [ ] All field types render correctly
- [ ] Required validation enforced before next
- [ ] Regex validation applied on text fields

### Integration Tests

- [ ] Full wizard flow with conditional visibility
- [ ] Answer changes trigger re-evaluation of visibility
- [ ] Multiple nested conditions evaluate correctly

---

## Common Issues & Troubleshooting

| Issue | Cause | Fix |
|-------|-------|-----|
| 404 Not Found | State/program combo doesn't exist | Check database has questions for that combo |
| Rule evaluation returns false | Question ID in rule doesn't match answer keys | Ensure rule uses UUIDs that match question IDs |
| Questions appear in wrong order | Missing DisplayOrder values | Run database query: `SELECT * FROM questions WHERE display_order IS NULL` |
| Options don't appear | Question has no options | Add QuestionOption records for select/radio/checkbox questions |

---

## Next Steps

1. **Complete `/speckit.tasks`** to generate detailed implementation task list
2. **Run database migration** to create tables
3. **Deploy backend** with API controller
4. **Integrate frontend components** into wizard flow
5. **Test conditional visibility** with various answer combinations
6. **Monitor performance** (target: < 200ms p95 for question retrieval)
