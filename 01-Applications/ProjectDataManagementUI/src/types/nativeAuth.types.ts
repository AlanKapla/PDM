export type NativeSignInStep = "credentials" | "code" | "completed";

export interface UseNativeSignInResult {
  step: NativeSignInStep;
  email: string;
  setEmail: (value: string) => void;
  password: string;
  setPassword: (value: string) => void;
  code: string;
  setCode: (value: string) => void;
  codeLength: number | null;
  error: string | null;
  isLoading: boolean;
  /** Trwa próba cichego wznowienia sesji z cache MSAL. */
  isResuming: boolean;
  isReady: boolean;
  submitCredentials: () => Promise<void>;
  submitCode: () => Promise<void>;
  reset: () => void;
}

export type NativeSignUpStep = "details" | "password" | "code" | "attributes";

export interface UseNativeSignUpResult {
  step: NativeSignUpStep;
  firstName: string;
  setFirstName: (value: string) => void;
  lastName: string;
  setLastName: (value: string) => void;
  email: string;
  setEmail: (value: string) => void;
  password: string;
  setPassword: (value: string) => void;
  code: string;
  setCode: (value: string) => void;
  codeLength: number | null;
  error: string | null;
  isLoading: boolean;
  isReady: boolean;
  submitDetails: () => Promise<void>;
  submitPassword: () => Promise<void>;
  submitCode: () => Promise<void>;
  submitAttributes: () => Promise<void>;
  reset: () => void;
}

export type NativeResetPasswordStep = "email" | "code" | "password" | "done";

export interface UseNativeResetPasswordResult {
  step: NativeResetPasswordStep;
  email: string;
  setEmail: (value: string) => void;
  code: string;
  setCode: (value: string) => void;
  password: string;
  setPassword: (value: string) => void;
  confirmPassword: string;
  setConfirmPassword: (value: string) => void;
  codeLength: number | null;
  error: string | null;
  successMessage: string | null;
  isLoading: boolean;
  isReady: boolean;
  submitEmail: () => Promise<void>;
  submitCode: () => Promise<void>;
  submitNewPassword: () => Promise<void>;
  reset: () => void;
}
