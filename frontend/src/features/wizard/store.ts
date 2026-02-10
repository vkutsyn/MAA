import { create } from "zustand";
import { devtools } from "zustand/middleware";
import { WizardSession, QuestionDto, Answer, WizardProgress } from "./types";

/**
 * Wizard state store using Zustand.
 * Manages wizard session, questions, answers, and navigation state.
 */
interface WizardState {
  // Session
  session: WizardSession | null;
  setSession: (session: WizardSession) => void;
  clearSession: () => void;

  // Questions
  questions: QuestionDto[];
  setQuestions: (questions: QuestionDto[]) => void;

  // Answers
  answers: Record<string, Answer>; // keyed by fieldKey
  setAnswer: (fieldKey: string, answer: Answer) => void;
  setAnswers: (answers: Answer[]) => void;
  getAnswer: (fieldKey: string) => Answer | undefined;

  // Progress
  currentStep: number;
  setCurrentStep: (step: number) => void;
  goToNextStep: () => void;
  goToPreviousStep: () => void;
  canGoBack: () => boolean;
  canGoNext: () => boolean;
  getProgress: () => WizardProgress;

  // State selection
  selectedState: { code: string; name: string } | null;
  setSelectedState: (code: string, name: string) => void;

  // Loading states
  isLoading: boolean;
  setIsLoading: (loading: boolean) => void;

  // Reset (for new session)
  reset: () => void;
}

const initialState = {
  session: null,
  questions: [],
  answers: {},
  currentStep: 0,
  selectedState: null,
  isLoading: false,
};

export const useWizardStore = create<WizardState>()(
  devtools(
    (set, get) => ({
      ...initialState,

      // Session management
      setSession: (session) => set({ session }),
      clearSession: () => set({ session: null }),

      // Question management
      setQuestions: (questions) => set({ questions, currentStep: 0 }),

      // Answer management
      setAnswer: (fieldKey, answer) =>
        set((state) => ({
          answers: {
            ...state.answers,
            [fieldKey]: answer,
          },
        })),

      setAnswers: (answers) =>
        set({
          answers: answers.reduce(
            (acc, answer) => {
              acc[answer.fieldKey] = answer;
              return acc;
            },
            {} as Record<string, Answer>,
          ),
        }),

      getAnswer: (fieldKey) => get().answers[fieldKey],

      // Navigation
      setCurrentStep: (step) => {
        const { questions } = get();
        const clampedStep = Math.max(0, Math.min(step, questions.length - 1));
        set({ currentStep: clampedStep });
      },

      goToNextStep: () => {
        const { currentStep, questions } = get();
        if (currentStep < questions.length - 1) {
          set({ currentStep: currentStep + 1 });
        }
      },

      goToPreviousStep: () => {
        const { currentStep } = get();
        if (currentStep > 0) {
          set({ currentStep: currentStep - 1 });
        }
      },

      canGoBack: () => get().currentStep > 0,

      canGoNext: () => {
        const { currentStep, questions, answers } = get();
        if (currentStep >= questions.length - 1) {
          return false;
        }
        const currentQuestion = questions[currentStep];
        if (currentQuestion?.required) {
          return currentQuestion.key in answers;
        }
        return true;
      },

      getProgress: (): WizardProgress => {
        const { currentStep, questions } = get();
        const totalSteps = questions.length;
        const completionPercent =
          totalSteps > 0 ? Math.round((currentStep / totalSteps) * 100) : 0;

        return {
          currentIndex: currentStep,
          totalSteps,
          canGoBack: get().canGoBack(),
          canGoNext: get().canGoNext(),
          completionPercent,
        };
      },

      // State selection
      setSelectedState: (code, name) => set({ selectedState: { code, name } }),

      // Loading state
      setIsLoading: (loading) => set({ isLoading: loading }),

      // Reset all state
      reset: () => set(initialState),
    }),
    { name: "WizardStore" },
  ),
);
