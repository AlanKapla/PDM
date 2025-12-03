import { useState, useContext } from "react";
import {
  Box,
  Button,
  Flex,
  Heading,
  Input,
  VStack,
  FormControl,
  FormLabel,
  InputGroup,
  InputRightElement,
  IconButton,
  Text,
  Divider,
  HStack,
} from "@chakra-ui/react";

import { useNavigate, useLocation } from "react-router-dom";
import { Eye, EyeOff, LogIn } from "lucide-react";
import { GoogleLogin } from "@react-oauth/google";

import { AuthContext } from "../context/AuthContext";

export default function Login() {
  const navigate = useNavigate();
  const location = useLocation();
  const { login, googleLogin } = useContext(AuthContext);

  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [loading, setLoading] = useState(false);

  const handleLocalLogin = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);

    const result = await login(email, password);
    setLoading(false);

    if (!result.success) {
      // zakładam, że login sam pokazuje toast / obsługuje błędy
      return;
    }

    const from = (location.state as any)?.from?.pathname || "/dashboard";
    navigate(from, { replace: true });
  };

  const handleGoogleLogin = async (credentialResponse: any) => {
    const token = credentialResponse.credential;
    if (!token) return;

    const result = await googleLogin(token);

    if (!result.success) {
      return;
    }

    navigate("/dashboard", { replace: true });
  };

  return (
    <Flex
      justify="center"
      align="center"
      minH="100vh"
      bg="gray.50"
      px={4}
    >
      <Box
        bg="white"
        border="1px solid"
        borderColor="gray.200"
        rounded="lg"
        p={10}
        maxW="420px"
        w="100%"
      >
        <VStack spacing={6} align="stretch">
          {/* Nagłówek */}
          <VStack spacing={1} align="center">
            <LogIn size={28} color="#e5e7eb" />
            <Heading size="lg" color="gray.100">
              Logowanie
            </Heading>
            <Text fontSize="sm" color="gray.400">
              Zaloguj się, aby przejść do panelu projektu.
            </Text>
          </VStack>

          {/* Formularz logowania lokalnego */}
          <VStack as="form" onSubmit={handleLocalLogin} spacing={4}>
            <FormControl>
              <FormLabel color="gray.700" fontSize="sm">
                Email
              </FormLabel>
              <Input
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                placeholder="twoj@email.com"
                bg="white"
                border="1px solid"
                borderColor="gray.300"
                _placeholder={{ color: "gray.600" }}
              />
            </FormControl>

            <FormControl>
              <FormLabel color="gray.700" fontSize="sm">
                Hasło
              </FormLabel>
              <InputGroup>
                <Input
                  type={showPassword ? "text" : "password"}
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  bg="white"
                  border="1px solid"
                  borderColor="gray.300"
                />
                <InputRightElement>
                  <IconButton
                    aria-label="Pokaż/ukryj hasło"
                    icon={showPassword ? <EyeOff size={18} /> : <Eye size={18} />}
                    onClick={() => setShowPassword(!showPassword)}
                    variant="ghost"
                    size="sm"
                  />
                </InputRightElement>
              </InputGroup>
            </FormControl>

            <Button
              type="submit"
              w="100%"
              colorScheme="blue"
              isLoading={loading}
            >
              Zaloguj się
            </Button>

            <Flex justify="space-between" fontSize="sm" color="gray.600">
              <Button
                variant="link"
                color="gray.600"
                onClick={() => navigate("/register")}
              >
                Utwórz konto
              </Button>
              <Button
                variant="link"
                color="gray.600"
                onClick={() => navigate("/forgot-password")}
              >
                Zapomniałeś hasła?
              </Button>
            </Flex>
          </VStack>

          {/* Separator */}
          <HStack align="center" spacing={3}>
            <Divider borderColor="#1e1e1e" />
            <Text fontSize="xs" color="gray.500">
              lub
            </Text>
            <Divider borderColor="#1e1e1e" />
          </HStack>

          {/* Google Login */}
          <Box textAlign="center">
            <GoogleLogin
              onSuccess={handleGoogleLogin}
              onError={() => {
                // ewentualnie log / toast, ale tutaj zostawiamy minimalistycznie
                console.error("Google login error");
              }}
            />
          </Box>
        </VStack>
      </Box>
    </Flex>
  );
}
