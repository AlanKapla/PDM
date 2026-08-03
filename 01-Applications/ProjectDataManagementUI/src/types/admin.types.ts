export interface SendWelcomeEmailsResultWeb {
  sentCount: number;
  skippedCount: number;
}

export interface AdminUserWeb {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  isActive: boolean;
  systemRole: string;
  createdAt: string;
  welcomeEmailSentAt: string | null;
  phoneNumber: string | null;
  companyName: string | null;
  taxId: string | null;
  street: string | null;
  city: string | null;
  postalCode: string | null;
  country: string | null;
}

export interface SendColdMailsRequest {
  emails: string[];
  subject: string;
  body: string;
}

export interface ColdMailSendItemWeb {
  recipientEmail: string;
  status: string;
  errorMessage: string | null;
}

export interface SendColdMailsResultWeb {
  batchId: string;
  queuedCount: number;
  failedCount: number;
  items: ColdMailSendItemWeb[];
}

export interface ColdMailHistoryWeb {
  id: string;
  batchId: string;
  recipientEmail: string;
  subject: string;
  body: string;
  htmlBody: string;
  status: string;
  errorMessage: string | null;
  sentByUserId: string;
  sentAt: string;
}

export interface ColdMailTemplateWeb {
  htmlTemplate: string;
  appUrl: string;
  ctaLabel: string;
}
