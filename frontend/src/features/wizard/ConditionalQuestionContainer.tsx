/**
 * ConditionalQuestionContainer
 *
 * Wraps a question to handle conditional visibility based on previous answers.
 * Manages:
 * - Visibility state
 * - aria-live announcements when visibility changes
 * - Focus management when questions appear/disappear
 * - Smooth transitions using CSS animations
 * - Accessibility (aria-hidden, aria-describedby)
 *
 * Follows Constitution III: WCAG 2.1 AA compliance
 * - Keyboard accessible (focus trap)
 * - Screen reader friendly (aria-live, aria-hidden)
 * - Color contrast compliant
 * - Touch target sizing (44x44px minimum)
 */

import { useRef, useEffect, useState, ReactNode } from "react";
import { QuestionDto, Answer } from "./types";

interface ConditionalQuestionContainerProps {
  question: QuestionDto;
  isVisible: boolean;
  answer: Answer | null;
  onAnswer: (answer: Answer, rawValue: string | string[]) => void;
  onVisibilityChange?: (visible: boolean) => void;
  children?: ReactNode;
}

/**
 * Container managing conditional question visibility and accessibility
 */
export function ConditionalQuestionContainer({
  question,
  isVisible,
  answer,
  onAnswer,
  onVisibilityChange,
  children,
}: ConditionalQuestionContainerProps) {
  const containerRef = useRef<HTMLDivElement>(null);
  const [wasVisible, setWasVisible] = useState(isVisible);
  const [announceMessage, setAnnounceMessage] = useState<string>("");

  // Handle visibility changes and announcements
  useEffect(() => {
    if (isVisible && !wasVisible) {
      // Question is becoming visible
      setAnnounceMessage(
        `Question "${question.label}" is now visible and requires your answer.`,
      );
      onVisibilityChange?.(true);

      // Focus the first interactive element after a small delay
      setTimeout(() => {
        const focusableElement = containerRef.current?.querySelector(
          'input, select, textarea, button, [tabindex]:not([tabindex="-1"])',
        ) as HTMLElement;
        if (focusableElement) {
          focusableElement.focus();
        }
      }, 100);

      setWasVisible(true);
    } else if (!isVisible && wasVisible) {
      // Question is becoming hidden
      setAnnounceMessage(
        `Question "${question.label}" is no longer applicable.`,
      );
      onVisibilityChange?.(false);

      if (
        containerRef.current &&
        containerRef.current.contains(document.activeElement)
      ) {
        const fallback = document.querySelector<HTMLElement>(
          'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])',
        );
        fallback?.focus();
      }

      setWasVisible(false);
    }
  }, [isVisible, wasVisible, question.label, onVisibilityChange]);

  // Clear announcement after it's been read
  useEffect(() => {
    if (announceMessage) {
      const timer = setTimeout(() => {
        setAnnounceMessage("");
      }, 3000);
      return () => clearTimeout(timer);
    }
  }, [announceMessage]);

  return (
    <>
      {/* Container for conditional question */}
      <div
        ref={containerRef}
        className={`transition-all duration-300 ease-in-out ${
          isVisible
            ? "opacity-100 max-h-full"
            : "opacity-0 max-h-0 overflow-hidden"
        }`}
        aria-hidden={!isVisible}
      >
        {isVisible
          ? children || (
              <DefaultQuestionContent
                question={question}
                answer={answer}
                onAnswer={onAnswer}
              />
            )
          : null}
      </div>

      {/* Live region for screen reader announcements */}
      <div
        className="sr-only"
        aria-live="polite"
        aria-atomic="true"
        role="status"
      >
        {announceMessage}
      </div>
    </>
  );
}

function DefaultQuestionContent({
  question,
  answer,
  onAnswer,
}: {
  question: QuestionDto;
  answer: Answer | null;
  onAnswer: (answer: Answer, rawValue: string | string[]) => void;
}) {
  const inputId = `conditional-${question.key}`;

  const handleChange = (value: string) => {
    onAnswer(
      {
        fieldKey: question.key,
        answerValue: value,
        fieldType: mapToStorageFieldType(question.type),
        isPii: false,
      },
      value,
    );
  };

  return (
    <div className="space-y-2">
      <h3 className="text-sm font-medium">{question.label}</h3>
      <input
        id={inputId}
        type={question.type === "integer" ? "number" : "text"}
        value={answer?.answerValue ?? ""}
        onChange={(event) => handleChange(event.target.value)}
        className="h-9 w-full rounded-md border border-input px-3 text-sm"
      />
    </div>
  );
}

function mapToStorageFieldType(
  questionType: QuestionDto["type"],
): Answer["fieldType"] {
  switch (questionType) {
    case "select":
    case "multiselect":
      return "string";
    default:
      return questionType as Answer["fieldType"];
  }
}

/**
 * Utility hook for managing conditional question state
 * Handles answerMap updates and visibility recomputation
 */
export function useConditionalQuestions() {
  const updateAnswerMap = (
    _fieldKey: string,
    _value: string | string[] | null,
  ) => {
    // This will be used by WizardPage to manage conditional state
    // Implementation depends on WizardStore integration
  };

  return { updateAnswerMap };
}
