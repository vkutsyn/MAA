import { useState, useMemo } from "react";
import { useWizardStore } from "./store";
import { saveAnswer } from "./answerApi";
import { Answer } from "./types";
import {
  getNextVisibleIndex,
  getPreviousVisibleIndex,
  shouldShowQuestion,
  calculateProgress,
} from "./flow";

/**
 * Hook for wizard navigation with conditional flow support.
 * Handles next/back navigation, answer persistence, and flow evaluation.
 */
export function useWizardNavigator() {
  const questions = useWizardStore((state) => state.questions);
  const currentStep = useWizardStore((state) => state.currentStep);
  const answers = useWizardStore((state) => state.answers);
  const setAnswer = useWizardStore((state) => state.setAnswer);
  const setCurrentStep = useWizardStore((state) => state.setCurrentStep);

  const [isSaving, setIsSaving] = useState(false);
  const [saveError, setSaveError] = useState<string | null>(null);

  /**
   * Navigate to the next visible question.
   * @param answer The answer to save before navigating
   * @returns true if navigation succeeded, false if at the end
   */
  const goNext = async (answer: Answer): Promise<boolean> => {
    setIsSaving(true);
    setSaveError(null);

    try {
      // Save answer to store
      setAnswer(answer.fieldKey, answer);

      // Persist answer to backend
      await saveAnswer({
        fieldKey: answer.fieldKey,
        fieldType: answer.fieldType,
        answerValue: answer.answerValue,
        isPii: answer.isPii,
      });

      // Get current answers including the new one
      const updatedAnswers = {
        ...answers,
        [answer.fieldKey]: answer,
      };

      // Find next visible question
      const nextIndex = getNextVisibleIndex(
        questions,
        currentStep,
        updatedAnswers,
      );

      if (nextIndex === -1) {
        // No more questions - flow is complete
        return false;
      }

      // Navigate to next question
      setCurrentStep(nextIndex);
      return true;
    } catch (error) {
      console.error("Failed to save answer:", error);
      setSaveError("Failed to save your answer. Please try again.");
      return false;
    } finally {
      setIsSaving(false);
    }
  };

  /**
   * Navigate to the previous visible question.
   * @returns true if navigation succeeded, false if at the start
   */
  const goBack = (): boolean => {
    const prevIndex = getPreviousVisibleIndex(questions, currentStep, answers);

    if (prevIndex === -1) {
      // At the start
      return false;
    }

    setCurrentStep(prevIndex);
    return true;
  };

  /**
   * Check if we can navigate back.
   */
  const canGoBack = (): boolean => {
    return getPreviousVisibleIndex(questions, currentStep, answers) !== -1;
  };

  /**
   * Check if we can navigate next (answer required check).
   */
  const canGoNext = (): boolean => {
    const currentQuestion = questions[currentStep];
    if (!currentQuestion) return false;

    // Check if current question is required and has an answer
    if (currentQuestion.required) {
      const answer = answers[currentQuestion.key];
      return answer !== undefined && answer.answerValue !== "";
    }

    return true;
  };

  /**
   * Check if the current question is the last visible question.
   */
  const isLastQuestion = (): boolean => {
    return getNextVisibleIndex(questions, currentStep, answers) === -1;
  };

  /**
   * Get the current question.
   */
  const getCurrentQuestion = () => {
    return questions[currentStep];
  };

  /**
   * Get progress information based on visible questions.
   */
  const getProgress = () => {
    return calculateProgress(questions, currentStep, answers);
  };

  /**
   * Check if a question should be shown (for conditional rendering).
   */
  const isQuestionVisible = (questionIndex: number): boolean => {
    const question = questions[questionIndex];
    if (!question) return false;
    return shouldShowQuestion(question, answers);
  };

  // Memoize computed values to prevent infinite re-renders
  const currentQuestion = useMemo(
    () => getCurrentQuestion(),
    [questions, currentStep],
  );
  const canGoBackValue = useMemo(
    () => canGoBack(),
    [questions, currentStep, answers],
  );
  const canGoNextValue = useMemo(
    () => canGoNext(),
    [questions, currentStep, answers],
  );
  const isLastQuestionValue = useMemo(
    () => isLastQuestion(),
    [questions, currentStep, answers],
  );
  const progress = useMemo(
    () => getProgress(),
    [questions, currentStep, answers],
  );

  return {
    // Navigation actions
    goNext,
    goBack,

    // Navigation state
    canGoBack: canGoBackValue,
    canGoNext: canGoNextValue,
    isLastQuestion: isLastQuestionValue,

    // Question access
    currentQuestion,

    // Progress
    progress,

    // Utility
    isQuestionVisible,

    // Async state
    isSaving,
    saveError,
  };
}
