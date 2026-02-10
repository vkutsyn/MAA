/**
 * E2E Tests: Conditional Answer Persistence
 *
 * Verifies that answers to conditionally visible questions are preserved
 * even when the question becomes hidden and then visible again.
 *
 * Test Strategy:
 * - Answer a conditionally visible question
 * - Hide the question by changing the trigger condition
 * - Show the question again
 * - Verify the answer is preserved
 */

import { describe, it, expect, beforeEach } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { WizardPage } from "@/features/wizard/WizardPage";
import { useWizardStore } from "@/features/wizard/store";
import { QuestionDto } from "@/features/wizard/types";

describe("E2E: Conditional Answer Persistence", () => {
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
      key: "has-job",
      label: "Do you currently have a job?",
      type: "boolean",
      required: true,
    },
    {
      key: "job-title",
      label: "What is your job title?",
      type: "text",
      required: false,
      conditions: [
        {
          fieldKey: "has-job",
          operator: "equals",
          value: "true",
        },
      ],
    },
    {
      key: "employer",
      label: "What is your employer name?",
      type: "text",
      required: false,
      conditions: [
        {
          fieldKey: "has-job",
          operator: "equals",
          value: "true",
        },
      ],
    },
  ];

  it("should preserve answer when conditional question is hidden and shown again", async () => {
    const user = userEvent.setup();

    useWizardStore.setState({
      session: {
        sessionId: "e2e-persist-1",
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

    // Answer "yes" to show dependent questions
    const yesButton = screen.getByRole("radio", { name: /yes/i });
    await user.click(yesButton);

    await waitFor(() => {
      expect(
        screen.getByRole("heading", { name: /what is your job title/i }),
      ).toBeTruthy();
    });

    // Answer the dependent question
    const jobTitleInput = screen.getByLabelText(/what is your job title/i, {
      selector: "input, textarea",
    });
    await new Promise((resolve) => setTimeout(resolve, 200));
    jobTitleInput.focus();
    await user.type(jobTitleInput, "Software Engineer");

    // Verify it's in the store
    await waitFor(() => {
      const store = useWizardStore.getState();
      expect(store.answers["job-title"]?.answerValue).toBe("Software Engineer");
    });

    // Change the trigger to "no" to hide the question
    const noButton = screen.getByRole("radio", { name: /no/i });
    await user.click(noButton);

    // Question should be hidden
    await waitFor(() => {
      expect(
        screen.queryByRole("heading", { name: /what is your job title/i }),
      ).toBeFalsy();
    });

    // Change back to "yes" to show the question again
    const yesButton2 = screen.getByRole("radio", { name: /yes/i });
    await user.click(yesButton2);

    // Question should reappear with preserved value
    await waitFor(() => {
      const restoredInput = screen.getByLabelText(/what is your job title/i, {
        selector: "input, textarea",
      }) as HTMLInputElement;
      expect(restoredInput.value).toBe("Software Engineer");
    });
  });

  it("should preserve multiple conditional answers when toggling visibility", async () => {
    const user = userEvent.setup();

    useWizardStore.setState({
      session: {
        sessionId: "e2e-persist-2",
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

    // Answer "yes"
    const yesButton = screen.getByRole("radio", { name: /yes/i });
    await user.click(yesButton);

    await waitFor(() => {
      expect(
        screen.getByRole("heading", { name: /what is your job title/i }),
      ).toBeTruthy();
      expect(
        screen.getByRole("heading", { name: /what is your employer name/i }),
      ).toBeTruthy();
    });

    // Answer both dependent questions
    const jobTitleInput = screen.getByLabelText(/what is your job title/i, {
      selector: "input, textarea",
    });
    const employerInput = screen.getByLabelText(/what is your employer name/i, {
      selector: "input, textarea",
    });

    await new Promise((resolve) => setTimeout(resolve, 200));
    jobTitleInput.focus();
    await user.type(jobTitleInput, "Software Engineer");
    employerInput.focus();
    await user.type(employerInput, "Tech Company Inc");

    // Toggle visibility by switching to "no"
    const noButton = screen.getByRole("radio", { name: /no/i });
    await user.click(noButton);

    await waitFor(() => {
      expect(
        screen.queryByRole("heading", { name: /what is your job title/i }),
      ).toBeFalsy();
      expect(
        screen.queryByRole("heading", { name: /what is your employer name/i }),
      ).toBeFalsy();
    });

    // Toggle back to "yes"
    const yesButton2 = screen.getByRole("radio", { name: /yes/i });
    await user.click(yesButton2);

    // Both answers should be preserved
    await waitFor(() => {
      const restoredInputs = screen.getAllByRole("textbox");
      expect((restoredInputs[0] as HTMLInputElement).value).toBe(
        "Software Engineer",
      );
      expect((restoredInputs[1] as HTMLInputElement).value).toBe(
        "Tech Company Inc",
      );
    });
  });

  it("should preserve answers in store even while question is hidden", async () => {
    const user = userEvent.setup();

    useWizardStore.setState({
      session: {
        sessionId: "e2e-persist-3",
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

    // Show by answering "yes"
    const yesButton = screen.getByRole("radio", { name: /yes/i });
    await user.click(yesButton);

    await waitFor(() => {
      expect(
        screen.getByRole("heading", { name: /what is your job title/i }),
      ).toBeTruthy();
    });

    // Answer question
    const jobTitleInput = screen.getByLabelText(/what is your job title/i, {
      selector: "input, textarea",
    });
    await new Promise((resolve) => setTimeout(resolve, 200));
    jobTitleInput.focus();
    await user.type(jobTitleInput, "Manager");

    // Check store has the answer
    await waitFor(() => {
      const store = useWizardStore.getState();
      expect(store.answers["job-title"]?.answerValue).toBe("Manager");
    });

    // Hide by answering "no"
    const noButton = screen.getByRole("radio", { name: /no/i });
    await user.click(noButton);

    await waitFor(() => {
      expect(
        screen.queryByRole("heading", { name: /what is your job title/i }),
      ).toBeFalsy();
    });

    // Check store STILL has the answer
    const store = useWizardStore.getState();
    expect(store.answers["job-title"]?.answerValue).toBe("Manager");
  });

  it("should allow changing answer of conditionally hidden question when re-revealed", async () => {
    const user = userEvent.setup();

    useWizardStore.setState({
      session: {
        sessionId: "e2e-persist-4",
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

    // Show and answer
    const yesButton = screen.getByRole("radio", { name: /yes/i });
    await user.click(yesButton);

    await waitFor(() => {
      expect(
        screen.getByRole("heading", { name: /what is your job title/i }),
      ).toBeTruthy();
    });

    const jobTitleInput = screen.getByLabelText(/what is your job title/i, {
      selector: "input, textarea",
    });
    await new Promise((resolve) => setTimeout(resolve, 200));
    jobTitleInput.focus();
    await user.type(jobTitleInput, "Engineer");

    // Hide
    const noButton = screen.getByRole("radio", { name: /no/i });
    await user.click(noButton);

    // Show again
    const yesButton2 = screen.getByRole("radio", { name: /yes/i });
    await user.click(yesButton2);

    // Change the answer
    await waitFor(() => {
      expect(
        screen.getByRole("heading", { name: /what is your job title/i }),
      ).toBeTruthy();
    });

    const restoredInput = screen.getByLabelText(/what is your job title/i, {
      selector: "input, textarea",
    });
    await user.clear(restoredInput);
    await user.type(restoredInput, "Director");

    // Verify new answer is stored
    await waitFor(() => {
      const store = useWizardStore.getState();
      expect(store.answers["job-title"]?.answerValue).toBe("Director");
    });
  });

  it("should handle rapid toggling of visibility without losing answers", async () => {
    const user = userEvent.setup();

    useWizardStore.setState({
      session: {
        sessionId: "e2e-persist-5",
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

    // Answer "yes"
    let yesButton = screen.getByRole("radio", { name: /yes/i });
    await user.click(yesButton);

    await waitFor(() => {
      expect(
        screen.getByRole("heading", { name: /what is your job title/i }),
      ).toBeTruthy();
    });

    // Answer question
    const jobTitleInput = screen.getByLabelText(/what is your job title/i, {
      selector: "input, textarea",
    });
    await new Promise((resolve) => setTimeout(resolve, 200));
    jobTitleInput.focus();
    await user.type(jobTitleInput, "Analyst");

    // Rapid toggle: yes -> no -> yes -> no -> yes
    for (let i = 0; i < 5; i++) {
      if (i % 2 === 0) {
        const noButton = screen.getByRole("radio", { name: /no/i });
        await user.click(noButton);
      } else {
        yesButton = screen.getByRole("radio", { name: /yes/i });
        await user.click(yesButton);
      }
      // Small delay between toggles
      await new Promise((resolve) => setTimeout(resolve, 50));
    }

    // Final state should be "yes" and answer preserved
    await waitFor(() => {
      expect(
        screen.getByRole("heading", { name: /what is your job title/i }),
      ).toBeTruthy();
    });

    const finalInput = screen.getByLabelText(/what is your job title/i, {
      selector: "input, textarea",
    }) as HTMLInputElement;
    expect(finalInput.value).toBe("Analyst");

    const store = useWizardStore.getState();
    expect(store.answers["job-title"]?.answerValue).toBe("Analyst");
  });
});
