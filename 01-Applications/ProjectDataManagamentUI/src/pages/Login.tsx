import { useState, useContext } from "react";
import type { ChangeEvent, FormEvent } from "react";
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

import { loginUser } from "../services/authService";
import { AuthContext } from "../context/AuthContext";
import { useNavigate } from "react-router-dom";

interface LoginForm {
  email: string;
  password: string;
}

export default function Login() {
  const toast = useToast();
  const navigate = useNavigate();
  const { setToken } = useContext(AuthContext);

  const [form, setForm] = useState<LoginForm>({
    email: "",
    password: "",
  });

  const [errors, setErrors] = useState<Partial<LoginForm>>({});

  const handleChange = (e: ChangeEvent<HTMLInputElement>) => {
    setForm({ ...form, [e.target.name]: e.target.value });
  };

  const validate = () => {
    const newErrors: Partial<LoginForm> = {};

    if (!form.email.includes("@")) newErrors.email = "Podaj poprawny email";
    if (form.password.length < 1) newErrors.password = "Podaj hasło";

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleLogin = async (e: FormEvent) => {
    e.preventDefault();

    if (!validate()) return;

    try {
      const result = await loginUser(form);

      setToken(result.token);
      localStorage.setItem("token", result.token);

      toast({
        title: "Zalogowano!",
        status: "success",
        duration: 3000,
        isClosable: true,
      });

      navigate("/");
    } catch (error) {
      toast({
        title: "Błąd logowania",
        description:
          error instanceof Error ? error.message : "Wystąpił nieoczekiwany błąd.",
        status: "error",
        duration: 4000,
        isClosable: true,
      });
    }
  };

  return (
    <Flex justify="center" align="center" minH="100vh" bg="gray.50">
      <Box bg="white" p={8} rounded="lg" shadow="lg" width="400px">
        <Heading mb={6} textAlign="center" size="lg">
          Logowanie
        </Heading>

        <VStack spacing={4} as="form" onSubmit={handleLogin}>
          <FormControl isInvalid={!!errors.email}>
            <FormLabel>Email</FormLabel>
            <Input
              type="email"
              name="email"
              value={form.email}
              onChange={handleChange}
              placeholder="email@example.com"
            />
            <FormErrorMessage>{errors.email}</FormErrorMessage>
          </FormControl>

          <FormControl isInvalid={!!errors.password}>
            <FormLabel>Hasło</FormLabel>
            <Input
              type="password"
              name="password"
              value={form.password}
              onChange={handleChange}
              placeholder="••••••••"
            />
            <FormErrorMessage>{errors.password}</FormErrorMessage>
          </FormControl>

          <Button width="100%" colorScheme="blue" type="submit">
            Zaloguj się
          </Button>

          <Box textAlign="center">
            Nie masz konta?{" "}
            <Button
              variant="link"
              colorScheme="blue"
              onClick={() => navigate("/register")}
            >
              Zarejestruj się
            </Button>
          </Box>

        </VStack>
      </Box>
    </Flex>
  );
}
