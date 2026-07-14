import { useQuery } from '@tanstack/react-query';
import { getUserTenants, getActiveInvitations } from '../../services/tenantService';
import type { UserTenant, TenantInvitationWeb } from '../../types/auth.types';

export const tenantKeys = {
  all: ['tenants'] as const,
  my: () => ['tenants', 'my'] as const,
  invitations: () => ['tenants', 'invitations'] as const,
};

export function useMyTenants(enabled: boolean = true) {
  return useQuery<UserTenant[]>({
    queryKey: tenantKeys.my(),
    queryFn: getUserTenants,
    enabled,
  });
}

export function useActiveInvitations(options?: {
  refetchInterval?: number;
  refetchIntervalInBackground?: boolean;
}) {
  return useQuery<TenantInvitationWeb[]>({
    queryKey: tenantKeys.invitations(),
    queryFn: getActiveInvitations,
    refetchInterval: options?.refetchInterval,
    refetchIntervalInBackground: options?.refetchIntervalInBackground ?? false,
  });
}
