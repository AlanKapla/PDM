import React from 'react';
import { COLOR_PALETTE } from '../utils/colors';

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
        borderBottom: `1px solid ${COLOR_PALETTE.border}`,
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
              fontSize: 13,
              fontWeight: isActive ? 500 : 400,
              color: isActive ? COLOR_PALETTE.purple600 : COLOR_PALETTE.gray600,
              background: 'none',
              border: 'none',
              borderBottom: isActive ? `2px solid ${COLOR_PALETTE.purple600}` : '2px solid transparent',
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
                background: isActive ? COLOR_PALETTE.purple600 : COLOR_PALETTE.gray100,
                color: isActive ? '#fff' : COLOR_PALETTE.gray600,
                borderRadius: 20,
                padding: '1px 7px',
                fontSize: 12,
                fontWeight: 600,
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
