import { useState, useContext } from "react";
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
} from "@chakra-ui/react";

import { loginUser } from "../services/authService";
import { AuthContext } from "../context/AuthContext";

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

    const result = await loginUser(form);

    if (!result) {
      toast({
        title: "Błąd logowania",
        status: "error",
        duration: 3000,
        isClosable: true,
      });
      return;
    }

    setToken(result.token);
    localStorage.setItem("token", result.token);

    toast({
      title: "Zalogowano!",
      status: "success",
      duration: 3000,
      isClosable: true,
    });

    navigate("/");
  };

  const cardBg = useColorModeValue("white", "gray.800");
  const pageBg = useColorModeValue("gray.50", "gray.900");
  const labelColor = useColorModeValue("gray.700", "gray.300");

  return (
    <Flex justify="center" align="center" minH="100vh" bg={pageBg}>
      <Box
        bg={cardBg}
        p={8}
        rounded="lg"
        shadow="lg"
        width="400px"
      >
        <Heading mb={6} textAlign="center" size="lg">
          Logowanie
        </Heading>

        <VStack spacing={4} as="form" onSubmit={handleLogin}>
          <FormControl isInvalid={!!errors.email}>
            <FormLabel color={labelColor}>Email</FormLabel>
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
            <FormLabel color={labelColor}>Hasło</FormLabel>
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

          <Text fontSize="sm">
            Nie masz konta?{" "}
            <Button
              variant="link"
              colorScheme="blue"
              onClick={() => navigate("/register")}
            >
              Zarejestruj się
            </Button>
          </Text>
        </VStack>
      </Box>
    </Flex>
  );
}
