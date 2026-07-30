import { useCallback, useEffect, useState } from "react";
import {
  SignInCodeRequiredState,
  SignInPasswordRequiredState,
  type AuthFlowStateBase,
  type CustomAuthAccountData,
  type ICustomAuthPublicClientApplication,
} from "@azure/msal-browser/custom-auth";
import { getCustomAuthClient } from "../auth/customAuthInstance";
import { finalizeNativeSession } from "../auth/finalizeNativeSession";
import { consumePendingLoginError } from "../auth/pendingLoginError";
import { getRememberedSignInEmail, isSoftLoggedOut } from "../auth/rememberedSignIn";
import { tryResumeNativeSession } from "../auth/tryResumeNativeSession";
import { withTimeout } from "../auth/withTimeout";
import { nativeSignInScopes } from "../config/customAuthConfig";
import type { NativeSignInStep, UseNativeSignInResult } from "../types/nativeAuth.types";

function mapSignInError(error: {
  isUserNotFound?: () => boolean;
  isInvalidUsername?: () => boolean;
  isPasswordIncorrect?: () => boolean;
  isInvalidPassword?: () => boolean;
  isInvalidCode?: () => boolean;
  isRedirectRequired?: () => boolean;
  errorData?: { errorDescription?: string; errorCodes?: number[] };
} | null | undefined): string {
  if (!error) {
    return "Wystąpił nieoczekiwany błąd logowania.";
  }
  if (error.isUserNotFound?.()) {
    return "Nie znaleziono konta z tym adresem e-mail.";
  }
  if (error.isInvalidUsername?.()) {
    return "Nieprawidłowy adres e-mail.";
  }
  if (
    error.isPasswordIncorrect?.() ||
    error.isInvalidPassword?.() ||
    error.errorData?.errorCodes?.includes(50126)
  ) {
    return "Nieprawidłowe hasło.";
  }
  if (error.isInvalidCode?.()) {
    return "Nieprawidłowy lub wygasły kod weryfikacyjny.";
  }
  if (error.isRedirectRequired?.()) {
    return "Ten typ logowania nie jest obsługiwany. Użyj e-maila i hasła.";
  }
  return error.errorData?.errorDescription ?? "Nie udało się zalogować.";
}

function resolveInitialEmail(client: ICustomAuthPublicClientApplication): string {
  const active = client.getActiveAccount();
  if (active?.username) {
    return active.username;
  }
  const accounts = client.getAllAccounts();
  if (accounts[0]?.username) {
    return accounts[0].username;
  }
  return getRememberedSignInEmail() ?? "";
}

/** Duck-typed — unika `instanceof` i type-predicate narrowing (`never` w tsc). */
interface SignInFlowResult {
  state: AuthFlowStateBase;
  data?: CustomAuthAccountData;
  error?: Parameters<typeof mapSignInError>[0];
  isFailed(): boolean;
  isCompleted(): boolean;
  isCodeRequired?(): boolean;
  isPasswordRequired?(): boolean;
  isMfaRequired?(): boolean;
  isAuthMethodRegistrationRequired?(): boolean;
}

function readStateType(state: AuthFlowStateBase): string {
  const typed = state as AuthFlowStateBase & { stateType?: string };
  return typed.stateType ?? "";
}

async function applySignInResult(
  result: SignInFlowResult,
  password: string,
  setSignInState: (state: AuthFlowStateBase | null) => void,
  setCodeLength: (value: number | null) => void,
  setStep: (step: NativeSignInStep) => void,
  setError: (value: string | null) => void,
  onCompleted: (data: CustomAuthAccountData | undefined) => Promise<void>
): Promise<void> {
  const stateType: string = readStateType(result.state);

  if (result.isCompleted() || stateType === "SignInCompletedState") {
    await onCompleted(result.data);
    return;
  }

  if (result.isFailed() || stateType === "SignInFailedState") {
    setError(mapSignInError(result.error));
    return;
  }

  if (result.isCodeRequired?.() || stateType === "SignInCodeRequiredState") {
    const codeState = result.state as SignInCodeRequiredState;
    setSignInState(codeState);
    setCodeLength(codeState.getCodeLength());
    setStep("code");
    return;
  }

  if (result.isPasswordRequired?.() || stateType === "SignInPasswordRequiredState") {
    const passwordState = result.state as SignInPasswordRequiredState;
    if (password) {
      const passwordResult = await passwordState.submitPassword(password);
      await applySignInResult(
        passwordResult,
        password,
        setSignInState,
        setCodeLength,
        setStep,
        setError,
        onCompleted
      );
      return;
    }
    setError("Podaj hasło.");
    setStep("credentials");
    return;
  }

  if (
    result.isMfaRequired?.() ||
    result.isAuthMethodRegistrationRequired?.() ||
    stateType === "MfaAwaitingState" ||
    stateType === "AuthMethodRegistrationRequiredState"
  ) {
    setError(
      "To konto wymaga dodatkowego kroku weryfikacji w Entra (MFA / rejestracja metody). Skontaktuj się z administratorem."
    );
    return;
  }

  setError(
    stateType
      ? `Nieobsługiwany krok logowania (${stateType}). Spróbuj ponownie.`
      : "Nieobsługiwany krok logowania. Spróbuj ponownie."
  );
}

export function useNativeSignIn(): UseNativeSignInResult {
  const [authClient, setAuthClient] = useState<ICustomAuthPublicClientApplication | null>(null);
  const [step, setStep] = useState<NativeSignInStep>("credentials");
  const [signInState, setSignInState] = useState<AuthFlowStateBase | null>(null);
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [code, setCode] = useState("");
  const [codeLength, setCodeLength] = useState<number | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [isResuming, setIsResuming] = useState(true);

  const completeSignIn = useCallback(
    async (data: CustomAuthAccountData | undefined): Promise<void> => {
      // Pełny reload — Native Auth nie emituje LOGIN_SUCCESS, więc msal-react
      // po SPA navigate nadal widzi isAuthenticated=false i ProtectedRoute
      // odsyła z /dashboard na /.
      await finalizeNativeSession(data, { redirectToDashboard: true });
    },
    []
  );

  useEffect(() => {
    const pending: string | null = consumePendingLoginError();
    if (pending) {
      setError(pending);
    }
  }, []);

  useEffect(() => {
    let cancelled = false;
    const RESUME_TIMEOUT_MS = 12_000;

    void (async () => {
      try {
        const client = await getCustomAuthClient();
        if (cancelled) {
          return;
        }

        setAuthClient(client);
        setEmail(resolveInitialEmail(client));

        if (isSoftLoggedOut()) {
          return;
        }

        let resume: Awaited<ReturnType<typeof tryResumeNativeSession>>;
        try {
          resume = await withTimeout(
            tryResumeNativeSession(client),
            RESUME_TIMEOUT_MS,
            "tryResumeNativeSession timed out"
          );
        } catch {
          resume = { resumed: false, accountEmail: null };
        }

        if (cancelled) {
          return;
        }

        if (resume.resumed) {
          return;
        }

        if (resume.accountEmail) {
          setEmail(resume.accountEmail);
        }
      } catch {
        if (!cancelled) {
          setError(
            "Nie udało się zainicjalizować logowania. Uruchom `npm run dev` (proxy Vite /native-auth)."
          );
        }
      } finally {
        if (!cancelled) {
          setIsResuming(false);
        }
      }
    })();

    return () => {
      cancelled = true;
    };
  }, []);

  const reset = useCallback((): void => {
    setStep("credentials");
    setSignInState(null);
    setPassword("");
    setCode("");
    setCodeLength(null);
    setError(null);
    setIsLoading(false);
  }, []);

  const submitCredentials = useCallback(async (): Promise<void> => {
    if (!authClient) {
      return;
    }
    const trimmedEmail = email.trim();
    if (!trimmedEmail) {
      setError("Podaj adres e-mail.");
      return;
    }
    if (!password) {
      setError("Podaj hasło.");
      return;
    }

    setError(null);
    setIsLoading(true);
    try {
      if (!isSoftLoggedOut()) {
        let resume: Awaited<ReturnType<typeof tryResumeNativeSession>>;
        try {
          resume = await withTimeout(
            tryResumeNativeSession(authClient),
            8_000,
            "tryResumeNativeSession(submit) timed out"
          );
        } catch {
          resume = { resumed: false, accountEmail: null };
        }
        if (resume.resumed) {
          return;
        }
      }

      if (authClient.getAllAccounts().length > 0) {
        try {
          await authClient.clearCache();
          authClient.setActiveAccount(null);
        } catch {
          // ignore
        }
      }

      const result = await authClient.signIn({
        username: trimmedEmail,
        password,
        scopes: nativeSignInScopes,
      });
      await applySignInResult(
        result,
        password,
        setSignInState,
        setCodeLength,
        setStep,
        setError,
        completeSignIn
      );
    } catch (caught: unknown) {
      const message =
        caught instanceof Error && caught.message.includes("Failed to fetch")
          ? "Brak połączenia z proxy Native Auth. Uruchom `npm run dev`."
          : caught instanceof Error
            ? caught.message
            : "Nie udało się zalogować.";
      setError(message);
    } finally {
      setIsLoading(false);
    }
  }, [authClient, completeSignIn, email, password]);

  const submitCode = useCallback(async (): Promise<void> => {
    if (!(signInState instanceof SignInCodeRequiredState)) {
      return;
    }
    if (!code.trim()) {
      setError("Podaj kod weryfikacyjny.");
      return;
    }

    setError(null);
    setIsLoading(true);
    try {
      const result = await signInState.submitCode(code.trim());
      await applySignInResult(
        result,
        password,
        setSignInState,
        setCodeLength,
        setStep,
        setError,
        completeSignIn
      );
    } catch (caught: unknown) {
      setError(
        caught instanceof Error ? caught.message : "Nie udało się zweryfikować kodu."
      );
    } finally {
      setIsLoading(false);
    }
  }, [code, completeSignIn, password, signInState]);

  return {
    step,
    email,
    setEmail,
    password,
    setPassword,
    code,
    setCode,
    codeLength,
    error,
    isLoading,
    isResuming,
    isReady: authClient !== null && !isResuming,
    submitCredentials,
    submitCode,
    reset,
  };
}
