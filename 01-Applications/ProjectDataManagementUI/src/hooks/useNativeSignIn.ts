import { useCallback, useEffect, useState } from "react";
import {
  SignInCodeRequiredState,
  SignInCompletedState,
  SignInFailedState,
  SignInPasswordRequiredState,
  type AuthFlowStateBase,
  type ICustomAuthPublicClientApplication,
} from "@azure/msal-browser/custom-auth";
import { getCustomAuthClient } from "../auth/customAuthInstance";
import { finalizeNativeSession } from "../auth/finalizeNativeSession";
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

export function useNativeSignIn(): UseNativeSignInResult {
  const [authClient, setAuthClient] = useState<ICustomAuthPublicClientApplication | null>(null);
  const [step, setStep] = useState<NativeSignInStep>("email");
  const [signInState, setSignInState] = useState<AuthFlowStateBase | null>(null);
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [code, setCode] = useState("");
  const [codeLength, setCodeLength] = useState<number | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(false);

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
            "Nie udało się zainicjalizować logowania. Uruchom `npm run dev:cors` (proxy na porcie 3001)."
          );
        }
      });
    return () => {
      cancelled = true;
    };
  }, []);

  const reset = useCallback((): void => {
    setStep("email");
    setSignInState(null);
    setPassword("");
    setCode("");
    setCodeLength(null);
    setError(null);
    setIsLoading(false);
  }, []);

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
    setIsLoading(true);
    try {
      const result = await authClient.signIn({
        username: trimmedEmail,
        scopes: nativeSignInScopes,
      });
      const { state } = result;

      if (state instanceof SignInFailedState) {
        setError(mapSignInError(result.error));
        return;
      }

      if (state instanceof SignInCompletedState) {
        await finalizeNativeSession(result.data);
        return;
      }

      if (state instanceof SignInCodeRequiredState) {
        setSignInState(state);
        setCodeLength(state.getCodeLength());
        setStep("code");
        return;
      }

      if (state instanceof SignInPasswordRequiredState) {
        setSignInState(state);
        setStep("password");
        return;
      }

      setError("Nieobsługiwany krok logowania. Spróbuj ponownie.");
    } catch (caught: unknown) {
      const message =
        caught instanceof Error && caught.message.includes("Failed to fetch")
          ? "Brak połączenia z proxy Native Auth. Uruchom `npm run dev:cors`."
          : caught instanceof Error
            ? caught.message
            : "Nie udało się rozpocząć logowania.";
      setError(message);
    } finally {
      setIsLoading(false);
    }
  }, [authClient, email]);

  const submitPassword = useCallback(async (): Promise<void> => {
    if (!(signInState instanceof SignInPasswordRequiredState)) {
      return;
    }
    if (!password) {
      setError("Podaj hasło.");
      return;
    }

    setError(null);
    setIsLoading(true);
    try {
      const result = await signInState.submitPassword(password);
      const { state } = result;

      if (state instanceof SignInFailedState) {
        setError(mapSignInError(result.error));
        return;
      }

      if (state instanceof SignInCompletedState) {
        await finalizeNativeSession(result.data);
        return;
      }

      setError("Wymagany dodatkowy krok (MFA) — skontaktuj się z administratorem.");
    } catch (caught: unknown) {
      setError(
        caught instanceof Error ? caught.message : "Nie udało się zweryfikować hasła."
      );
    } finally {
      setIsLoading(false);
    }
  }, [password, signInState]);

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
      const { state } = result;

      if (state instanceof SignInFailedState) {
        setError(mapSignInError(result.error));
        return;
      }

      if (state instanceof SignInCompletedState) {
        await finalizeNativeSession(result.data);
        return;
      }

      if (state instanceof SignInPasswordRequiredState) {
        setSignInState(state);
        setStep("password");
        return;
      }

      setError("Wymagany dodatkowy krok weryfikacji — skontaktuj się z administratorem.");
    } catch (caught: unknown) {
      setError(
        caught instanceof Error ? caught.message : "Nie udało się zweryfikować kodu."
      );
    } finally {
      setIsLoading(false);
    }
  }, [code, signInState]);

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
    isReady: authClient !== null,
    submitEmail,
    submitPassword,
    submitCode,
    reset,
  };
}
