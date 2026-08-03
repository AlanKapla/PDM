import React from "react";
import {
  Alert,
  AlertIcon,
  Button,
  Flex,
  Link,
  Spinner,
  Text,
  VStack,
} from "@chakra-ui/react";
import { Link as RouterLink, useSearchParams } from "react-router-dom";
import { NativeSignInCodeForm } from "../features/auth/components/NativeSignInCodeForm";
import { NativeSignInEmailForm } from "../features/auth/components/NativeSignInEmailForm";
import { NativeResetPasswordNewPasswordForm } from "../features/auth/components/NativeResetPasswordNewPasswordForm";
import {
  AuthPageHeading,
  AuthPageShell,
} from "../features/auth/components/AuthPageShell";
import { useNativeResetPassword } from "../hooks/useNativeResetPassword";

export default function ResetPasswordPage(): React.ReactElement {
  const [searchParams] = useSearchParams();
  const initialEmail = searchParams.get("email") ?? "";

  const {
    step,
    email,
    setEmail,
    code,
    setCode,
    password,
    setPassword,
    confirmPassword,
    setConfirmPassword,
    error,
    successMessage,
    isLoading,
    isReady,
    submitEmail,
    submitCode,
    submitNewPassword,
    reset,
  } = useNativeResetPassword(initialEmail);

  const stepTitle: string =
    step === "code"
      ? "Kod z e-maila"
      : step === "password"
        ? "Nowe hasło"
        : step === "done"
          ? "Hasło zmienione"
          : "Zmiana hasła";

  const stepHint: string | null =
    step === "email"
      ? "Wyślemy kod weryfikacyjny na podany adres e-mail."
      : step === "code"
        ? "Sprawdź skrzynkę i wpisz kod jednorazowy."
        : step === "password"
          ? "Ustaw nowe hasło do konta."
          : null;

  return (
    <AuthPageShell
      footer={
        <Text fontSize="sm" color="neutral.600">
          Pamiętasz hasło?{" "}
          <Link as={RouterLink} to="/login" color="primary.600" fontWeight="medium">
            Zaloguj się
          </Link>
        </Text>
      }
    >
      <VStack spacing={5} align="stretch">
        <AuthPageHeading title={stepTitle} hint={stepHint} />

        {!isReady && (
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
            submitLabel="Wyślij kod"
            loadingText="Wysyłanie..."
          />
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

        {isReady && step === "password" && (
          <NativeResetPasswordNewPasswordForm
            password={password}
            onPasswordChange={setPassword}
            confirmPassword={confirmPassword}
            onConfirmPasswordChange={setConfirmPassword}
            onSubmit={() => {
              void submitNewPassword();
            }}
            onBack={reset}
            isLoading={isLoading}
          />
        )}

        {isReady && step === "done" && (
          <VStack spacing={4} align="stretch">
            {successMessage && (
              <Alert status="success" borderRadius="md">
                <AlertIcon />
                {successMessage}
              </Alert>
            )}
            <Button as={RouterLink} to="/login" colorScheme="primary" size="lg" w="full">
              Przejdź do logowania
            </Button>
          </VStack>
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
