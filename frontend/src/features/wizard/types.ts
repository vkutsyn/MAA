/**
 * TypeScript types for the Eligibility Wizard UI.
 * Matches backend DTOs and OpenAPI contract.
 */

// ==================== Session Types ====================

export interface CreateSessionDto {
  ipAddress?: string;
  userAgent?: string;
  timeoutMinutes?: number;
  inactivityTimeoutMinutes?: number;
}

export interface SessionDto {
  id: string;
  state: string;
  userId?: string | null;
  ipAddress: string;
  userAgent: string;
  sessionType: string;
  encryptionKeyVersion: number;
  expiresAt: string;
  inactivityTimeoutAt: string;
  lastActivityAt: string;
  isRevoked: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface SessionStatusDto {
  sessionId: string;
  isValid: boolean;
  minutesUntilExpiry?: number;
  minutesUntilInactivity?: number;
  expiresAt?: string;
  inactivityTimeoutAt?: string;
}

// ==================== Answer Types ====================

export interface SaveAnswerDto {
  fieldKey: string;
  fieldType: "currency" | "integer" | "string" | "boolean" | "date" | "text";
  answerValue: string;
  isPii: boolean;
}

export interface SessionAnswerDto {
  id: string;
  sessionId: string;
  fieldKey: string;
  fieldType: string;
  answerValue: string | null;
  isPii: boolean;
  keyVersion: number;
  validationErrors: string | null;
  createdAt: string;
  updatedAt: string;
}

// ==================== State Types ====================

export interface StateInfo {
  code: string;
  name: string;
  isPilot: boolean;
}

export interface StateLookupResponse {
  code: string;
  name: string;
  source: "zip";
}

// ==================== Question Types ====================

export interface QuestionOption {
  value: string;
  label: string;
}

export interface QuestionCondition {
  fieldKey: string;
  operator: "equals" | "not_equals" | "gt" | "gte" | "lt" | "lte" | "includes";
  value: string;
}

export interface QuestionDto {
  key: string;
  label: string;
  type:
    | "currency"
    | "integer"
    | "string"
    | "boolean"
    | "date"
    | "text"
    | "select"
    | "multiselect";
  required: boolean;
  helpText?: string;
  options?: QuestionOption[];
  conditions?: QuestionCondition[];
}

export interface QuestionSet {
  state: string;
  version: string;
  questions: QuestionDto[];
}

// ==================== Wizard State Types (Frontend-only) ====================

export interface WizardSession {
  sessionId: string;
  stateCode: string;
  stateName: string;
  currentStep: number;
  totalSteps: number;
  expiresAt: string;
}

export interface Answer {
  fieldKey: string;
  answerValue: string;
  fieldType: SaveAnswerDto["fieldType"];
  isPii: boolean;
}

export interface WizardProgress {
  currentIndex: number;
  totalSteps: number;
  canGoBack: boolean;
  canGoNext: boolean;
  completionPercent: number;
}
