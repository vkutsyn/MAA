/**
 * Explanation List Component
 * Displays explanation bullets with icons and formatting
 */

import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Alert, AlertDescription } from '@/components/ui/alert';
import { Lightbulb, CheckCircle2, AlertCircle } from 'lucide-react';

interface ExplanationListProps {
  explanation: string;
  bullets: string[];
}

/**
 * Get icon and color based on bullet content
 */
function getBulletIcon(bullet: string) {
  const lowerBullet = bullet.toLowerCase();

  if (
    lowerBullet.includes('income') ||
    lowerBullet.includes('asset') ||
    lowerBullet.includes('meet') ||
    lowerBullet.includes('qualify') ||
    lowerBullet.includes('eligible')
  ) {
    return { Icon: CheckCircle2, color: 'text-green-600' };
  }

  if (
    lowerBullet.includes('exceed') ||
    lowerBullet.includes('exceed') ||
    lowerBullet.includes('above') ||
    lowerBullet.includes('too') ||
    lowerBullet.includes('ineligible')
  ) {
    return { Icon: AlertCircle, color: 'text-yellow-600' };
  }

  return { Icon: Lightbulb, color: 'text-blue-600' };
}

export function ExplanationList({ explanation, bullets }: ExplanationListProps) {
  const hasBullets = bullets && bullets.length > 0;

  return (
    <Card>
      <CardHeader>
        <div className="flex items-start justify-between">
          <div>
            <CardTitle className="text-lg">Why this result?</CardTitle>
            <CardDescription>Key factors in the eligibility determination</CardDescription>
          </div>
          <Lightbulb className="h-5 w-5 text-gray-600" />
        </div>
      </CardHeader>
      <CardContent className="space-y-4">
        {/* Main Explanation */}
        {explanation && (
          <Alert>
            <AlertDescription className="text-sm text-gray-700">{explanation}</AlertDescription>
          </Alert>
        )}

        {/* Explanation Bullets */}
        {hasBullets ? (
          <div className="space-y-2">
            <p className="text-sm font-semibold text-gray-900">Key factors:</p>
            <ul className="space-y-2">
              {bullets.map((bullet, idx) => {
                const { Icon, color } = getBulletIcon(bullet);

                return (
                  <li key={idx} className="flex gap-3">
                    <Icon className={`h-5 w-5 flex-shrink-0 ${color} mt-0.5`} />
                    <span className="text-sm text-gray-700">{bullet}</span>
                  </li>
                );
              })}
            </ul>
          </div>
        ) : (
          <div className="rounded-lg bg-gray-50 p-3">
            <p className="text-sm text-gray-600">
              No additional details available for this assessment.
            </p>
          </div>
        )}

        {/* Note about personalization */}
        <div className="text-xs text-gray-500 border-t pt-3 mt-3">
          <p>
            The factors shown above are based on the information you provided. For official
            explanations and next steps, please contact your state Medicaid agency.
          </p>
        </div>
      </CardContent>
    </Card>
  );
}
