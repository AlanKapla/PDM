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
import { getRememberedSignInEmail, isSoftLoggedOut } from "../auth/rememberedSignIn";
import { tryResumeNativeSession } from "../auth/tryResumeNativeSession";
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

async function applySignInResult(
  result: {
    state: AuthFlowStateBase;
    error?: Parameters<typeof mapSignInError>[0];
    data?: Parameters<typeof finalizeNativeSession>[0];
  },
  password: string,
  setSignInState: (state: AuthFlowStateBase | null) => void,
  setCodeLength: (value: number | null) => void,
  setStep: (step: NativeSignInStep) => void,
  setError: (value: string | null) => void
): Promise<void> {
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
    // Hasło było w formularzu, ale Entra i tak zwróciła PasswordRequired — dociśnij.
    if (password) {
      const passwordResult = await state.submitPassword(password);
      await applySignInResult(
        passwordResult,
        password,
        setSignInState,
        setCodeLength,
        setStep,
        setError
      );
      return;
    }
    setError("Podaj hasło.");
    setStep("credentials");
    return;
  }

  setError("Nieobsługiwany krok logowania. Spróbuj ponownie.");
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

  useEffect(() => {
    let cancelled = false;

    void (async () => {
      let resumed = false;
      try {
        const client = await getCustomAuthClient();
        if (cancelled) {
          return;
        }

        setAuthClient(client);
        setEmail(resolveInitialEmail(client));

        // Po soft logout nie wznawiaj automatycznie — użytkownik mógł wybrać „inne konto”.
        // Resume jest na przycisku „Kontynuuj jako…” (Home / LoggedOut).
        if (isSoftLoggedOut()) {
          return;
        }

        const resume = await tryResumeNativeSession(client);
        resumed = resume.resumed;
        if (cancelled || resumed) {
          return;
        }

        if (resume.accountEmail) {
          setEmail(resume.accountEmail);
        }
      } catch {
        if (!cancelled) {
          setError(
            "Nie udało się zainicjalizować logowania. Uruchom `npm run dev:cors` (proxy na porcie 3001)."
          );
        }
      } finally {
        if (!cancelled && !resumed) {
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
      // Soft logout + formularz = świadome logowanie (może być inne konto) — bez auto-resume.
      if (!isSoftLoggedOut()) {
        const resume = await tryResumeNativeSession(authClient);
        if (resume.resumed) {
          return;
        }
      }

      // Stale account w cache blokuje signIn (UserAlreadySignedIn) — wyczyść lokalnie.
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
        setError
      );
    } catch (caught: unknown) {
      const message =
        caught instanceof Error && caught.message.includes("Failed to fetch")
          ? "Brak połączenia z proxy Native Auth. Uruchom `npm run dev:cors`."
          : caught instanceof Error
            ? caught.message
            : "Nie udało się zalogować.";
      setError(message);
    } finally {
      setIsLoading(false);
    }
  }, [authClient, email, password]);

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
        setError
      );
    } catch (caught: unknown) {
      setError(
        caught instanceof Error ? caught.message : "Nie udało się zweryfikować kodu."
      );
    } finally {
      setIsLoading(false);
    }
  }, [code, password, signInState]);

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
