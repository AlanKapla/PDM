import React from "react";
import {
  Button,
  FormControl,
  FormLabel,
  Input,
  VStack,
} from "@chakra-ui/react";

export interface NativeSignInCodeFormProps {
  code: string;
  onCodeChange: (value: string) => void;
  onSubmit: () => void;
  onBack: () => void;
  isLoading: boolean;
}

export function NativeSignInCodeForm({
  code,
  onCodeChange,
  onSubmit,
  onBack,
  isLoading,
}: NativeSignInCodeFormProps): React.ReactElement {
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
        <FormLabel>Kod weryfikacyjny</FormLabel>
        <Input
          type="text"
          inputMode="numeric"
          autoComplete="one-time-code"
          value={code}
          onChange={(event) => onCodeChange(event.target.value)}
          placeholder="Kod z e-maila"
          isDisabled={isLoading}
        />
      </FormControl>
      <Button
        type="submit"
        colorScheme="primary"
        size="lg"
        w="full"
        isLoading={isLoading}
        loadingText="Weryfikacja..."
      >
        Potwierdź
      </Button>
      <Button
        type="button"
        variant="ghost"
        size="sm"
        onClick={onBack}
        isDisabled={isLoading}
      >
        Wróć
      </Button>
    </VStack>
  );
}
