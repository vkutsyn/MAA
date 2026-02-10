import axios from "axios";
import { apiClient } from "@/lib/api";

export interface QuestionOptionDto {
  optionId: string;
  optionLabel: string;
  optionValue: string;
  displayOrder: number;
}

export interface QuestionDefinitionDto {
  questionId: string;
  displayOrder: number;
  questionText: string;
  fieldType: "text" | "select" | "checkbox" | "radio" | "date" | "currency";
  isRequired: boolean;
  helpText?: string | null;
  validationRegex?: string | null;
  conditionalRuleId?: string | null;
  options?: QuestionOptionDto[] | null;
}

export interface ConditionalRuleDto {
  conditionalRuleId: string;
  ruleExpression: string;
  description?: string | null;
}

export interface GetQuestionsResponse {
  stateCode: string;
  programCode: string;
  questions: QuestionDefinitionDto[];
  conditionalRules: ConditionalRuleDto[];
}

export interface ErrorResponse {
  error: string;
  code?: string;
  timestamp?: string;
}

export async function getQuestionDefinitions(
  stateCode: string,
  programCode: string,
): Promise<GetQuestionsResponse> {
  try {
    const response = await apiClient.get<GetQuestionsResponse>(
      `/questions/${stateCode}/${programCode}`,
    );
    return response.data;
  } catch (error) {
    if (axios.isAxiosError(error) && error.response) {
      const errorResponse = error.response.data as ErrorResponse;
      throw new Error(
        errorResponse.error || "Failed to retrieve question definitions",
      );
    }
    throw error;
  }
}
