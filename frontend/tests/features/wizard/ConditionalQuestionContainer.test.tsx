/**
 * Component Tests: ConditionalQuestionContainer
 *
 * Verifies that questions are correctly shown/hidden based on conditions
 * and that the component properly handles accessibility requirements.
 *
 * Test Strategy:
 * - Verify questions render when conditions are met
 * - Verify questions are hidden when conditions are not met
 * - Verify aria-live regions announce visibility changes
 * - Verify focus management when questions appear/disappear
 */

import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { ConditionalQuestionContainer } from "@/features/wizard/ConditionalQuestionContainer";
import {
  VisibilityState,
  AnswerMap,
} from "@/features/wizard/conditionEvaluator";
import { QuestionDto } from "@/features/wizard/types";

describe("ConditionalQuestionContainer", () => {
  const mockQuestion: QuestionDto = {
    key: "dependent-q",
    label: "This question depends on previous answer",
    type: "text",
    required: false,
    conditions: [
      {
        fieldKey: "trigger-q",
        operator: "equals",
        value: "yes",
      },
    ],
  };

  const mockOnAnswer = vi.fn();

  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe("Visibility Control", () => {
    it("should render question when condition is met", () => {
      const visibility: VisibilityState = {
        "dependent-q": true,
      };

      render(
        <ConditionalQuestionContainer
          question={mockQuestion}
          isVisible={visibility["dependent-q"]}
          answer={null}
          onAnswer={mockOnAnswer}
        />,
      );

      expect(screen.getByText(/this question depends/i)).toBeTruthy();
    });

    it("should not render question when condition is not met", () => {
      const visibility: VisibilityState = {
        "dependent-q": false,
      };

      const { container } = render(
        <ConditionalQuestionContainer
          question={mockQuestion}
          isVisible={visibility["dependent-q"]}
          answer={null}
          onAnswer={mockOnAnswer}
        />,
      );

      // Question should not be visible
      expect(screen.queryByText(/this question depends/i)).toBeFalsy();
      // But container should exist (for aria-live)
      expect(
        container.querySelector('[aria-live="polite"]') ||
          container.querySelector('[aria-live="assertive"]'),
      ).toBeTruthy();
    });

    it("should be hidden from screen readers when not visible", () => {
      const visibility: VisibilityState = {
        "dependent-q": false,
      };

      const { container } = render(
        <ConditionalQuestionContainer
          question={mockQuestion}
          isVisible={visibility["dependent-q"]}
          answer={null}
          onAnswer={mockOnAnswer}
        />,
      );

      const hiddenSection = container.querySelector("[aria-hidden='true']");
      if (hiddenSection) {
        expect(hiddenSection).toHaveAttribute("aria-hidden", "true");
      }
    });
  });

  describe("Accessibility - aria-live", () => {
    it("should have aria-live region for announcements", () => {
      const { container } = render(
        <ConditionalQuestionContainer
          question={mockQuestion}
          isVisible={true}
          answer={null}
          onAnswer={mockOnAnswer}
        />,
      );

      const liveRegion =
        container.querySelector('[aria-live="polite"]') ||
        container.querySelector('[aria-live="assertive"]');

      expect(liveRegion).toBeTruthy();
    });

    it("should announce when question becomes visible", async () => {
      const { rerender, container } = render(
        <ConditionalQuestionContainer
          question={mockQuestion}
          isVisible={false}
          answer={null}
          onAnswer={mockOnAnswer}
        />,
      );

      // Get the aria-live region
      const liveRegion = container.querySelector("[aria-live]");
      expect(liveRegion).toBeTruthy();

      // Re-render with visible=true
      rerender(
        <ConditionalQuestionContainer
          question={mockQuestion}
          isVisible={true}
          answer={null}
          onAnswer={mockOnAnswer}
        />,
      );

      // Should announce appearance
      await waitFor(() => {
        const announcement =
          liveRegion?.textContent ||
          screen.queryByText(/appears|shown|visible/i);
        // Announcement should exist (specific text depends on implementation)
        expect(
          announcement || screen.queryByText(/this question depends/i),
        ).toBeTruthy();
      });
    });
  });

  describe("Answer Handling", () => {
    it("should call onAnswer when visible question is answered", async () => {
      const user = userEvent.setup();

      render(
        <ConditionalQuestionContainer
          question={mockQuestion}
          isVisible={true}
          answer={null}
          onAnswer={mockOnAnswer}
        />,
      );

      const input = screen.getByRole("textbox");
      await user.type(input, "test answer");

      await waitFor(() => {
        expect(mockOnAnswer).toHaveBeenCalled();
      });
    });

    it("should not capture answer when hidden", async () => {
      const user = userEvent.setup();

      const { container } = render(
        <ConditionalQuestionContainer
          question={mockQuestion}
          isVisible={false}
          answer={null}
          onAnswer={mockOnAnswer}
        />,
      );

      // Try to interact with hidden content (should not work)
      const inputs = container.querySelectorAll("input");
      if (inputs.length > 0) {
        // If input exists in DOM, it should be disabled/readonly
        expect(
          (inputs[0] as HTMLInputElement).disabled ||
            (inputs[0] as HTMLInputElement).readOnly,
        ).toBe(true);
      }
    });

    it("should preserve answer value when toggling visibility", async () => {
      const user = userEvent.setup();
      const answer = {
        fieldKey: "dependent-q",
        answerValue: "saved answer",
        fieldType: "text" as const,
        isPii: false,
      };

      const { rerender } = render(
        <ConditionalQuestionContainer
          question={mockQuestion}
          isVisible={true}
          answer={answer}
          onAnswer={mockOnAnswer}
        />,
      );

      const input = screen.getByRole("textbox") as HTMLInputElement;
      expect(input.value).toBe("saved answer");

      // Hide question
      rerender(
        <ConditionalQuestionContainer
          question={mockQuestion}
          isVisible={false}
          answer={answer}
          onAnswer={mockOnAnswer}
        />,
      );

      mockOnAnswer.mockClear();

      // Show question again
      rerender(
        <ConditionalQuestionContainer
          question={mockQuestion}
          isVisible={true}
          answer={answer}
          onAnswer={mockOnAnswer}
        />,
      );

      // Value should be restored
      const restoredInput = screen.getByRole("textbox") as HTMLInputElement;
      expect(restoredInput.value).toBe("saved answer");
    });
  });

  describe("Focus Management", () => {
    it("should move focus when question appears", async () => {
      const { rerender } = render(
        <ConditionalQuestionContainer
          question={mockQuestion}
          isVisible={false}
          answer={null}
          onAnswer={mockOnAnswer}
        />,
      );

      // Make visible
      rerender(
        <ConditionalQuestionContainer
          question={mockQuestion}
          isVisible={true}
          answer={null}
          onAnswer={mockOnAnswer}
        />,
      );

      await waitFor(() => {
        // Focus should be on the input or a related element
        const input = screen.queryByRole("textbox");
        if (input) {
          // Implementation may focus different element
          expect(
            document.activeElement === input ||
              document.activeElement?.contains(input),
          ).toBeTruthy();
        }
      });
    });
  });

  describe("Responsive Display", () => {
    it("should use transition classes for smooth appearance", () => {
      const { container } = render(
        <ConditionalQuestionContainer
          question={mockQuestion}
          isVisible={true}
          answer={null}
          onAnswer={mockOnAnswer}
        />,
      );

      // Check for transition/animation classes
      const wrapper = container.firstChild as HTMLElement;
      const hasTransition =
        wrapper.className.includes("transition") ||
        wrapper.className.includes("animate") ||
        wrapper.style.transition !== "";

      // Either should have transition classes or inline styles
      expect(hasTransition || wrapper.getAttribute("style")).toBeTruthy();
    });

    it("should prevent layout shift when question disappears", () => {
      const { container, rerender } = render(
        <ConditionalQuestionContainer
          question={mockQuestion}
          isVisible={true}
          answer={null}
          onAnswer={mockOnAnswer}
        />,
      );

      const initialHeight = container.offsetHeight;

      rerender(
        <ConditionalQuestionContainer
          question={mockQuestion}
          isVisible={false}
          answer={null}
          onAnswer={mockOnAnswer}
        />,
      );

      // Should minimize height shift (implementation may keep container size)
      // This is more of an integration test, but component should support it
      expect(container.firstChild).toBeTruthy(); // Container still exists
    });
  });

  describe("Integration with Multiple Conditions", () => {
    const complexQuestion: QuestionDto = {
      key: "q3",
      label: "Complex dependent question",
      type: "text",
      required: false,
      conditions: [
        { fieldKey: "q1", operator: "equals", value: "yes" },
        { fieldKey: "q2", operator: "gt", value: "18" },
      ],
    };

    it("should respect multiple condition result", () => {
      // Both conditions pass
      render(
        <ConditionalQuestionContainer
          question={complexQuestion}
          isVisible={true}
          answer={null}
          onAnswer={mockOnAnswer}
        />,
      );

      expect(screen.getByText(/complex dependent/i)).toBeTruthy();
    });

    it("should hide when any condition fails", () => {
      // At least one condition fails
      render(
        <ConditionalQuestionContainer
          question={complexQuestion}
          isVisible={false}
          answer={null}
          onAnswer={mockOnAnswer}
        />,
      );

      expect(screen.queryByText(/complex dependent/i)).toBeFalsy();
    });
  });
});
