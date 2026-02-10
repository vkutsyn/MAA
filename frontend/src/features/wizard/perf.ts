/**
 * Performance monitoring utilities for the Eligibility Wizard.
 *
 * Purpose:
 * - Measure step transition timing (FR-009: <500ms for 95% of interactions)
 * - Track performance metrics for optimization
 * - Provide debugging tools for slow interactions
 *
 * Usage:
 * ```tsx
 * const tracker = startTransition('step_advance')
 * // ... perform navigation
 * tracker.end() // Logs if exceeds threshold
 * ```
 */

/** Performance thresholds based on requirements */
export const PERF_THRESHOLDS = {
  /** Step transitions must complete within 500ms (FR-009) */
  STEP_TRANSITION_MS: 500,
  /** First question render should be under 1s */
  FIRST_QUESTION_MS: 1000,
  /** API calls should complete within 2s */
  API_CALL_MS: 2000,
} as const;

/** Performance metric types */
export type PerfMetricType =
  | "step_advance"
  | "step_back"
  | "session_start"
  | "session_restore"
  | "answer_save"
  | "question_load"
  | "state_lookup"
  | "api_call";

interface PerfMetric {
  type: PerfMetricType;
  startTime: number;
  endTime?: number;
  duration?: number;
  metadata?: Record<string, unknown>;
}

interface TransitionTracker {
  end: (metadata?: Record<string, unknown>) => number;
  cancel: () => void;
}

/** In-memory performance metrics store */
const metrics: PerfMetric[] = [];
const MAX_METRICS = 100; // Keep last 100 metrics

/**
 * Start tracking a performance metric.
 *
 * @param type - Type of operation being tracked
 * @param metadata - Optional metadata to attach
 * @returns Tracker object with end() and cancel() methods
 *
 * @example
 * ```tsx
 * const tracker = startTransition('step_advance')
 * // Perform navigation
 * const duration = tracker.end({ fromStep: 1, toStep: 2 })
 * ```
 */
export function startTransition(
  type: PerfMetricType,
  metadata?: Record<string, unknown>,
): TransitionTracker {
  const startTime = performance.now();
  let cancelled = false;

  const metric: PerfMetric = {
    type,
    startTime,
    metadata,
  };

  return {
    end: (endMetadata?: Record<string, unknown>) => {
      if (cancelled) return 0;

      const endTime = performance.now();
      const duration = endTime - startTime;

      metric.endTime = endTime;
      metric.duration = duration;
      metric.metadata = { ...metadata, ...endMetadata };

      // Store metric
      metrics.push(metric);
      if (metrics.length > MAX_METRICS) {
        metrics.shift();
      }

      // Log performance warnings
      const threshold = getThreshold(type);
      if (duration > threshold) {
        console.warn(
          `[PERF] ${type} took ${duration.toFixed(2)}ms (threshold: ${threshold}ms)`,
          metric.metadata,
        );
      }

      // Log to analytics (if available)
      if (typeof window !== "undefined" && "gtag" in window) {
        // Example: Google Analytics event
        (window as any).gtag("event", "timing_complete", {
          name: type,
          value: Math.round(duration),
          event_category: "wizard_performance",
        });
      }

      return duration;
    },
    cancel: () => {
      cancelled = true;
    },
  };
}

/**
 * Get performance threshold for a given metric type.
 */
function getThreshold(type: PerfMetricType): number {
  switch (type) {
    case "step_advance":
    case "step_back":
      return PERF_THRESHOLDS.STEP_TRANSITION_MS;
    case "session_start":
    case "question_load":
      return PERF_THRESHOLDS.FIRST_QUESTION_MS;
    case "session_restore":
    case "answer_save":
    case "state_lookup":
    case "api_call":
      return PERF_THRESHOLDS.API_CALL_MS;
    default:
      return PERF_THRESHOLDS.STEP_TRANSITION_MS;
  }
}

/**
 * Get performance statistics for a metric type.
 *
 * @param type - Optional metric type to filter by
 * @returns Performance statistics
 *
 * @example
 * ```tsx
 * const stats = getPerformanceStats('step_advance')
 * console.log(`P95: ${stats.p95}ms`)
 * ```
 */
export function getPerformanceStats(type?: PerfMetricType) {
  const filteredMetrics = type
    ? metrics.filter((m) => m.type === type && m.duration !== undefined)
    : metrics.filter((m) => m.duration !== undefined);

  if (filteredMetrics.length === 0) {
    return {
      count: 0,
      min: 0,
      max: 0,
      mean: 0,
      median: 0,
      p95: 0,
      p99: 0,
    };
  }

  const durations = filteredMetrics
    .map((m) => m.duration!)
    .sort((a, b) => a - b);

  const sum = durations.reduce((acc, d) => acc + d, 0);
  const mean = sum / durations.length;

  return {
    count: durations.length,
    min: durations[0],
    max: durations[durations.length - 1],
    mean,
    median: percentile(durations, 50),
    p95: percentile(durations, 95),
    p99: percentile(durations, 99),
  };
}

/**
 * Calculate percentile from sorted array.
 */
function percentile(sortedArray: number[], p: number): number {
  const index = (p / 100) * (sortedArray.length - 1);
  const lower = Math.floor(index);
  const upper = Math.ceil(index);
  const weight = index % 1;

  if (upper >= sortedArray.length) {
    return sortedArray[sortedArray.length - 1];
  }

  return sortedArray[lower] * (1 - weight) + sortedArray[upper] * weight;
}

/**
 * Get all recorded metrics.
 * Useful for debugging or exporting performance data.
 */
export function getAllMetrics(): ReadonlyArray<PerfMetric> {
  return [...metrics];
}

/**
 * Clear all recorded metrics.
 * Useful for testing or resetting performance tracking.
 */
export function clearMetrics(): void {
  metrics.length = 0;
}

/**
 * Log performance summary to console.
 * Useful for debugging and development.
 *
 * @example
 * ```tsx
 * // After completing wizard flow
 * logPerformanceSummary()
 * ```
 */
export function logPerformanceSummary(): void {
  console.group("🔍 Wizard Performance Summary");

  const types: PerfMetricType[] = [
    "step_advance",
    "step_back",
    "session_start",
    "answer_save",
  ];

  types.forEach((type) => {
    const stats = getPerformanceStats(type);
    if (stats.count === 0) return;

    const threshold = getThreshold(type);
    const exceedsThreshold = stats.p95 > threshold;

    console.log(
      `${type}: ${stats.count} samples | P95: ${stats.p95.toFixed(2)}ms ${
        exceedsThreshold ? "⚠️ SLOW" : "✓"
      } (threshold: ${threshold}ms)`,
    );
    console.log(
      `  min: ${stats.min.toFixed(2)}ms | max: ${stats.max.toFixed(2)}ms | mean: ${stats.mean.toFixed(2)}ms`,
    );
  });

  console.groupEnd();
}

/**
 * Check if wizard performance meets FR-009 requirement.
 * FR-009: Step transitions MUST complete in under 500ms for 95% of interactions.
 *
 * @returns true if P95 for step transitions is under 500ms
 */
export function meetsPerformanceRequirement(): boolean {
  const stepAdvanceStats = getPerformanceStats("step_advance");
  const stepBackStats = getPerformanceStats("step_back");

  if (stepAdvanceStats.count === 0 && stepBackStats.count === 0) {
    // No data yet
    return true;
  }

  const stepAdvanceOk =
    stepAdvanceStats.count === 0 ||
    stepAdvanceStats.p95 <= PERF_THRESHOLDS.STEP_TRANSITION_MS;

  const stepBackOk =
    stepBackStats.count === 0 ||
    stepBackStats.p95 <= PERF_THRESHOLDS.STEP_TRANSITION_MS;

  return stepAdvanceOk && stepBackOk;
}

/**
 * React hook for tracking component render performance.
 *
 * @param componentName - Name of component for logging
 *
 * @example
 * ```tsx
 * function WizardStep() {
 *   useRenderPerformance('WizardStep')
 *   // ... component logic
 * }
 * ```
 */
export function useRenderPerformance(componentName: string): void {
  if (typeof window === "undefined") return;

  // Use React's Profiler API if available
  if ("PerformanceObserver" in window) {
    const observer = new PerformanceObserver((list) => {
      for (const entry of list.getEntries()) {
        if (
          entry.entryType === "measure" &&
          entry.name.includes(componentName)
        ) {
          if (entry.duration > 16) {
            // > 1 frame at 60fps
            console.warn(
              `[RENDER] ${componentName} took ${entry.duration.toFixed(2)}ms`,
            );
          }
        }
      }
    });

    observer.observe({ entryTypes: ["measure"] });

    // Note: In production, you would need to handle cleanup with useEffect
    // This is a simplified version for performance monitoring during development
  }
}

/**
 * Measure API call performance.
 * Wrapper around fetch or axios that tracks timing.
 *
 * @param promise - Promise to track
 * @param type - Type of API call
 * @returns Promise with same result
 *
 * @example
 * ```tsx
 * const data = await measureApiCall(
 *   fetchQuestions(stateCode),
 *   'question_load'
 * )
 * ```
 */
export async function measureApiCall<T>(
  promise: Promise<T>,
  type: PerfMetricType = "api_call",
  metadata?: Record<string, unknown>,
): Promise<T> {
  const tracker = startTransition(type, metadata);

  try {
    const result = await promise;
    tracker.end({ success: true });
    return result;
  } catch (error) {
    tracker.end({ success: false, error: String(error) });
    throw error;
  }
}

/**
 * Debounce function for performance-sensitive operations.
 *
 * @param fn - Function to debounce
 * @param delay - Delay in milliseconds
 * @returns Debounced function
 */
export function debounce<T extends (...args: any[]) => any>(
  fn: T,
  delay: number,
): (...args: Parameters<T>) => void {
  let timeoutId: ReturnType<typeof setTimeout> | null = null;

  return (...args: Parameters<T>) => {
    if (timeoutId) {
      clearTimeout(timeoutId);
    }

    timeoutId = setTimeout(() => {
      fn(...args);
      timeoutId = null;
    }, delay);
  };
}

/**
 * Throttle function for performance-sensitive operations.
 *
 * @param fn - Function to throttle
 * @param delay - Minimum delay between calls in milliseconds
 * @returns Throttled function
 */
export function throttle<T extends (...args: any[]) => any>(
  fn: T,
  delay: number,
): (...args: Parameters<T>) => void {
  let lastCall = 0;

  return (...args: Parameters<T>) => {
    const now = Date.now();

    if (now - lastCall >= delay) {
      lastCall = now;
      fn(...args);
    }
  };
}
