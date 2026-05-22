import React, { createContext, useContext } from 'react';

const DashboardCurrencyContext = createContext<string>('zł');

export interface DashboardCurrencyProviderProps {
  children: React.ReactNode;
  currencySymbol: string;
}

export function DashboardCurrencyProvider({
  children,
  currencySymbol,
}: DashboardCurrencyProviderProps): React.ReactElement {
  return (
    <DashboardCurrencyContext.Provider value={currencySymbol}>
      {children}
    </DashboardCurrencyContext.Provider>
  );
}

export const useDashboardCurrency = (): string =>
  useContext(DashboardCurrencyContext);
