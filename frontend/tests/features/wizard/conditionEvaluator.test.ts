/**
 * Unit Tests: Condition Evaluator
 *
 * Verifies pure functions for evaluating question visibility conditions.
 * Tests all supported operators: equals, not_equals, gt, gte, lt, lte, includes
 *
 * Test Strategy:
 * - Each operator gets comprehensive test cases
 * - Test with various data types (string, number, array)
 * - Test edge cases (null, undefined, empty)
 * - Verify condition evaluation is pure (no side effects)
 */

import { describe, it, expect } from "vitest";
import {
  evaluateCondition,
  computeVisibility,
  AnswerMap,
} from "@/features/wizard/conditionEvaluator";
import { QuestionCondition } from "@/features/wizard/types";

describe("evaluateCondition - All Operators", () => {
  describe("equals operator", () => {
    it("should return true when string field equals condition value", () => {
      const condition: QuestionCondition = {
        fieldKey: "state",
        operator: "equals",
        value: "CA",
      };
      const answers: AnswerMap = { state: "CA" };

      expect(evaluateCondition(condition, answers)).toBe(true);
    });

    it("should return false when string field does not equal condition value", () => {
      const condition: QuestionCondition = {
        fieldKey: "state",
        operator: "equals",
        value: "CA",
      };
      const answers: AnswerMap = { state: "NY" };

      expect(evaluateCondition(condition, answers)).toBe(false);
    });

    it("should return true when multiselect array includes condition value", () => {
      const condition: QuestionCondition = {
        fieldKey: "languages",
        operator: "equals",
        value: "Spanish",
      };
      const answers: AnswerMap = {
        languages: ["English", "Spanish", "French"],
      };

      expect(evaluateCondition(condition, answers)).toBe(true);
    });

    it("should return false when multiselect array does not include condition value", () => {
      const condition: QuestionCondition = {
        fieldKey: "languages",
        operator: "equals",
        value: "German",
      };
      const answers: AnswerMap = { languages: ["English", "Spanish"] };

      expect(evaluateCondition(condition, answers)).toBe(false);
    });

    it("should return false when field is not answered", () => {
      const condition: QuestionCondition = {
        fieldKey: "state",
        operator: "equals",
        value: "CA",
      };
      const answers: AnswerMap = {};

      expect(evaluateCondition(condition, answers)).toBe(false);
    });

    it("should return false when field is null", () => {
      const condition: QuestionCondition = {
        fieldKey: "state",
        operator: "equals",
        value: "CA",
      };
      const answers: AnswerMap = { state: null };

      expect(evaluateCondition(condition, answers)).toBe(false);
    });
  });

  describe("not_equals operator", () => {
    it("should return true when field does not equal condition value", () => {
      const condition: QuestionCondition = {
        fieldKey: "state",
        operator: "not_equals",
        value: "CA",
      };
      const answers: AnswerMap = { state: "NY" };

      expect(evaluateCondition(condition, answers)).toBe(true);
    });

    it("should return false when field equals condition value", () => {
      const condition: QuestionCondition = {
        fieldKey: "state",
        operator: "not_equals",
        value: "CA",
      };
      const answers: AnswerMap = { state: "CA" };

      expect(evaluateCondition(condition, answers)).toBe(false);
    });

    it("should return true when multiselect array does not include value", () => {
      const condition: QuestionCondition = {
        fieldKey: "languages",
        operator: "not_equals",
        value: "German",
      };
      const answers: AnswerMap = { languages: ["English", "Spanish"] };

      expect(evaluateCondition(condition, answers)).toBe(true);
    });

    it("should return false when field is not answered", () => {
      const condition: QuestionCondition = {
        fieldKey: "state",
        operator: "not_equals",
        value: "CA",
      };
      const answers: AnswerMap = {};

      expect(evaluateCondition(condition, answers)).toBe(false);
    });
  });

  describe("gt (greater than) operator", () => {
    it("should return true when number is greater than condition value", () => {
      const condition: QuestionCondition = {
        fieldKey: "age",
        operator: "gt",
        value: "18",
      };
      const answers: AnswerMap = { age: "25" };

      expect(evaluateCondition(condition, answers)).toBe(true);
    });

    it("should return false when number equals condition value", () => {
      const condition: QuestionCondition = {
        fieldKey: "age",
        operator: "gt",
        value: "18",
      };
      const answers: AnswerMap = { age: "18" };

      expect(evaluateCondition(condition, answers)).toBe(false);
    });

    it("should return false when number is less than condition value", () => {
      const condition: QuestionCondition = {
        fieldKey: "age",
        operator: "gt",
        value: "18",
      };
      const answers: AnswerMap = { age: "16" };

      expect(evaluateCondition(condition, answers)).toBe(false);
    });

    it("should handle decimal numbers", () => {
      const condition: QuestionCondition = {
        fieldKey: "income",
        operator: "gt",
        value: "50000.50",
      };
      const answers: AnswerMap = { income: "50000.51" };

      expect(evaluateCondition(condition, answers)).toBe(true);
    });

    it("should return false for non-numeric strings", () => {
      const condition: QuestionCondition = {
        fieldKey: "age",
        operator: "gt",
        value: "18",
      };
      const answers: AnswerMap = { age: "abc" };

      expect(evaluateCondition(condition, answers)).toBe(false);
    });
  });

  describe("gte (greater than or equal) operator", () => {
    it("should return true when number is greater than condition value", () => {
      const condition: QuestionCondition = {
        fieldKey: "age",
        operator: "gte",
        value: "18",
      };
      const answers: AnswerMap = { age: "25" };

      expect(evaluateCondition(condition, answers)).toBe(true);
    });

    it("should return true when number equals condition value", () => {
      const condition: QuestionCondition = {
        fieldKey: "age",
        operator: "gte",
        value: "18",
      };
      const answers: AnswerMap = { age: "18" };

      expect(evaluateCondition(condition, answers)).toBe(true);
    });

    it("should return false when number is less than condition value", () => {
      const condition: QuestionCondition = {
        fieldKey: "age",
        operator: "gte",
        value: "18",
      };
      const answers: AnswerMap = { age: "17" };

      expect(evaluateCondition(condition, answers)).toBe(false);
    });
  });

  describe("lt (less than) operator", () => {
    it("should return true when number is less than condition value", () => {
      const condition: QuestionCondition = {
        fieldKey: "age",
        operator: "lt",
        value: "65",
      };
      const answers: AnswerMap = { age: "50" };

      expect(evaluateCondition(condition, answers)).toBe(true);
    });

    it("should return false when number equals condition value", () => {
      const condition: QuestionCondition = {
        fieldKey: "age",
        operator: "lt",
        value: "65",
      };
      const answers: AnswerMap = { age: "65" };

      expect(evaluateCondition(condition, answers)).toBe(false);
    });

    it("should return false when number is greater than condition value", () => {
      const condition: QuestionCondition = {
        fieldKey: "age",
        operator: "lt",
        value: "65",
      };
      const answers: AnswerMap = { age: "70" };

      expect(evaluateCondition(condition, answers)).toBe(false);
    });
  });

  describe("lte (less than or equal) operator", () => {
    it("should return true when number is less than condition value", () => {
      const condition: QuestionCondition = {
        fieldKey: "age",
        operator: "lte",
        value: "65",
      };
      const answers: AnswerMap = { age: "50" };

      expect(evaluateCondition(condition, answers)).toBe(true);
    });

    it("should return true when number equals condition value", () => {
      const condition: QuestionCondition = {
        fieldKey: "age",
        operator: "lte",
        value: "65",
      };
      const answers: AnswerMap = { age: "65" };

      expect(evaluateCondition(condition, answers)).toBe(true);
    });

    it("should return false when number is greater than condition value", () => {
      const condition: QuestionCondition = {
        fieldKey: "age",
        operator: "lte",
        value: "65",
      };
      const answers: AnswerMap = { age: "70" };

      expect(evaluateCondition(condition, answers)).toBe(false);
    });
  });

  describe("includes operator", () => {
    it("should return true when string includes substring", () => {
      const condition: QuestionCondition = {
        fieldKey: "address",
        operator: "includes",
        value: "Street",
      };
      const answers: AnswerMap = { address: "123 Main Street" };

      expect(evaluateCondition(condition, answers)).toBe(true);
    });

    it("should return false when string does not include substring", () => {
      const condition: QuestionCondition = {
        fieldKey: "address",
        operator: "includes",
        value: "Avenue",
      };
      const answers: AnswerMap = { address: "123 Main Street" };

      expect(evaluateCondition(condition, answers)).toBe(false);
    });

    it("should return true when array element includes substring", () => {
      const condition: QuestionCondition = {
        fieldKey: "experiences",
        operator: "includes",
        value: "management",
      };
      const answers: AnswerMap = {
        experiences: ["Project Management", "Team Lead"],
      };

      expect(evaluateCondition(condition, answers)).toBe(true);
    });

    it("should be case-sensitive", () => {
      const condition: QuestionCondition = {
        fieldKey: "name",
        operator: "includes",
        value: "john",
      };
      const answers: AnswerMap = { name: "John Doe" };

      expect(evaluateCondition(condition, answers)).toBe(false);
    });
  });

  describe("edge cases", () => {
    it("should handle whitespace in values", () => {
      const condition: QuestionCondition = {
        fieldKey: "name",
        operator: "equals",
        value: " CA ",
      };
      const answers: AnswerMap = { name: " CA " };

      expect(evaluateCondition(condition, answers)).toBe(true);
    });

    it("should handle numeric strings correctly", () => {
      const condition: QuestionCondition = {
        fieldKey: "zipcode",
        operator: "equals",
        value: "90210",
      };
      const answers: AnswerMap = { zipcode: "90210" };

      expect(evaluateCondition(condition, answers)).toBe(true);
    });

    it("should return false for undefined field", () => {
      const condition: QuestionCondition = {
        fieldKey: "missing",
        operator: "equals",
        value: "value",
      };
      const answers: AnswerMap = {};

      expect(evaluateCondition(condition, answers)).toBe(false);
    });

    it("should handle boolean values converted to strings", () => {
      const condition: QuestionCondition = {
        fieldKey: "confirmed",
        operator: "equals",
        value: "true",
      };
      const answers: AnswerMap = { confirmed: "true" };

      expect(evaluateCondition(condition, answers)).toBe(true);
    });
  });
});

describe("computeVisibility", () => {
  it("should make all questions visible when no conditions exist", () => {
    const questions = [{ key: "q1" }, { key: "q2" }, { key: "q3" }];
    const answers: AnswerMap = {};

    const visibility = computeVisibility(questions, answers);

    expect(visibility.q1).toBe(true);
    expect(visibility.q2).toBe(true);
    expect(visibility.q3).toBe(true);
  });

  it("should hide question when single condition fails", () => {
    const questions = [
      { key: "q1", conditions: undefined },
      {
        key: "q2",
        conditions: [
          { fieldKey: "q1", operator: "equals" as const, value: "yes" },
        ],
      },
    ];
    const answers: AnswerMap = { q1: "no" };

    const visibility = computeVisibility(questions, answers);

    expect(visibility.q2).toBe(false);
  });

  it("should show question when all conditions pass", () => {
    const questions = [
      { key: "q1", conditions: undefined },
      {
        key: "q2",
        conditions: [
          { fieldKey: "q1", operator: "equals" as const, value: "yes" },
        ],
      },
    ];
    const answers: AnswerMap = { q1: "yes" };

    const visibility = computeVisibility(questions, answers);

    expect(visibility.q2).toBe(true);
  });

  it("should require ALL conditions to pass (AND logic)", () => {
    const questions = [
      { key: "q1", conditions: undefined },
      { key: "q2", conditions: undefined },
      {
        key: "q3",
        conditions: [
          { fieldKey: "q1", operator: "equals" as const, value: "yes" },
          { fieldKey: "q2", operator: "equals" as const, value: "no" },
        ],
      },
    ];

    // Both conditions pass
    let visibility = computeVisibility(questions, { q1: "yes", q2: "no" });
    expect(visibility.q3).toBe(true);

    // First fails
    visibility = computeVisibility(questions, { q1: "no", q2: "no" });
    expect(visibility.q3).toBe(false);

    // Second fails
    visibility = computeVisibility(questions, { q1: "yes", q2: "yes" });
    expect(visibility.q3).toBe(false);

    // Both fail
    visibility = computeVisibility(questions, { q1: "no", q2: "yes" });
    expect(visibility.q3).toBe(false);
  });

  it("should return complete visibility map for all questions", () => {
    const questions = [
      { key: "q1", conditions: undefined },
      { key: "q2", conditions: undefined },
      {
        key: "q3",
        conditions: [
          { fieldKey: "q1", operator: "equals" as const, value: "yes" },
        ],
      },
    ];
    const answers: AnswerMap = { q1: "yes" };

    const visibility = computeVisibility(questions, answers);

    expect(Object.keys(visibility)).toContain("q1");
    expect(Object.keys(visibility)).toContain("q2");
    expect(Object.keys(visibility)).toContain("q3");
    expect(Object.keys(visibility).length).toBe(3);
  });

  it("should handle complex dependency chains", () => {
    const questions = [
      { key: "q1", conditions: undefined },
      {
        key: "q2",
        conditions: [
          { fieldKey: "q1", operator: "equals" as const, value: "yes" },
        ],
      },
      {
        key: "q3",
        conditions: [
          { fieldKey: "q2", operator: "equals" as const, value: "yes" },
        ],
      },
    ];

    // q1=yes makes q2 visible, q2=yes makes q3 visible
    let visibility = computeVisibility(questions, { q1: "yes", q2: "yes" });
    expect(visibility.q1).toBe(true);
    expect(visibility.q2).toBe(true);
    expect(visibility.q3).toBe(true);

    // q1=no hides q2, and q2 is hidden so q3 becomes visible only if condition met
    visibility = computeVisibility(questions, { q1: "no", q2: "yes" });
    expect(visibility.q1).toBe(true);
    expect(visibility.q2).toBe(false);
    expect(visibility.q3).toBe(true); // q3 shows because q2=yes even though q2 is hidden
  });

  it("should be pure (no side effects)", () => {
    const questions = [
      {
        key: "q1",
        conditions: [
          { fieldKey: "q2", operator: "equals" as const, value: "yes" },
        ],
      },
    ];
    const answers: AnswerMap = { q2: "yes" };

    const result1 = computeVisibility(questions, answers);
    const result2 = computeVisibility(questions, answers);

    expect(result1).toEqual(result2);
    // Original objects unchanged
    expect(questions).toHaveLength(1);
    expect(Object.keys(answers)).toEqual(["q2"]);
  });
});
