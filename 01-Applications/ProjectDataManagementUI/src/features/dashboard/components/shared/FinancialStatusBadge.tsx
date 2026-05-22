import React from 'react';
import { Badge } from './Badge';
import { FinancialStatus } from '../../types/projectDashboard.types';
import { FINANCIAL_STATUS_CONFIG } from '../../utils/formatters';

export interface FinancialStatusBadgeProps {
  status: FinancialStatus;
  small?: boolean;
}

/** Badge dla statusu finansowego — konfiguracja z FINANCIAL_STATUS_CONFIG. */
export function FinancialStatusBadge({ status, small }: FinancialStatusBadgeProps): React.ReactElement {
  const config = FINANCIAL_STATUS_CONFIG(status);
  return <Badge text={config.label} bg={config.bg} color={config.color} small={small} />;
}

export default FinancialStatusBadge;
