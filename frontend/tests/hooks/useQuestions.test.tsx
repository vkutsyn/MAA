import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { renderHook, waitFor } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { useQuestions } from "@/hooks/useQuestions";
import { getQuestionDefinitions } from "@/services/questionService";
import type { ReactNode } from "react";

vi.mock("@/services/questionService", () => ({
  getQuestionDefinitions: vi.fn(),
}));

const mockedGetQuestionDefinitions = vi.mocked(getQuestionDefinitions);

function createWrapper() {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
    },
  });

  return function Wrapper({ children }: { children: ReactNode }) {
    return (
      <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
    );
  };
}

afterEach(() => {
  mockedGetQuestionDefinitions.mockReset();
});

describe("useQuestions", () => {
  it("fetches question definitions for valid state/program", async () => {
    const response = {
      stateCode: "CA",
      programCode: "MEDI-CAL",
      questions: [],
      conditionalRules: [],
    };

    mockedGetQuestionDefinitions.mockResolvedValue(response);

    const { result } = renderHook(
      () => useQuestions("CA", "MEDI-CAL"),
      { wrapper: createWrapper() },
    );

    await waitFor(() => expect(result.current.isSuccess).toBe(true));

    expect(result.current.data).toEqual(response);
    expect(mockedGetQuestionDefinitions).toHaveBeenCalledWith("CA", "MEDI-CAL");
  });

  it("does not run when state or program is missing", async () => {
    const { result } = renderHook(() => useQuestions(undefined, "MEDI-CAL"), {
      wrapper: createWrapper(),
    });

    await waitFor(() => expect(result.current.isFetching).toBe(false));

    expect(result.current.data).toBeUndefined();
    expect(mockedGetQuestionDefinitions).not.toHaveBeenCalled();
  });
});
