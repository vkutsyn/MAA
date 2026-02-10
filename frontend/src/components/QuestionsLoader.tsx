import { useMemo } from "react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { useQuestions } from "@/hooks/useQuestions";
import {
  isQuestionVisible,
  type AnswerValue,
} from "@/lib/evaluateConditionalRules";

interface QuestionsLoaderProps {
  stateCode: string;
  programCode: string;
  answers?: Record<string, AnswerValue>;
}

export function QuestionsLoader({
  stateCode,
  programCode,
  answers = {},
}: QuestionsLoaderProps) {
  const { data, isLoading, error } = useQuestions(stateCode, programCode);

  const visibleQuestions = useMemo(() => {
    if (!data) return [];
    return data.questions.filter((question) =>
      isQuestionVisible(question, answers, data.conditionalRules),
    );
  }, [data, answers]);

  if (isLoading) {
    return (
      <div className="flex min-h-[12rem] items-center justify-center">
        <p className="text-sm text-muted-foreground">Loading questions...</p>
      </div>
    );
  }

  if (error) {
    return (
      <div
        role="alert"
        className="rounded-md border border-destructive bg-destructive/10 p-4"
      >
        <p className="text-sm text-destructive">{error.message}</p>
      </div>
    );
  }

  if (!data || visibleQuestions.length === 0) {
    return (
      <div className="flex min-h-[12rem] items-center justify-center">
        <p className="text-sm text-muted-foreground">
          No questions available for this program.
        </p>
      </div>
    );
  }

  return (
    <div className="space-y-4">
      {visibleQuestions.map((question) => (
        <Card key={question.questionId}>
          <CardHeader>
            <CardTitle className="text-base">{question.questionText}</CardTitle>
          </CardHeader>
          <CardContent className="space-y-2">
            {question.helpText && (
              <p className="text-sm text-muted-foreground">
                {question.helpText}
              </p>
            )}
            <p className="text-xs text-muted-foreground">
              Field type: {question.fieldType} · Required:
              {question.isRequired ? " yes" : " no"}
            </p>
          </CardContent>
        </Card>
      ))}
    </div>
  );
}
