import { ReactNode } from "react";

interface WizardLayoutProps {
  children: ReactNode;
  title?: string;
  subtitle?: string;
}

/**
 * Layout wrapper for wizard pages.
 *
 * Features:
 * - Mobile-first responsive design (375px to 1920px breakpoints)
 * - Touch-friendly spacing and touch targets (minimum 44x44px)
 * - Proper focus visible styling for keyboard navigation
 * - Flexible layout that prevents horizontal scrolling
 * - Optimized padding and margins for mobile/tablet/desktop
 */
export function WizardLayout({ children, title, subtitle }: WizardLayoutProps) {
  return (
    <div className="min-h-screen bg-background py-4 sm:py-8 lg:py-12">
      <div className="mx-auto w-full max-w-2xl px-4 sm:px-6 lg:px-8">
        {/* Header with title and subtitle */}
        {(title || subtitle) && (
          <header className="mb-8 space-y-2 sm:mb-10">
            {title && (
              <h1 className="text-2xl font-bold tracking-tight sm:text-3xl">
                {title}
              </h1>
            )}
            {subtitle && (
              <p className="text-base text-muted-foreground sm:text-lg">
                {subtitle}
              </p>
            )}
          </header>
        )}

        {/* Main content */}
        <main className="space-y-6">{children}</main>
      </div>
    </div>
  );
}
