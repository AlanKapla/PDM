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
  InputGroup,
  InputRightElement,
  IconButton,
  List,
  ListItem,
  ListIcon,
  Divider,
} from "@chakra-ui/react";

import { Eye, EyeOff, Check, X } from "lucide-react";
import { GoogleLogin } from "@react-oauth/google";

import { registerUser, type RegisterForm } from "../services/authService";
import { AuthContext } from "../context/AuthContext";

interface FormWithConfirm extends RegisterForm {
  confirmPassword: string;
}

export default function Register() {
  const toast = useToast();
  const navigate = useNavigate();
  const { googleRegister } = useContext(AuthContext);

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

  const validatePassword = (password: string) => {
    const rules: string[] = [];
    if (password.length < 8) rules.push("Co najmniej 8 znaków");
    if (!/[A-Z]/.test(password)) rules.push("Wielka litera");
    if (!/[a-z]/.test(password)) rules.push("Mała litera");
    if (!/[0-9]/.test(password)) rules.push("Cyfra");
    if (!/[^a-zA-Z0-9]/.test(password)) rules.push("Znak specjalny");
    return rules;
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
    if (!form.email.includes("@")) newErrors.email = "Podaj poprawny email";

    const passwordErrors = validatePassword(form.password);
    if (passwordErrors.length > 0) {
      newErrors.password = "Hasło nie spełnia wymagań";
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
          description: "Spróbuj ponownie",
          status: "error",
          duration: 3000,
        });
        return;
      }

      toast({
        title: "Konto utworzone",
        description: "Sprawdź skrzynkę i aktywuj konto",
        status: "success",
        duration: 6000,
      });

      navigate("/login");
    } catch (err) {
      toast({
        title: "Błąd serwera",
        status: "error",
        duration: 3000,
      });
    }
  };

  const cardBg = useColorModeValue("white", "gray.800");
  const pageBg = useColorModeValue("gray.50", "gray.900");
  const labelColor = useColorModeValue("gray.700", "gray.300");

  return (
    <Flex justify="center" align="center" minH="100vh" bg={pageBg} px={4}>
      <Box bg={cardBg} p={8} rounded="xl" shadow="lg" w="100%" maxW="420px">
        <Heading mb={6} textAlign="center" size="lg">
          Rejestracja
        </Heading>

        <VStack spacing={4} as="form" onSubmit={handleSubmit}>
          <FormControl isInvalid={!!errors.firstName}>
            <FormLabel color={labelColor}>Imię</FormLabel>
            <Input name="firstName" value={form.firstName} onChange={handleChange} />
            <FormErrorMessage>{errors.firstName}</FormErrorMessage>
          </FormControl>

          <FormControl isInvalid={!!errors.lastName}>
            <FormLabel color={labelColor}>Nazwisko</FormLabel>
            <Input name="lastName" value={form.lastName} onChange={handleChange} />
            <FormErrorMessage>{errors.lastName}</FormErrorMessage>
          </FormControl>

          <FormControl isInvalid={!!errors.email}>
            <FormLabel color={labelColor}>Email</FormLabel>
            <Input name="email" type="email" value={form.email} onChange={handleChange} />
            <FormErrorMessage>{errors.email}</FormErrorMessage>
          </FormControl>

          {/* PASSWORD */}
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
                  aria-label="Pokaż/ukryj"
                  icon={showPassword ? <EyeOff size={18} /> : <Eye size={18} />}
                  onClick={() => setShowPassword(!showPassword)}
                  variant="ghost"
                  size="sm"
                />
              </InputRightElement>
            </InputGroup>

            {form.password && (
              <List spacing={1} mt={2} fontSize="sm">
                {passwordChecklist.map((item, idx) => {
                  const valid = item.fn(form.password);
                  return (
                    <ListItem key={idx} color={valid ? "green.500" : "gray.500"}>
                      <ListIcon as={valid ? Check : X} />
                      {item.label}
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
                value={form.confirmPassword}
                onChange={handleChange}
              />
              <InputRightElement>
                <IconButton
                  aria-label="Pokaż/ukryj"
                  icon={showConfirmPassword ? <EyeOff size={18} /> : <Eye size={18} />}
                  onClick={() => setShowConfirmPassword(!showConfirmPassword)}
                  variant="ghost"
                  size="sm"
                />
              </InputRightElement>
            </InputGroup>
            <FormErrorMessage>{errors.confirmPassword}</FormErrorMessage>
          </FormControl>

          <Button w="100%" colorScheme="blue" type="submit">
            Zarejestruj
          </Button>

          <Divider />

          <VStack spacing={2} w="100%">
            <Text fontSize="sm" color="gray.600">
              lub użyj konta Google
            </Text>

            <GoogleLogin
              onSuccess={async (response: any) => {
                const result = await googleRegister(response.credential);
                if (!result.success) {
                  toast({
                    title: result.message || "Błąd logowania Google",
                    status: "error",
                  });
                  return;
                }
                toast({ title: "Zarejestrowano przez Google", status: "success" });
                navigate("/dashboard");
              }}
              onError={() =>
                toast({ title: "Błąd Google", status: "error", duration: 3000 })
              }
            />
          </VStack>

          <Text fontSize="sm">
            Masz już konto?{" "}
            <Button variant="link" onClick={() => navigate("/login")} colorScheme="blue">
              Zaloguj się
            </Button>
          </Text>
        </VStack>
      </Box>
    </Flex>
  );
}
