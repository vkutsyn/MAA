/**
 * E2E Tests: Tooltip Interaction Flow
 *
 * Verifies tooltip behavior when used within the wizard question UI.
 * Focuses on end-to-end user interactions.
 */

import { describe, it, expect, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { WizardStep } from "@/features/wizard/WizardStep";
import { QuestionDto } from "@/features/wizard/types";

const baseQuestion: QuestionDto = {
  key: "income",
  label: "What is your monthly income?",
  type: "currency",
  required: true,
  helpText: "We use this to determine financial eligibility.",
};

describe("E2E: Tooltip Interaction", () => {
  it("shows and hides tooltip on hover and leave", async () => {
    const user = userEvent.setup();

    render(
      <WizardStep
        question={baseQuestion}
        answer={null}
        onAnswer={vi.fn()}
        onNext={vi.fn()}
        onBack={vi.fn()}
      />,
    );

    const tooltipButton = screen.getByRole("button", {
      name: /why we ask this/i,
    });

    await user.hover(tooltipButton);

    await waitFor(() => {
      expect(
        screen.getAllByText(/determine financial eligibility/i).length,
      ).toBeGreaterThan(0);
    });

    await user.keyboard("{Escape}");

    await waitFor(() => {
      expect(
        screen.queryAllByText(/determine financial eligibility/i).length,
      ).toBe(0);
    });
  });

  it("shows tooltip on focus for keyboard interactions", async () => {
    const user = userEvent.setup();

    render(
      <WizardStep
        question={baseQuestion}
        answer={null}
        onAnswer={vi.fn()}
        onNext={vi.fn()}
        onBack={vi.fn()}
      />,
    );

    const tooltipButton = screen.getByRole("button", {
      name: /why we ask this/i,
    });

    await user.tab();
    expect(tooltipButton).toHaveFocus();

    await waitFor(() => {
      expect(
        screen.getAllByText(/determine financial eligibility/i).length,
      ).toBeGreaterThan(0);
    });
  });
});
