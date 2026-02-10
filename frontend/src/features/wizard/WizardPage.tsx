import { useEffect, useMemo, useRef } from "react";
import { useNavigate } from "react-router-dom";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { useWizardStore } from "./store";
import { WizardProgress } from "./WizardProgress";
import { WizardStep } from "./WizardStep";
import { useWizardNavigator } from "./useWizardNavigator";
import { saveWizardState, clearWizardState } from "./useResumeWizard";
import { Answer } from "./types";
import { ConditionalQuestionContainer } from "./ConditionalQuestionContainer";
import { computeVisibility, AnswerMap } from "./conditionEvaluator";
import { debounce } from "./perf";

/**
 * Main wizard page that orchestrates the question flow.
 * Displays current question, handles navigation, and persists answers.
 */
export function WizardPage() {
  const navigate = useNavigate();
  const session = useWizardStore((state) => state.session);
  const questions = useWizardStore((state) => state.questions);
  const currentStep = useWizardStore((state) => state.currentStep);
  const selectedState = useWizardStore((state) => state.selectedState);
  const answers = useWizardStore((state) => state.answers);
  const storedAnswerMap = useWizardStore((state) => state.answerMap);
  const setAnswer = useWizardStore((state) => state.setAnswer);
  const updateAnswerMap = useWizardStore((state) => state.updateAnswerMap);

  const navigator = useWizardNavigator();

  const debouncedUpdateAnswerMap = useMemo(
    () =>
      debounce((fieldKey: string, value: string | string[] | null) => {
        updateAnswerMap(fieldKey, value);
      }, 150),
    [updateAnswerMap],
  );

  const visibilityState = useMemo(
    () => computeVisibility(questions, storedAnswerMap),
    [questions, storedAnswerMap],
  );

  const questionKeySignature = useMemo(
    () => questions.map((question) => question.key).join("|"),
    [questions],
  );
  const seededSignatureRef = useRef<string>("");
  const userHasInteractedRef = useRef(false);

  useEffect(() => {
    if (questions.length === 0 || userHasInteractedRef.current) return;
    if (seededSignatureRef.current === questionKeySignature) return;
    const seededMap = buildAnswerMap(questions, answers);
    if (Object.keys(seededMap).length === 0) return;
    Object.entries(seededMap).forEach(([fieldKey, value]) => {
      updateAnswerMap(fieldKey, value);
    });
    seededSignatureRef.current = questionKeySignature;
  }, [questions, answers, updateAnswerMap, questionKeySignature]);

  // Redirect to state-context if no session or questions
  useEffect(() => {
    if (!session || questions.length === 0) {
      navigate("/state-context");
    }
  }, [session, questions, navigate]);

  // Save wizard state to localStorage whenever step changes
  useEffect(() => {
    if (session && selectedState) {
      saveWizardState(selectedState.code, selectedState.name, currentStep);
    }
  }, [session, selectedState, currentStep]);

  // Handle answer submission and move to next step
  const handleNext = async (answer: Answer) => {
    const canContinue = await navigator.goNext(answer);

    if (!canContinue) {
      // Reached the end of the wizard
      clearWizardState();
      alert("Wizard complete! Results page coming in next phase.");
      navigate("/");
    }
  };

  // Handle back navigation
  const handleBack = () => {
    navigator.goBack();
  };

  const handleAnswerChange = (
    nextAnswer: Answer,
    rawValue: string | string[],
  ) => {
    userHasInteractedRef.current = true;
    setAnswer(nextAnswer.fieldKey, nextAnswer);
    debouncedUpdateAnswerMap(nextAnswer.fieldKey, rawValue);
  };

  // Guard: Show loading if not ready
  if (!session || questions.length === 0) {
    return (
      <div className="flex min-h-[50vh] items-center justify-center">
        <p className="text-muted-foreground">Loading...</p>
      </div>
    );
  }

  const currentQuestion = navigator.currentQuestion;

  // Show error if navigation failed
  if (!currentQuestion) {
    return (
      <div className="flex min-h-[50vh] items-center justify-center">
        <div
          role="alert"
          className="max-w-md rounded-md border border-destructive bg-destructive/10 p-4"
        >
          <p className="text-sm text-destructive">
            Unable to load question. Please try refreshing the page.
          </p>
        </div>
      </div>
    );
  }

  return (
    <div className="mx-auto max-w-2xl space-y-6">
      {/* Header */}
      <div className="space-y-2">
        <h1 className="text-2xl font-bold">
          {selectedState?.name} Medicaid Eligibility
        </h1>
        <p className="text-sm text-muted-foreground">
          Answer each question to check your eligibility. Your progress is
          automatically saved.
        </p>
      </div>

      {/* Progress indicator */}
      <WizardProgress />

      {/* Question card */}
      <Card>
        <CardHeader>
          <CardTitle className="text-lg">Question {currentStep + 1}</CardTitle>
        </CardHeader>
        <CardContent className="space-y-6">
          {navigator.saveError && (
            <div
              role="alert"
              className="mb-4 rounded-md border border-destructive bg-destructive/10 p-3"
            >
              <p className="text-sm text-destructive">{navigator.saveError}</p>
            </div>
          )}
          {questions.map((question) => {
            const isVisible = visibilityState[question.key] ?? true;
            const isCurrent = question.key === currentQuestion?.key;
            const answer = answers[question.key] ?? null;

            return (
              <ConditionalQuestionContainer
                key={question.key}
                question={question}
                isVisible={isVisible}
                answer={answer}
                onAnswer={handleAnswerChange}
              >
                <WizardStep
                  question={question}
                  answer={answer}
                  onAnswer={handleAnswerChange}
                  onNext={handleNext}
                  onBack={handleBack}
                  canGoBack={isCurrent ? navigator.canGoBack : false}
                  canGoNext={isCurrent ? navigator.canGoNext : false}
                  isSaving={isCurrent ? navigator.isSaving : false}
                  showNavigation={isCurrent}
                />
              </ConditionalQuestionContainer>
            );
          })}
        </CardContent>
      </Card>

      {/* Help section */}
      <Card className="bg-muted/50">
        <CardContent className="pt-6">
          <p className="text-sm text-muted-foreground">
            <strong>Need help?</strong> Your answers are saved automatically.
            You can close this page and return later - your session will remain
            active for 30 minutes of inactivity.
          </p>
        </CardContent>
      </Card>
    </div>
  );
}

function buildAnswerMap(
  questions: Array<{ key: string; type: string }>,
  answers: Record<string, Answer>,
): AnswerMap {
  const map: AnswerMap = {};

  questions.forEach((question) => {
    const answer = answers[question.key];
    if (!answer) return;

    if (question.type === "multiselect") {
      map[question.key] = answer.answerValue
        ? answer.answerValue
            .split(",")
            .map((value) => value.trim())
            .filter(Boolean)
        : [];
      return;
    }

    map[question.key] = answer.answerValue ?? "";
  });

  return map;
}
