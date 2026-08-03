import React from "react";
import {
  Button,
  FormControl,
  FormLabel,
  Input,
  VStack,
} from "@chakra-ui/react";

export interface NativeSignInEmailFormProps {
  email: string;
  onEmailChange: (value: string) => void;
  onSubmit: () => void;
  isLoading: boolean;
  isDisabled: boolean;
  submitLabel?: string;
  loadingText?: string;
}

export function NativeSignInEmailForm({
  email,
  onEmailChange,
  onSubmit,
  isLoading,
  isDisabled,
  submitLabel = "Dalej",
  loadingText = "Sprawdzanie...",
}: NativeSignInEmailFormProps): React.ReactElement {
  return (
    <VStack
      as="form"
      spacing={4}
      align="stretch"
      onSubmit={(event: React.FormEvent) => {
        event.preventDefault();
        onSubmit();
      }}
    >
      <FormControl isRequired>
        <FormLabel>E-mail</FormLabel>
        <Input
          type="email"
          autoComplete="username"
          value={email}
          onChange={(event) => onEmailChange(event.target.value)}
          placeholder="jan@firma.pl"
          isDisabled={isDisabled || isLoading}
        />
      </FormControl>
      <Button
        type="submit"
        colorScheme="primary"
        size="lg"
        w="full"
        isLoading={isLoading}
        isDisabled={isDisabled}
        loadingText={loadingText}
      >
        {submitLabel}
      </Button>
    </VStack>
  );
}
