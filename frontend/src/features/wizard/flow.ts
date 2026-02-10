import { QuestionDto, QuestionCondition, Answer } from './types'

/**
 * Conditional flow evaluation for wizard questions.
 * Determines which questions should be displayed based on previous answers.
 */

/**
 * Evaluates a single condition against an answer value.
 * @param condition The condition to evaluate
 * @param answerValue The answer value to check (as string)
 * @returns true if the condition is met, false otherwise
 */
export function evaluateCondition(
  condition: QuestionCondition,
  answerValue: string | undefined
): boolean {
  // If no answer exists, condition fails
  if (answerValue === undefined || answerValue === null || answerValue === '') {
    return false
  }

  const { operator, value: conditionValue } = condition

  switch (operator) {
    case 'equals':
      return answerValue === conditionValue

    case 'not_equals':
      return answerValue !== conditionValue

    case 'gt': {
      const numAnswer = parseFloat(answerValue)
      const numCondition = parseFloat(conditionValue)
      return !isNaN(numAnswer) && !isNaN(numCondition) && numAnswer > numCondition
    }

    case 'gte': {
      const numAnswer = parseFloat(answerValue)
      const numCondition = parseFloat(conditionValue)
      return !isNaN(numAnswer) && !isNaN(numCondition) && numAnswer >= numCondition
    }

    case 'lt': {
      const numAnswer = parseFloat(answerValue)
      const numCondition = parseFloat(conditionValue)
      return !isNaN(numAnswer) && !isNaN(numCondition) && numAnswer < numCondition
    }

    case 'lte': {
      const numAnswer = parseFloat(answerValue)
      const numCondition = parseFloat(conditionValue)
      return !isNaN(numAnswer) && !isNaN(numCondition) && numAnswer <= numCondition
    }

    case 'includes':
      return answerValue.toLowerCase().includes(conditionValue.toLowerCase())

    default:
      console.warn(`Unknown operator: ${operator}`)
      return false
  }
}

/**
 * Evaluates all conditions for a question (AND logic).
 * @param question The question with conditions to evaluate
 * @param answers Map of fieldKey to answer value
 * @returns true if all conditions are met (or no conditions exist), false otherwise
 */
export function shouldShowQuestion(
  question: QuestionDto,
  answers: Record<string, Answer>
): boolean {
  // If no conditions, always show
  if (!question.conditions || question.conditions.length === 0) {
    return true
  }

  // All conditions must be met (AND logic)
  return question.conditions.every((condition) => {
    const answer = answers[condition.fieldKey]
    return evaluateCondition(condition, answer?.answerValue)
  })
}

/**
 * Filters the question list to only include questions that should be visible.
 * @param allQuestions Full list of questions
 * @param answers Current answers
 * @returns Filtered list of visible questions
 */
export function getVisibleQuestions(
  allQuestions: QuestionDto[],
  answers: Record<string, Answer>
): QuestionDto[] {
  return allQuestions.filter((question) => shouldShowQuestion(question, answers))
}

/**
 * Finds the next visible question index after the current index.
 * @param allQuestions Full list of questions
 * @param currentIndex Current question index (in full list)
 * @param answers Current answers
 * @returns Next visible question index, or -1 if at end
 */
export function getNextVisibleIndex(
  allQuestions: QuestionDto[],
  currentIndex: number,
  answers: Record<string, Answer>
): number {
  for (let i = currentIndex + 1; i < allQuestions.length; i++) {
    if (shouldShowQuestion(allQuestions[i], answers)) {
      return i
    }
  }
  return -1 // No more questions
}

/**
 * Finds the previous visible question index before the current index.
 * @param allQuestions Full list of questions
 * @param currentIndex Current question index (in full list)
 * @param answers Current answers
 * @returns Previous visible question index, or -1 if at start
 */
export function getPreviousVisibleIndex(
  allQuestions: QuestionDto[],
  currentIndex: number,
  answers: Record<string, Answer>
): number {
  for (let i = currentIndex - 1; i >= 0; i--) {
    if (shouldShowQuestion(allQuestions[i], answers)) {
      return i
    }
  }
  return -1 // No previous questions
}

/**
 * Calculates progress based on visible questions only.
 * @param allQuestions Full list of questions
 * @param currentIndex Current question index (in full list)
 * @param answers Current answers
 * @returns Progress information
 */
export function calculateProgress(
  allQuestions: QuestionDto[],
  currentIndex: number,
  answers: Record<string, Answer>
): {
  currentVisibleIndex: number
  totalVisibleQuestions: number
  completionPercent: number
} {
  const visibleQuestions = getVisibleQuestions(allQuestions, answers)
  const currentQuestion = allQuestions[currentIndex]
  
  // Find the position of the current question in the visible list
  const currentVisibleIndex = visibleQuestions.findIndex(
    (q) => q.key === currentQuestion?.key
  )

  const totalVisibleQuestions = visibleQuestions.length
  const completionPercent =
    totalVisibleQuestions > 0
      ? Math.round(((currentVisibleIndex + 1) / totalVisibleQuestions) * 100)
      : 0

  return {
    currentVisibleIndex: currentVisibleIndex >= 0 ? currentVisibleIndex : 0,
    totalVisibleQuestions,
    completionPercent,
  }
}
