/**
 * Component Tests: Answer Persistence
 *
 * Verifies that user answers are correctly captured, stored, and preserved
 * as users navigate through the wizard step-by-step.
 *
 * Test Strategy:
 * - Answer value is captured when user types/selects
 * - Answer persists during navigation (forward/back)
 * - Answer is available to retrieve from store
 * - Multiple answers persist simultaneously
 */

import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { WizardPage } from "@/features/wizard/WizardPage";
import { useWizardStore } from "@/features/wizard/store";
import { QuestionDto } from "@/features/wizard/types";

describe("Answer Persistence", () => {
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

  const mockQuestions: QuestionDto[] = [
    {
      key: "name",
      label: "What is your name?",
      type: "text",
      required: true,
      helpText: "Your full legal name",
    },
    {
      key: "age",
      label: "What is your age?",
      type: "integer",
      required: true,
    },
    {
      key: "state",
      label: "What state do you live in?",
      type: "select",
      required: true,
      options: [
        { value: "CA", label: "California" },
        { value: "NY", label: "New York" },
        { value: "TX", label: "Texas" },
      ],
    },
  ];

  it("should save answer when user enters text", async () => {
    const user = userEvent.setup();

    useWizardStore.setState({
      session: {
        sessionId: "test-123",
        stateCode: "CA",
        stateName: "California",
        currentStep: 0,
        totalSteps: 3,
        expiresAt: new Date(Date.now() + 1000 * 60 * 30).toISOString(),
      },
      questions: mockQuestions,
      totalSteps: 3,
    });

    render(
      <MemoryRouter>
        <WizardPage />
      </MemoryRouter>,
    );

    const nameInput = screen.getByRole("textbox", {
      name: /what is your name/i,
    });
    await user.type(nameInput, "John Doe");

    await waitFor(() => {
      const store = useWizardStore.getState();
      expect(store.answers["name"]).toBeTruthy();
      expect(store.answers["name"].answerValue).toBe("John Doe");
    });
  });

  it("should save answer when user selects from dropdown", async () => {
    const user = userEvent.setup();

    useWizardStore.setState({
      session: {
        sessionId: "test-123",
        stateCode: "CA",
        stateName: "California",
        currentStep: 2,
        totalSteps: 3,
        expiresAt: new Date(Date.now() + 1000 * 60 * 30).toISOString(),
      },
      questions: mockQuestions,
      currentStep: 2,
    });

    render(
      <MemoryRouter>
        <WizardPage />
      </MemoryRouter>,
    );

    const select = screen.getByRole("combobox", { name: /what state/i });
    await user.selectOptions(select, "CA");

    await waitFor(() => {
      const store = useWizardStore.getState();
      expect(store.answers["state"]).toBeTruthy();
      expect(store.answers["state"].answerValue).toBe("CA");
    });
  });

  it("should preserve answer when navigating forward and back", async () => {
    const user = userEvent.setup();

    useWizardStore.setState({
      session: {
        sessionId: "test-123",
        stateCode: "CA",
        stateName: "California",
        currentStep: 0,
        totalSteps: 3,
        expiresAt: new Date(Date.now() + 1000 * 60 * 30).toISOString(),
      },
      questions: mockQuestions,
    });

    render(
      <MemoryRouter>
        <WizardPage />
      </MemoryRouter>,
    );

    // Enter name on first question
    const nameInput = screen.getByRole("textbox", {
      name: /what is your name/i,
    });
    await user.type(nameInput, "Jane Smith");

    // Navigate forward
    const nextButton = screen.getByRole("button", { name: /next/i });
    await user.click(nextButton);

    await waitFor(() => {
      expect(
        screen.getByRole("spinbutton", { name: /what is your age/i }),
      ).toBeTruthy();
    });

    // Navigate back
    const backButton = screen.getByRole("button", { name: /back/i });
    await user.click(backButton);

    await waitFor(() => {
      const restoredInput = screen.getByRole("textbox", {
        name: /what is your name/i,
      }) as HTMLInputElement;
      expect(restoredInput.value).toBe("Jane Smith");
    });
  });

  it("should store multiple answers simultaneously", async () => {
    const user = userEvent.setup();

    useWizardStore.setState({
      session: {
        sessionId: "test-123",
        stateCode: "CA",
        stateName: "California",
        currentStep: 0,
        totalSteps: 3,
        expiresAt: new Date(Date.now() + 1000 * 60 * 30).toISOString(),
      },
      questions: mockQuestions,
    });

  it("should store multiple answers simultaneously", async () => {
    const user = userEvent.setup();

    useWizardStore.setState({
      session: {
        sessionId: "test-123",
        stateCode: "CA",
        stateName: "California",
        currentStep: 0,
        totalSteps: 3,
        expiresAt: new Date(Date.now() + 1000 * 60 * 30).toISOString(),
      },
      questions: mockQuestions,
    });

    render(
      <MemoryRouter>
        <WizardPage />
      </MemoryRouter>,
    );

    // Answer first question
    const nameInput = screen.getByRole("textbox", {
      name: /what is your name/i,
    });
    await user.type(nameInput, "Alice Johnson");

    // Navigate to next question and answer it
    const nextButton = screen.getByRole("button", { name: /next/i });
    await user.click(nextButton);

    await waitFor(() => {
      expect(
        screen.getByRole("spinbutton", { name: /what is your age/i }),
      ).toBeTruthy();
    });

    const ageInput = screen.getByRole("spinbutton", {
      name: /what is your age/i,
    });
    await user.type(ageInput, "35");

    await waitFor(() => {
      const store = useWizardStore.getState();
      expect(store.answers["name"]?.answerValue).toBe("Alice Johnson");
      expect(store.answers["age"]?.answerValue).toBe("35");
    });
  });

  it("should update existing answer when changed", async () => {
    const user = userEvent.setup();

    useWizardStore.setState({
      session: {
        sessionId: "test-123",
        stateCode: "CA",
        stateName: "California",
        currentStep: 0,
        totalSteps: 3,
        expiresAt: new Date(Date.now() + 1000 * 60 * 30).toISOString(),
      },
      questions: mockQuestions,
      answers: {
        name: {
          fieldKey: "name",
          answerValue: "Bob",
          fieldType: "text",
          isPii: true,
        },
      },
    });

    render(
      <MemoryRouter>
        <WizardPage />
      </MemoryRouter>,
    );

    const nameInput = screen.getByRole("textbox", {
      name: /what is your name/i,
    }) as HTMLInputElement;
    expect(nameInput.value).toBe("Bob");

    // Clear and type new answer
    await userEvent.clear(nameInput);
    await userEvent.type(nameInput, "Robert");

    await waitFor(() => {
      const store = useWizardStore.getState();
      expect(store.answers["name"].answerValue).toBe("Robert");
    });
  });

  it("should preserve answer metadata (fieldType, isPii)", async () => {
    const user = userEvent.setup();

    useWizardStore.setState({
      session: {
        sessionId: "test-123",
        stateCode: "CA",
        stateName: "California",
        currentStep: 0,
        totalSteps: 3,
        expiresAt: new Date(Date.now() + 1000 * 60 * 30).toISOString(),
      },
      questions: mockQuestions,
    });

    render(
      <MemoryRouter>
        <WizardPage />
      </MemoryRouter>,
    );

    const nameInput = screen.getByRole("textbox", {
      name: /what is your name/i,
    });
    await user.type(nameInput, "Charlie Brown");

    await waitFor(() => {
      const store = useWizardStore.getState();
      const answer = store.answers["name"];
      expect(answer.fieldKey).toBe("name");
      expect(answer.fieldType).toBe("text");
      expect(answer.isPii).toBe(true);
    });
  });
});
});
