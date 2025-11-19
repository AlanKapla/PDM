import { useEffect, useState } from "react";
import {
  Box,
  Heading,
  Text,
  Spinner,
  VStack,
  useColorModeValue,
  Button,
} from "@chakra-ui/react";
import MainLayout from "../layout/MainLayout";
import { getUserDetails } from "../services/userService";

interface UserDetails {
  email: string;
  lastTenantId?: string | null;
}

export default function Profile() {
  const [user, setUser] = useState<UserDetails | null>(null);
  const [loading, setLoading] = useState(true);

  const cardBg = useColorModeValue("white", "gray.800");
  const cardText = useColorModeValue("gray.700", "gray.300");
  const pageBg = useColorModeValue("gray.50", "gray.900");

  useEffect(() => {
    async function load() {
      try {
        const data = await getUserDetails();
        setUser(data);
      } catch (e) {
        console.error("Błąd ładowania profilu:", e);
      } finally {
        setLoading(false);
      }
    }
    load();
  }, []);

  if (loading) {
    return (
      <MainLayout>
        <VStack justify="center" minH="100vh">
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
          p={8}
          rounded="2xl"
          shadow="xl"
          maxW="600px"
          mx="auto"
        >
          <Heading mb={4} color={useColorModeValue("black", "white")}>
            Profil użytkownika
          </Heading>

          <Text fontSize="lg" mb={4} color={cardText}>
            <strong>Email:</strong> {user?.email}
          </Text>

          <Text fontSize="lg" mb={6} color={cardText}>
            <strong>Tenant ID:</strong>{" "}
            {user?.lastTenantId ?? "Brak"}
          </Text>

          <Button colorScheme="blue" onClick={() => alert("Edytowanie profilu (przykład)")}>
            Edytuj profil
          </Button>
        </Box>
      </Box>
    </MainLayout>
  );
}
