import type { CostDocumentType } from '../types/ai.types';

export type AICostReviewContext = 'costs' | 'dashboard';

export function getCostDocumentTypeForContext(context: AICostReviewContext): CostDocumentType {
  return context === 'dashboard' ? 'TrackedCost' : 'ProjectCost';
}

export function getAICostReviewPath(projectId: string, context: AICostReviewContext): string {
  if (context === 'dashboard') {
    return `/projects/${projectId}/dashboard/ai-review`;
  }
  return `/projects/${projectId}/costs/ai-review`;
}

export function getAICostReviewBackPath(projectId: string, context: AICostReviewContext): string {
  if (context === 'dashboard') {
    return `/projects/${projectId}/dashboard`;
  }
  return `/projects/${projectId}/costs`;
}

export function getAICostReviewBackLabel(context: AICostReviewContext): string {
  if (context === 'dashboard') {
    return 'Powrót do dashboardu';
  }
  return 'Powrót do wydatków';
}

export function detectAICostReviewContext(pathname: string): AICostReviewContext {
  if (pathname.includes('/dashboard/ai-review')) {
    return 'dashboard';
  }
  return 'costs';
}
