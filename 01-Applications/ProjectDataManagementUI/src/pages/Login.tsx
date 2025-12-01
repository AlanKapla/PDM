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
      bg="#0f0f0f"
      px={4}
    >
      <Box
        bg="#131313"
        border="1px solid #1d1d1d"
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
              <FormLabel color="gray.300" fontSize="sm">
                Email
              </FormLabel>
              <Input
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                placeholder="twoj@email.com"
                bg="#0f0f0f"
                border="1px solid #2a2a2a"
                _placeholder={{ color: "gray.600" }}
              />
            </FormControl>

            <FormControl>
              <FormLabel color="gray.300" fontSize="sm">
                Hasło
              </FormLabel>
              <InputGroup>
                <Input
                  type={showPassword ? "text" : "password"}
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  bg="#0f0f0f"
                  border="1px solid #2a2a2a"
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

            <Flex justify="space-between" fontSize="sm" color="gray.400">
              <Button
                variant="link"
                color="gray.400"
                onClick={() => navigate("/register")}
              >
                Utwórz konto
              </Button>
              <Button
                variant="link"
                color="gray.400"
                onClick={() => navigate("/forgot-password")}
              >
                Zapomniałeś hasła?
              </Button>
            </Flex>
          </VStack>

          {/* Separator */}
          <HStack align="center" spacing={3}>
            <Divider borderColor="#2a2a2a" />
            <Text fontSize="xs" color="gray.500">
              lub
            </Text>
            <Divider borderColor="#2a2a2a" />
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
