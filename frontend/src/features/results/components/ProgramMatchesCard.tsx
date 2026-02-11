/**
 * Program Matches Card Component
 * Displays the list of matched programs with eligibility status and confidence
 */

import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Alert, AlertDescription } from '@/components/ui/alert';
import { EligibilityResultView, OverallStatus } from '../types';
import { CheckCircle2, AlertCircle, HelpCircle, Package } from 'lucide-react';

interface ProgramMatchesCardProps {
  result: EligibilityResultView;
}

/**
 * Get status badge styling
 */
function getStatusBadgeClass(status?: OverallStatus): string {
  switch (status) {
    case 'Likely Eligible':
      return 'bg-green-100 text-green-800';
    case 'Possibly Eligible':
      return 'bg-yellow-100 text-yellow-800';
    case 'Unlikely Eligible':
      return 'bg-red-100 text-red-800';
    default:
      return 'bg-gray-100 text-gray-800';
  }
}

/**
 * Get status icon
 */
function getStatusIcon(status?: OverallStatus) {
  switch (status) {
    case 'Likely Eligible':
      return CheckCircle2;
    case 'Possibly Eligible':
      return HelpCircle;
    case 'Unlikely Eligible':
      return AlertCircle;
    default:
      return Package;
  }
}

/**
 * Format factors into readable list
 */
function renderFactorsList(factors: string[]): React.ReactNode {
  if (factors.length === 0) return null;

  return (
    <ul className="text-xs space-y-1 mt-2">
      {factors.map((factor, idx) => (
        <li key={idx} className="flex items-start gap-2">
          <span className="text-blue-600 font-bold mt-0.5">•</span>
          <span className="text-gray-700">{factor}</span>
        </li>
      ))}
    </ul>
  );
}

export function ProgramMatchesCard({ result }: ProgramMatchesCardProps) {
  const hasMatches = result.matchedPrograms.length > 0;

  return (
    <Card>
      <CardHeader>
        <div className="flex items-start justify-between">
          <div>
            <CardTitle className="text-lg">Matched Programs</CardTitle>
            <CardDescription>
              {hasMatches
                ? `You may be eligible for ${result.matchedPrograms.length} program(s)`
                : 'No programs matched your profile'}
            </CardDescription>
          </div>
          <Package className="h-5 w-5 text-gray-600" />
        </div>
      </CardHeader>
      <CardContent className="space-y-4">
        {!hasMatches ? (
          <Alert>
            <AlertCircle className="h-4 w-4" />
            <AlertDescription>
              Based on your responses, you may not be eligible for any programs at this time.
              You can still apply directly with your state&apos;s Medicaid agency or contact a
              local benefits counselor for more information.
            </AlertDescription>
          </Alert>
        ) : (
          <div className="space-y-3">
            {result.matchedPrograms.map((program) => {
              const StatusIcon = getStatusIcon(program.eligibilityStatus ?? undefined);

              return (
                <div
                  key={program.programId}
                  className="border rounded-lg p-4 hover:bg-gray-50 transition-colors"
                >
                  {/* Program Header */}
                  <div className="flex items-start justify-between mb-2">
                    <div className="flex items-start gap-2 flex-1">
                      <StatusIcon className="h-5 w-5 mt-0.5 flex-shrink-0 text-gray-600" />
                      <div className="flex-1">
                        <h4 className="font-semibold text-sm text-gray-900">
                          {program.programName}
                        </h4>
                        {program.eligibilityStatus && (
                          <Badge className={`mt-1 text-xs ${getStatusBadgeClass(program.eligibilityStatus)}`}>
                            {program.eligibilityStatus}
                          </Badge>
                        )}
                      </div>
                    </div>
                    <div className="text-right flex-shrink-0">
                      <p className="text-xs font-semibold text-gray-600">Confidence</p>
                      <p className="text-sm font-bold text-gray-900">{program.confidenceScore}%</p>
                    </div>
                  </div>

                  {/* Program Details */}
                  {(program.explanation ||
                    program.matchingFactors.length > 0 ||
                    program.disqualifyingFactors.length > 0) && (
                    <div className="space-y-2 text-xs">
                      {program.explanation && (
                        <p className="text-gray-700">{program.explanation}</p>
                      )}

                      {program.matchingFactors.length > 0 && (
                        <div>
                          <p className="font-semibold text-green-700 mb-1">Matching factors:</p>
                          {renderFactorsList(program.matchingFactors)}
                        </div>
                      )}

                      {program.disqualifyingFactors.length > 0 && (
                        <div>
                          <p className="font-semibold text-red-700 mb-1">Potential barriers:</p>
                          {renderFactorsList(program.disqualifyingFactors)}
                        </div>
                      )}
                    </div>
                  )}
                </div>
              );
            })}
          </div>
        )}

        {/* Footer info */}
        <div className="text-xs text-gray-500 border-t pt-3 mt-4">
          <p>
            These results are estimates based on your responses. Contact your state Medicaid
            agency for official eligibility determination.
          </p>
        </div>
      </CardContent>
    </Card>
  );
}
