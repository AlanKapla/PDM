import React, { useState } from "react";
import {
  Button,
  FormControl,
  FormLabel,
  IconButton,
  Input,
  InputGroup,
  InputRightElement,
  VStack,
} from "@chakra-ui/react";
import { Eye, EyeOff } from "lucide-react";

export interface NativeSignInCredentialsFormProps {
  email: string;
  onEmailChange: (value: string) => void;
  password: string;
  onPasswordChange: (value: string) => void;
  onSubmit: () => void;
  isLoading: boolean;
  isDisabled: boolean;
}

export function NativeSignInCredentialsForm({
  email,
  onEmailChange,
  password,
  onPasswordChange,
  onSubmit,
  isLoading,
  isDisabled,
}: NativeSignInCredentialsFormProps): React.ReactElement {
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
            autoComplete="current-password"
            value={password}
            onChange={(event) => onPasswordChange(event.target.value)}
            placeholder="Wpisz hasło"
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
      </FormControl>

      <Button
        type="submit"
        colorScheme="primary"
        size="lg"
        w="full"
        isLoading={isLoading}
        isDisabled={isDisabled}
        loadingText="Logowanie..."
      >
        Zaloguj się
      </Button>
    </VStack>
  );
}
