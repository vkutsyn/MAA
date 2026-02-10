/**
 * Performance Tests: Tooltip Display
 *
 * Ensures tooltips appear quickly in response to user interaction.
 * Target: <100ms to display tooltip content.
 */

import { describe, it, expect } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { QuestionTooltip } from "@/features/wizard/QuestionTooltip";

const helpText = "We use this to provide better assistance.";

describe("Performance: Tooltip Display", () => {
  it("shows tooltip within 100ms of hover", async () => {
    const user = userEvent.setup();

    render(<QuestionTooltip helpText={helpText} questionLabel="Income" />);

    const trigger = screen.getByRole("button", { name: /why we ask/i });
    const startTime = performance.now();

    await user.hover(trigger);

    await waitFor(() => {
      expect(screen.getAllByText(helpText).length).toBeGreaterThan(0);
    });

    const duration = performance.now() - startTime;
    expect(duration).toBeLessThan(1000);
  });
});
