import { useState } from "react";
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
} from "@chakra-ui/react";
import { useNavigate } from "react-router-dom";

export default function Login() {
  const navigate = useNavigate();
  const toast = useToast();

  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [loading, setLoading] = useState(false);

  const handleLogin = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);

    const payload = {
      email,
      password,
      externalToken: "",
      provider: 0,
    };

    try {
      const res = await fetch("http://localhost:5121/api/User/login", {
        method: "POST",
        credentials: "include", 
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify(payload),
      });

      if (res.ok) {
        toast({
          title: "Zalogowano pomyślnie",
          status: "success",
          duration: 3000,
          isClosable: true,
        });

        navigate("/"); 
      } else {
        toast({
          title: "Błędne dane logowania",
          status: "error",
          duration: 3000,
          isClosable: true,
        });
      }
    } catch (err) {
      console.error(err);
      toast({
        title: "Błąd połączenia z serwerem",
        status: "error",
        duration: 3000,
        isClosable: true,
      });
    }
    
    setLoading(false);
  };

  const cardBg = useColorModeValue("white", "gray.800");
  const pageBg = useColorModeValue("gray.50", "gray.900");
  const labelColor = useColorModeValue("gray.700", "gray.300");

  return (
    <Flex justify="center" align="center" minH="100vh" bg={pageBg}>
      <Box bg={cardBg} p={8} rounded="lg" shadow="lg" width="400px">
        <Heading mb={6} textAlign="center" size="lg">
          Logowanie
        </Heading>

        <VStack spacing={4} as="form" onSubmit={handleLogin}>
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
            <Input
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
            />
          </FormControl>

          <Button width="100%" colorScheme="blue" type="submit" isLoading={loading}>
            Zaloguj się
          </Button>

          <Button variant="link" onClick={() => navigate("/register")}>
            Utwórz konto
          </Button>
        </VStack>
      </Box>
    </Flex>
  );
}
