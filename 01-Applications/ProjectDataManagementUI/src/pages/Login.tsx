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
  useToast,
  useColorModeValue,
  InputGroup,
  InputRightElement,
  IconButton,
  Text,
  Divider,
} from "@chakra-ui/react";

import { useNavigate, useLocation } from "react-router-dom";
import { Eye, EyeOff } from "lucide-react";
import { GoogleLogin } from "@react-oauth/google";

import { AuthContext } from "../context/AuthContext";

export default function Login() {
  const navigate = useNavigate();
  const location = useLocation();
  const toast = useToast();

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
      toast({
        title: result.message || "Błędne dane",
        status: "error",
        duration: 3000,
      });
      return;
    }

    toast({
      title: "Zalogowano",
      status: "success",
      duration: 2000,
    });

    const from = (location.state as any)?.from?.pathname || "/dashboard";
    navigate(from, { replace: true });
  };

  const handleGoogleLogin = async (credential: any) => {
    const result = await googleLogin(credential.credential);

    if (!result.success) {
      toast({
        title: result.message || "Błąd Google",
        status: "error",
      });
      return;
    }

    navigate("/dashboard", { replace: true });
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
        maxW="420px"
        border="1px solid"
        borderColor="rgba(0,0,0,0.06)"
      >
        <Heading mb={6} textAlign="center" fontWeight="700">
          Zaloguj się
        </Heading>

        <VStack spacing={4} as="form" onSubmit={handleLocalLogin}>
          <FormControl>
            <FormLabel color={labelColor} fontWeight="500">
              Email
            </FormLabel>
            <Input
              type="email"
              value={email}
              size="lg"
              onChange={(e) => setEmail(e.target.value)}
            />
          </FormControl>

          <FormControl>
            <FormLabel color={labelColor} fontWeight="500">
              Hasło
            </FormLabel>
            <InputGroup>
              <Input
                type={showPassword ? "text" : "password"}
                value={password}
                size="lg"
                onChange={(e) => setPassword(e.target.value)}
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
          </FormControl>

          <Button w="100%" colorScheme="blue" size="lg" type="submit" isLoading={loading}>
            Zaloguj się
          </Button>
        </VStack>

        <Divider my={6} />

        <Text textAlign="center" color={textSecondary} mb={3} fontSize="sm">
          lub użyj konta Google
        </Text>

        <Flex justify="center">
          <GoogleLogin
            onSuccess={handleGoogleLogin}
            onError={() =>
              toast({ title: "Google error", status: "error" })
            }
          />
        </Flex>

        <Button variant="link" mt={6} width="100%" onClick={() => navigate("/register")}>
          Utwórz nowe konto
        </Button>
      </Box>
    </Flex>
  );
}
