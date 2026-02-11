/**
 * Mapper to convert wizard answers to eligibility input
 */

import type { Answer, WizardSession } from '@/features/wizard/types';
import { UserEligibilityInput } from './types';

/**
 * Convert wizard session answers to eligibility input format
 * Maps session data and answers to the API contract
 */
export function mapAnswersToEligibilityInput(
  session: WizardSession,
  answers: Answer[]
): UserEligibilityInput {
  // Create a map of answers by field key for easier lookup
  const answerMap = new Map(answers.map((a) => [a.fieldKey, a]));

  // Extract values with fallbacks to session metadata if available
  const stateCode = session.stateCode || 'CA'; // Default to CA if not set
  const householdSize = getAnswerAsNumber(answerMap, 'household_size') || 1;
  const monthlyIncomeCents = getAnswerAsNumber(answerMap, 'monthly_income_cents') || 0;
  const age = getAnswerAsNumber(answerMap, 'age') || 0;
  const assetsCents = getAnswerAsNumber(answerMap, 'assets_cents');

  const hasDisability = getAnswerAsBoolean(answerMap, 'has_disability', false);
  const isPregnant = getAnswerAsBoolean(answerMap, 'is_pregnant', false);
  const receivesSsi = getAnswerAsBoolean(answerMap, 'receives_ssi', false);
  const isCitizen = getAnswerAsBoolean(answerMap, 'is_citizen', true);

  return {
    state_code: stateCode,
    household_size: Math.max(1, Math.min(8, householdSize)), // Clamp to valid range
    monthly_income_cents: Math.max(0, monthlyIncomeCents),
    age: Math.max(0, Math.min(150, age)), // Clamp to valid range
    has_disability: hasDisability,
    is_pregnant: isPregnant,
    receives_ssi: receivesSsi,
    is_citizen: isCitizen,
    assets_cents: assetsCents !== null ? Math.max(0, assetsCents) : undefined,
  };
}

/**
 * Helper to get a numeric answer value
 */
function getAnswerAsNumber(
  answerMap: Map<string, Answer>,
  fieldKey: string
): number | null {
  const answer = answerMap.get(fieldKey);
  if (!answer || !answer.answerValue) return null;

  const parsed = parseInt(answer.answerValue, 10);
  return !isNaN(parsed) ? parsed : null;
}

/**
 * Helper to get a boolean answer value
 */
function getAnswerAsBoolean(
  answerMap: Map<string, Answer>,
  fieldKey: string,
  defaultValue: boolean = false
): boolean {
  const answer = answerMap.get(fieldKey);
  if (!answer) return defaultValue;

  if (answer.answerValue) {
    const lower = answer.answerValue.toLowerCase();
    if (lower === 'true' || lower === 'yes' || lower === '1') return true;
    if (lower === 'false' || lower === 'no' || lower === '0') return false;
  }

  return defaultValue;
}

/**
 * Validate the eligibility input before sending to API
 */
export function validateEligibilityInput(input: UserEligibilityInput): string[] {
  const errors: string[] = [];

  if (!input.state_code || input.state_code.length !== 2) {
    errors.push('State code must be a valid 2-letter code');
  }

  if (input.household_size < 1 || input.household_size > 8) {
    errors.push('Household size must be between 1 and 8');
  }

  if (input.monthly_income_cents < 0) {
    errors.push('Monthly income cannot be negative');
  }

  if (input.age < 0 || input.age > 150) {
    errors.push('Age must be between 0 and 150');
  }

  if (input.assets_cents !== undefined && input.assets_cents !== null && input.assets_cents < 0) {
    errors.push('Assets cannot be negative');
  }

  return errors;
}
