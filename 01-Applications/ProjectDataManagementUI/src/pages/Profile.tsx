import { useEffect, useState } from "react";
import {
  Box,
  Heading,
  Text,
  Spinner,
  VStack,
  useColorModeValue,
  Button,
  Input,
  FormControl,
  FormLabel,
  HStack,
  useToast,
} from "@chakra-ui/react";
import MainLayout from "../layout/MainLayout";
import { getUserDetails, updateUserProfile } from "../services/userService";
import type { UserProfile } from "../types/auth.types";

export default function Profile() {
  const [user, setUser] = useState<UserProfile | null>(null);
  const [loading, setLoading] = useState(true);
  const [isEditing, setIsEditing] = useState(false);
  const [saving, setSaving] = useState(false);
  
  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName] = useState("");
  
  const toast = useToast();

  const cardBg = useColorModeValue("white", "gray.800");
  const cardText = useColorModeValue("gray.700", "gray.300");
  const pageBg = useColorModeValue("gray.50", "gray.900");
  const labelColor = useColorModeValue("gray.700", "gray.300");

  useEffect(() => {
    async function load() {
      try {
        const userData = await getUserDetails();
        
        setUser(userData);
        
        if (userData) {
          setFirstName(userData.firstName);
          setLastName(userData.lastName);
        }
      } catch (error) {
        console.error("Błąd ładowania danych:", error);
      } finally {
        setLoading(false);
      }
    }
    load();
  }, []);

  const handleEdit = () => {
    setIsEditing(true);
  };

  const handleCancel = () => {
    setIsEditing(false);
    if (user) {
      setFirstName(user.firstName);
      setLastName(user.lastName);
    }
  };

  const handleSave = async () => {
    if (!firstName.trim() || !lastName.trim()) {
      toast({
        title: "Błąd walidacji",
        description: "Imię i nazwisko nie mogą być puste",
        status: "error",
        duration: 3000,
        isClosable: true,
      });
      return;
    }

    setSaving(true);
    try {
      const success = await updateUserProfile(firstName, lastName);
      
      if (success) {
        setUser((prev) => prev ? { ...prev, firstName, lastName } : null);
        setIsEditing(false);
        toast({
          title: "Profil zaktualizowany",
          status: "success",
          duration: 3000,
          isClosable: true,
        });
      } else {
        toast({
          title: "Błąd aktualizacji",
          description: "Nie udało się zaktualizować profilu",
          status: "error",
          duration: 3000,
          isClosable: true,
        });
      }
    } catch (error) {
      console.error("Błąd aktualizacji profilu:", error);
      toast({
        title: "Błąd",
        description: "Wystąpił problem z połączeniem",
        status: "error",
        duration: 3000,
        isClosable: true,
      });
    } finally {
      setSaving(false);
    }
  };

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
      <Box p={{ base: 4, md: 10 }} bg={pageBg} minH="100vh">
        <Box
          bg={cardBg}
          p={{ base: 6, md: 8 }}
          rounded="2xl"
          shadow="xl"
          maxW="600px"
          mx="auto"
        >
          <Heading mb={6} color={useColorModeValue("black", "white")}>
            Profil użytkownika
          </Heading>

          <VStack spacing={4} align="stretch">
            {!isEditing ? (
              <>
                <Box>
                  <Text fontSize="sm" color={labelColor} mb={1}>
                    Imię
                  </Text>
                  <Text fontSize="lg" color={cardText}>
                    {user?.firstName}
                  </Text>
                </Box>

                <Box>
                  <Text fontSize="sm" color={labelColor} mb={1}>
                    Nazwisko
                  </Text>
                  <Text fontSize="lg" color={cardText}>
                    {user?.lastName}
                  </Text>
                </Box>

                <Box>
                  <Text fontSize="sm" color={labelColor} mb={1}>
                    Email
                  </Text>
                  <Text fontSize="lg" color={cardText}>
                    {user?.email}
                  </Text>
                </Box>

                <Button colorScheme="blue" onClick={handleEdit} mt={4}>
                  Edytuj profil
                </Button>
              </>
            ) : (
              <>
                <FormControl>
                  <FormLabel color={labelColor}>Imię</FormLabel>
                  <Input
                    value={firstName}
                    onChange={(e) => setFirstName(e.target.value)}
                    placeholder="Podaj imię"
                  />
                </FormControl>

                <FormControl>
                  <FormLabel color={labelColor}>Nazwisko</FormLabel>
                  <Input
                    value={lastName}
                    onChange={(e) => setLastName(e.target.value)}
                    placeholder="Podaj nazwisko"
                  />
                </FormControl>

                <Box>
                  <Text fontSize="sm" color={labelColor} mb={1}>
                    Email
                  </Text>
                  <Text fontSize="lg" color={cardText}>
                    {user?.email}
                  </Text>
                  <Text fontSize="xs" color="gray.500" mt={1}>
                    Email nie może być edytowany
                  </Text>
                </Box>

                <HStack spacing={3} mt={4}>
                  <Button
                    colorScheme="blue"
                    onClick={handleSave}
                    isLoading={saving}
                    flex={1}
                  >
                    Zapisz
                  </Button>
                  <Button
                    variant="outline"
                    onClick={handleCancel}
                    isDisabled={saving}
                    flex={1}
                  >
                    Anuluj
                  </Button>
                </HStack>
              </>
            )}
          </VStack>
        </Box>
      </Box>
    </MainLayout>
  );
}
