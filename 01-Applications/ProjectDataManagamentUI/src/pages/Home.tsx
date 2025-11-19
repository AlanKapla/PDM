import { useContext, useEffect, useState } from "react";
import {
  Box,
  Button,
  Heading,
  Text,
  Spinner,
  VStack,
  useColorModeValue,
} from "@chakra-ui/react";

import { AuthContext } from "../context/AuthContext";
import { getUserDetails } from "../services/userService";
import MainLayout from "../layout/MainLayout";

interface UserDetails {
  email: string;
  lastTenantId?: string | null;
}

export default function Home() {
  const { logout } = useContext(AuthContext);
  const [user, setUser] = useState<UserDetails | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    async function loadUser() {
      try {
        const data = await getUserDetails();
        setUser(data);
      } catch (err) {
        console.error("Błąd pobierania danych użytkownika", err);
      } finally {
        setLoading(false);
      }
    }

    loadUser();
  }, []);

  const cardBg = useColorModeValue("white", "gray.800");
  const cardTextColor = useColorModeValue("gray.600", "gray.300");
  const pageBg = useColorModeValue("gray.50", "gray.900");

  if (loading) {
    return (
      <MainLayout>
        <VStack minH="100vh" justify="center">
          <Spinner size="xl" />
        </VStack>
      </MainLayout>
    );
  }

  return (
    <MainLayout>
      <Box p={10} bg={pageBg} minH="100vh">
        <Box
          bg={cardBg}
          color={cardTextColor}
          p={8}
          rounded="2xl"
          shadow="xl"
          maxW="600px"
          mx="auto"
          textAlign="center"
        >
          <Heading mb={4} color={useColorModeValue("black", "white")}>
            Witaj ponownie!
          </Heading>

          <Text fontSize="lg" mb={6}>
            Jesteś zalogowany jako:
            <br />
            <strong>{user?.email}</strong>
          </Text>

          <Button colorScheme="red" onClick={logout}>
            Wyloguj się
          </Button>
        </Box>
      </Box>
    </MainLayout>
  );
}
