/**
 * Eligibility Status Card Component
 * Displays the overall eligibility status with badge and basic information
 */

import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { OverallStatus, EligibilityResultView } from '../types';
import { AlertCircle, CheckCircle2, HelpCircle } from 'lucide-react';

interface EligibilityStatusCardProps {
  result: EligibilityResultView;
}

/**
 * Get status color and icon based on overall status
 */
function getStatusDisplay(status: OverallStatus) {
  switch (status) {
    case 'Likely Eligible':
      return {
        color: 'bg-green-100 text-green-800',
        borderColor: 'border-green-200',
        icon: CheckCircle2,
        label: 'Likely Eligible',
      };
    case 'Possibly Eligible':
      return {
        color: 'bg-yellow-100 text-yellow-800',
        borderColor: 'border-yellow-200',
        icon: HelpCircle,
        label: 'Possibly Eligible',
      };
    case 'Unlikely Eligible':
      return {
        color: 'bg-red-100 text-red-800',
        borderColor: 'border-red-200',
        icon: AlertCircle,
        label: 'Unlikely Eligible',
      };
    default:
      return {
        color: 'bg-gray-100 text-gray-800',
        borderColor: 'border-gray-200',
        icon: HelpCircle,
        label: 'Unknown',
      };
  }
}

/**
 * Format date string to readable format
 */
function formatDate(dateString: string): string {
  try {
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'long',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  } catch {
    return dateString;
  }
}

export function EligibilityStatusCard({ result }: EligibilityStatusCardProps) {
  const statusDisplay = getStatusDisplay(result.overallStatus);
  const StatusIcon = statusDisplay.icon;

  return (
    <Card className={`border-2 ${statusDisplay.borderColor}`}>
      <CardHeader className="pb-3">
        <div className="flex items-start justify-between">
          <div>
            <CardTitle className="text-2xl font-bold">Eligibility Status</CardTitle>
            <CardDescription>Based on current information</CardDescription>
          </div>
          <StatusIcon className="h-6 w-6 text-gray-600" />
        </div>
      </CardHeader>
      <CardContent className="space-y-4">
        {/* Status Badge */}
        <div className="flex items-center gap-2">
          <Badge className={`text-sm font-semibold px-3 py-1 ${statusDisplay.color}`}>
            {statusDisplay.label}
          </Badge>
        </div>

        {/* Status Explanation */}
        <div className="p-3 rounded-lg bg-gray-50">
          <p className="text-sm text-gray-700 leading-relaxed">{result.explanation}</p>
        </div>

        {/* Confidence Score */}
        <div className="grid grid-cols-2 gap-4">
          <div>
            <p className="text-sm font-semibold text-gray-600">Confidence Level</p>
            <p className="text-lg font-bold text-gray-900">{result.confidenceLabel.label}</p>
            <p className="text-xs text-gray-500">{result.confidenceScore}%</p>
          </div>
          <div>
            <p className="text-sm font-semibold text-gray-600">Evaluation Date</p>
            <p className="text-xs text-gray-900">{formatDate(result.evaluationDate)}</p>
            {result.stateCode && (
              <p className="text-xs text-gray-500 mt-1">State: {result.stateCode}</p>
            )}
          </div>
        </div>

        {/* Metadata */}
        {result.evaluationDurationMs && (
          <div className="text-xs text-gray-500 border-t pt-2">
            Evaluated in {result.evaluationDurationMs}ms
          </div>
        )}
      </CardContent>
    </Card>
  );
}
