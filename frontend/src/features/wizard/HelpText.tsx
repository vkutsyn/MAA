import { ReactNode } from "react";
import { cn } from "@/lib/utils";

interface HelpTextProps {
  id?: string;
  children: ReactNode;
  className?: string;
}

/**
 * Help text component for providing context and guidance to users.
 *
 * Usage:
 * - Place below form labels to explain what information is needed
 * - Use plain language (8th grade reading level)
 * - Keep messages concise (1-2 sentences)
 * - Link to help text via aria-describedby on input fields
 *
 * Accessibility:
 * - Associated with form fields via aria-describedby
 * - Announced by screen readers when field receives focus
 * - Visible to all users (not hidden or tooltip-only)
 *
 * Example:
 * ```tsx
 * <Label htmlFor="income">Annual Income</Label>
 * <HelpText id="income-help">
 *   Include all sources: wages, benefits, social security, etc.
 * </HelpText>
 * <Input
 *   id="income"
 *   aria-describedby="income-help"
 * />
 * ```
 */
export function HelpText({ id, children, className }: HelpTextProps) {
  return (
    <p id={id} className={cn("text-sm text-muted-foreground", className)}>
      {children}
    </p>
  );
}

interface HelpTextInlineProps {
  children: ReactNode;
  className?: string;
}

/**
 * Inline help text component for additional context.
 * Displayed inline with minimal visual weight.
 *
 * Example:
 * ```tsx
 * <Label>
 *   ZIP Code <HelpTextInline>(5 digits)</HelpTextInline>
 * </Label>
 * ```
 */
export function HelpTextInline({ children, className }: HelpTextInlineProps) {
  return (
    <span
      className={cn("text-sm font-normal text-muted-foreground", className)}
    >
      {children}
    </span>
  );
}

interface HelpBoxProps {
  title?: string;
  children: ReactNode;
  variant?: "info" | "tip" | "warning";
  className?: string;
}

/**
 * Help box component for detailed guidance or tips.
 * More prominent than HelpText for important information.
 *
 * Variants:
 * - info: General informational content (blue)
 * - tip: Helpful tips or best practices (green)
 * - warning: Important warnings or cautions (amber)
 *
 * Example:
 * ```tsx
 * <HelpBox variant="tip" title="Pro Tip">
 *   Gathering your tax documents before starting will speed up this process.
 * </HelpBox>
 * ```
 */
export function HelpBox({
  title,
  children,
  variant = "info",
  className,
}: HelpBoxProps) {
  const variantStyles = {
    info: "border-blue-200 bg-blue-50 text-blue-900 dark:border-blue-800 dark:bg-blue-950 dark:text-blue-100",
    tip: "border-green-200 bg-green-50 text-green-900 dark:border-green-800 dark:bg-green-950 dark:text-green-100",
    warning:
      "border-amber-200 bg-amber-50 text-amber-900 dark:border-amber-800 dark:bg-amber-950 dark:text-amber-100",
  };

  const iconMap = {
    info: "💡",
    tip: "✓",
    warning: "⚠",
  };

  return (
    <div
      role="note"
      className={cn("rounded-md border p-4", variantStyles[variant], className)}
    >
      {title && (
        <div className="mb-2 flex items-center gap-2">
          <span aria-hidden="true">{iconMap[variant]}</span>
          <strong className="text-sm font-semibold">{title}</strong>
        </div>
      )}
      <div className="text-sm leading-relaxed">{children}</div>
    </div>
  );
}

interface WhyWeAskProps {
  children: ReactNode;
  className?: string;
}

/**
 * "Why we ask" explanation component.
 * Explains the purpose of collecting specific information.
 *
 * Best practices:
 * - Be transparent about why information is needed
 * - Explain how it affects the outcome
 * - Build trust by being upfront
 *
 * Example:
 * ```tsx
 * <WhyWeAsk>
 *   We need your household size to determine the income threshold for your state.
 * </WhyWeAsk>
 * ```
 */
export function WhyWeAsk({ children, className }: WhyWeAskProps) {
  return (
    <details className={cn("text-sm text-muted-foreground", className)}>
      <summary className="cursor-pointer font-medium hover:text-foreground">
        Why we ask this
      </summary>
      <p className="mt-2 leading-relaxed">{children}</p>
    </details>
  );
}
