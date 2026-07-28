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
import { NativeSignInCredentialsForm } from "../features/auth/components/NativeSignInCredentialsForm";
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
    submitCredentials,
    submitCode,
    reset,
  } = useNativeSignIn();

  const stepTitle: string = step === "code" ? "Kod z e-maila" : "Zaloguj się";

  const stepHint: string | null =
    isResuming
      ? "Sprawdzamy, czy masz już aktywną sesję…"
      : step === "code"
        ? "Sprawdź skrzynkę i wpisz kod jednorazowy."
        : "Podaj e-mail i hasło. Jeśli sesja jest jeszcze ważna, zalogujemy Cię automatycznie.";

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

        {isReady && step === "credentials" && (
          <VStack spacing={3} align="stretch">
            <NativeSignInCredentialsForm
              email={email}
              onEmailChange={setEmail}
              password={password}
              onPasswordChange={setPassword}
              onSubmit={() => {
                void submitCredentials();
              }}
              isLoading={isLoading}
              isDisabled={!isReady}
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
