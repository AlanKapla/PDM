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
  Text,
  Icon,
  useToast,
} from "@chakra-ui/react";
import { Mail, CheckCircle2 } from "lucide-react";
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

    if (!email.trim()) {
      toast({
        title: "Podaj adres email",
        status: "warning",
        duration: 2500,
      });
      return;
    }

    setLoading(true);

    try {
      const ok = await requestPasswordReset(email);

      if (ok) {
        setSent(true);
      } else {
        toast({
          title: "Błąd",
          description: "Nie udało się wysłać wiadomości",
          status: "error",
          duration: 2500,
        });
      }
    } catch {
      toast({
        title: "Błąd połączenia",
        status: "error",
        duration: 2500,
      });
    } finally {
      setLoading(false);
    }
  };

  //
  // ---------- WIDOK: EMAIL WYSŁANY ----------
  //
  if (sent) {
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
          textAlign="center"
        >
          <Icon as={CheckCircle2} color="green.400" boxSize={20} mb={4} />

          <Heading size="lg" color="gray.800" mb={4}>
            Email wysłany
          </Heading>

          <Text color="gray.600" fontSize="sm" mb={6}>
            Jeśli konto z tym adresem istnieje, otrzymasz link do resetowania
            hasła. Sprawdź również folder spam.
          </Text>

          <Button
            colorScheme="blue"
            w="100%"
            onClick={() => navigate("/login")}
          >
            Powrót do logowania
          </Button>
        </Box>
      </Flex>
    );
  }

  //
  // ---------- WIDOK: FORMULARZ RESETU ----------
  //
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
        <Flex direction="column" align="center" mb={6}>
          <Icon as={Mail} color="gray.600" boxSize={20} mb={3} />
          <Heading size="lg" color="gray.800">
            Resetowanie hasła
          </Heading>
        </Flex>

        <VStack as="form" onSubmit={handleSubmit} spacing={5}>
          <Text color="gray.600" fontSize="sm" textAlign="center">
            Podaj adres email przypisany do Twojego konta, a wyślemy do Ciebie
            link do ustawienia nowego hasła.
          </Text>

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

          <Button
            type="submit"
            w="100%"
            colorScheme="blue"
            isLoading={loading}
          >
            Wyślij link resetujący
          </Button>

          <Button
            variant="link"
            color="gray.600"
            onClick={() => navigate("/login")}
          >
            Powrót do logowania
          </Button>
        </VStack>
      </Box>
    </Flex>
  );
}
