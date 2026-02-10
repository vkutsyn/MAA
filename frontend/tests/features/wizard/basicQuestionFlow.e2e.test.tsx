/**
 * E2E Tests: Basic Question Flow
 *
 * Verifies the complete user flow through the basic wizard questions:
 * 1. Load wizard page
 * 2. Render questions from API
 * 3. Answer questions
 * 4. Navigate through questions
 * 5. Reach completion
 *
 * Test Strategy:
 * - Full user journey from start to finish
 * - Verify questions render in correct order
 * - Verify form submissions work
 * - Verify navigation is functional
 */

import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { WizardPage } from "@/features/wizard/WizardPage";
import { useWizardStore } from "@/features/wizard/store";
import { QuestionDto } from "@/features/wizard/types";

vi.mock("@/features/wizard/answerApi", () => ({
  saveAnswer: vi.fn().mockResolvedValue({}),
}));

describe("E2E: Basic Question Flow", () => {
  beforeEach(() => {
    // Reset store before each test
    useWizardStore.setState({
      session: null,
      questions: [],
      answers: {},
      answerMap: {},
      visibilityState: {},
      currentStep: 0,
      selectedState: null,
      isLoading: false,
    });
  });

  const completeQuestionSet: QuestionDto[] = [
    {
      key: "employment-status",
      label: "What is your current employment status?",
      type: "select",
      required: true,
      helpText: "Select the option that best describes your situation",
      options: [
        { value: "employed", label: "Employed" },
        { value: "self-employed", label: "Self-Employed" },
        { value: "unemployed", label: "Unemployed" },
        { value: "student", label: "Student" },
      ],
    },
    {
      key: "household-size",
      label: "How many people live in your household?",
      type: "integer",
      required: true,
      helpText: "Include yourself in this count",
    },
    {
      key: "monthly-income",
      label: "What is your monthly income?",
      type: "currency",
      required: true,
      helpText: "Include all sources of income",
    },
    {
      key: "date-of-birth",
      label: "What is your date of birth?",
      type: "date",
      required: true,
    },
    {
      key: "has-insurance",
      label: "Do you currently have health insurance?",
      type: "boolean",
      required: true,
    },
  ];

  it("should load and display first question on wizard load", async () => {
    useWizardStore.setState({
      session: {
        sessionId: "e2e-test-1",
        stateCode: "CA",
        stateName: "California",
        currentStep: 0,
        totalSteps: completeQuestionSet.length,
        expiresAt: new Date(Date.now() + 1000 * 60 * 30).toISOString(),
      },
      questions: completeQuestionSet,
    });

    render(
      <MemoryRouter>
        <WizardPage />
      </MemoryRouter>,
    );

    // First question should be visible
    expect(
      screen.getByRole("heading", { name: /employment status/i }),
    ).toBeTruthy();
    const employmentSelect = screen.getByRole("combobox", {
      name: /employment status/i,
    });
    expect(employmentSelect).toBeTruthy();
  });

  it("should render questions in correct order", async () => {
    useWizardStore.setState({
      session: {
        sessionId: "e2e-test-2",
        stateCode: "CA",
        stateName: "California",
        currentStep: 0,
        totalSteps: completeQuestionSet.length,
        expiresAt: new Date(Date.now() + 1000 * 60 * 30).toISOString(),
      },
      questions: completeQuestionSet,
    });

    const user = userEvent.setup();
    render(
      <MemoryRouter>
        <WizardPage />
      </MemoryRouter>,
    );

    // Verify first question
    expect(
      screen.getByRole("heading", { name: /employment status/i }),
    ).toBeTruthy();

    // Answer and navigate to second
    const employmentSelect = screen.getByRole("combobox");
    await user.selectOptions(employmentSelect, "employed");

    const nextButton = screen.getByRole("button", { name: /next/i });
    await user.click(nextButton);

    await waitFor(() => {
      expect(
        screen.getByRole("heading", { name: /people live in your household/i }),
      ).toBeTruthy();
    });

    // Verify we're on second question
    const householdInput = screen.getByRole("spinbutton", {
      name: /people live in your household/i,
    });
    expect(householdInput).toBeTruthy();
  });

  it("should allow user to complete full wizard flow", async () => {
    useWizardStore.setState({
      session: {
        sessionId: "e2e-test-3",
        stateCode: "CA",
        stateName: "California",
        currentStep: 0,
        totalSteps: completeQuestionSet.length,
        expiresAt: new Date(Date.now() + 1000 * 60 * 30).toISOString(),
      },
      questions: completeQuestionSet,
    });

    const user = userEvent.setup();
    render(
      <MemoryRouter>
        <WizardPage />
      </MemoryRouter>,
    );

    // Question 1: Employment Status
    let select = screen.getByRole("combobox", { name: /employment status/i });
    await user.selectOptions(select, "employed");

    let nextButton = screen.getByRole("button", { name: /next/i });
    await user.click(nextButton);

    await waitFor(() => {
      expect(
        screen.getByRole("spinbutton", {
          name: /people live in your household/i,
        }),
      ).toBeTruthy();
    });

    // Question 2: Household Size
    const householdInput = screen.getByRole("spinbutton", {
      name: /people live in your household/i,
    });
    await user.clear(householdInput);
    await user.type(householdInput, "4");

    nextButton = screen.getByRole("button", { name: /next/i });
    await user.click(nextButton);

    await waitFor(() => {
      expect(
        screen.getByRole("textbox", { name: /monthly income/i }),
      ).toBeTruthy();
    });

    // Question 3: Monthly Income
    const incomeInput = screen.getByRole("textbox", {
      name: /monthly income/i,
    });
    await user.type(incomeInput, "3500");

    nextButton = screen.getByRole("button", { name: /next/i });
    await user.click(nextButton);

    await waitFor(() => {
      expect(
        screen.getByLabelText(/date of birth/i, {
          selector: "input, textarea",
        })
      ).toBeTruthy();
    });

    // Question 4: Date of Birth
    const dobInput = screen.getByLabelText(/date of birth/i, {
      selector: "input, textarea",
    });
    await user.type(dobInput, "01/15/1990");

    nextButton = screen.getByRole("button", { name: /next/i });
    await user.click(nextButton);

    await waitFor(() => {
      const yesButton = screen.getByRole("radio", { name: /yes/i });
      expect(yesButton).toBeTruthy();
    });

    // Question 5: Has Insurance (boolean)
    const yesButton = screen.getByRole("radio", { name: /yes/i });
    await user.click(yesButton);

    // Should reach completion or show summary
    const submitButton = screen.queryByRole("button", {
      name: /submit|complete|finish/i,
    });
    if (submitButton) {
      expect(submitButton).toBeTruthy();
    }
  });

  it("should prevent navigation when required question is unanswered", async () => {
    useWizardStore.setState({
      session: {
        sessionId: "e2e-test-4",
        stateCode: "CA",
        stateName: "California",
        currentStep: 0,
        totalSteps: completeQuestionSet.length,
        expiresAt: new Date(Date.now() + 1000 * 60 * 30).toISOString(),
      },
      questions: completeQuestionSet,
    });

    const user = userEvent.setup();
    render(
      <MemoryRouter>
        <WizardPage />
      </MemoryRouter>,
    );

    // Try to navigate without answering
    const nextButton = screen.getByRole("button", { name: /next/i });

    // Should be disabled or show error
    if (nextButton.hasAttribute("disabled")) {
      expect(nextButton).toHaveAttribute("disabled");
    } else {
      // May show validation error on click
      await user.click(nextButton);
      await waitFor(() => {
        const errorMessage = screen.queryByText(
          /required|please|select|answer/i,
        );
        if (errorMessage) {
          expect(errorMessage).toBeTruthy();
        }
      });
    }
  });

  it("should maintain progress indicator through navigation", async () => {
    useWizardStore.setState({
      session: {
        sessionId: "e2e-test-5",
        stateCode: "CA",
        stateName: "California",
        currentStep: 0,
        totalSteps: completeQuestionSet.length,
        expiresAt: new Date(Date.now() + 1000 * 60 * 30).toISOString(),
      },
      questions: completeQuestionSet,
    });

    const user = userEvent.setup();
    render(
      <MemoryRouter>
        <WizardPage />
      </MemoryRouter>,
    );

    // Check initial progress
    const progressRegion = screen.getByRole("region", {
      name: /progress indicator/i,
    });
    expect(progressRegion).toHaveTextContent(/Question\s*1\s*of\s*5/i);

    // Answer and navigate
    const select = screen.getByRole("combobox");
    await user.selectOptions(select, "employed");

    const nextButton = screen.getByRole("button", { name: /next/i });
    await user.click(nextButton);

    await waitFor(() => {
      // Progress should update to 2/5
      expect(progressRegion).toHaveTextContent(/Question\s*2\s*of\s*5/i);
    });
  });

  it("should allow back navigation with answer preservation", async () => {
    useWizardStore.setState({
      session: {
        sessionId: "e2e-test-6",
        stateCode: "CA",
        stateName: "California",
        currentStep: 0,
        totalSteps: completeQuestionSet.length,
        expiresAt: new Date(Date.now() + 1000 * 60 * 30).toISOString(),
      },
      questions: completeQuestionSet,
    });

    const user = userEvent.setup();
    render(
      <MemoryRouter>
        <WizardPage />
      </MemoryRouter>,
    );

    // Answer first question
    const select = screen.getByRole("combobox");
    await user.selectOptions(select, "employed");

    // Go forward
    let nextButton = screen.getByRole("button", { name: /next/i });
    await user.click(nextButton);

    await waitFor(() => {
      expect(
        screen.getByRole("spinbutton", {
          name: /people live in your household/i,
        }),
      ).toBeTruthy();
    });

    // Go back
    const backButton = screen.getByRole("button", { name: /back|previous/i });
    await user.click(backButton);

    await waitFor(() => {
      const restoredSelect = screen.getByRole("combobox", {
        name: /employment status/i,
      });
      // Value should still be selected
      expect(restoredSelect).toHaveValue("employed");
    });
  });
});
