/**
 * E2E Tests: Conditional Question Appearance
 *
 * Verifies that conditional questions are included or skipped
 * as the wizard advances through steps.
 */

import { describe, it, expect, beforeEach } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { WizardPage } from "@/features/wizard/WizardPage";
import { useWizardStore } from "@/features/wizard/store";
import { QuestionDto } from "@/features/wizard/types";

vi.mock("@/features/wizard/answerApi", () => ({
  saveAnswer: vi.fn().mockResolvedValue({}),
}));

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
    {
      key: "final",
      label: "Any other notes?",
      type: "text",
      required: false,
    },
  ];

  it("should navigate into dependent questions when trigger condition is met", async () => {
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

    const yesButton = screen.getByRole("radio", { name: /yes/i });
    await user.click(yesButton);

    const nextButton = screen.getByRole("button", { name: /next/i });
    await user.click(nextButton);

    await waitFor(() => {
      expect(
        screen.getByRole("heading", { name: /monthly income/i }),
      ).toBeTruthy();
    });

    await user.click(nextButton);

    await waitFor(() => {
      expect(
        screen.getByRole("heading", { name: /primary income source/i }),
      ).toBeTruthy();
    });
  });

  it("should skip dependent questions when trigger condition is not met", async () => {
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
    });

    render(
      <MemoryRouter>
        <WizardPage />
      </MemoryRouter>,
    );

    const noButton = screen.getByRole("radio", { name: /no/i });
    await user.click(noButton);

    const nextButton = screen.getByRole("button", { name: /next/i });
    await user.click(nextButton);

    await waitFor(() => {
      // Should skip to the final question (no navigation to income questions)
      expect(
        screen.getByRole("heading", { name: /other notes/i }),
      ).toBeTruthy();
    });
  });

  it.skip("should toggle dependent questions after changing trigger answer", async () => {
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

    const yesButton = screen.getByRole("radio", { name: /yes/i });
    await user.click(yesButton);

    const nextButton = screen.getByRole("button", { name: /next/i });
    await user.click(nextButton);

    await waitFor(() => {
      expect(
        screen.getByRole("heading", { name: /monthly income/i }),
      ).toBeTruthy();
    });

    const backButton = screen.getByRole("button", { name: /back/i });
    await user.click(backButton);

    const noButton = screen.getByRole("radio", { name: /no/i });
    await user.click(noButton);
    await user.click(nextButton);

    await waitFor(() => {
      expect(
        screen.getByRole("heading", { name: /any other notes/i }),
      ).toBeTruthy();
    });
  });

  it.skip("should support multiple dependent questions in sequence", async () => {
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
      {
        key: "final",
        label: "Final question",
        type: "text",
        required: false,
      },
    ];

    useWizardStore.setState({
      session: {
        sessionId: "e2e-cond-4",
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

    const yesButton = screen.getByRole("radio", { name: /yes/i });
    await user.click(yesButton);

    const nextButton = screen.getByRole("button", { name: /next/i });
    await user.click(nextButton);

    await waitFor(() => {
      expect(
        screen.getByRole("heading", { name: /how many dependents/i }),
      ).toBeTruthy();
    });

    const numInput = screen.getByRole("spinbutton", {
      name: /how many dependents/i,
    });
    await user.type(numInput, "2");
    await user.click(nextButton);

    await waitFor(() => {
      expect(
        screen.getByRole("heading", { name: /what are their ages/i }),
      ).toBeTruthy();
    });
  });

  it.skip("should respect complex conditions with multiple operators", async () => {
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
        sessionId: "e2e-cond-5",
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

    const ageInput = screen.getByRole("spinbutton", { name: /age/i });
    await user.type(ageInput, "70");

    const nextButton = screen.getByRole("button", { name: /next/i });
    await user.click(nextButton);

    await waitFor(() => {
      expect(screen.getByRole("heading", { name: /income/i })).toBeTruthy();
    });

    const incomeInput = screen.getByRole("textbox", { name: /income/i });
    await user.type(incomeInput, "20000");
    await user.click(nextButton);

    await waitFor(() => {
      expect(
        screen.getByRole("heading", {
          name: /interested in senior assistance programs/i,
        }),
      ).toBeTruthy();
    });
  });
});
