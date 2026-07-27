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

export interface NativeSignInPasswordFormProps {
  password: string;
  onPasswordChange: (value: string) => void;
  onSubmit: () => void;
  onBack: () => void;
  isLoading: boolean;
  submitLabel?: string;
  loadingText?: string;
}

export function NativeSignInPasswordForm({
  password,
  onPasswordChange,
  onSubmit,
  onBack,
  isLoading,
  submitLabel = "Zaloguj się",
  loadingText = "Logowanie...",
}: NativeSignInPasswordFormProps): React.ReactElement {
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
        <FormLabel>Hasło</FormLabel>
        <InputGroup>
          <Input
            type={showPassword ? "text" : "password"}
            autoComplete="current-password"
            value={password}
            onChange={(event) => onPasswordChange(event.target.value)}
            placeholder="Hasło"
            isDisabled={isLoading}
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
        loadingText={loadingText}
      >
        {submitLabel}
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
