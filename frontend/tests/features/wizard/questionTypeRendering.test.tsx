/**
 * Unit Tests: Question Type Rendering
 *
 * Verifies that WizardStep component correctly renders all supported question types
 * with appropriate input controls.
 *
 * Test Strategy:
 * - Each question type gets a dedicated test
 * - Verify correct input element is rendered
 * - Verify labels and attributes are present
 * - Verify options are rendered for select/multiselect types
 */

import { describe, it, expect, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import { WizardStep } from "@/features/wizard/WizardStep";
import { QuestionDto } from "@/features/wizard/types";

describe("WizardStep - Question Type Rendering", () => {
  const mockOnAnswer = vi.fn();
  const mockOnNext = vi.fn();
  const mockOnBack = vi.fn();

  const baseQuestion: Omit<QuestionDto, "type"> = {
    key: "test-question",
    label: "Test Question",
    required: true,
    helpText: "This is helpful",
  };

  describe("Text Input", () => {
    it("should render text input for 'text' type", () => {
      const question: QuestionDto = {
        ...baseQuestion,
        type: "text",
      };

      render(
        <WizardStep
          question={question}
          answer={null}
          onAnswer={mockOnAnswer}
          onNext={mockOnNext}
          onBack={mockOnBack}
          canGoBack={true}
          canGoNext={true}
        />,
      );

      const input = screen.getByLabelText("Test Question", {
        selector: "input, textarea",
      });
      expect(input).toBeTruthy();
      expect(input.tagName).toBe("TEXTAREA");
    });
  });

  describe("String Input", () => {
    it("should render text input for 'string' type", () => {
      const question: QuestionDto = {
        ...baseQuestion,
        type: "string",
      };

      render(
        <WizardStep
          question={question}
          answer={null}
          onAnswer={mockOnAnswer}
          onNext={mockOnNext}
          onBack={mockOnBack}
          canGoBack={true}
          canGoNext={true}
        />,
      );

      const input = screen.getByLabelText("Test Question", {
        selector: "input, textarea",
      });
      expect(input).toBeTruthy();
    });
  });

  describe("Number Input", () => {
    it("should render number input for 'integer' type", () => {
      const question: QuestionDto = {
        ...baseQuestion,
        type: "integer",
      };

      render(
        <WizardStep
          question={question}
          answer={null}
          onAnswer={mockOnAnswer}
          onNext={mockOnNext}
          onBack={mockOnBack}
          canGoBack={true}
          canGoNext={true}
        />,
      );

      const input = screen.getByRole("spinbutton", { name: /test question/i });
      expect(input).toBeTruthy();
    });
  });

  describe("Currency Input", () => {
    it("should render currency input for 'currency' type", () => {
      const question: QuestionDto = {
        ...baseQuestion,
        type: "currency",
      };

      render(
        <WizardStep
          question={question}
          answer={null}
          onAnswer={mockOnAnswer}
          onNext={mockOnNext}
          onBack={mockOnBack}
          canGoBack={true}
          canGoNext={true}
        />,
      );

      const input = screen.getByLabelText("Test Question", {
        selector: "input, textarea",
      });
      expect(input).toBeTruthy();
      // Currency inputs should have special formatting
      expect(input).toHaveClass(/currency|money|amount/i);
    });
  });

  describe("Date Input", () => {
    it("should render date input for 'date' type", () => {
      const question: QuestionDto = {
        ...baseQuestion,
        type: "date",
      };

      render(
        <WizardStep
          question={question}
          answer={null}
          onAnswer={mockOnAnswer}
          onNext={mockOnNext}
          onBack={mockOnBack}
          canGoBack={true}
          canGoNext={true}
        />,
      );

      const input = screen.getByLabelText("Test Question", {
        selector: "input, textarea",
      });
      expect(input).toBeTruthy();
      expect(input).toHaveAttribute("type", "date");
    });
  });

  describe("Boolean (Yes/No)", () => {
    it("should render radio buttons or toggle for 'boolean' type", () => {
      const question: QuestionDto = {
        ...baseQuestion,
        type: "boolean",
      };

      render(
        <WizardStep
          question={question}
          answer={null}
          onAnswer={mockOnAnswer}
          onNext={mockOnNext}
          onBack={mockOnBack}
          canGoBack={true}
          canGoNext={true}
        />,
      );

      // Should render yes/no options
      const yesButton = screen.getByRole("radio", { name: /yes/i });
      const noButton = screen.getByRole("radio", { name: /no/i });

      expect(yesButton).toBeTruthy();
      expect(noButton).toBeTruthy();
    });
  });

  describe("Select (Dropdown)", () => {
    it("should render dropdown for 'select' type with options", () => {
      const question: QuestionDto = {
        ...baseQuestion,
        type: "select",
        options: [
          { value: "opt1", label: "Option 1" },
          { value: "opt2", label: "Option 2" },
          { value: "opt3", label: "Option 3" },
        ],
      };

      render(
        <WizardStep
          question={question}
          answer={null}
          onAnswer={mockOnAnswer}
          onNext={mockOnNext}
          onBack={mockOnBack}
          canGoBack={true}
          canGoNext={true}
        />,
      );

      const select = screen.getByRole("combobox", { name: /test question/i });
      expect(select).toBeTruthy();

      // Options should be rendered
      expect(screen.getByRole("option", { name: /option 1/i })).toBeTruthy();
      expect(screen.getByRole("option", { name: /option 2/i })).toBeTruthy();
      expect(screen.getByRole("option", { name: /option 3/i })).toBeTruthy();
    });
  });

  describe("Multiselect (Checkboxes)", () => {
    it("should render checkboxes for 'multiselect' type with options", () => {
      const question: QuestionDto = {
        ...baseQuestion,
        type: "multiselect",
        options: [
          { value: "opt1", label: "Option 1" },
          { value: "opt2", label: "Option 2" },
          { value: "opt3", label: "Option 3" },
        ],
      };

      render(
        <WizardStep
          question={question}
          answer={null}
          onAnswer={mockOnAnswer}
          onNext={mockOnNext}
          onBack={mockOnBack}
          canGoBack={true}
          canGoNext={true}
        />,
      );

      const checkboxes = screen.getAllByRole("checkbox");
      expect(checkboxes.length).toBe(3);

      // All options should be rendered
      expect(screen.getByRole("checkbox", { name: /option 1/i })).toBeTruthy();
      expect(screen.getByRole("checkbox", { name: /option 2/i })).toBeTruthy();
      expect(screen.getByRole("checkbox", { name: /option 3/i })).toBeTruthy();
    });
  });

  describe("Question Label and Help Text", () => {
    it("should render question label", () => {
      const question: QuestionDto = {
        ...baseQuestion,
        type: "text",
        label: "What is your name?",
      };

      render(
        <WizardStep
          question={question}
          answer={null}
          onAnswer={mockOnAnswer}
          onNext={mockOnNext}
          onBack={mockOnBack}
          canGoBack={true}
          canGoNext={true}
        />,
      );

      expect(
        screen.getByRole("heading", { name: /what is your name/i }),
      ).toBeTruthy();
    });

    it("should indicate required questions", () => {
      const question: QuestionDto = {
        ...baseQuestion,
        type: "text",
        required: true,
      };

      render(
        <WizardStep
          question={question}
          answer={null}
          onAnswer={mockOnAnswer}
          onNext={mockOnNext}
          onBack={mockOnBack}
          canGoBack={true}
          canGoNext={true}
        />,
      );

      const requiredIndicator = screen.queryByText(/\*/);
      expect(requiredIndicator).toBeTruthy();
    });
  });

  describe("Question Rendering with Answer", () => {
    it("should display pre-filled answer value", () => {
      const question: QuestionDto = {
        ...baseQuestion,
        type: "text",
      };

      const existingAnswer = {
        fieldKey: "test-question",
        answerValue: "John Doe",
        fieldType: "string" as const,
        isPii: true,
      };

      render(
        <WizardStep
          question={question}
          answer={existingAnswer}
          onAnswer={mockOnAnswer}
          onNext={mockOnNext}
          onBack={mockOnBack}
          canGoBack={true}
          canGoNext={true}
        />,
      );

      const input = screen.getByLabelText("Test Question", {
        selector: "input, textarea",
      }) as HTMLInputElement;

      expect(input.value).toBe("John Doe");
    });
  });
});
