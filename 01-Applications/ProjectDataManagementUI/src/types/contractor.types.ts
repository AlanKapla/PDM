export interface ContractorWeb {
  id: string;
  tenantId: string;
  name: string;
  taxId: string | null;
  email: string | null;
  phoneNumber: string | null;
  street: string | null;
  city: string | null;
  postalCode: string | null;
  country: string | null;
  notes: string | null;
  createdAt: string;
  updatedAt: string | null;
}

export interface ContractorListItemWeb {
  id: string;
  name: string;
  taxId: string | null;
  city: string | null;
}

export interface CreateContractorRequest {
  name: string;
  taxId?: string | null;
  email?: string | null;
  phoneNumber?: string | null;
  street?: string | null;
  city?: string | null;
  postalCode?: string | null;
  country?: string | null;
  notes?: string | null;
}

export interface UpdateContractorRequest extends CreateContractorRequest {
  id: string;
}
