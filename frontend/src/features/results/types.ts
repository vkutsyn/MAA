/**
 * Types and interfaces for the Eligibility Results feature
 */

/**
 * API DTOs - matching the backend contract
 */

export interface UserEligibilityInput {
  state_code: string;
  household_size: number;
  monthly_income_cents: number;
  age: number;
  has_disability?: boolean;
  is_pregnant?: boolean;
  receives_ssi?: boolean;
  is_citizen: boolean;
  assets_cents?: number | null;
}

export interface EligibilityResultDto {
  evaluationDate: string;
  overallStatus: OverallStatus;
  confidenceScore: number;
  explanation: string;
  matchedPrograms: ProgramMatchDto[];
  failedProgramEvaluations?: ProgramMatchDto[];
  ruleVersionUsed: number | null;
  stateCode: string;
  evaluationDurationMs?: number;
  userInputSummary?: string | null;
}

export interface ProgramMatchDto {
  programId: string;
  programName: string;
  eligibilityStatus?: OverallStatus | null;
  confidenceScore: number;
  explanation?: string | null;
  matchingFactors?: string[];
  disqualifyingFactors?: string[];
  ruleVersionUsed?: number | null;
  evaluatedAt?: string | null;
  eligibilityPathway?: EligibilityPathway | null;
}

export type OverallStatus = 'Likely Eligible' | 'Possibly Eligible' | 'Unlikely Eligible';

export type EligibilityPathway =
  | 'MAGI'
  | 'NonMAGI_Aged'
  | 'NonMAGI_Disabled'
  | 'SSI_Linked'
  | 'Pregnancy'
  | 'Other';

/**
 * Confidence Label - derived from score
 */
export interface ConfidenceLabel {
  label: string;
  range: string;
  description: string;
}

/**
 * View Models - for UI rendering
 */

export interface EligibilityResultView {
  evaluationDate: string;
  overallStatus: OverallStatus;
  confidenceScore: number;
  confidenceLabel: ConfidenceLabel;
  confidenceDescription: string;
  explanation: string;
  explanationBullets: string[];
  matchedPrograms: ProgramMatchView[];
  stateCode: string;
  ruleVersionUsed: number | null;
  evaluationDurationMs?: number;
}

export interface ProgramMatchView {
  programId: string;
  programName: string;
  eligibilityStatus?: OverallStatus | null;
  confidenceScore: number;
  confidenceLabel: ConfidenceLabel;
  explanation?: string | null;
  matchingFactors: string[];
  disqualifyingFactors: string[];
}

/**
 * Confidence thresholds for label derivation
 */
export const CONFIDENCE_THRESHOLDS = {
  UNCERTAIN: { min: 0, max: 19, label: 'Uncertain', description: 'Unable to determine eligibility' },
  LOW: { min: 20, max: 39, label: 'Low confidence', description: 'Eligibility is unlikely' },
  SOME: { min: 40, max: 59, label: 'Some confidence', description: 'Eligibility is possible' },
  HIGH: { min: 60, max: 79, label: 'High confidence', description: 'Eligibility is likely' },
  VERY_HIGH: { min: 80, max: 100, label: 'Very confident', description: 'Eligibility is very likely' },
} as const;

/**
 * Get confidence label from score
 */
export function getConfidenceLabel(score: number): ConfidenceLabel {
  if (score < 20) {
    return {
      label: CONFIDENCE_THRESHOLDS.UNCERTAIN.label,
      range: '0-19',
      description: CONFIDENCE_THRESHOLDS.UNCERTAIN.description,
    };
  } else if (score < 40) {
    return {
      label: CONFIDENCE_THRESHOLDS.LOW.label,
      range: '20-39',
      description: CONFIDENCE_THRESHOLDS.LOW.description,
    };
  } else if (score < 60) {
    return {
      label: CONFIDENCE_THRESHOLDS.SOME.label,
      range: '40-59',
      description: CONFIDENCE_THRESHOLDS.SOME.description,
    };
  } else if (score < 80) {
    return {
      label: CONFIDENCE_THRESHOLDS.HIGH.label,
      range: '60-79',
      description: CONFIDENCE_THRESHOLDS.HIGH.description,
    };
  } else {
    return {
      label: CONFIDENCE_THRESHOLDS.VERY_HIGH.label,
      range: '80-100',
      description: CONFIDENCE_THRESHOLDS.VERY_HIGH.description,
    };
  }
}

/**
 * Map API DTO to UI View Model
 */
export function mapDtoToViewModel(dto: EligibilityResultDto): EligibilityResultView {
  const confidenceLabel = getConfidenceLabel(dto.confidenceScore);

  return {
    evaluationDate: dto.evaluationDate,
    overallStatus: dto.overallStatus,
    confidenceScore: dto.confidenceScore,
    confidenceLabel,
    confidenceDescription: confidenceLabel.description,
    explanation: dto.explanation,
    explanationBullets: parseExplanationBullets(dto.explanation),
    matchedPrograms: dto.matchedPrograms.map((program) => ({
      programId: program.programId,
      programName: program.programName,
      eligibilityStatus: program.eligibilityStatus || undefined,
      confidenceScore: program.confidenceScore,
      confidenceLabel: getConfidenceLabel(program.confidenceScore),
      explanation: program.explanation || undefined,
      matchingFactors: program.matchingFactors || [],
      disqualifyingFactors: program.disqualifyingFactors || [],
    })),
    stateCode: dto.stateCode,
    ruleVersionUsed: dto.ruleVersionUsed,
    evaluationDurationMs: dto.evaluationDurationMs,
  };
}

/**
 * Parse explanation text into bullet points
 * Splits on newlines and filters empty lines
 */
export function parseExplanationBullets(explanation: string): string[] {
  if (!explanation) return [];
  return explanation
    .split('\n')
    .map((line) => line.trim())
    .filter((line) => line.length > 0)
    .slice(0, 5); // Limit to 5 bullets
}

/**
 * API Response wrapper for error handling
 */
export interface ApiErrorResponse {
  errors?: Array<{
    field?: string;
    message: string;
  }>;
  message?: string;
}
