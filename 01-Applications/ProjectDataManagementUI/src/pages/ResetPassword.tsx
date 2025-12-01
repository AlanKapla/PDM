import { useState, useEffect, type FormEvent, type ChangeEvent } from "react";
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
import { useNavigate, useSearchParams } from "react-router-dom";
import { Eye, EyeOff, Check, X } from "lucide-react";
import { resetPassword } from "../services/authService";

export default function ResetPassword() {
  const navigate = useNavigate();
  const toast = useToast();
  const [searchParams] = useSearchParams();

  const [token, setToken] = useState<string | null>(null);
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");

  const [showPassword, setShowPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);

  const [loading, setLoading] = useState(false);

  const [passwordError, setPasswordError] = useState("");
  const [confirmError, setConfirmError] = useState("");

  useEffect(() => {
    const t = searchParams.get("token");
    setToken(t);
  }, [searchParams]);

  const validateStrength = (pwd: string) => {
    const errors: string[] = [];
    
    if (pwd.length < 8) errors.push("Minimum 8 znaków");
    if (!/[A-Z]/.test(pwd)) errors.push("Wielka litera (A-Z)");
    if (!/[a-z]/.test(pwd)) errors.push("Mała litera (a-z)");
    if (!/[0-9]/.test(pwd)) errors.push("Cyfra (0-9)");
    if (!/[^a-zA-Z0-9]/.test(pwd)) errors.push("Znak specjalny");

    return errors;
  };

  const checklist = [
    { label: "Co najmniej 8 znaków", test: (p: string) => p.length >= 8 },
    { label: "Wielka litera (A-Z)", test: (p: string) => /[A-Z]/.test(p) },
    { label: "Mała litera (a-z)", test: (p: string) => /[a-z]/.test(p) },
    { label: "Cyfra (0-9)", test: (p: string) => /[0-9]/.test(p) },
    { label: "Znak specjalny (!@#$…)", test: (p: string) => /[^a-zA-Z0-9]/.test(p) },
  ];

  const validate = () => {
    const errs = validateStrength(password);
    if (errs.length > 0) {
      setPasswordError("Hasło nie spełnia wymagań");
      return false;
    }

    if (password !== confirmPassword) {
      setConfirmError("Hasła muszą być identyczne");
      return false;
    }

    setPasswordError("");
    setConfirmError("");
    return true;
  };

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();

    if (!token) {
      toast({
        title: "Nieprawidłowy link",
        description: "Brakuje tokenu resetowania",
        status: "error",
      });
      return;
    }

    if (!validate()) return;

    setLoading(true);
    try {
      const success = await resetPassword(token, password);
      if (!success) {
        toast({
          title: "Błąd resetowania",
          description: "Token jest nieprawidłowy lub wygasł",
          status: "error",
        });
        return;
      }

      toast({
        title: "Hasło zmienione",
        description: "Możesz teraz się zalogować",
        status: "success",
      });

      navigate("/login");
    } catch (err) {
      toast({
        title: "Błąd serwera",
        status: "error",
      });
    } finally {
      setLoading(false);
    }
  };

  const cardBg = useColorModeValue("white", "gray.800");
  const pageBg = useColorModeValue("gray.50", "gray.900");

  // -------------------------------------------------------------------
  // EKRAN BEZ TOKENU – zamiast brzydkiego pola tokenu
  // -------------------------------------------------------------------
  if (!token) {
    return (
      <Flex justify="center" align="center" minH="100vh" bg={pageBg}>
        <Box bg={cardBg} p={8} rounded="lg" shadow="lg" maxW="400px" textAlign="center">
          <Heading size="md" mb={4}>Nieprawidłowy link</Heading>
          <Text mb={6} color="gray.500">
            Link resetowania hasła jest nieprawidłowy lub wygasł.
          </Text>
          <Button colorScheme="blue" onClick={() => navigate("/forgot-password")} w="100%">
            Wyślij nowy link
          </Button>
        </Box>
      </Flex>
    );
  }

  // -------------------------------------------------------------------
  // PRAWIDŁOWY FORMULARZ RESETOWANIA
  // -------------------------------------------------------------------
  return (
    <Flex justify="center" align="center" minH="100vh" bg={pageBg} px={4}>
      <Box bg={cardBg} p={8} rounded="lg" shadow="lg" maxW="400px" w="100%">
        <Heading mb={6} textAlign="center">
          Ustaw nowe hasło
        </Heading>

        <VStack spacing={4} as="form" onSubmit={handleSubmit}>
          <FormControl isInvalid={!!passwordError}>
            <FormLabel>Nowe hasło</FormLabel>
            <InputGroup>
              <Input
                type={showPassword ? "text" : "password"}
                value={password}
                onChange={(e: ChangeEvent<HTMLInputElement>) => setPassword(e.target.value)}
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

            {password && (
              <List spacing={1} mt={2} fontSize="sm">
                {checklist.map((item, i) => {
                  const valid = item.test(password);
                  return (
                    <ListItem key={i} color={valid ? "green.500" : "gray.500"}>
                      <ListIcon as={valid ? Check : X} />
                      {item.label}
                    </ListItem>
                  );
                })}
              </List>
            )}

            <FormErrorMessage>{passwordError}</FormErrorMessage>
          </FormControl>

          <FormControl isInvalid={!!confirmError}>
            <FormLabel>Potwierdź hasło</FormLabel>
            <InputGroup>
              <Input
                type={showConfirmPassword ? "text" : "password"}
                value={confirmPassword}
                onChange={(e: ChangeEvent<HTMLInputElement>) =>
                  setConfirmPassword(e.target.value)
                }
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
            <FormErrorMessage>{confirmError}</FormErrorMessage>
          </FormControl>

          <Button colorScheme="blue" type="submit" w="100%" isLoading={loading}>
            Zmień hasło
          </Button>

          <Button variant="link" onClick={() => navigate("/login")}>
            Powrót do logowania
          </Button>
        </VStack>
      </Box>
    </Flex>
  );
}
