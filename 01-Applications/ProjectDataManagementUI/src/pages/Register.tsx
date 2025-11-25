import { useState } from "react";
import type { ChangeEvent, FormEvent } from "react";
import { useNavigate } from "react-router-dom";

import {
  Box,
  Button,
  Flex,
  Heading,
  Input,
  VStack,
  FormControl,
  FormLabel,
  FormErrorMessage,
  useToast,
  useColorModeValue,
  Text,
  InputGroup,
  InputRightElement,
  IconButton,
  List,
  ListItem,
  ListIcon,
} from "@chakra-ui/react";
import { Eye, EyeOff, Check, X } from "lucide-react";

import { registerUser, type RegisterForm } from "../services/authService";

interface FormWithConfirm extends RegisterForm {
  confirmPassword: string;
}

export default function Register() {
  const toast = useToast();
  const navigate = useNavigate();

  const [form, setForm] = useState<FormWithConfirm>({
    email: "",
    password: "",
    confirmPassword: "",
    firstName: "",
    lastName: "",
  });

  const [showPassword, setShowPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);
  const [errors, setErrors] = useState<Partial<FormWithConfirm>>({});

  const handleChange = (e: ChangeEvent<HTMLInputElement>) => {
    setForm({ ...form, [e.target.name]: e.target.value });
  };

  const validatePassword = (password: string) => {
    const errors: string[] = [];
    
    if (password.length < 8) {
      errors.push("Hasło musi mieć co najmniej 8 znaków");
    }
    if (!/[A-Z]/.test(password)) {
      errors.push("Hasło musi zawierać co najmniej jedną wielką literę");
    }
    if (!/[a-z]/.test(password)) {
      errors.push("Hasło musi zawierać co najmniej jedną małą literę");
    }
    if (!/[0-9]/.test(password)) {
      errors.push("Hasło musi zawierać co najmniej jedną cyfrę");
    }
    if (!/[^a-zA-Z0-9]/.test(password)) {
      errors.push("Hasło musi zawierać co najmniej jeden znak specjalny");
    }
    
    return errors;
  };

  const getPasswordChecks = (password: string) => {
    return [
      { label: "Co najmniej 8 znaków", valid: password.length >= 8 },
      { label: "Wielka litera (A-Z)", valid: /[A-Z]/.test(password) },
      { label: "Mała litera (a-z)", valid: /[a-z]/.test(password) },
      { label: "Cyfra (0-9)", valid: /[0-9]/.test(password) },
      { label: "Znak specjalny (!@#$...)", valid: /[^a-zA-Z0-9]/.test(password) },
    ];
  };

  const validate = () => {
    const newErrors: Partial<FormWithConfirm> = {};

    if (!form.email.includes("@")) newErrors.email = "Podaj poprawny email";
    if (!form.firstName.trim()) newErrors.firstName = "Podaj imię";
    if (!form.lastName.trim()) newErrors.lastName = "Podaj nazwisko";

    const passwordErrors = validatePassword(form.password);
    if (passwordErrors.length > 0) {
      newErrors.password = passwordErrors.join(". ");
    }

    if (form.password !== form.confirmPassword) {
      newErrors.confirmPassword = "Hasła muszą być identyczne";
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    if (!validate()) return;

    try {
      const success = await registerUser(form);

      if (!success) {
        toast({
          title: "Błąd rejestracji",
          description: "Spróbuj ponownie.",
          status: "error",
          duration: 3000,
          isClosable: true,
        });
        return;
      }

      toast({
        title: "Zarejestrowano pomyślnie!",
        description: "Sprawdź swoją skrzynkę email i aktywuj konto, aby się zalogować.",
        status: "success",
        duration: 7000,
        isClosable: true,
      });

      navigate("/login");
    } catch (error) {
      console.error("Błąd rejestracji:", error);
      toast({
        title: "Błąd serwera",
        status: "error",
        duration: 3000,
        isClosable: true,
      });
    }
  };

  const cardBg = useColorModeValue("white", "gray.800");
  const pageBg = useColorModeValue("gray.50", "gray.900");
  const labelColor = useColorModeValue("gray.700", "gray.300");

  return (
    <Flex justify="center" align="center" minH="100vh" bg={pageBg} px={{ base: 4, md: 0 }}>
      <Box bg={cardBg} p={{ base: 6, md: 8 }} rounded="lg" shadow="lg" width="100%" maxW="400px">
        <Heading mb={6} textAlign="center" size="lg">
          Rejestracja
        </Heading>

        <VStack spacing={4} as="form" onSubmit={handleSubmit}>
          <FormControl isInvalid={!!errors.firstName}>
            <FormLabel color={labelColor}>Imię</FormLabel>
            <Input
              name="firstName"
              value={form.firstName}
              onChange={handleChange}
            />
            <FormErrorMessage>{errors.firstName}</FormErrorMessage>
          </FormControl>

          <FormControl isInvalid={!!errors.lastName}>
            <FormLabel color={labelColor}>Nazwisko</FormLabel>
            <Input
              name="lastName"
              value={form.lastName}
              onChange={handleChange}
            />
            <FormErrorMessage>{errors.lastName}</FormErrorMessage>
          </FormControl>

          <FormControl isInvalid={!!errors.email}>
            <FormLabel color={labelColor}>Email</FormLabel>
            <Input
              name="email"
              type="email"
              value={form.email}
              onChange={handleChange}
            />
            <FormErrorMessage>{errors.email}</FormErrorMessage>
          </FormControl>

          <FormControl isInvalid={!!errors.password}>
            <FormLabel color={labelColor}>Hasło</FormLabel>
            <InputGroup>
              <Input
                name="password"
                type={showPassword ? "text" : "password"}
                value={form.password}
                onChange={handleChange}
              />
              <InputRightElement>
                <IconButton
                  aria-label={showPassword ? "Ukryj hasło" : "Pokaż hasło"}
                  icon={showPassword ? <EyeOff size={18} /> : <Eye size={18} />}
                  onClick={() => setShowPassword(!showPassword)}
                  variant="ghost"
                  size="sm"
                />
              </InputRightElement>
            </InputGroup>
            {form.password && (
              <List spacing={1} mt={2} fontSize="sm">
                {getPasswordChecks(form.password).map((check, idx) => (
                  <ListItem key={idx} color={check.valid ? "green.500" : "gray.500"}>
                    <ListIcon as={check.valid ? Check : X} />
                    {check.label}
                  </ListItem>
                ))}
              </List>
            )}
            <FormErrorMessage>{errors.password}</FormErrorMessage>
          </FormControl>

          <FormControl isInvalid={!!errors.confirmPassword}>
            <FormLabel color={labelColor}>Potwierdź hasło</FormLabel>
            <InputGroup>
              <Input
                name="confirmPassword"
                type={showConfirmPassword ? "text" : "password"}
                value={form.confirmPassword}
                onChange={handleChange}
              />
              <InputRightElement>
                <IconButton
                  aria-label={showConfirmPassword ? "Ukryj hasło" : "Pokaż hasło"}
                  icon={showConfirmPassword ? <EyeOff size={18} /> : <Eye size={18} />}
                  onClick={() => setShowConfirmPassword(!showConfirmPassword)}
                  variant="ghost"
                  size="sm"
                />
              </InputRightElement>
            </InputGroup>
            <FormErrorMessage>{errors.confirmPassword}</FormErrorMessage>
          </FormControl>

          <Button width="100%" colorScheme="blue" type="submit">
            Zarejestruj
          </Button>

          <Text fontSize="sm">
            Masz już konto?{" "}
            <Button
              variant="link"
              colorScheme="blue"
              onClick={() => navigate("/login")}
            >
              Zaloguj się
            </Button>
          </Text>
        </VStack>
      </Box>
    </Flex>
  );
}
