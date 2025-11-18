import { useContext, useEffect, useState } from "react";
import { Box, Button, Heading, Text, Spinner, VStack } from "@chakra-ui/react";
import { AuthContext } from "../context/AuthContext";
import { getUserDetails } from "../services/userService";

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

  if (loading) {
    return (
      <VStack minH="100vh" justify="center">
        <Spinner size="xl" />
      </VStack>
    );
  }

  return (
    <Box p={10} bg="gray.50" minH="100vh">
      <Box
        bg="white"
        p={8}
        rounded="2xl"
        shadow="xl"
        maxW="600px"
        mx="auto"
        textAlign="center"
      >
        <Heading mb={4}>Witaj ponownie!</Heading>

        <Text fontSize="lg" mb={6} color="gray.600">
          Jesteś zalogowany jako:
          <br />
          <strong>{user?.email}</strong>
        </Text>

        <Button colorScheme="red" onClick={logout}>
          Wyloguj się
        </Button>
      </Box>
    </Box>
  );
}
