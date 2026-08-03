import { useCallback, useEffect, useState } from "react";
import {
  ResetPasswordCodeRequiredState,
  ResetPasswordCompletedState,
  ResetPasswordFailedState,
  ResetPasswordPasswordRequiredState,
  type AuthFlowStateBase,
  type ICustomAuthPublicClientApplication,
} from "@azure/msal-browser/custom-auth";
import { getCustomAuthClient } from "../auth/customAuthInstance";
import { finalizeNativeSession } from "../auth/finalizeNativeSession";
import { msalInstance } from "../auth/msalInstance";
import { nativeSignInScopes } from "../config/customAuthConfig";
import type {
  NativeResetPasswordStep,
  UseNativeResetPasswordResult,
} from "../types/nativeAuth.types";

function mapResetPasswordError(error: {
  isUserNotFound?: () => boolean;
  isInvalidUsername?: () => boolean;
  isInvalidCode?: () => boolean;
  isInvalidPassword?: () => boolean;
  isPasswordResetFailed?: () => boolean;
  isUnsupportedChallengeType?: () => boolean;
  isRedirectRequired?: () => boolean;
  errorData?: { errorDescription?: string };
} | null | undefined): string {
  if (!error) {
    return "Wystąpił nieoczekiwany błąd zmiany hasła.";
  }
  if (error.isUserNotFound?.()) {
    return "Nie znaleziono konta z tym adresem e-mail.";
  }
  if (error.isInvalidUsername?.()) {
    return "Nieprawidłowy adres e-mail.";
  }
  if (error.isInvalidCode?.()) {
    return "Nieprawidłowy lub wygasły kod weryfikacyjny.";
  }
  if (error.isInvalidPassword?.()) {
    return "Hasło nie spełnia wymagań bezpieczeństwa.";
  }
  if (error.isPasswordResetFailed?.()) {
    return "Nie udało się zmienić hasła. Spróbuj ponownie.";
  }
  if (error.isUnsupportedChallengeType?.() || error.isRedirectRequired?.()) {
    return "Zmiana hasła nie jest dostępna dla tego konta.";
  }
  return error.errorData?.errorDescription ?? "Nie udało się zmienić hasła.";
}

/** Lokalne wylogowanie przed resetem — bez redirectu na /logged-out. */
function clearLocalSessionIfNeeded(): void {
  try {
    msalInstance.setActiveAccount(null);
  } catch {
    // ignore
  }
  Object.keys(localStorage)
    .filter((key) => key.startsWith("msal."))
    .forEach((key) => localStorage.removeItem(key));
}

export function useNativeResetPassword(
  initialEmail: string = ""
): UseNativeResetPasswordResult {
  const [authClient, setAuthClient] = useState<ICustomAuthPublicClientApplication | null>(null);
  const [step, setStep] = useState<NativeResetPasswordStep>("email");
  const [resetState, setResetState] = useState<AuthFlowStateBase | null>(null);
  const [email, setEmail] = useState(initialEmail);
  const [code, setCode] = useState("");
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [codeLength, setCodeLength] = useState<number | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(false);

  useEffect(() => {
    if (initialEmail.trim()) {
      setEmail(initialEmail.trim());
    }
  }, [initialEmail]);

  useEffect(() => {
    let cancelled = false;
    void getCustomAuthClient()
      .then((client) => {
        if (!cancelled) {
          setAuthClient(client);
        }
      })
      .catch(() => {
        if (!cancelled) {
          setError(
            "Nie udało się zainicjalizować zmiany hasła. Uruchom `npm run dev:cors` (proxy na porcie 3001)."
          );
        }
      });
    return () => {
      cancelled = true;
    };
  }, []);

  const reset = useCallback((): void => {
    setStep("email");
    setResetState(null);
    setCode("");
    setPassword("");
    setConfirmPassword("");
    setCodeLength(null);
    setError(null);
    setSuccessMessage(null);
    setIsLoading(false);
  }, []);

  const continueAfterCompleted = useCallback(
    async (completedState: ResetPasswordCompletedState): Promise<void> => {
      const signInResult = await completedState.signIn({
        scopes: nativeSignInScopes,
      });
      // Cast: MSAL `this is` predicates narrow sibling branches to `never` in tsc.
      const flow = signInResult as {
        data?: Parameters<typeof finalizeNativeSession>[0];
        isCompleted(): boolean;
        isFailed(): boolean;
      };

      if (flow.isCompleted()) {
        await finalizeNativeSession(flow.data, { redirectToDashboard: true });
        return;
      }

      if (flow.isFailed()) {
        setStep("done");
        setSuccessMessage(
          "Hasło zostało zmienione. Zaloguj się nowym hasłem."
        );
        setError(null);
        return;
      }

      setStep("done");
      setSuccessMessage("Hasło zostało zmienione. Zaloguj się nowym hasłem.");
    },
    []
  );

  const applyResetState = useCallback(
    async (
      state: AuthFlowStateBase,
      resultError?: Parameters<typeof mapResetPasswordError>[0]
    ): Promise<void> => {
      if (state instanceof ResetPasswordFailedState) {
        setError(mapResetPasswordError(resultError));
        return;
      }

      if (state instanceof ResetPasswordCompletedState) {
        await continueAfterCompleted(state);
        return;
      }

      if (state instanceof ResetPasswordCodeRequiredState) {
        setResetState(state);
        setCodeLength(state.getCodeLength());
        setCode("");
        setStep("code");
        return;
      }

      if (state instanceof ResetPasswordPasswordRequiredState) {
        setResetState(state);
        setPassword("");
        setConfirmPassword("");
        setStep("password");
        return;
      }

      setError("Nieobsługiwany krok zmiany hasła. Spróbuj ponownie.");
    },
    [continueAfterCompleted]
  );

  const submitEmail = useCallback(async (): Promise<void> => {
    if (!authClient) {
      return;
    }
    const trimmedEmail = email.trim();
    if (!trimmedEmail) {
      setError("Podaj adres e-mail.");
      return;
    }

    setError(null);
    setSuccessMessage(null);
    setIsLoading(true);
    try {
      clearLocalSessionIfNeeded();
      const result = await authClient.resetPassword({
        username: trimmedEmail,
      });
      await applyResetState(result.state, result.error);
    } catch (caught: unknown) {
      const message =
        caught instanceof Error && caught.message.includes("Failed to fetch")
          ? "Brak połączenia z proxy Native Auth. Uruchom `npm run dev:cors`."
          : caught instanceof Error
            ? caught.message
            : "Nie udało się rozpocząć zmiany hasła.";
      setError(message);
    } finally {
      setIsLoading(false);
    }
  }, [applyResetState, authClient, email]);

  const submitCode = useCallback(async (): Promise<void> => {
    if (!(resetState instanceof ResetPasswordCodeRequiredState)) {
      return;
    }
    if (!code.trim()) {
      setError("Podaj kod weryfikacyjny.");
      return;
    }

    setError(null);
    setIsLoading(true);
    try {
      const result = await resetState.submitCode(code.trim());
      await applyResetState(result.state, result.error);
    } catch (caught: unknown) {
      setError(
        caught instanceof Error ? caught.message : "Nie udało się zweryfikować kodu."
      );
    } finally {
      setIsLoading(false);
    }
  }, [applyResetState, code, resetState]);

  const submitNewPassword = useCallback(async (): Promise<void> => {
    if (!(resetState instanceof ResetPasswordPasswordRequiredState)) {
      return;
    }
    if (!password) {
      setError("Podaj nowe hasło.");
      return;
    }
    if (password !== confirmPassword) {
      setError("Hasła nie są takie same.");
      return;
    }

    setError(null);
    setIsLoading(true);
    try {
      const result = await resetState.submitNewPassword(password);
      await applyResetState(result.state, result.error);
    } catch (caught: unknown) {
      setError(
        caught instanceof Error ? caught.message : "Nie udało się ustawić nowego hasła."
      );
    } finally {
      setIsLoading(false);
    }
  }, [applyResetState, confirmPassword, password, resetState]);

  return {
    step,
    email,
    setEmail,
    code,
    setCode,
    password,
    setPassword,
    confirmPassword,
    setConfirmPassword,
    codeLength,
    error,
    successMessage,
    isLoading,
    isReady: authClient !== null,
    submitEmail,
    submitCode,
    submitNewPassword,
    reset,
  };
}
