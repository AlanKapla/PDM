const EMPTY_TENANT_ID = "00000000-0000-0000-0000-000000000000";

export function hasActiveTenant(activeTenantId: string | null | undefined): boolean {
  return Boolean(
    activeTenantId &&
    activeTenantId !== EMPTY_TENANT_ID &&
    activeTenantId.trim() !== ""
  );
}
