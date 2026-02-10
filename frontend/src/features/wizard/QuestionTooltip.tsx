/**
 * QuestionTooltip Component
 *
 * Displays "Why we ask this" contextual help for questions.
 * Uses shadcn/ui Tooltip component built on Radix UI primitives.
 *
 * Features:
 * - Keyboard accessible (Tab, Enter, Escape)
 * - Screen reader support (aria-label, aria-describedby)
 * - WCAG 2.1 AA color contrast (4.5:1 ratio)
 * - Touch-friendly (44x44px minimum touch target)
 * - Tooltip positioning with fallback
 * - Mobile optimized
 *
 * Follows Constitution III: WCAG 2.1 AA Compliance
 */

import { HelpCircle } from "lucide-react";
import {
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from "@/components/ui/tooltip";
import { cn } from "@/lib/utils";

interface QuestionTooltipProps {
  helpText: string;
  questionLabel: string;
  /**
   * Optional additional CSS classes for the help icon button
   */
  className?: string;
}

/**
 * Tooltip trigger button with help icon
 * Displays contextual help text in a tooltip
 *
 * @param helpText - The help text to display
 * @param questionLabel - The question label (for accessibility context)
 * @param className - Optional CSS classes
 */
export function QuestionTooltip({
  helpText,
  questionLabel,
  className,
}: QuestionTooltipProps) {
  return (
    <TooltipProvider>
      <Tooltip>
        <TooltipTrigger asChild>
          <button
            type="button"
            className={cn(
              // Base button styling
              "inline-flex items-center justify-center",
              "rounded-full",
              "p-1.5", // 24x24px base size
              "min-h-11 min-w-11", // 44x44px minimum touch target
              "ml-2",
              // Accessibility: minimum touch target 44x44px
              "focus:outline-none focus:ring-2 focus:ring-offset-2",
              "focus:ring-blue-500 focus:ring-offset-white",
              // Hover state
              "hover:bg-gray-100 dark:hover:bg-gray-800",
              // Color: High contrast (WCAG AA compliant)
              "text-gray-600 dark:text-gray-400",
              // Transition
              "transition-colors duration-200",
              // Additional classes
              className,
            )}
            // Accessibility attributes
            aria-label={`Why we ask this: ${questionLabel}`}
            aria-describedby={`tooltip-help-${questionLabel.replace(/\s+/g, "-").toLowerCase()}`}
            title={helpText}
          >
            <HelpCircleIcon />
          </button>
        </TooltipTrigger>

        {/* Tooltip content */}
        <TooltipContent
          id={`tooltip-help-${questionLabel.replace(/\s+/g, "-").toLowerCase()}`}
          side="right"
          sideOffset={8}
          className={cn(
            // Styling
            "max-w-xs",
            "px-3 py-2",
            "text-sm",
            "font-normal",
            // Color contrast: WCAG AA (4.5:1 ratio)
            "bg-gray-900 text-white",
            "dark:bg-gray-100 dark:text-gray-900",
            // Border for clarity
            "border border-gray-700 dark:border-gray-300",
            // Rounded corners
            "rounded-md",
            // Shadow for depth
            "shadow-lg",
            // Animation
            "animate-in fade-in-0 zoom-in-95",
            "data-[state=closed]:animate-out data-[state=closed]:fade-out-0 data-[state=closed]:zoom-out-95",
            "duration-200",
          )}
        >
          {helpText}
        </TooltipContent>
      </Tooltip>
    </TooltipProvider>
  );
}

/**
 * Help Circle Icon Component
 * 12x12px icon (button provides padding to 24x24px size)
 * High contrast: gray-600 / gray-400 for WCAG AA compliance
 */
function HelpCircleIcon() {
  return (
    <HelpCircle className="h-4 w-4" strokeWidth={2.5} aria-hidden="true" />
  );
}

/**
 * Export a wrapped version that doesn't require manual TooltipProvider
 * Use this in most cases
 */
export function QuestionHelpTooltip({
  helpText,
  questionLabel,
  className,
}: QuestionTooltipProps) {
  return (
    <QuestionTooltip
      helpText={helpText}
      questionLabel={questionLabel}
      className={className}
    />
  );
}
