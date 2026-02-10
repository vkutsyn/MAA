/**
 * E2E Tests: Conditional Question Appearance
 *
 * Verifies that questions appear and disappear correctly based on user answers
 * in a full end-to-end flow.
 *
 * Test Strategy:
 * - Answer trigger question
 * - Verify dependent questions appear
 * - Answer trigger question differently
 * - Verify dependent questions disappear
 * - Verify smooth transitions
 */

import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { WizardPage } from "@/features/wizard/WizardPage";
import { useWizardStore } from "@/features/wizard/store";
import { QuestionDto } from "@/features/wizard/types";

describe("E2E: Conditional Question Appearance", () => {
  beforeEach(() => {
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

  const questionSet: QuestionDto[] = [
    {
      key: "has-income",
      label: "Do you have any income?",
      type: "boolean",
      required: true,
    },
    {
      key: "income-amount",
      label: "What is your monthly income?",
      type: "currency",
      required: false,
      conditions: [
        {
          fieldKey: "has-income",
          operator: "equals",
          value: "true",
        },
      ],
    },
    {
      key: "income-source",
      label: "What is your primary income source?",
      type: "select",
      required: false,
      options: [
        { value: "employment", label: "Employment" },
        { value: "self-employed", label: "Self-Employed" },
        { value: "benefits", label: "Government Benefits" },
        { value: "other", label: "Other" },
      ],
      conditions: [
        {
          fieldKey: "has-income",
          operator: "equals",
          value: "true",
        },
      ],
    },
  ];

  it("should show dependent questions when trigger condition is met", async () => {
    const user = userEvent.setup();

    useWizardStore.setState({
      session: {
        sessionId: "e2e-cond-1",
        stateCode: "CA",
        stateName: "California",
        currentStep: 0,
        totalSteps: questionSet.length,
        expiresAt: new Date(Date.now() + 1000 * 60 * 30).toISOString(),
      },
      questions: questionSet,
    });

    render(
      <MemoryRouter>
        <WizardPage />
      </MemoryRouter>,
    );

    // Dependent questions should not be visible initially
    expect(
      screen.queryByRole("heading", { name: /what is your monthly income/i }),
    ).toBeFalsy();
    expect(
      screen.queryByRole("heading", {
        name: /what is your primary income source/i,
      }),
    ).toBeFalsy();

    // Answer "yes" to trigger question
    const yesButton = screen.getByRole("radio", { name: /yes/i });
    await user.click(yesButton);

    // Dependent questions should now appear
    await waitFor(() => {
      expect(
        screen.getByRole("heading", { name: /what is your monthly income/i }),
      ).toBeTruthy();
      expect(
        screen.getByRole("heading", {
          name: /what is your primary income source/i,
        }),
      ).toBeTruthy();
    });
  });

  it("should hide dependent questions when trigger condition is not met", async () => {
    const user = userEvent.setup();

    useWizardStore.setState({
      session: {
        sessionId: "e2e-cond-2",
        stateCode: "CA",
        stateName: "California",
        currentStep: 0,
        totalSteps: questionSet.length,
        expiresAt: new Date(Date.now() + 1000 * 60 * 30).toISOString(),
      },
      questions: questionSet,
      answers: {
        "has-income": {
          fieldKey: "has-income",
          answerValue: "true",
          fieldType: "boolean",
          isPii: false,
        },
      },
    });

    render(
      <MemoryRouter>
        <WizardPage />
      </MemoryRouter>,
    );

    // Dependent questions should be visible initially
    await waitFor(() => {
      expect(
        screen.getByRole("heading", { name: /what is your monthly income/i }),
      ).toBeTruthy();
      expect(
        screen.getByRole("heading", {
          name: /what is your primary income source/i,
        }),
      ).toBeTruthy();
    });

    // Switch to "no"
    const noButton = screen.getByRole("radio", { name: /no/i });
    await user.click(noButton);

    // Dependent questions should disappear
    await waitFor(() => {
      expect(
        screen.queryByRole("heading", { name: /what is your monthly income/i }),
      ).toBeFalsy();
      expect(
        screen.queryByRole("heading", {
          name: /what is your primary income source/i,
        }),
      ).toBeFalsy();
    });
  });

  it("should toggle dependent questions when trigger value is changed", async () => {
    const user = userEvent.setup();

    useWizardStore.setState({
      session: {
        sessionId: "e2e-cond-3",
        stateCode: "CA",
        stateName: "California",
        currentStep: 0,
        totalSteps: questionSet.length,
        expiresAt: new Date(Date.now() + 1000 * 60 * 30).toISOString(),
      },
      questions: questionSet,
    });

    render(
      <MemoryRouter>
        <WizardPage />
      </MemoryRouter>,
    );

    // Start with "No"
    let noButton = screen.getByRole("radio", { name: /no/i });
    await user.click(noButton);

    expect(
      screen.queryByRole("heading", { name: /what is your monthly income/i }),
    ).toBeFalsy();

    // Switch to "Yes"
    let yesButton = screen.getByRole("radio", { name: /yes/i });
    await user.click(yesButton);

    await waitFor(() => {
      expect(
        screen.getByRole("heading", { name: /what is your monthly income/i }),
      ).toBeTruthy();
    });

    // Switch back to "No"
    noButton = screen.getByRole("radio", { name: /no/i });
    await user.click(noButton);

    await waitFor(() => {
      expect(
        screen.queryByRole("heading", { name: /what is your monthly income/i }),
      ).toBeFalsy();
    });
  });

  it("should avoid layout shift when questions appear/disappear", async () => {
    const user = userEvent.setup();

    useWizardStore.setState({
      session: {
        sessionId: "e2e-cond-4",
        stateCode: "CA",
        stateName: "California",
        currentStep: 0,
        totalSteps: questionSet.length,
        expiresAt: new Date(Date.now() + 1000 * 60 * 30).toISOString(),
      },
      questions: questionSet,
    });

    const { container } = render(
      <MemoryRouter>
        <WizardPage />
      </MemoryRouter>,
    );

    const initialLayout = container.getBoundingClientRect();

    // Trigger question appearance
    const yesButton = screen.getByRole("radio", { name: /yes/i });
    await user.click(yesButton);

    await waitFor(() => {
      expect(
        screen.getByRole("heading", { name: /what is your monthly income/i }),
      ).toBeTruthy();
    });

    const afterAppearLayout = container.getBoundingClientRect();

    // Check that layout didn't jump dramatically
    // Allow some growth but not excessive shift
    const heightGrowth = afterAppearLayout.height - initialLayout.height;
    expect(heightGrowth).toBeGreaterThanOrEqual(0);
    expect(heightGrowth).toBeLessThan(500); // Reasonable threshold
  });

  it("should support multiple dependent questions", async () => {
    const user = userEvent.setup();

    const complexSet: QuestionDto[] = [
      {
        key: "has-dependents",
        label: "Do you have dependents?",
        type: "boolean",
        required: true,
      },
      {
        key: "num-dependents",
        label: "How many dependents?",
        type: "integer",
        required: false,
        conditions: [
          {
            fieldKey: "has-dependents",
            operator: "equals",
            value: "true",
          },
        ],
      },
      {
        key: "dependent-ages",
        label: "What are their ages?",
        type: "text",
        required: false,
        conditions: [
          {
            fieldKey: "has-dependents",
            operator: "equals",
            value: "true",
          },
          {
            fieldKey: "num-dependents",
            operator: "gt",
            value: "0",
          },
        ],
      },
    ];

    useWizardStore.setState({
      session: {
        sessionId: "e2e-cond-5",
        stateCode: "CA",
        stateName: "California",
        currentStep: 0,
        totalSteps: complexSet.length,
        expiresAt: new Date(Date.now() + 1000 * 60 * 30).toISOString(),
      },
      questions: complexSet,
    });

    render(
      <MemoryRouter>
        <WizardPage />
      </MemoryRouter>,
    );

    // Initially no dependent questions visible
    expect(screen.queryByText(/how many dependents/i)).toBeFalsy();
    expect(screen.queryByText(/what are their ages/i)).toBeFalsy();

    // Answer "yes" to show first dependent
    const yesButton = screen.getByRole("radio", { name: /yes/i });
    await user.click(yesButton);

    await waitFor(() => {
      expect(screen.getByText(/how many dependents/i)).toBeTruthy();
    });

    // But third question still hidden (needs num-dependents > 0)
    expect(screen.queryByText(/what are their ages/i)).toBeFalsy();

    // Answer num-dependents
    const numInput = screen.getByRole("spinbutton", {
      name: /how many dependents/i,
    });
    await user.type(numInput, "2");

    // Now third question should appear
    await waitFor(() => {
      expect(screen.getByText(/what are their ages/i)).toBeTruthy();
    });
  });

  it("should respect complex conditions with multiple operators", async () => {
    const user = userEvent.setup();

    const complexConditions: QuestionDto[] = [
      {
        key: "age",
        label: "What is your age?",
        type: "integer",
        required: true,
      },
      {
        key: "income",
        label: "What is your income?",
        type: "currency",
        required: true,
      },
      {
        key: "senior-assistance",
        label: "Are you interested in senior assistance programs?",
        type: "boolean",
        required: false,
        conditions: [
          {
            fieldKey: "age",
            operator: "gte",
            value: "65",
          },
          {
            fieldKey: "income",
            operator: "lt",
            value: "25000",
          },
        ],
      },
    ];

    useWizardStore.setState({
      session: {
        sessionId: "e2e-cond-6",
        stateCode: "CA",
        stateName: "California",
        currentStep: 0,
        totalSteps: complexConditions.length,
        expiresAt: new Date(Date.now() + 1000 * 60 * 30).toISOString(),
      },
      questions: complexConditions,
    });

    render(
      <MemoryRouter>
        <WizardPage />
      </MemoryRouter>,
    );

    // Question should be hidden initially
    expect(
      screen.queryByRole("heading", {
        name: /interested in senior assistance programs/i,
      }),
    ).toBeFalsy();

    // Answer age: 70 (meets first condition)
    const ageInput = screen.getByRole("spinbutton", { name: /age/i });
    await user.type(ageInput, "70");

    await waitFor(() => {
      expect(useWizardStore.getState().answerMap.age).toBe("70");
    });

    // Still hidden because income condition not met
    expect(
      screen.queryByRole("heading", {
        name: /interested in senior assistance programs/i,
      }),
    ).toBeFalsy();

    // Answer income: 20000 (meets second condition)
    const incomeInput = screen.getByRole("textbox", { name: /income/i });
    await user.type(incomeInput, "20000");

    await waitFor(() => {
      expect(useWizardStore.getState().answerMap.income).toBe("20000");
    });

    // Now should appear (both conditions met)
    await waitFor(() => {
      expect(
        screen.getByRole("heading", {
          name: /interested in senior assistance programs/i,
        }),
      ).toBeTruthy();
    });
  });
});
