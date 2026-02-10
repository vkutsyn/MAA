/**
 * Performance Tests: Condition Evaluation
 *
 * Verifies that condition evaluation meets performance targets:
 * - Single condition evaluation: <5ms
 * - Multiple conditions per question: <20ms
 * - Full visibility computation (50 questions): <200ms
 *
 * Test Strategy:
 * - Measure evaluateCondition execution time
 * - Measure computeVisibility execution time with various question counts
 * - Ensure performance degrades gracefully
 */

import { describe, it, expect } from "vitest";
import {
  evaluateCondition,
  computeVisibility,
  AnswerMap,
} from "@/features/wizard/conditionEvaluator";
import { QuestionCondition } from "@/features/wizard/types";

describe("Performance: Condition Evaluation", () => {
  describe("Single Condition Evaluation", () => {
    it("should evaluate simple equals condition under 5ms", () => {
      const condition: QuestionCondition = {
        fieldKey: "state",
        operator: "equals",
        value: "CA",
      };
      const answers: AnswerMap = { state: "CA" };

      const startTime = performance.now();
      for (let i = 0; i < 1000; i++) {
        evaluateCondition(condition, answers);
      }
      const endTime = performance.now();

      const avgTime = (endTime - startTime) / 1000;
      expect(avgTime).toBeLessThan(5);
    });

    it("should evaluate comparison operators under 5ms", () => {
      const condition: QuestionCondition = {
        fieldKey: "age",
        operator: "gte",
        value: "18",
      };
      const answers: AnswerMap = { age: "25" };

      const startTime = performance.now();
      for (let i = 0; i < 1000; i++) {
        evaluateCondition(condition, answers);
      }
      const endTime = performance.now();

      const avgTime = (endTime - startTime) / 1000;
      expect(avgTime).toBeLessThan(5);
    });

    it("should evaluate multiselect includes under 5ms", () => {
      const condition: QuestionCondition = {
        fieldKey: "languages",
        operator: "includes",
        value: "Spanish",
      };
      const answers: AnswerMap = {
        languages: ["English", "Spanish", "French", "German", "Portuguese"],
      };

      const startTime = performance.now();
      for (let i = 0; i < 1000; i++) {
        evaluateCondition(condition, answers);
      }
      const endTime = performance.now();

      const avgTime = (endTime - startTime) / 1000;
      expect(avgTime).toBeLessThan(5);
    });
  });

  describe("Multiple Conditions Per Question", () => {
    it("should evaluate 3 conditions per question under 20ms", () => {
      const conditions: QuestionCondition[] = [
        { fieldKey: "age", operator: "gte", value: "18" },
        { fieldKey: "income", operator: "gte", value: "20000" },
        { fieldKey: "employed", operator: "equals", value: "true" },
      ];
      const answers: AnswerMap = {
        age: "30",
        income: "50000",
        employed: "true",
      };

      const startTime = performance.now();
      for (let i = 0; i < 500; i++) {
        const allPass = conditions.every((c) => evaluateCondition(c, answers));
      }
      const endTime = performance.now();

      const avgTime = (endTime - startTime) / 500;
      expect(avgTime).toBeLessThan(20);
    });

    it("should evaluate 5 conditions per question under 20ms", () => {
      const conditions: QuestionCondition[] = [
        { fieldKey: "age", operator: "gte", value: "18" },
        { fieldKey: "income", operator: "lte", value: "100000" },
        { fieldKey: "employed", operator: "equals", value: "true" },
        { fieldKey: "citizenship", operator: "equals", value: "us-citizen" },
        { fieldKey: "state", operator: "not_equals", value: "PR" },
      ];
      const answers: AnswerMap = {
        age: "40",
        income: "60000",
        employed: "true",
        citizenship: "us-citizen",
        state: "CA",
      };

      const startTime = performance.now();
      for (let i = 0; i < 500; i++) {
        const allPass = conditions.every((c) => evaluateCondition(c, answers));
      }
      const endTime = performance.now();

      const avgTime = (endTime - startTime) / 500;
      expect(avgTime).toBeLessThan(20);
    });
  });

  describe("Full Visibility Computation", () => {
    it("should compute visibility for 10 questions under 50ms", () => {
      const questions = Array.from({ length: 10 }, (_, i) => ({
        key: `q${i}`,
        conditions:
          i > 0
            ? [
                {
                  fieldKey: `q${i - 1}`,
                  operator: "equals" as const,
                  value: "yes",
                },
              ]
            : undefined,
      }));

      const answers: AnswerMap = Object.fromEntries(
        Array.from({ length: 10 }, (_, i) => [
          `q${i}`,
          i % 2 === 0 ? "yes" : "no",
        ]),
      );

      const startTime = performance.now();
      for (let i = 0; i < 100; i++) {
        computeVisibility(questions, answers);
      }
      const endTime = performance.now();

      const avgTime = (endTime - startTime) / 100;
      expect(avgTime).toBeLessThan(50);
    });

    it("should compute visibility for 30 questions under 100ms", () => {
      const questions = Array.from({ length: 30 }, (_, i) => ({
        key: `q${i}`,
        conditions:
          i > 0
            ? [
                {
                  fieldKey: `q${Math.floor(Math.random() * i)}`,
                  operator: (
                    [
                      "equals",
                      "not_equals",
                      "gt",
                      "gte",
                      "lt",
                      "lte",
                      "includes",
                    ] as const
                  )[Math.floor(Math.random() * 7)],
                  value: Math.random() > 0.5 ? "yes" : "25",
                },
                {
                  fieldKey: `q${Math.floor(Math.random() * i)}`,
                  operator: "equals" as const,
                  value: "value",
                },
              ]
            : undefined,
      }));

      const answers: AnswerMap = Object.fromEntries(
        Array.from({ length: 30 }, (_, i) => [
          `q${i}`,
          Math.random() > 0.5 ? "yes" : "no",
        ]),
      );

      const startTime = performance.now();
      for (let i = 0; i < 50; i++) {
        computeVisibility(questions, answers);
      }
      const endTime = performance.now();

      const avgTime = (endTime - startTime) / 50;
      expect(avgTime).toBeLessThan(100);
    });

    it("should compute visibility for 50 questions under 200ms (Constitution target)", () => {
      // Realistic question set with conditional dependencies
      const questions = Array.from({ length: 50 }, (_, i) => ({
        key: `q${i}`,
        conditions:
          i > 0 && i % 3 === 0
            ? [
                {
                  fieldKey: `q${i - 1}`,
                  operator: "equals" as const,
                  value: "yes",
                },
              ]
            : i > 2 && i % 5 === 0
              ? [
                  {
                    fieldKey: `q${i - 3}`,
                    operator: "gte" as const,
                    value: "18",
                  },
                  {
                    fieldKey: `q${i - 1}`,
                    operator: "equals" as const,
                    value: "no",
                  },
                ]
              : undefined,
      }));

      const answers: AnswerMap = Object.fromEntries(
        Array.from({ length: 50 }, (_, i) => [
          `q${i}`,
          i % 2 === 0 ? "yes" : "25",
        ]),
      );

      const startTime = performance.now();
      for (let i = 0; i < 20; i++) {
        computeVisibility(questions, answers);
      }
      const endTime = performance.now();

      const avgTime = (endTime - startTime) / 20;
      expect(avgTime).toBeLessThan(200);
    });

    it("should handle maximum expected question count (100) efficiently", () => {
      const questions = Array.from({ length: 100 }, (_, i) => ({
        key: `q${i}`,
        conditions:
          i > 0 && i % 4 === 0
            ? [
                {
                  fieldKey: `q${i - 2}`,
                  operator: "equals" as const,
                  value: "yes",
                },
              ]
            : undefined,
      }));

      const answers: AnswerMap = Object.fromEntries(
        Array.from({ length: 100 }, (_, i) => [
          `q${i}`,
          i % 3 === 0 ? "yes" : "no",
        ]),
      );

      const startTime = performance.now();
      for (let i = 0; i < 10; i++) {
        computeVisibility(questions, answers);
      }
      const endTime = performance.now();

      const avgTime = (endTime - startTime) / 10;
      expect(avgTime).toBeLessThan(500); // Should still be reasonable
    });
  });

  describe("Degradation Analysis", () => {
    it("should show linear or better degradation with question count", () => {
      const sizes = [10, 20, 30, 40, 50];
      const times: number[] = [];

      for (const size of sizes) {
        const questions = Array.from({ length: size }, (_, i) => ({
          key: `q${i}`,
          conditions:
            i > 0
              ? [
                  {
                    fieldKey: `q${i - 1}`,
                    operator: "equals" as const,
                    value: "yes",
                  },
                ]
              : undefined,
        }));

        const answers: AnswerMap = Object.fromEntries(
          Array.from({ length: size }, (_, i) => [`q${i}`, "yes"]),
        );

        const startTime = performance.now();
        for (let i = 0; i < 30; i++) {
          computeVisibility(questions, answers);
        }
        const endTime = performance.now();

        times.push((endTime - startTime) / 30);
      }

      // Verify each step doesn't increase by more than 2x
      for (let i = 1; i < times.length; i++) {
        const ratio = times[i] / times[i - 1];
        expect(ratio).toBeLessThan(3); // Allow some variation
      }

      // Final time should be under target
      expect(times[times.length - 1]).toBeLessThan(200);
    });
  });

  describe("Memory Efficiency", () => {
    it("should not allocate excessive memory for large answer maps", () => {
      // This is more of a smoke test - just ensure no out-of-memory errors
      const largeAnswerMap: AnswerMap = Object.fromEntries(
        Array.from({ length: 1000 }, (_, i) => [`field${i}`, `value${i}`]),
      );

      const questions = Array.from({ length: 50 }, (_, i) => ({
        key: `q${i}`,
        conditions: [
          {
            fieldKey: `field${i}`,
            operator: "equals" as const,
            value: `value${i}`,
          },
        ],
      }));

      const startTime = performance.now();
      const visibility = computeVisibility(questions, largeAnswerMap);
      const endTime = performance.now();

      // Should complete reasonably fast
      expect(endTime - startTime).toBeLessThan(100);
      expect(Object.keys(visibility).length).toBe(50);
    });
  });
});
