import React, { useState } from "react";
import {
  Button,
  FormControl,
  FormLabel,
  IconButton,
  Input,
  InputGroup,
  InputRightElement,
  SimpleGrid,
  Text,
  VStack,
} from "@chakra-ui/react";
import { Eye, EyeOff } from "lucide-react";

export interface NativeSignUpDetailsFormProps {
  firstName: string;
  onFirstNameChange: (value: string) => void;
  lastName: string;
  onLastNameChange: (value: string) => void;
  email: string;
  onEmailChange: (value: string) => void;
  password: string;
  onPasswordChange: (value: string) => void;
  onSubmit: () => void;
  isLoading: boolean;
  isDisabled: boolean;
}

export function NativeSignUpDetailsForm({
  firstName,
  onFirstNameChange,
  lastName,
  onLastNameChange,
  email,
  onEmailChange,
  password,
  onPasswordChange,
  onSubmit,
  isLoading,
  isDisabled,
}: NativeSignUpDetailsFormProps): React.ReactElement {
  const [showPassword, setShowPassword] = useState(false);

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
            isDisabled={isDisabled || isLoading}
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
            isDisabled={isDisabled || isLoading}
          />
        </FormControl>
      </SimpleGrid>

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

      <FormControl isRequired>
        <FormLabel>Hasło</FormLabel>
        <InputGroup>
          <Input
            type={showPassword ? "text" : "password"}
            name="brickly-native-signup-password"
            autoComplete="new-password"
            value={password}
            onChange={(event) => onPasswordChange(event.target.value)}
            placeholder="Utwórz hasło"
            isDisabled={isDisabled || isLoading}
            pr="3rem"
          />
          <InputRightElement>
            <IconButton
              aria-label={showPassword ? "Ukryj hasło" : "Pokaż hasło"}
              icon={
                showPassword ? (
                  <EyeOff size={16} aria-hidden="true" />
                ) : (
                  <Eye size={16} aria-hidden="true" />
                )
              }
              size="sm"
              variant="ghost"
              onClick={() => setShowPassword((previous) => !previous)}
              tabIndex={-1}
            />
          </InputRightElement>
        </InputGroup>
        <Text fontSize="xs" color="neutral.600" mt={2}>
          Imię i nazwisko są wymagane przez Microsoft Entra External ID.
        </Text>
      </FormControl>

      <Button
        type="submit"
        colorScheme="primary"
        size="lg"
        w="full"
        isLoading={isLoading}
        isDisabled={isDisabled}
        loadingText="Rejestracja..."
      >
        Utwórz konto
      </Button>
    </VStack>
  );
}
