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
  Divider,
} from "@chakra-ui/react";
import { ArrowLeft } from "lucide-react";
import { useNavigate } from "react-router-dom";
import MainLayout from "../layout/MainLayout";
import { AuthContext } from "../context/AuthContext";
import { LoadingSpinner } from "../components/common";
import { useToastNotification } from "../hooks/useToastNotification";
import { axiosClient } from "../api/axiosClient";
import { hasActiveTenant } from "../utils/tenantUtils";
import { LegalLinks } from "../components/legal/LegalLinks";

export default function Profile() {
  const navigate = useNavigate();
  const { user, refreshUser } = useContext(AuthContext);
  const [isEditing, setIsEditing] = useState(false);
  const [saving, setSaving] = useState(false);

  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName] = useState("");
  const [phoneNumber, setPhoneNumber] = useState("");
  const [companyName, setCompanyName] = useState("");
  const [taxId, setTaxId] = useState("");
  const [street, setStreet] = useState("");
  const [city, setCity] = useState("");
  const [postalCode, setPostalCode] = useState("");
  const [country, setCountry] = useState("");

  const { showSuccess, showError, showApiError } = useToastNotification();

  const cardBg = useColorModeValue("white", "gray.800");
  const cardText = useColorModeValue("gray.700", "gray.300");
  const pageBg = useColorModeValue("gray.50", "gray.900");
  const labelColor = useColorModeValue("gray.700", "gray.300");
  const sectionHeadingColor = useColorModeValue("gray.500", "gray.400");

  const syncFormFromUser = () => {
    if (user) {
      setFirstName(user.firstName || "");
      setLastName(user.lastName || "");
      setPhoneNumber(user.phoneNumber || "");
      setCompanyName(user.companyName || "");
      setTaxId(user.taxId || "");
      setStreet(user.street || "");
      setCity(user.city || "");
      setPostalCode(user.postalCode || "");
      setCountry(user.country || "");
    }
  };

  useEffect(() => {
    syncFormFromUser();
  }, [user]);

  const handleEdit = () => {
    setIsEditing(true);
  };

  const handleCancel = () => {
    setIsEditing(false);
    syncFormFromUser();
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
        phoneNumber: phoneNumber.trim() || null,
        companyName: companyName.trim() || null,
        taxId: taxId.trim() || null,
        street: street.trim() || null,
        city: city.trim() || null,
        postalCode: postalCode.trim() || null,
        country: country.trim() || null,
      });
      await refreshUser();
      showSuccess("Profil zaktualizowany", "Twoje dane zostały zapisane");
      setIsEditing(false);
    } catch (error) {
      showApiError(error);
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
        {!hasActiveTenant(user.activeTenantId) && (
          <Button
            variant="ghost"
            leftIcon={<ArrowLeft size={16} />}
            onClick={() => navigate("/dashboard")}
            mb={4}
            color="gray.600"
            _hover={{ bg: "gray.100" }}
          >
            Strona główna
          </Button>
        )}

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
            {/* === DANE PODSTAWOWE === */}
            <Text fontSize="xs" fontWeight="semibold" textTransform="uppercase" color={sectionHeadingColor} letterSpacing="wider">
              Dane podstawowe
            </Text>

            {!isEditing ? (
              <>
                <HStack spacing={6} align="start">
                  <Box flex={1}>
                    <Text fontSize="sm" color={labelColor} mb={1}>Imię</Text>
                    <Text fontSize="lg" color={cardText}>{user.firstName}</Text>
                  </Box>
                  <Box flex={1}>
                    <Text fontSize="sm" color={labelColor} mb={1}>Nazwisko</Text>
                    <Text fontSize="lg" color={cardText}>{user.lastName}</Text>
                  </Box>
                </HStack>

                <Box>
                  <Text fontSize="sm" color={labelColor} mb={1}>Email</Text>
                  <Text fontSize="lg" color={cardText}>{user.email}</Text>
                </Box>

                {/* Kontakt */}
                <Divider />
                <Text fontSize="xs" fontWeight="semibold" textTransform="uppercase" color={sectionHeadingColor} letterSpacing="wider">
                  Kontakt
                </Text>
                <Box>
                  <Text fontSize="sm" color={labelColor} mb={1}>Telefon</Text>
                  <Text fontSize="lg" color={cardText}>{user.phoneNumber || "—"}</Text>
                </Box>

                {/* Firma */}
                <Divider />
                <Text fontSize="xs" fontWeight="semibold" textTransform="uppercase" color={sectionHeadingColor} letterSpacing="wider">
                  Firma
                </Text>
                <HStack spacing={6} align="start">
                  <Box flex={1}>
                    <Text fontSize="sm" color={labelColor} mb={1}>Nazwa firmy</Text>
                    <Text fontSize="lg" color={cardText}>{user.companyName || "—"}</Text>
                  </Box>
                  <Box flex={1}>
                    <Text fontSize="sm" color={labelColor} mb={1}>NIP</Text>
                    <Text fontSize="lg" color={cardText}>{user.taxId || "—"}</Text>
                  </Box>
                </HStack>

                {/* Adres */}
                <Divider />
                <Text fontSize="xs" fontWeight="semibold" textTransform="uppercase" color={sectionHeadingColor} letterSpacing="wider">
                  Adres
                </Text>
                <Box>
                  <Text fontSize="sm" color={labelColor} mb={1}>Ulica i numer</Text>
                  <Text fontSize="lg" color={cardText}>{user.street || "—"}</Text>
                </Box>
                <HStack spacing={6} align="start">
                  <Box flex={1}>
                    <Text fontSize="sm" color={labelColor} mb={1}>Kod pocztowy</Text>
                    <Text fontSize="lg" color={cardText}>{user.postalCode || "—"}</Text>
                  </Box>
                  <Box flex={2}>
                    <Text fontSize="sm" color={labelColor} mb={1}>Miasto</Text>
                    <Text fontSize="lg" color={cardText}>{user.city || "—"}</Text>
                  </Box>
                </HStack>
                <Box>
                  <Text fontSize="sm" color={labelColor} mb={1}>Kraj</Text>
                  <Text fontSize="lg" color={cardText}>{user.country || "—"}</Text>
                </Box>

                <HStack spacing={3} mt={4} flexWrap="wrap">
                  <Button colorScheme="primary" onClick={handleEdit}>
                    Edytuj profil
                  </Button>
                  <Button
                    variant="outline"
                    colorScheme="primary"
                    onClick={() => {
                      const emailQuery = user.email
                        ? `?email=${encodeURIComponent(user.email)}`
                        : "";
                      navigate(`/reset-password${emailQuery}`);
                    }}
                  >
                    Zmień hasło
                  </Button>
                </HStack>
              </>
            ) : (
              <>
                {/* Dane podstawowe — edycja */}
                <HStack spacing={4}>
                  <FormControl isRequired>
                    <FormLabel color={labelColor}>Imię</FormLabel>
                    <Input
                      value={firstName}
                      onChange={(e) => setFirstName(e.target.value)}
                      placeholder="Podaj imię"
                    />
                  </FormControl>
                  <FormControl isRequired>
                    <FormLabel color={labelColor}>Nazwisko</FormLabel>
                    <Input
                      value={lastName}
                      onChange={(e) => setLastName(e.target.value)}
                      placeholder="Podaj nazwisko"
                    />
                  </FormControl>
                </HStack>

                <Box>
                  <Text fontSize="sm" color={labelColor} mb={1}>Email</Text>
                  <Text fontSize="lg" color={cardText}>{user.email}</Text>
                  <Text fontSize="xs" color="neutral.500" mt={1}>Email nie może być edytowany</Text>
                </Box>

                {/* Kontakt — edycja */}
                <Divider />
                <Text fontSize="xs" fontWeight="semibold" textTransform="uppercase" color={sectionHeadingColor} letterSpacing="wider">
                  Kontakt
                </Text>
                <FormControl>
                  <FormLabel color={labelColor}>Telefon</FormLabel>
                  <Input
                    value={phoneNumber}
                    onChange={(e) => setPhoneNumber(e.target.value)}
                    placeholder="np. +48 123 456 789"
                    maxLength={20}
                  />
                </FormControl>

                {/* Firma — edycja */}
                <Divider />
                <Text fontSize="xs" fontWeight="semibold" textTransform="uppercase" color={sectionHeadingColor} letterSpacing="wider">
                  Firma
                </Text>
                <HStack spacing={4}>
                  <FormControl>
                    <FormLabel color={labelColor}>Nazwa firmy</FormLabel>
                    <Input
                      value={companyName}
                      onChange={(e) => setCompanyName(e.target.value)}
                      placeholder="Nazwa firmy"
                      maxLength={200}
                    />
                  </FormControl>
                  <FormControl>
                    <FormLabel color={labelColor}>NIP</FormLabel>
                    <Input
                      value={taxId}
                      onChange={(e) => setTaxId(e.target.value)}
                      placeholder="np. 123-456-78-90"
                      maxLength={50}
                    />
                  </FormControl>
                </HStack>

                {/* Adres — edycja */}
                <Divider />
                <Text fontSize="xs" fontWeight="semibold" textTransform="uppercase" color={sectionHeadingColor} letterSpacing="wider">
                  Adres
                </Text>
                <FormControl>
                  <FormLabel color={labelColor}>Ulica i numer</FormLabel>
                  <Input
                    value={street}
                    onChange={(e) => setStreet(e.target.value)}
                    placeholder="np. ul. Przykładowa 1/2"
                    maxLength={200}
                  />
                </FormControl>
                <HStack spacing={4}>
                  <FormControl maxW="140px">
                    <FormLabel color={labelColor}>Kod pocztowy</FormLabel>
                    <Input
                      value={postalCode}
                      onChange={(e) => setPostalCode(e.target.value)}
                      placeholder="00-000"
                      maxLength={20}
                    />
                  </FormControl>
                  <FormControl>
                    <FormLabel color={labelColor}>Miasto</FormLabel>
                    <Input
                      value={city}
                      onChange={(e) => setCity(e.target.value)}
                      placeholder="Miasto"
                      maxLength={100}
                    />
                  </FormControl>
                </HStack>
                <FormControl>
                  <FormLabel color={labelColor}>Kraj</FormLabel>
                  <Input
                    value={country}
                    onChange={(e) => setCountry(e.target.value)}
                    placeholder="Polska"
                    maxLength={100}
                  />
                </FormControl>

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

        <Box mt={6} textAlign="center">
          <LegalLinks size="sm" variant="footer" />
        </Box>
      </Box>
    </MainLayout>
  );
}
