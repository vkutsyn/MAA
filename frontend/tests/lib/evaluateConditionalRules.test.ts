import { describe, expect, it } from "vitest";
import {
  evaluateRuleExpression,
  isQuestionVisible,
} from "@/lib/evaluateConditionalRules";
import type {
  ConditionalRuleDto,
  QuestionDefinitionDto,
} from "@/services/questionService";

describe("evaluateRuleExpression", () => {
  it("evaluates equality and numeric comparisons", () => {
    const questionId = "550e8400-e29b-41d4-a716-446655440000";
    const expression = `${questionId} == 'yes' AND ${questionId} != 'no'`;

    expect(
      evaluateRuleExpression(expression, { [questionId]: "yes" }),
    ).toBe(true);
    expect(
      evaluateRuleExpression(expression, { [questionId]: "no" }),
    ).toBe(false);
  });

  it("supports IN lists", () => {
    const questionId = "550e8400-e29b-41d4-a716-446655440001";
    const expression = `${questionId} IN ['a','b','c']`;

    expect(evaluateRuleExpression(expression, { [questionId]: "b" })).toBe(
      true,
    );
    expect(evaluateRuleExpression(expression, { [questionId]: "z" })).toBe(
      false,
    );
  });

  it("returns false when answer is missing", () => {
    const questionId = "550e8400-e29b-41d4-a716-446655440002";
    const expression = `${questionId} > 5`;

    expect(evaluateRuleExpression(expression, {})).toBe(false);
  });
});

describe("isQuestionVisible", () => {
  it("returns true when question has no rule", () => {
    const question: QuestionDefinitionDto = {
      questionId: "550e8400-e29b-41d4-a716-446655440003",
      displayOrder: 1,
      questionText: "Test",
      fieldType: "text",
      isRequired: false,
      conditionalRuleId: null,
    };

    expect(isQuestionVisible(question, {}, [])).toBe(true);
  });

  it("evaluates rule when rule exists", () => {
    const questionId = "550e8400-e29b-41d4-a716-446655440004";
    const ruleId = "660f9511-f30c-52e5-b827-557766551111";

    const question: QuestionDefinitionDto = {
      questionId,
      displayOrder: 1,
      questionText: "Test",
      fieldType: "text",
      isRequired: false,
      conditionalRuleId: ruleId,
    };

    const rules: ConditionalRuleDto[] = [
      {
        conditionalRuleId: ruleId,
        ruleExpression: `${questionId} == 'yes'`,
      },
    ];

    expect(isQuestionVisible(question, { [questionId]: "yes" }, rules)).toBe(
      true,
    );
    expect(isQuestionVisible(question, { [questionId]: "no" }, rules)).toBe(
      false,
    );
  });
});
