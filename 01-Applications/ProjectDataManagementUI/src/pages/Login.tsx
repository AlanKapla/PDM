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
        title: result.message || "Błędne dane logowania",
        status: "error",
        duration: 3000,
      });
      return;
    }

    toast({
      title: "Zalogowano pomyślnie",
      status: "success",
      duration: 2000,
    });

    const from = (location.state as any)?.from?.pathname || "/dashboard";
    navigate(from, { replace: true });
  };

  const handleGoogleLogin = async (credentialResponse: any) => {
    const token = credentialResponse.credential;

    const result = await googleLogin(token);

    if (!result.success) {
      toast({
        title: result.message || "Błąd logowania przez Google",
        status: "error",
        duration: 3000,
      });
      return;
    }

    toast({
      title: "Zalogowano przez Google",
      status: "success",
      duration: 2000,
    });

    navigate("/dashboard", { replace: true });
  };

  const cardBg = useColorModeValue("white", "gray.800");
  const pageBg = useColorModeValue("gray.50", "gray.900");
  const labelColor = useColorModeValue("gray.700", "gray.300");

  return (
    <Flex justify="center" align="center" minH="100vh" bg={pageBg}>
      <Box bg={cardBg} p={8} rounded="lg" shadow="lg" maxW="400px" width="100%">
        <Heading mb={6} textAlign="center">Logowanie</Heading>

        {/* LOGOWANIE LOKALNE */}
        <VStack spacing={4} as="form" onSubmit={handleLocalLogin}>
          <FormControl>
            <FormLabel color={labelColor}>Email</FormLabel>
            <Input 
              type="email" 
              value={email}
              onChange={(e) => setEmail(e.target.value)} 
            />
          </FormControl>

          <FormControl>
            <FormLabel color={labelColor}>Hasło</FormLabel>
            <InputGroup>
              <Input
                type={showPassword ? "text" : "password"}
                value={password}
                onChange={(e) => setPassword(e.target.value)}
              />
              <InputRightElement>
                <IconButton
                  aria-label="toggle password"
                  icon={showPassword ? <EyeOff size={18} /> : <Eye size={18} />}
                  onClick={() => setShowPassword(!showPassword)}
                  variant="ghost"
                  size="sm"
                />
              </InputRightElement>
            </InputGroup>
          </FormControl>

          <Button width="100%" colorScheme="blue" type="submit" isLoading={loading}>
            Zaloguj się
          </Button>
        </VStack>

        {/* GOOGLE LOGIN */}
        <Box mt={6} textAlign="center">
          <GoogleLogin
            onSuccess={handleGoogleLogin}
            onError={() =>
              toast({
                title: "Google login error",
                status: "error",
              })
            }
          />
        </Box>

        <Button variant="link" mt={4} width="100%" onClick={() => navigate("/register")}>
          Utwórz konto
        </Button>
      </Box>
    </Flex>
  );
}
