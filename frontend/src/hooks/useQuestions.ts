import { useQuery } from "@tanstack/react-query";
import { getQuestionDefinitions } from "@/services/questionService";
import type { GetQuestionsResponse } from "@/services/questionService";

export const questionKeys = {
  all: ["questionDefinitions"] as const,
  byStateProgram: (stateCode: string, programCode: string) =>
    [...questionKeys.all, stateCode, programCode] as const,
};

export function useQuestions(
  stateCode: string | undefined,
  programCode: string | undefined,
  options?: {
    enabled?: boolean;
    retry?: boolean | number;
  },
) {
  const isEnabled =
    (options?.enabled ?? true) && !!stateCode && !!programCode;

  return useQuery<GetQuestionsResponse, Error>({
    queryKey: questionKeys.byStateProgram(stateCode || "", programCode || ""),
    queryFn: () => {
      if (!stateCode || !programCode) {
        throw new Error("State code and program code are required");
      }
      return getQuestionDefinitions(stateCode, programCode);
    },
    enabled: isEnabled,
    retry: options?.retry ?? 1,
    staleTime: 24 * 60 * 60 * 1000,
  });
}
