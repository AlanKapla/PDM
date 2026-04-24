import React from 'react';
import { Badge } from './Badge';
import { TimelineStatus } from '../../types/projectDashboard.types';
import { TIMELINE_STATUS_CONFIG } from '../../utils/formatters';

export interface TimelineStatusBadgeProps {
  status: TimelineStatus;
  small?: boolean;
}

/** Badge dla statusu harmonogramu — konfiguracja z TIMELINE_STATUS_CONFIG. */
export function TimelineStatusBadge({ status, small }: TimelineStatusBadgeProps): React.ReactElement {
  const config = TIMELINE_STATUS_CONFIG(status);
  return <Badge text={config.label} bg={config.bg} color={config.color} small={small} />;
}

export default TimelineStatusBadge;
