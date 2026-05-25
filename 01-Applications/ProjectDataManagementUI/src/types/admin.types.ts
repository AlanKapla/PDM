export interface AdminUserListItemWeb {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  systemRole: string;
  isActive: boolean;
  createdAt: string;
  tenantCount: number;
}

export interface AdminUserTenantMembershipWeb {
  tenantId: string;
  tenantName: string;
  roleName: string;
  joinedAt: string;
}

export interface AdminUserDetailsWeb {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  systemRole: string;
  isActive: boolean;
  createdAt: string;
  phoneNumber: string | null;
  companyName: string | null;
  taxId: string | null;
  street: string | null;
  city: string | null;
  postalCode: string | null;
  country: string | null;
  tenantMemberships: AdminUserTenantMembershipWeb[];
}

// --- Tenants ---

export interface AdminTenantListItemWeb {
  id: string;
  name: string;
  isActive: boolean;
  createdAt: string;
  memberCount: number;
  projectCount: number;
}

export interface AdminTenantProjectItemWeb {
  id: string;
  name: string;
  isActive: boolean;
  createdAt: string;
  memberCount: number;
  budgetNet: number | null;
  budgetGross: number | null;
}

export interface AdminTenantDetailsWeb {
  id: string;
  name: string;
  isActive: boolean;
  createdAt: string;
  memberCount: number;
  projects: AdminTenantProjectItemWeb[];
}

// --- Subscription Plans ---

export interface SubscriptionPlanDefinitionWeb {
  id: string;
  plan: string;
  name: string;
  maxProjects: number;
  maxUsers: number;
  price: number;
  currency: string;
  isActive: boolean;
}

export interface UpdateSubscriptionPlanRequest {
  name: string;
  maxProjects: number;
  maxUsers: number;
  price: number;
  currency: string;
  isActive: boolean;
}
