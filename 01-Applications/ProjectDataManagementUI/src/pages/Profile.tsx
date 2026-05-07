import { useEffect, useState, useContext } from "react";
import {
  Box,
  Heading,
  Text,
  VStack,
  useColorModeValue,
  Button,
  Input,
  FormControl,
  FormLabel,
  HStack,
} from "@chakra-ui/react";
import MainLayout from "../layout/MainLayout";
import { AuthContext } from "../context/AuthContext";
import { LoadingSpinner } from "../components/common";
import { useToastNotification } from "../hooks/useToastNotification";
import { axiosClient } from "../api/axiosClient";

export default function Profile() {
  const { user, refreshUser } = useContext(AuthContext);
  const [isEditing, setIsEditing] = useState(false);
  const [saving, setSaving] = useState(false);
  
  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName] = useState("");
  
  const { showSuccess, showError } = useToastNotification();

  const cardBg = useColorModeValue("white", "gray.800");
  const cardText = useColorModeValue("gray.700", "gray.300");
  const pageBg = useColorModeValue("gray.50", "gray.900");
  const labelColor = useColorModeValue("gray.700", "gray.300");

  useEffect(() => {
    if (user) {
      setFirstName(user.firstName || "");
      setLastName(user.lastName || "");
    }
  }, [user]);

  const handleEdit = () => {
    setIsEditing(true);
  };

  const handleCancel = () => {
    setIsEditing(false);
    if (user) {
      setFirstName(user.firstName || "");
      setLastName(user.lastName || "");
    }
  };

  const handleSave = async () => {
    if (!firstName.trim() || !lastName.trim()) {
      showError("Błąd walidacji", "Imię i nazwisko nie mogą być puste");
      return;
    }

    setSaving(true);
    try {
      await axiosClient.put("/user/me", {
        firstName: firstName.trim(),
        lastName: lastName.trim(),
      });
      await refreshUser();
      showSuccess("Profil zaktualizowany", "Twoje dane zostały zapisane");
      setIsEditing(false);
    } catch (error: any) {
      const message =
        error?.response?.data?.message ??
        error?.response?.data?.detail ??
        "Nie udało się zaktualizować profilu";
      showError("Błąd", message);
    } finally {
      setSaving(false);
    }
  };

  if (!user) {
    return (
      <MainLayout>
        <LoadingSpinner fullScreen />
      </MainLayout>
    );
  }

  return (
    <MainLayout>
      <Box p={{ base: 3, sm: 4, md: 10 }} bg={pageBg} minH="100vh">
        <Box
          bg={cardBg}
          p={{ base: 4, md: 8 }}
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

                <Button colorScheme="primary" onClick={handleEdit} mt={4}>
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
                  <Text fontSize="xs" color="neutral.500" mt={1}>
                    Email nie może być edytowany
                  </Text>
                </Box>

                <HStack spacing={3} mt={4}>
                  <Button
                    colorScheme="primary"
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
