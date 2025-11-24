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
  Text,
} from "@chakra-ui/react";
import { useNavigate } from "react-router-dom";
import { requestPasswordReset } from "../services/authService";

export default function ForgotPassword() {
  const navigate = useNavigate();
  const toast = useToast();

  const [email, setEmail] = useState("");
  const [loading, setLoading] = useState(false);
  const [sent, setSent] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!email) {
      toast({
        title: "Podaj adres email",
        status: "warning",
        duration: 3000,
        isClosable: true,
      });
      return;
    }

    setLoading(true);

    try {
      const success = await requestPasswordReset(email);

      if (success) {
        setSent(true);
        toast({
          title: "Email wysłany",
          description: "Sprawdź swoją skrzynkę pocztową",
          status: "success",
          duration: 5000,
          isClosable: true,
        });
      } else {
        toast({
          title: "Błąd",
          description: "Nie udało się wysłać emaila",
          status: "error",
          duration: 3000,
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

  if (sent) {
    return (
      <Flex justify="center" align="center" minH="100vh" bg={pageBg}>
        <Box bg={cardBg} p={8} rounded="lg" shadow="lg" width="400px">
          <Heading mb={6} textAlign="center" size="lg">
            Email wysłany
          </Heading>

          <VStack spacing={4}>
            <Text textAlign="center">
              Jeśli konto z tym adresem email istnieje, otrzymasz wiadomość z linkiem do resetowania hasła.
            </Text>

            <Text textAlign="center" fontSize="sm" color="gray.500">
              Sprawdź również folder spam.
            </Text>

            <Button width="100%" colorScheme="blue" onClick={() => navigate("/login")}>
              Powrót do logowania
            </Button>
          </VStack>
        </Box>
      </Flex>
    );
  }

  return (
    <Flex justify="center" align="center" minH="100vh" bg={pageBg}>
      <Box bg={cardBg} p={8} rounded="lg" shadow="lg" width="400px">
        <Heading mb={6} textAlign="center" size="lg">
          Resetowanie hasła
        </Heading>

        <VStack spacing={4} as="form" onSubmit={handleSubmit}>
          <Text textAlign="center" fontSize="sm" color={labelColor}>
            Podaj adres email przypisany do Twojego konta. Wyślemy Ci link do resetowania hasła.
          </Text>

          <FormControl>
            <FormLabel color={labelColor}>Email</FormLabel>
            <Input
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="twoj@email.com"
              required
            />
          </FormControl>

          <Button 
            width="100%" 
            colorScheme="blue" 
            type="submit" 
            isLoading={loading}
          >
            Wyślij link resetujący
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
