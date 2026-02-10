/**
 * Keyboard navigation and focus management utilities for the Eligibility Wizard.
 * Supports WCAG 2.1 AA keyboard navigation and focus visibility.
 *
 * Reference: WCAG 2.1 Level AA
 * - 2.1.1 Keyboard: All functionality must be available by keyboard
 * - 2.1.2 No Keyboard Trap: If keyboard focus can be moved to a component, focus can be moved away
 * - 2.4.3 Focus Order: Focus order is logical and meaningful
 * - 2.4.7 Focus Visible: Visible indication of keyboard focus
 */

/**
 * Focus management helper for wizard navigation.
 * Ensures focus is moved to the appropriate element after navigation.
 *
 * @param elementId - CSS selector or element ID to focus
 * @param delay - Optional delay before focus (default: 0ms)
 */
export function setFocusAnnounced(elementId: string, delay = 0) {
  setTimeout(() => {
    const element =
      document.getElementById(elementId) || document.querySelector(elementId);
    if (element instanceof HTMLElement) {
      element.focus();
      // Announce to screen readers that focus has moved
      announceToScreenReader("Step loaded, ready for input", "polite");
    }
  }, delay);
}

/**
 * Navigate between wizard steps using keyboard.
 * Handles common navigation patterns:
 * - Tab / Shift+Tab: Move between form elements
 * - Enter: Submit form (on buttons)
 * - Escape: No-op (allow native browser behavior)
 *
 * @param event - Keyboard event
 * @param handlers - Navigation handlers
 * @returns true if event was handled, false otherwise
 */
export function handleWizardKeydown(
  event: React.KeyboardEvent,
  handlers: {
    onNext?: () => void;
    onBack?: () => void;
    onSubmit?: () => void;
  },
): boolean {
  // Only handle Enter, not from within textareas
  if (
    event.key === "Enter" &&
    event.currentTarget instanceof HTMLButtonElement
  ) {
    event.preventDefault();
    if (handlers.onSubmit) {
      handlers.onSubmit();
      return true;
    }
  }

  // Let Tab and Shift+Tab flow naturally (managed by browser)
  if (event.key === "Tab") {
    return false;
  }

  return false;
}

/**
 * Manage focus trap for modal-like components.
 * Prevents focus from escaping the wizard while it's the primary interaction target.
 *
 * @param event - Keyboard event
 * @param containerId - ID of container to trap focus within
 * @returns true if focus was trapped, false otherwise
 */
export function handleFocusTrap(
  event: KeyboardEvent,
  containerId: string,
): boolean {
  if (event.key !== "Tab") {
    return false;
  }

  const container = document.getElementById(containerId);
  if (!container) {
    return false;
  }

  // Get all focusable elements
  const focusableElements = Array.from(
    container.querySelectorAll(
      'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])',
    ),
  ).filter((el) => {
    const element = el as HTMLElement;
    return !element.hasAttribute("disabled") && element.offsetParent !== null;
  });

  if (focusableElements.length === 0) {
    return false;
  }

  const firstElement = focusableElements[0] as HTMLElement;
  const lastElement = focusableElements[
    focusableElements.length - 1
  ] as HTMLElement;

  if (event.shiftKey) {
    // Shift+Tab on first element - move to last
    if (document.activeElement === firstElement) {
      event.preventDefault();
      lastElement.focus();
      return true;
    }
  } else {
    // Tab on last element - move to first
    if (document.activeElement === lastElement) {
      event.preventDefault();
      firstElement.focus();
      return true;
    }
  }

  return false;
}

/**
 * Announce text to screen reader users.
 * Uses aria-live region for non-intrusive announcements.
 *
 * @param message - Message to announce
 * @param priority - 'polite' (default) or 'assertive'
 */
export function announceToScreenReader(
  message: string,
  priority: "polite" | "assertive" = "polite",
) {
  // Get or create announcement region
  let region = document.querySelector(
    `[role="status"][aria-live="${priority}"]`,
  ) as HTMLElement | null;
  if (!region) {
    region = document.createElement("div");
    region.setAttribute("role", "status");
    region.setAttribute("aria-live", priority);
    region.setAttribute("aria-atomic", "true");
    region.style.position = "absolute";
    region.style.left = "-10000px";
    region.style.width = "1px";
    region.style.height = "1px";
    region.style.overflow = "hidden";
    document.body.appendChild(region);
  }

  region.textContent = message;

  // Clear after announcement (to avoid repetition on subsequent changes)
  setTimeout(() => {
    region!.textContent = "";
  }, 2000);
}

/**
 * Check if keyboard event is a supported navigation key.
 *
 * @param event - Keyboard event
 * @returns true if key should be handled as navigation
 */
export function isNavigationKey(event: KeyboardEvent): boolean {
  return ["Tab", "Enter", "Escape", "ArrowUp", "ArrowDown"].includes(event.key);
}

/**
 * Get all focusable elements within a container.
 * Useful for managing focus order and keyboard traps.
 *
 * @param container - Container element or ID
 * @returns Array of focusable elements
 */
export function getFocusableElements(
  container: HTMLElement | string,
): HTMLElement[] {
  const el =
    typeof container === "string"
      ? document.getElementById(container)
      : container;

  if (!el) {
    return [];
  }

  return Array.from(
    el.querySelectorAll(
      'button:not([disabled]), [href], input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])',
    ),
  ).filter((el) => {
    const element = el as HTMLElement;
    return element.offsetParent !== null; // Visible elements only
  }) as HTMLElement[];
}

/**
 * Move focus to next focusable element.
 *
 * @param container - Container element or ID
 * @param reverse - If true, move to previous element
 * @returns true if focus was moved, false if already at end
 */
export function moveFocus(
  container: HTMLElement | string,
  reverse = false,
): boolean {
  const focusableElements = getFocusableElements(container);
  if (focusableElements.length === 0) {
    return false;
  }

  const currentIndex = focusableElements.indexOf(
    document.activeElement as HTMLElement,
  );
  const nextIndex = reverse ? currentIndex - 1 : currentIndex + 1;

  if (nextIndex >= 0 && nextIndex < focusableElements.length) {
    focusableElements[nextIndex].focus();
    return true;
  }

  return false;
}

/**
 * Skip link helper for bypassing repetitive content.
 * Provides a keyboard-accessible "Skip to main content" link.
 *
 * @param targetId - ID of main content element
 */
export function initSkipLink(targetId: string) {
  const skipButton = document.querySelector("[data-skip-link]") as HTMLElement;
  if (!skipButton) {
    return;
  }

  skipButton.addEventListener("click", (e) => {
    e.preventDefault();
    const target = document.getElementById(targetId);
    if (target) {
      target.focus();
      target.scrollIntoView({ behavior: "smooth" });
    }
  });
}

/**
 * Disable automatic focus restoration on component remount.
 * Useful for preventing focus loss during navigation.
 *
 * @param ref - Reference to form or container element
 */
export function manageFocusRestore(ref: React.RefObject<HTMLDivElement>) {
  return () => {
    if (ref.current && document.activeElement === document.body) {
      const firstFocusable = ref.current.querySelector(
        'button, input, select, textarea, [tabindex]:not([tabindex="-1"])',
      ) as HTMLElement | null;

      if (firstFocusable) {
        firstFocusable.focus();
      }
    }
  };
}
