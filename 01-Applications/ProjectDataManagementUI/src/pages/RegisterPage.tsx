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
import { NativeSignInPasswordForm } from "../features/auth/components/NativeSignInPasswordForm";
import { NativeSignUpAttributesForm } from "../features/auth/components/NativeSignUpAttributesForm";
import { NativeSignUpDetailsForm } from "../features/auth/components/NativeSignUpDetailsForm";
import {
  AuthPageHeading,
  AuthPageShell,
} from "../features/auth/components/AuthPageShell";
import { useNativeSignUp } from "../hooks/useNativeSignUp";

export default function RegisterPage(): React.ReactElement {
  const {
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
    error,
    isLoading,
    isReady,
    submitDetails,
    submitPassword,
    submitCode,
    submitAttributes,
    reset,
  } = useNativeSignUp();

  const stepTitle: string =
    step === "password"
      ? "Ustaw hasło"
      : step === "code"
        ? "Potwierdź e-mail"
        : step === "attributes"
          ? "Uzupełnij profil"
          : "Utwórz konto";

  const stepHint: string | null =
    step === "details"
      ? "Imię i nazwisko są wymagane przez Entra External ID."
      : step === "code"
        ? "Wpisz kod jednorazowy wysłany na podany adres e-mail."
        : step === "attributes"
          ? "Tenant wymaga imienia i nazwiska, aby dokończyć rejestrację."
          : null;

  return (
    <AuthPageShell
      footer={
        <Text fontSize="sm" color="neutral.600">
          Masz już konto?{" "}
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

        {isReady && step === "details" && (
          <NativeSignUpDetailsForm
            firstName={firstName}
            onFirstNameChange={setFirstName}
            lastName={lastName}
            onLastNameChange={setLastName}
            email={email}
            onEmailChange={setEmail}
            password={password}
            onPasswordChange={setPassword}
            onSubmit={() => {
              void submitDetails();
            }}
            isLoading={isLoading}
            isDisabled={!isReady}
          />
        )}

        {isReady && step === "password" && (
          <NativeSignInPasswordForm
            password={password}
            onPasswordChange={setPassword}
            onSubmit={() => {
              void submitPassword();
            }}
            onBack={reset}
            isLoading={isLoading}
            submitLabel="Ustaw hasło"
            loadingText="Zapisywanie..."
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

        {isReady && step === "attributes" && (
          <NativeSignUpAttributesForm
            firstName={firstName}
            onFirstNameChange={setFirstName}
            lastName={lastName}
            onLastNameChange={setLastName}
            onSubmit={() => {
              void submitAttributes();
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
