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
  Divider,
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
    setForm((prev) => ({ ...prev, [e.target.name]: e.target.value }));
  };

  const passwordChecklist = [
    { label: "Co najmniej 8 znaków", fn: (p: string) => p.length >= 8 },
    { label: "Wielka litera (A-Z)", fn: (p: string) => /[A-Z]/.test(p) },
    { label: "Mała litera (a-z)", fn: (p: string) => /[a-z]/.test(p) },
    { label: "Cyfra (0-9)", fn: (p: string) => /[0-9]/.test(p) },
    { label: "Znak specjalny (!@#$…)", fn: (p: string) => /[^a-zA-Z0-9]/.test(p) },
  ];

  const validate = () => {
    const newErrors: Partial<FormWithConfirm> = {};

    if (!form.firstName.trim()) newErrors.firstName = "Podaj imię";
    if (!form.lastName.trim()) newErrors.lastName = "Podaj nazwisko";
    if (!form.email.includes("@")) newErrors.email = "Podaj prawidłowy email";

    const invalidRules = passwordChecklist.filter((r) => !r.fn(form.password));
    if (invalidRules.length > 0) newErrors.password = "Hasło nie spełnia wymagań";

    if (form.password !== form.confirmPassword)
      newErrors.confirmPassword = "Hasła muszą być identyczne";

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
          status: "error",
          duration: 3000,
        });
        return;
      }

      toast({
        title: "Konto utworzone",
        description: "Sprawdź skrzynkę i aktywuj konto",
        status: "success",
      });

      navigate("/login");
    } catch {
      toast({
        title: "Błąd serwera",
        status: "error",
      });
    }
  };

  const bg = useColorModeValue("#ffffff", "#1a1a1a");
  const cardBg = useColorModeValue("white", "gray.800");
  const labelColor = useColorModeValue("gray.700", "gray.300");
  const textSecondary = useColorModeValue("gray.600", "gray.400");

  return (
    <Flex justify="center" align="center" minH="100vh" bg={bg} px={4}>
      <Box
        bg={cardBg}
        p={8}
        rounded="2xl"
        shadow="md"
        w="100%"
        maxW="460px"
        border="1px solid"
        borderColor="rgba(0,0,0,0.06)"
      >
        <Heading mb={6} textAlign="center" fontWeight="700">
          Utwórz konto
        </Heading>

        <VStack spacing={4} as="form" onSubmit={handleSubmit}>
          <FormControl isInvalid={!!errors.firstName}>
            <FormLabel color={labelColor} fontWeight="500">
              Imię
            </FormLabel>
            <Input name="firstName" size="lg" value={form.firstName} onChange={handleChange} />
            <FormErrorMessage>{errors.firstName}</FormErrorMessage>
          </FormControl>

          <FormControl isInvalid={!!errors.lastName}>
            <FormLabel color={labelColor} fontWeight="500">
              Nazwisko
            </FormLabel>
            <Input name="lastName" size="lg" value={form.lastName} onChange={handleChange} />
            <FormErrorMessage>{errors.lastName}</FormErrorMessage>
          </FormControl>

          <FormControl isInvalid={!!errors.email}>
            <FormLabel color={labelColor} fontWeight="500">
              Email
            </FormLabel>
            <Input name="email" size="lg" type="email" value={form.email} onChange={handleChange} />
            <FormErrorMessage>{errors.email}</FormErrorMessage>
          </FormControl>

          {/* PASSWORD */}
          <FormControl isInvalid={!!errors.password}>
            <FormLabel color={labelColor}>Hasło</FormLabel>
            <InputGroup>
              <Input
                name="password"
                type={showPassword ? "text" : "password"}
                size="lg"
                value={form.password}
                onChange={handleChange}
              />
              <InputRightElement>
                <IconButton
                  aria-label="toggle"
                  icon={showPassword ? <EyeOff size={18} /> : <Eye size={18} />}
                  onClick={() => setShowPassword(!showPassword)}
                  variant="ghost"
                  size="sm"
                />
              </InputRightElement>
            </InputGroup>

            {form.password && (
              <List spacing={1} mt={2} fontSize="sm">
                {passwordChecklist.map((rule, i) => {
                  const valid = rule.fn(form.password);
                  return (
                    <ListItem key={i} color={valid ? "green.500" : textSecondary}>
                      <ListIcon as={valid ? Check : X} />
                      {rule.label}
                    </ListItem>
                  );
                })}
              </List>
            )}
            <FormErrorMessage>{errors.password}</FormErrorMessage>
          </FormControl>

          {/* CONFIRM PASSWORD */}
          <FormControl isInvalid={!!errors.confirmPassword}>
            <FormLabel color={labelColor}>Potwierdź hasło</FormLabel>
            <InputGroup>
              <Input
                name="confirmPassword"
                type={showConfirmPassword ? "text" : "password"}
                size="lg"
                value={form.confirmPassword}
                onChange={handleChange}
              />
              <InputRightElement>
                <IconButton
                  aria-label="toggle"
                  icon={showConfirmPassword ? <EyeOff size={18} /> : <Eye size={18} />}
                  onClick={() => setShowConfirmPassword(!showConfirmPassword)}
                  variant="ghost"
                  size="sm"
                />
              </InputRightElement>
            </InputGroup>
            <FormErrorMessage>{errors.confirmPassword}</FormErrorMessage>
          </FormControl>

          <Button w="100%" colorScheme="blue" size="lg" type="submit">
            Utwórz konto
          </Button>
        </VStack>

        <Divider my={6} />

        <Text textAlign="center" mt={6} fontSize="sm">
          Masz już konto?{" "}
          <Button variant="link" colorScheme="blue" onClick={() => navigate("/login")}>
            Zaloguj się
          </Button>
        </Text>
      </Box>
    </Flex>
  );
}
