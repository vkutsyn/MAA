/**
 * State Context types and interfaces
 * Feature: State Context Initialization Step
 */

/**
 * State Context entity - represents the Medicaid jurisdiction context for a session
 */
export interface StateContext {
  id: string;
  sessionId: string;
  stateCode: string;
  stateName: string;
  zipCode: string;
  isManualOverride: boolean;
  effectiveDate: string; // ISO 8601 date string
  createdAt: string;
  updatedAt?: string;
}

/**
 * State Configuration entity - state-specific Medicaid program configuration
 */
export interface StateConfiguration {
  stateCode: string;
  stateName: string;
  medicaidProgramName: string;
  contactInfo: ContactInfo;
  eligibilityThresholds: EligibilityThresholds;
  requiredDocuments: string[];
  additionalNotes?: string;
}

/**
 * Contact information for state Medicaid program
 */
export interface ContactInfo {
  phone: string;
  website: string;
  applicationUrl: string;
}

/**
 * Eligibility thresholds for state Medicaid program
 */
export interface EligibilityThresholds {
  fplPercentages: FplPercentages;
  assetLimits: AssetLimits;
}

/**
 * Federal Poverty Level percentages by category
 */
export interface FplPercentages {
  adults: number;
  children: number;
  pregnant: number;
}

/**
 * Asset limits for eligibility
 */
export interface AssetLimits {
  individual: number;
  couple: number;
}

/**
 * Request to initialize state context from ZIP code
 */
export interface InitializeStateContextRequest {
  sessionId: string;
  zipCode: string;
  stateCodeOverride?: string;
}

/**
 * Response from initializing state context
 */
export interface StateContextResponse {
  stateContext: StateContext;
  stateConfiguration: StateConfiguration;
}

/**
 * Request to update state context (manual override)
 */
export interface UpdateStateContextRequest {
  sessionId: string;
  stateCode: string;
  zipCode: string;
  isManualOverride: boolean;
}

/**
 * Request to validate a ZIP code
 */
export interface ValidateZipRequest {
  zipCode: string;
}

/**
 * Response from ZIP code validation
 */
export interface ValidateZipResponse {
  isValid: boolean;
  stateCode?: string;
  errorMessage?: string;
}

/**
 * API error response
 */
export interface ErrorResponse {
  error: string;
  message: string;
  details?: ErrorDetail[];
}

/**
 * Error detail item
 */
export interface ErrorDetail {
  field: string;
  message: string;
}
