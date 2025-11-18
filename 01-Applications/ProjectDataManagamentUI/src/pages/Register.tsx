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
} from "@chakra-ui/react";

import { registerUser } from "../services/authService";

interface RegisterForm {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
}

export default function Register() {
  const toast = useToast();
  const navigate = useNavigate();   

  const [form, setForm] = useState<RegisterForm>({
    email: "",
    password: "",
    firstName: "",
    lastName: "",
  });

  const [errors, setErrors] = useState<Partial<RegisterForm>>({});

  const handleChange = (e: ChangeEvent<HTMLInputElement>) => {
    setForm({ ...form, [e.target.name]: e.target.value });
  };

  const validate = () => {
    const newErrors: Partial<RegisterForm> = {};

    if (!form.email.includes("@")) newErrors.email = "Podaj poprawny email";
    if (form.password.length < 6)
      newErrors.password = "Hasło musi mieć minimum 6 znaków";
    if (!form.firstName.trim()) newErrors.firstName = "Podaj imię";
    if (!form.lastName.trim()) newErrors.lastName = "Podaj nazwisko";

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
        status: "success",
        duration: 3000,
        isClosable: true,
      });

      setForm({
        email: "",
        password: "",
        firstName: "",
        lastName: "",
      });

      navigate("/login");

    } catch (err) {
      toast({
        title: "Błąd serwera",
        status: "error",
        duration: 3000,
        isClosable: true,
      });
    }
  };

  return (
    <Flex justify="center" align="center" minH="100vh" bg="gray.50">
      <Box bg="white" p={8} rounded="lg" shadow="lg" width="400px">
        <Heading mb={6} textAlign="center" size="lg">
          Rejestracja
        </Heading>

        <VStack spacing={4} as="form" onSubmit={handleSubmit}>
          <FormControl isInvalid={!!errors.firstName}>
            <FormLabel>Imię</FormLabel>
            <Input
              name="firstName"
              value={form.firstName}
              onChange={handleChange}
            />
            <FormErrorMessage>{errors.firstName}</FormErrorMessage>
          </FormControl>

          <FormControl isInvalid={!!errors.lastName}>
            <FormLabel>Nazwisko</FormLabel>
            <Input
              name="lastName"
              value={form.lastName}
              onChange={handleChange}
            />
            <FormErrorMessage>{errors.lastName}</FormErrorMessage>
          </FormControl>

          <FormControl isInvalid={!!errors.email}>
            <FormLabel>Email</FormLabel>
            <Input
              name="email"
              type="email"
              value={form.email}
              onChange={handleChange}
            />
            <FormErrorMessage>{errors.email}</FormErrorMessage>
          </FormControl>

          <FormControl isInvalid={!!errors.password}>
            <FormLabel>Hasło</FormLabel>
            <Input
              name="password"
              type="password"
              value={form.password}
              onChange={handleChange}
            />
            <FormErrorMessage>{errors.password}</FormErrorMessage>
          </FormControl>

          <Button width="100%" colorScheme="blue" type="submit">
            Zarejestruj
          </Button>

          <Box textAlign="center">
            Masz już konto?{" "}
            <Button
              variant="link"
              colorScheme="blue"
              onClick={() => navigate("/login")}
            >
              Zaloguj się
            </Button>
          </Box>
        </VStack>
      </Box>
    </Flex>
  );
}
