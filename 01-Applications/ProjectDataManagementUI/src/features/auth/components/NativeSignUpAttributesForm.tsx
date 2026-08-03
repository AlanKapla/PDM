import React from "react";
import {
  Button,
  FormControl,
  FormLabel,
  Input,
  SimpleGrid,
  VStack,
} from "@chakra-ui/react";

export interface NativeSignUpAttributesFormProps {
  firstName: string;
  onFirstNameChange: (value: string) => void;
  lastName: string;
  onLastNameChange: (value: string) => void;
  onSubmit: () => void;
  onBack: () => void;
  isLoading: boolean;
}

export function NativeSignUpAttributesForm({
  firstName,
  onFirstNameChange,
  lastName,
  onLastNameChange,
  onSubmit,
  onBack,
  isLoading,
}: NativeSignUpAttributesFormProps): React.ReactElement {
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
      <SimpleGrid columns={{ base: 1, sm: 2 }} spacing={4}>
        <FormControl isRequired>
          <FormLabel>Imię</FormLabel>
          <Input
            type="text"
            autoComplete="given-name"
            value={firstName}
            onChange={(event) => onFirstNameChange(event.target.value)}
            placeholder="Jan"
            maxLength={64}
            isDisabled={isLoading}
          />
        </FormControl>
        <FormControl isRequired>
          <FormLabel>Nazwisko</FormLabel>
          <Input
            type="text"
            autoComplete="family-name"
            value={lastName}
            onChange={(event) => onLastNameChange(event.target.value)}
            placeholder="Kowalski"
            maxLength={64}
            isDisabled={isLoading}
          />
        </FormControl>
      </SimpleGrid>
      <Button
        type="submit"
        colorScheme="primary"
        size="lg"
        w="full"
        isLoading={isLoading}
        loadingText="Zapisywanie..."
      >
        Zapisz i kontynuuj
      </Button>
      <Button type="button" variant="ghost" size="sm" onClick={onBack} isDisabled={isLoading}>
        Wróć
      </Button>
    </VStack>
  );
}
