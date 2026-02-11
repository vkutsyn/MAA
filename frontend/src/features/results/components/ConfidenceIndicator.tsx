/**
 * Confidence Indicator Component
 * Displays the confidence level with visual and text representation
 */

import { ConfidenceLabel } from '../types';
import { Progress } from '@/components/ui/progress';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Gauge } from 'lucide-react';

interface ConfidenceIndicatorProps {
  confidenceScore: number;
  confidenceLabel: ConfidenceLabel;
  description?: string;
}

/**
 * Get text color for confidence level
 */
function getConfidenceTextColor(score: number): string {
  if (score < 20) return 'text-red-700';
  if (score < 40) return 'text-orange-700';
  if (score < 60) return 'text-yellow-700';
  if (score < 80) return 'text-lime-700';
  return 'text-green-700';
}

export function ConfidenceIndicator({
  confidenceScore,
  confidenceLabel,
  description,
}: ConfidenceIndicatorProps) {
  return (
    <Card>
      <CardHeader>
        <div className="flex items-start justify-between">
          <div>
            <CardTitle className="text-lg">How confident are we?</CardTitle>
            <CardDescription>Confidence level in this eligibility assessment</CardDescription>
          </div>
          <Gauge className="h-5 w-5 text-gray-600" />
        </div>
      </CardHeader>
      <CardContent className="space-y-4">
        {/* Confidence Score Gauge */}
        <div className="space-y-2">
          <div className="flex items-end justify-between">
            <span className="text-sm font-semibold text-gray-700">Score</span>
            <span className={`text-2xl font-bold ${getConfidenceTextColor(confidenceScore)}`}>
              {confidenceScore}%
            </span>
          </div>
          <Progress value={confidenceScore} className="h-2" />
        </div>

        {/* Confidence Level Label */}
        <div className="rounded-lg bg-gray-50 p-3">
          <p className="text-xs font-semibold text-gray-600 uppercase tracking-wider">
            Confidence Level
          </p>
          <p className={`text-lg font-bold ${getConfidenceTextColor(confidenceScore)}`}>
            {confidenceLabel.label}
          </p>
          <p className="text-xs text-gray-500 mt-1">Range: {confidenceLabel.range}</p>
        </div>

        {/* Interpretation */}
        <div className="space-y-2 text-sm">
          <p className="font-semibold text-gray-900">What this means:</p>
          <p className="text-gray-700 leading-relaxed">
            {description || confidenceLabel.description}
          </p>
        </div>

        {/* Disclaimer */}
        <div className="text-xs text-gray-500 border-t pt-3 mt-3">
          <p>
            This confidence level reflects the accuracy of our assessment based on available
            information. Official eligibility determination requires verification of all
            information by your state's Medicaid agency.
          </p>
        </div>
      </CardContent>
    </Card>
  );
}
