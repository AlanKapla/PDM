import { useCallback, useEffect, useState } from "react";
import {
  SignInCompletedState,
  SignInFailedState,
  SignUpAttributesRequiredState,
  SignUpCodeRequiredState,
  SignUpCompletedState,
  SignUpFailedState,
  SignUpPasswordRequiredState,
  type AuthFlowStateBase,
  type ICustomAuthPublicClientApplication,
  type UserAccountAttributes,
} from "@azure/msal-browser/custom-auth";
import { getCustomAuthClient } from "../auth/customAuthInstance";
import { finalizeNativeSession } from "../auth/finalizeNativeSession";
import { nativeSignInScopes } from "../config/customAuthConfig";
import type { NativeSignUpStep, UseNativeSignUpResult } from "../types/nativeAuth.types";

function buildSignUpAttributes(firstName: string, lastName: string): UserAccountAttributes {
  const givenName = firstName.trim();
  const surname = lastName.trim();
  return {
    givenName,
    surname,
    displayName: `${givenName} ${surname}`.trim(),
  };
}

function mapSignUpError(error: {
  isUserAlreadyExists?: () => boolean;
  isInvalidUsername?: () => boolean;
  isInvalidPassword?: () => boolean;
  isInvalidCode?: () => boolean;
  isMissingRequiredAttributes?: () => boolean;
  isAttributesValidationFailed?: () => boolean;
  isRedirectRequired?: () => boolean;
  isUnsupportedChallengeType?: () => boolean;
  errorData?: { errorDescription?: string };
} | null | undefined): string {
  if (!error) {
    return "Wystąpił nieoczekiwany błąd rejestracji.";
  }
  if (error.isUserAlreadyExists?.()) {
    return "Konto z tym adresem e-mail już istnieje. Zaloguj się.";
  }
  if (error.isInvalidUsername?.()) {
    return "Nieprawidłowy adres e-mail.";
  }
  if (error.isInvalidPassword?.()) {
    return "Hasło nie spełnia wymagań bezpieczeństwa.";
  }
  if (error.isInvalidCode?.()) {
    return "Nieprawidłowy lub wygasły kod weryfikacyjny.";
  }
  if (error.isMissingRequiredAttributes?.()) {
    return "Podaj imię i nazwisko — są wymagane przez Entra External ID.";
  }
  if (error.isAttributesValidationFailed?.()) {
    return "Nieprawidłowe imię lub nazwisko.";
  }
  if (error.isUnsupportedChallengeType?.() || error.isRedirectRequired?.()) {
    return "Ten typ rejestracji nie jest obsługiwany. Użyj e-maila, hasła oraz imienia i nazwiska.";
  }
  return error.errorData?.errorDescription ?? "Nie udało się zarejestrować.";
}

export function useNativeSignUp(): UseNativeSignUpResult {
  const [authClient, setAuthClient] = useState<ICustomAuthPublicClientApplication | null>(null);
  const [step, setStep] = useState<NativeSignUpStep>("details");
  const [signUpState, setSignUpState] = useState<AuthFlowStateBase | null>(null);
  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName] = useState("");
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
            "Nie udało się zainicjalizować rejestracji. Uruchom `npm run dev:cors` (proxy na porcie 3001)."
          );
        }
      });
    return () => {
      cancelled = true;
    };
  }, []);

  const reset = useCallback((): void => {
    setStep("details");
    setSignUpState(null);
    setPassword("");
    setCode("");
    setCodeLength(null);
    setError(null);
    setIsLoading(false);
  }, []);

  const continueAfterSignUpCompleted = useCallback(
    async (completedState: SignUpCompletedState): Promise<void> => {
      const signInResult = await completedState.signIn({
        scopes: nativeSignInScopes,
      });
      const { state } = signInResult;

      if (state instanceof SignInFailedState) {
        setError(
          signInResult.error?.errorData?.errorDescription ??
            "Konto utworzone, ale automatyczne logowanie nie powiodło się. Przejdź do logowania."
        );
        return;
      }

      if (state instanceof SignInCompletedState) {
        await finalizeNativeSession(signInResult.data);
        return;
      }

      setError(
        "Konto utworzone. Zaloguj się ręcznie — wymagany jest dodatkowy krok weryfikacji."
      );
    },
    []
  );

  const applySignUpState = useCallback(
    async (state: AuthFlowStateBase, resultError?: Parameters<typeof mapSignUpError>[0]): Promise<void> => {
      if (state instanceof SignUpFailedState) {
        setError(mapSignUpError(resultError));
        return;
      }

      if (state instanceof SignUpCompletedState) {
        await continueAfterSignUpCompleted(state);
        return;
      }

      if (state instanceof SignUpCodeRequiredState) {
        setSignUpState(state);
        setCodeLength(state.getCodeLength());
        setCode("");
        setStep("code");
        return;
      }

      if (state instanceof SignUpPasswordRequiredState) {
        setSignUpState(state);
        setStep("password");
        return;
      }

      if (state instanceof SignUpAttributesRequiredState) {
        setSignUpState(state);
        setStep("attributes");
        return;
      }

      setError("Nieobsługiwany krok rejestracji. Spróbuj ponownie lub skontaktuj się z pomocą.");
    },
    [continueAfterSignUpCompleted]
  );

  const submitDetails = useCallback(async (): Promise<void> => {
    if (!authClient) {
      return;
    }

    const trimmedFirst = firstName.trim();
    const trimmedLast = lastName.trim();
    const trimmedEmail = email.trim();

    if (!trimmedFirst) {
      setError("Podaj imię.");
      return;
    }
    if (!trimmedLast) {
      setError("Podaj nazwisko.");
      return;
    }
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
      const result = await authClient.signUp({
        username: trimmedEmail,
        password,
        attributes: buildSignUpAttributes(trimmedFirst, trimmedLast),
      });
      await applySignUpState(result.state, result.error);
    } catch (caught: unknown) {
      const message =
        caught instanceof Error && caught.message.includes("Failed to fetch")
          ? "Brak połączenia z proxy Native Auth. Uruchom `npm run dev:cors`."
          : caught instanceof Error
            ? caught.message
            : "Nie udało się rozpocząć rejestracji.";
      setError(message);
    } finally {
      setIsLoading(false);
    }
  }, [applySignUpState, authClient, email, firstName, lastName, password]);

  const submitPassword = useCallback(async (): Promise<void> => {
    if (!(signUpState instanceof SignUpPasswordRequiredState)) {
      return;
    }
    if (!password) {
      setError("Podaj hasło.");
      return;
    }

    setError(null);
    setIsLoading(true);
    try {
      const result = await signUpState.submitPassword(password);
      await applySignUpState(result.state, result.error);
    } catch (caught: unknown) {
      setError(
        caught instanceof Error ? caught.message : "Nie udało się ustawić hasła."
      );
    } finally {
      setIsLoading(false);
    }
  }, [applySignUpState, password, signUpState]);

  const submitCode = useCallback(async (): Promise<void> => {
    if (!(signUpState instanceof SignUpCodeRequiredState)) {
      return;
    }
    if (!code.trim()) {
      setError("Podaj kod weryfikacyjny.");
      return;
    }

    setError(null);
    setIsLoading(true);
    try {
      const result = await signUpState.submitCode(code.trim());
      await applySignUpState(result.state, result.error);
    } catch (caught: unknown) {
      setError(
        caught instanceof Error ? caught.message : "Nie udało się zweryfikować kodu."
      );
    } finally {
      setIsLoading(false);
    }
  }, [applySignUpState, code, signUpState]);

  const submitAttributes = useCallback(async (): Promise<void> => {
    if (!(signUpState instanceof SignUpAttributesRequiredState)) {
      return;
    }

    const trimmedFirst = firstName.trim();
    const trimmedLast = lastName.trim();
    if (!trimmedFirst || !trimmedLast) {
      setError("Podaj imię i nazwisko.");
      return;
    }

    setError(null);
    setIsLoading(true);
    try {
      const result = await signUpState.submitAttributes(
        buildSignUpAttributes(trimmedFirst, trimmedLast)
      );
      await applySignUpState(result.state, result.error);
    } catch (caught: unknown) {
      setError(
        caught instanceof Error ? caught.message : "Nie udało się zapisać danych profilu."
      );
    } finally {
      setIsLoading(false);
    }
  }, [applySignUpState, firstName, lastName, signUpState]);

  return {
    step,
    firstName,
    setFirstName,
    lastName,
    setLastName,
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
    submitDetails,
    submitPassword,
    submitCode,
    submitAttributes,
    reset,
  };
}
