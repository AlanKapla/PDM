import { useEffect, useState } from "react";
import {
  Box,
  Text,
  VStack,
  HStack,
  Avatar,
  Input,
  Button,
  Spinner,
  useColorModeValue,
  useToast,
  FormControl,
  FormLabel,
} from "@chakra-ui/react";
import MainLayout from "../layout/MainLayout";
import { PageHeader } from "../components/PageHeader";
import { getUserDetails, updateUserProfile } from "../services/userService";
import type { UserProfile } from "../types/auth.types";

export default function Profile() {
  const [user, setUser] = useState<UserProfile | null>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [isEditing, setIsEditing] = useState(false);

  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName] = useState("");

  const toast = useToast();

  const panelBg = useColorModeValue("#1a1a1a", "#1a1a1a");
  const panelHover = useColorModeValue("#232323", "#232323");
  const border = useColorModeValue("#2a2a2a", "#2a2a2a");

  useEffect(() => {
    async function load() {
      try {
        const data = await getUserDetails();
        setUser(data);

        if (data) {
          setFirstName(data.firstName);
          setLastName(data.lastName);
        }
      } catch (err) {
        console.error("Błąd ładowania profilu:", err);
      } finally {
        setLoading(false);
      }
    }
    load();
  }, []);

  const handleSave = async () => {
    if (!firstName.trim() || !lastName.trim()) {
      toast({
        title: "Błąd walidacji",
        description: "Imię i nazwisko nie mogą być puste.",
        status: "error",
      });
      return;
    }

    setSaving(true);
    try {
      const success = await updateUserProfile(firstName, lastName);
      if (success) {
        setUser((prev) =>
          prev ? { ...prev, firstName, lastName } : prev
        );
        setIsEditing(false);
        toast({ title: "Profil zaktualizowany", status: "success" });
      } else {
        toast({
          title: "Nie udało się zapisać zmian",
          status: "error",
        });
      }
    } catch (err) {
      toast({ title: "Błąd połączenia", status: "error" });
    } finally {
      setSaving(false);
    }
  };

  const handleCancel = () => {
    setIsEditing(false);
    if (user) {
      setFirstName(user.firstName);
      setLastName(user.lastName);
    }
  };

  if (loading) {
    return (
      <MainLayout>
        <VStack justify="center" minH="80vh">
          <Spinner size="xl" />
        </VStack>
      </MainLayout>
    );
  }

  return (
    <MainLayout>
      <Box px={12} py={10}>
        <PageHeader
          title="Ustawienia profilu"
          breadcrumb={["Ustawienia", "Profil użytkownika"]}
        />

        <VStack spacing={10} align="stretch" maxW="700px" mx="auto">

          {/* PANEL: DANE UŻYTKOWNIKA */}
          <Box
            bg={panelBg}
            border="1px solid"
            borderColor={border}
            borderRadius="lg"
            p={8}
            transition="0.2s"
            _hover={{ bg: panelHover }}
          >
            <Text fontSize="lg" fontWeight="semibold" mb={6} color="gray.200">
              Dane użytkownika
            </Text>

            <HStack spacing={6} mb={8}>
              <Avatar
                size="xl"
                bg="gray.700"
                color="white"
                name={`${user?.firstName} ${user?.lastName}`}
              />

              <VStack align="flex-start" spacing={0}>
                <Text fontSize="lg" color="gray.200">
                  {user?.firstName} {user?.lastName}
                </Text>
                <Text fontSize="sm" color="gray.400">
                  {user?.email}
                </Text>
              </VStack>
            </HStack>

            <VStack spacing={6} align="stretch">
              {isEditing ? (
                <>
                  <FormControl>
                    <FormLabel color="gray.400">Imię</FormLabel>
                    <Input
                      value={firstName}
                      onChange={(e) => setFirstName(e.target.value)}
                      bg="#131313"
                      border="1px solid"
                      borderColor={border}
                      _focus={{
                        borderColor: "#6366f1",
                        boxShadow: "0 0 0 1px #6366f1",
                      }}
                    />
                  </FormControl>

                  <FormControl>
                    <FormLabel color="gray.400">Nazwisko</FormLabel>
                    <Input
                      value={lastName}
                      onChange={(e) => setLastName(e.target.value)}
                      bg="#131313"
                      border="1px solid"
                      borderColor={border}
                      _focus={{
                        borderColor: "#6366f1",
                        boxShadow: "0 0 0 1px #6366f1",
                      }}
                    />
                  </FormControl>

                  <Box>
                    <Text fontSize="sm" color="gray.400">
                      Email
                    </Text>
                    <Text fontSize="md" color="gray.300">
                      {user?.email}
                    </Text>
                    <Text fontSize="xs" color="gray.500">
                      Email nie może być edytowany.
                    </Text>
                  </Box>

                  <HStack spacing={4} pt={2}>
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
              ) : (
                <>
                  <Box>
                    <Text fontSize="sm" color="gray.400">
                      Imię
                    </Text>
                    <Text fontSize="lg" color="gray.200">
                      {user?.firstName}
                    </Text>
                  </Box>

                  <Box>
                    <Text fontSize="sm" color="gray.400">
                      Nazwisko
                    </Text>
                    <Text fontSize="lg" color="gray.200">
                      {user?.lastName}
                    </Text>
                  </Box>

                  <Box>
                    <Text fontSize="sm" color="gray.400">
                      Email
                    </Text>
                    <Text fontSize="lg" color="gray.300">
                      {user?.email}
                    </Text>
                  </Box>

                  <Button
                    colorScheme="blue"
                    mt={4}
                    onClick={() => setIsEditing(true)}
                  >
                    Edytuj profil
                  </Button>
                </>
              )}
            </VStack>
          </Box>
        </VStack>
      </Box>
    </MainLayout>
  );
}
