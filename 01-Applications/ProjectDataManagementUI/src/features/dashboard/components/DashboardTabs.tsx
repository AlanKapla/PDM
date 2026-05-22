import React from 'react';
import { useToken } from '@chakra-ui/react';

export type DashboardTab = 'estimates' | 'schedules' | 'additional' | 'all';

export interface DashboardTabsProps {
  activeTab: DashboardTab;
  onTabChange: (tab: DashboardTab) => void;
  estimatesCount: number;
  schedulesCount: number;
  additionalCount: number;
  allCostsCount: number;
}

const tabs: Array<{ key: DashboardTab; label: string; countKey: keyof DashboardTabsProps }> = [
  { key: 'estimates', label: 'Kosztorysy', countKey: 'estimatesCount' },
  { key: 'schedules', label: 'Harmonogramy', countKey: 'schedulesCount' },
  { key: 'additional', label: 'Koszty główne', countKey: 'additionalCount' },
  { key: 'all', label: 'Wszystkie koszty', countKey: 'allCostsCount' },
];

/** Pasek zakładek dashboardu z licznikami. */
export function DashboardTabs({
  activeTab,
  onTabChange,
  estimatesCount,
  schedulesCount,
  additionalCount,
  allCostsCount,
}: DashboardTabsProps): React.ReactElement {
  const [neutral200, level2600, neutral600, neutral100] = useToken('colors', [
    'neutral.200', 'level2.600', 'neutral.600', 'neutral.100',
  ]);

  const counts: Record<DashboardTab, number> = {
    estimates: estimatesCount,
    schedules: schedulesCount,
    additional: additionalCount,
    all: allCostsCount,
  };

  return (
    <div
      className="dashboard-tabs-bar"
      style={{
        display: 'flex',
        gap: 0,
        borderBottom: `1px solid ${neutral200}`,
        marginBottom: 16,
      }}
    >
      {tabs.map(({ key, label }) => {
        const isActive = activeTab === key;
        return (
          <button
            key={key}
            onClick={() => onTabChange(key)}
            className="dashboard-tab-btn"
            style={{
              padding: '10px 16px',
              fontSize: "sm",
              fontWeight: isActive ? 500 : 400,
              color: isActive ? level2600 : neutral600,
              background: 'none',
              border: 'none',
              borderBottom: isActive ? `2px solid ${level2600}` : '2px solid transparent',
              cursor: 'pointer',
              display: 'flex',
              alignItems: 'center',
              gap: 6,
              marginBottom: -1,
            }}
          >
            {label}
            <span
              style={{
                background: isActive ? level2600 : neutral100,
                color: isActive ? '#fff' : neutral600,
                borderRadius: 20,
                padding: '1px 7px',
                fontSize: "xs",
                fontWeight: "semibold",
              }}
            >
              {counts[key]}
            </span>
          </button>
        );
      })}
    </div>
  );
}

export default DashboardTabs;
