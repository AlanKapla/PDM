import React, { useState } from "react";
import {
  Button,
  FormControl,
  FormLabel,
  IconButton,
  Input,
  InputGroup,
  InputRightElement,
  Text,
  VStack,
} from "@chakra-ui/react";
import { Eye, EyeOff } from "lucide-react";

export interface NativeResetPasswordNewPasswordFormProps {
  password: string;
  onPasswordChange: (value: string) => void;
  confirmPassword: string;
  onConfirmPasswordChange: (value: string) => void;
  onSubmit: () => void;
  onBack: () => void;
  isLoading: boolean;
}

export function NativeResetPasswordNewPasswordForm({
  password,
  onPasswordChange,
  confirmPassword,
  onConfirmPasswordChange,
  onSubmit,
  onBack,
  isLoading,
}: NativeResetPasswordNewPasswordFormProps): React.ReactElement {
  const [showPassword, setShowPassword] = useState(false);
  const [showConfirm, setShowConfirm] = useState(false);

  return (
    <VStack
      as="form"
      spacing={4}
      align="stretch"
      autoComplete="off"
      onSubmit={(event: React.FormEvent) => {
        event.preventDefault();
        onSubmit();
      }}
    >
      <FormControl isRequired>
        <FormLabel>Nowe hasło</FormLabel>
        <InputGroup>
          <Input
            type={showPassword ? "text" : "password"}
            name="brickly-new-password"
            autoComplete="new-password"
            value={password}
            onChange={(event) => onPasswordChange(event.target.value)}
            placeholder="Utwórz nowe hasło"
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

      <FormControl isRequired>
        <FormLabel>Powtórz hasło</FormLabel>
        <InputGroup>
          <Input
            type={showConfirm ? "text" : "password"}
            name="brickly-confirm-password"
            autoComplete="new-password"
            value={confirmPassword}
            onChange={(event) => onConfirmPasswordChange(event.target.value)}
            placeholder="Powtórz nowe hasło"
            isDisabled={isLoading}
            pr="3rem"
          />
          <InputRightElement>
            <IconButton
              aria-label={showConfirm ? "Ukryj hasło" : "Pokaż hasło"}
              icon={
                showConfirm ? (
                  <EyeOff size={16} aria-hidden="true" />
                ) : (
                  <Eye size={16} aria-hidden="true" />
                )
              }
              size="sm"
              variant="ghost"
              onClick={() => setShowConfirm((previous) => !previous)}
              tabIndex={-1}
            />
          </InputRightElement>
        </InputGroup>
        <Text fontSize="xs" color="neutral.600" mt={2}>
          Hasło musi spełniać wymagania polityki Entra External ID.
        </Text>
      </FormControl>

      <Button
        type="submit"
        colorScheme="primary"
        size="lg"
        w="full"
        isLoading={isLoading}
        loadingText="Zapisywanie..."
      >
        Ustaw nowe hasło
      </Button>
      <Button type="button" variant="ghost" size="sm" onClick={onBack} isDisabled={isLoading}>
        Wróć
      </Button>
    </VStack>
  );
}
