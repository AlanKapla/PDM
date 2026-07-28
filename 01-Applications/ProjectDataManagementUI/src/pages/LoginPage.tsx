import React from "react";
import {
  Alert,
  AlertIcon,
  Flex,
  Link,
  Spinner,
  Text,
  VStack,
} from "@chakra-ui/react";
import { Link as RouterLink } from "react-router-dom";
import { NativeSignInCodeForm } from "../features/auth/components/NativeSignInCodeForm";
import { NativeSignInEmailForm } from "../features/auth/components/NativeSignInEmailForm";
import { NativeSignInPasswordForm } from "../features/auth/components/NativeSignInPasswordForm";
import {
  AuthPageHeading,
  AuthPageShell,
} from "../features/auth/components/AuthPageShell";
import { useNativeSignIn } from "../hooks/useNativeSignIn";

export default function LoginPage(): React.ReactElement {
  const {
    step,
    email,
    setEmail,
    password,
    setPassword,
    code,
    setCode,
    error,
    isLoading,
    isResuming,
    isReady,
    submitEmail,
    submitPassword,
    submitCode,
    reset,
  } = useNativeSignIn();

  const stepTitle: string =
    step === "password"
      ? "Podaj hasło"
      : step === "code"
        ? "Kod z e-maila"
        : "Zaloguj się";

  const stepHint: string | null =
    isResuming
      ? "Sprawdzamy, czy masz już aktywną sesję…"
      : step === "email"
        ? "Jeśli sesja jest jeszcze ważna, zalogujemy Cię automatycznie. W przeciwnym razie poprosimy o hasło lub kod."
        : step === "code"
          ? "Sprawdź skrzynkę i wpisz kod jednorazowy."
          : null;

  return (
    <AuthPageShell
      footer={
        <Text fontSize="sm" color="neutral.600">
          Nie masz konta?{" "}
          <Link as={RouterLink} to="/register" color="primary.600" fontWeight="medium">
            Zarejestruj się
          </Link>
          {" · "}
          <Link as={RouterLink} to="/reset-password" color="primary.600" fontWeight="medium">
            Zmień hasło
          </Link>
        </Text>
      }
    >
      <VStack spacing={5} align="stretch">
        <AuthPageHeading title={stepTitle} hint={stepHint} />

        {(isResuming || !isReady) && (
          <Flex justify="center" py={6}>
            <Spinner size="lg" color="primary.500" thickness="3px" />
          </Flex>
        )}

        {isReady && step === "email" && (
          <NativeSignInEmailForm
            email={email}
            onEmailChange={setEmail}
            onSubmit={() => {
              void submitEmail();
            }}
            isLoading={isLoading}
            isDisabled={!isReady}
            submitLabel={email.trim() ? "Zaloguj" : "Dalej"}
            loadingText="Logowanie..."
          />
        )}

        {isReady && step === "password" && (
          <VStack spacing={3} align="stretch">
            <NativeSignInPasswordForm
              password={password}
              onPasswordChange={setPassword}
              onSubmit={() => {
                void submitPassword();
              }}
              onBack={reset}
              isLoading={isLoading}
            />
            <Text fontSize="sm" textAlign="center">
              <Link
                as={RouterLink}
                to={`/reset-password${email ? `?email=${encodeURIComponent(email)}` : ""}`}
                color="primary.600"
                fontWeight="medium"
              >
                Nie pamiętasz hasła?
              </Link>
            </Text>
          </VStack>
        )}

        {isReady && step === "code" && (
          <NativeSignInCodeForm
            code={code}
            onCodeChange={setCode}
            onSubmit={() => {
              void submitCode();
            }}
            onBack={reset}
            isLoading={isLoading}
          />
        )}

        {error && (
          <Alert status="error" borderRadius="md" role="alert">
            <AlertIcon />
            {error}
          </Alert>
        )}
      </VStack>
    </AuthPageShell>
  );
}
