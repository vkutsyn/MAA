/**
 * Condition Evaluator Utility
 *
 * Provides pure functions for evaluating question visibility conditions.
 * Follows Constitution I: No I/O dependencies, pure data structures, testable logic.
 */

import { QuestionCondition } from "./types";

/**
 * AnswerMap: Maps field keys to their current answer values
 * Used for evaluating conditional visibility
 */
export type AnswerMap = Record<string, string | string[] | null>;

/**
 * VisibilityState: Maps question keys to their visibility status
 * true = visible, false = hidden due to failed conditions
 */
export interface VisibilityState {
  [questionKey: string]: boolean;
}

/**
 * Evaluates a single condition against current answers
 *
 * @param condition - The condition to evaluate
 * @param answers - Current answer map
 * @returns true if condition passes, false otherwise
 *
 * @example
 * evaluateCondition(
 *   { fieldKey: "age", operator: "gte", value: "18" },
 *   { age: "25" }
 * ) // true
 */
export function evaluateCondition(
  condition: QuestionCondition,
  answers: AnswerMap,
): boolean {
  const fieldValue = answers[condition.fieldKey];

  // Field not answered - condition fails
  if (fieldValue === null || fieldValue === undefined) {
    return false;
  }

  // Normalize values for comparison
  const fieldStr = Array.isArray(fieldValue)
    ? fieldValue.join(",")
    : String(fieldValue);
  const conditionStr = String(condition.value);

  switch (condition.operator) {
    case "equals":
      if (Array.isArray(fieldValue)) {
        return fieldValue.includes(conditionStr);
      }
      return fieldStr === conditionStr;

    case "not_equals":
      if (Array.isArray(fieldValue)) {
        return !fieldValue.includes(conditionStr);
      }
      return fieldStr !== conditionStr;

    case "gt": {
      const fieldNum = parseFloat(fieldStr);
      const condNum = parseFloat(conditionStr);
      return !isNaN(fieldNum) && !isNaN(condNum) && fieldNum > condNum;
    }

    case "gte": {
      const fieldNum = parseFloat(fieldStr);
      const condNum = parseFloat(conditionStr);
      return !isNaN(fieldNum) && !isNaN(condNum) && fieldNum >= condNum;
    }

    case "lt": {
      const fieldNum = parseFloat(fieldStr);
      const condNum = parseFloat(conditionStr);
      return !isNaN(fieldNum) && !isNaN(condNum) && fieldNum < condNum;
    }

    case "lte": {
      const fieldNum = parseFloat(fieldStr);
      const condNum = parseFloat(conditionStr);
      return !isNaN(fieldNum) && !isNaN(condNum) && fieldNum <= condNum;
    }

    case "includes":
      if (Array.isArray(fieldValue)) {
        // For multiselect values, check each entry for the substring match.
        const needle = conditionStr.toLowerCase();
        return fieldValue.some((v) => String(v).toLowerCase().includes(needle));
      }
      return fieldStr.includes(conditionStr);

    default:
      return false;
  }
}

/**
 * Computes visibility state for all questions based on conditions
 *
 * Logic:
 * - Question with no conditions: always visible
 * - Question with conditions: ALL must pass (AND logic)
 * - Failed condition: question is hidden
 *
 * @param questions - Questions with their condition definitions
 * @param answers - Current answers
 * @returns visibility map for all questions
 *
 * @example
 * const visibility = computeVisibility(
 *   [
 *     { key: "q1", conditions: undefined },
 *     { key: "q2", conditions: [{ fieldKey: "q1", operator: "equals", value: "yes" }] }
 *   ],
 *   { q1: "yes" }
 * )
 * // { q1: true, q2: true }
 */
export function computeVisibility(
  questions: Array<{ key: string; conditions?: QuestionCondition[] }>,
  answers: AnswerMap,
): VisibilityState {
  const visibility: VisibilityState = {};

  for (const question of questions) {
    // No conditions = always visible
    if (!question.conditions || question.conditions.length === 0) {
      visibility[question.key] = true;
      continue;
    }

    // All conditions must pass (AND logic)
    visibility[question.key] = question.conditions.every((condition) =>
      evaluateCondition(condition, answers),
    );
  }

  return visibility;
}
