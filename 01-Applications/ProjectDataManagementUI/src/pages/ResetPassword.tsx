import { useState, useEffect } from "react";
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

  const [token, setToken] = useState("");
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);
  const [loading, setLoading] = useState(false);
  const [passwordError, setPasswordError] = useState("");
  const [confirmError, setConfirmError] = useState("");

  useEffect(() => {
    const tokenFromUrl = searchParams.get("token");
    if (tokenFromUrl) {
      setToken(tokenFromUrl);
    }
  }, [searchParams]);

  const validatePasswordStrength = (pwd: string) => {
    const errors: string[] = [];
    
    if (pwd.length < 8) {
      errors.push("Password must be at least 8 characters long");
    }
    if (!/[A-Z]/.test(pwd)) {
      errors.push("Password must contain at least one uppercase letter");
    }
    if (!/[a-z]/.test(pwd)) {
      errors.push("Password must contain at least one lowercase letter");
    }
    if (!/[0-9]/.test(pwd)) {
      errors.push("Password must contain at least one digit");
    }
    if (!/[^a-zA-Z0-9]/.test(pwd)) {
      errors.push("Password must contain at least one special character");
    }
    
    return errors;
  };

  const getPasswordChecks = (pwd: string) => {
    return [
      { label: "Co najmniej 8 znaków", valid: pwd.length >= 8 },
      { label: "Wielka litera (A-Z)", valid: /[A-Z]/.test(pwd) },
      { label: "Mała litera (a-z)", valid: /[a-z]/.test(pwd) },
      { label: "Cyfra (0-9)", valid: /[0-9]/.test(pwd) },
      { label: "Znak specjalny (!@#$...)", valid: /[^a-zA-Z0-9]/.test(pwd) },
    ];
  };

  const validatePassword = () => {
    const strengthErrors = validatePasswordStrength(password);
    if (strengthErrors.length > 0) {
      setPasswordError(strengthErrors.join(". "));
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

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!token) {
      toast({
        title: "Brak tokenu",
        description: "Link resetowania hasła jest nieprawidłowy",
        status: "error",
        duration: 3000,
        isClosable: true,
      });
      return;
    }

    if (!validatePassword()) {
      return;
    }

    setLoading(true);

    try {
      const success = await resetPassword(token, password);

      if (success) {
        toast({
          title: "Hasło zmienione",
          description: "Możesz teraz zalogować się nowym hasłem",
          status: "success",
          duration: 5000,
          isClosable: true,
        });
        navigate("/login");
      } else {
        toast({
          title: "Błąd",
          description: "Token jest nieprawidłowy lub wygasł",
          status: "error",
          duration: 5000,
          isClosable: true,
        });
      }
    } catch (error) {
      console.error("Błąd resetowania hasła:", error);
      toast({
        title: "Błąd połączenia z serwerem",
        status: "error",
        duration: 3000,
        isClosable: true,
      });
    } finally {
      setLoading(false);
    }
  };

  const cardBg = useColorModeValue("white", "gray.800");
  const pageBg = useColorModeValue("gray.50", "gray.900");
  const labelColor = useColorModeValue("gray.700", "gray.300");

  return (
    <Flex justify="center" align="center" minH="100vh" bg={pageBg}>
      <Box bg={cardBg} p={8} rounded="lg" shadow="lg" width="400px">
        <Heading mb={6} textAlign="center" size="lg">
          Ustaw nowe hasło
        </Heading>

        <VStack spacing={4} as="form" onSubmit={handleSubmit}>
          {!token && (
            <Text color="red.500" fontSize="sm" textAlign="center">
              Brak tokenu resetowania. Użyj linku z emaila.
            </Text>
          )}

          <FormControl>
            <FormLabel color={labelColor}>Token (opcjonalnie)</FormLabel>
            <Input
              type="text"
              value={token}
              onChange={(e) => setToken(e.target.value)}
              placeholder="Token z emaila"
            />
            <Text fontSize="xs" color="gray.500" mt={1}>
              Token został automatycznie wczytany z linku
            </Text>
          </FormControl>

          <FormControl isInvalid={!!passwordError}>
            <FormLabel color={labelColor}>Nowe hasło</FormLabel>
            <InputGroup>
              <Input
                type={showPassword ? "text" : "password"}
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                placeholder="Minimum 8 znaków"
                required
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
            {password && (
              <List spacing={1} mt={2} fontSize="sm">
                {getPasswordChecks(password).map((check, idx) => (
                  <ListItem key={idx} color={check.valid ? "green.500" : "gray.500"}>
                    <ListIcon as={check.valid ? Check : X} />
                    {check.label}
                  </ListItem>
                ))}
              </List>
            )}
            <FormErrorMessage>{passwordError}</FormErrorMessage>
          </FormControl>

          <FormControl isInvalid={!!confirmError}>
            <FormLabel color={labelColor}>Potwierdź hasło</FormLabel>
            <InputGroup>
              <Input
                type={showConfirmPassword ? "text" : "password"}
                value={confirmPassword}
                onChange={(e) => setConfirmPassword(e.target.value)}
                placeholder="Powtórz nowe hasło"
                required
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
            <FormErrorMessage>{confirmError}</FormErrorMessage>
          </FormControl>

          <Button 
            width="100%" 
            colorScheme="blue" 
            type="submit" 
            isLoading={loading}
            isDisabled={!token}
          >
            Zmień hasło
          </Button>

          <Button 
            variant="link" 
            onClick={() => navigate("/login")}
          >
            Powrót do logowania
          </Button>
        </VStack>
      </Box>
    </Flex>
  );
}
