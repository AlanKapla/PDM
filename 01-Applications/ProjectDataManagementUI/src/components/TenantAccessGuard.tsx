import { type ReactNode, useContext, useEffect, useRef, useState } from "react";
import {
  Box,
  Button,
  Divider,
  Flex,
  FormControl,
  FormErrorMessage,
  FormLabel,
  Heading,
  HStack,
  Input,
  Modal,
  ModalBody,
  ModalCloseButton,
  ModalContent,
  ModalFooter,
  ModalHeader,
  ModalOverlay,
  Spinner,
  Stack,
  Text,
  useColorModeValue,
  useDisclosure,
  VStack,
} from "@chakra-ui/react";
import { Building2, Mail, Plus } from "lucide-react";
import { useNavigate } from "react-router-dom";
import { AuthContext } from "../context/AuthContext";
import {
  acceptTenantInvitation,
  changeActiveTenant,
  createTenant,
  getActiveInvitations,
  getUserTenants,
} from "../services/tenantService";
import type { TenantInvitationWeb, UserTenant } from "../types/auth.types";
import { InvitationStatus } from "../types/auth.types";
import { RoleCodes } from "../constants/roleCodes";
import { SubscriptionStatus } from "../types/subscription";
import { useToastNotification } from "../hooks/useToastNotification";

function isSubscriptionBlocked(status: SubscriptionStatus | undefined | null): boolean {
  if (status == null) return false;
  return (
    status === SubscriptionStatus.PastDue ||
    status === SubscriptionStatus.Canceled ||
    status === SubscriptionStatus.GracePeriod
  );
}

type AccessScreen = "loading" | "checking" | "allowed" | "invitations" | "no-access";

// ---------------------------------------------------------------------------
// PendingInvitationsScreen
// ---------------------------------------------------------------------------

interface PendingInvitationsScreenProps {
  invitations: TenantInvitationWeb[];
  onAccepted: (tenantId: string) => Promise<void>;
}

function PendingInvitationsScreen({ invitations, onAccepted }: PendingInvitationsScreenProps) {
  const [accepting, setAccepting] = useState<string | null>(null);
  const [localInvitations, setLocalInvitations] = useState(invitations);
  const { showSuccess, showError, showWarning, showInfo, toast } = useToastNotification();

  useEffect(() => {
    // Synchronizujemy lokalny stan z propsami, aby uniknąć niespójności UI
    setLocalInvitations(invitations);
  }, [invitations]);

  const pageBg = useColorModeValue("gray.50", "gray.900");
  const cardBg = useColorModeValue("white", "gray.800");
  const borderColor = useColorModeValue("gray.200", "gray.700");
  const mutedText = useColorModeValue("gray.600", "gray.400");
  const iconBg = useColorModeValue("primary.50", "primary.900");

  const handleAccept = async (inv: TenantInvitationWeb) => {
    setAccepting(inv.invitationId);
    try {
      const success = await acceptTenantInvitation(inv.token);
      if (success) {
        toast({
          title: "Zaproszenie zaakceptowane",
          description: `Dołączyłeś do organizacji "${inv.tenantName}"`,
          status: "success",
          duration: 4000,
          isClosable: true,
        });
        await onAccepted(inv.tenantId);
      } else {
        toast({
          title: "Nie udało się zaakceptować zaproszenia",
          description: "Zaproszenie może być nieaktualne lub wygasłe.",
          status: "error",
          duration: 5000,
          isClosable: true,
        });
        setLocalInvitations((prev) => prev.filter((i) => i.invitationId !== inv.invitationId));
      }
    } catch {
      toast({
        title: "Wystąpił błąd połączenia",
        description: "Sprawdź połączenie internetowe i spróbuj ponownie.",
        status: "error",
        duration: 5000,
        isClosable: true,
      });
    } finally {
      setAccepting(null);
    }
  };

  return (
    <Flex minH="100vh" bg={pageBg} align="center" justify="center" p={4}>
      <Box w="full" maxW="580px">
        <VStack spacing={8} align="stretch">
          {/* Header */}
          <VStack spacing={3} textAlign="center">
            <Flex
              w={16}
              h={16}
              bg={iconBg}
              borderRadius="full"
              align="center"
              justify="center"
              mx="auto"
            >
              <Mail size={28} color="var(--chakra-colors-blue-500)" />
            </Flex>
            <Heading size="lg">Zaproszenia do organizacji</Heading>
            <Text color={mutedText} maxW="420px" lineHeight="tall">
              Masz oczekujące zaproszenia. Zaakceptuj jedno z nich, aby uzyskać dostęp do
              platformy.
            </Text>
          </VStack>

          {/* Invitation list */}
          <Box
            bg={cardBg}
            rounded="xl"
            shadow="md"
            borderWidth="1px"
            borderColor={borderColor}
            overflow="hidden"
          >
            {localInvitations.length === 0 ? (
              <Box p={8} textAlign="center">
                <Text color={mutedText}>Brak aktywnych zaproszeń.</Text>
              </Box>
            ) : (
              <Stack spacing={0} divider={<Divider borderColor={borderColor} />}>
                {localInvitations.map((inv) => (
                  <HStack key={inv.invitationId} p={5} spacing={4} align="center">
                    <VStack align="start" spacing={1} flex={1} minW={0}>
                      <Text fontWeight="semibold" fontSize="md" noOfLines={1}>
                        {inv.tenantName}
                      </Text>
                      <Text fontSize="sm" color={mutedText} noOfLines={1}>
                        Zaproszono przez: {inv.invitedByUserName} ({inv.invitedByUserEmail})
                      </Text>
                      {inv.expiresAt && (
                        <Text fontSize="xs" color="orange.500">
                          Ważne do: {new Date(inv.expiresAt).toLocaleDateString("pl-PL")}
                        </Text>
                      )}
                    </VStack>
                    <Button
                      colorScheme="primary"
                      size="sm"
                      flexShrink={0}
                      isLoading={accepting === inv.invitationId}
                      isDisabled={accepting !== null}
                      onClick={() => handleAccept(inv)}
                    >
                      Akceptuj
                    </Button>
                  </HStack>
                ))}
              </Stack>
            )}
          </Box>

          <Text fontSize="sm" color={mutedText} textAlign="center">
            Nie chcesz teraz akceptować?{" "}
            <Text as="span">
              Skontaktuj się z administratorem organizacji, który Cię zaprosił.
            </Text>
          </Text>
        </VStack>
      </Box>
    </Flex>
  );
}

// ---------------------------------------------------------------------------
// NoTenantAccessScreen
// ---------------------------------------------------------------------------

interface NoTenantAccessScreenProps {
  onOrganizationCreated: () => Promise<void>;
}

function NoTenantAccessScreen({ onOrganizationCreated }: NoTenantAccessScreenProps) {
  const { isOpen, onOpen, onClose } = useDisclosure();
  const [orgName, setOrgName] = useState("");
  const [creating, setCreating] = useState(false);
  const [nameError, setNameError] = useState("");
  const { showSuccess, showError, showWarning, showInfo, toast } = useToastNotification();

  const pageBg = useColorModeValue("gray.50", "gray.900");
  const cardBg = useColorModeValue("white", "gray.800");
  const borderColor = useColorModeValue("gray.200", "gray.700");
  const mutedText = useColorModeValue("gray.600", "gray.400");
  const hintBg = useColorModeValue("primary.50", "primary.900");
  const hintBorder = useColorModeValue("primary.100", "primary.700");
  const hintTextHeading = useColorModeValue("primary.700", "primary.200");
  const hintTextBody = useColorModeValue("primary.600", "primary.300");
  const iconBg = useColorModeValue("gray.100", "gray.700");

  const handleCreate = async () => {
    const trimmed = orgName.trim();
    if (!trimmed) {
      setNameError("Nazwa organizacji jest wymagana.");
      return;
    }
    if (trimmed.length < 2) {
      setNameError("Nazwa musi zawierać co najmniej 2 znaki.");
      return;
    }
    if (trimmed.length > 100) {
      setNameError("Nazwa nie może przekraczać 100 znaków.");
      return;
    }

    setCreating(true);
    try {
      const tenant = await createTenant(trimmed);
      if (tenant) {
        // Activate the newly created tenant
        await changeActiveTenant(tenant.id);
        toast({
          title: "Organizacja utworzona",
          description: `Organizacja „${trimmed}” została pomyślnie utworzona.`,
          status: "success",
          duration: 4000,
          isClosable: true,
        });
        onClose();
        await onOrganizationCreated();
      } else {
        toast({
          title: "Nie udało się utworzyć organizacji",
          description: "Spróbuj ponownie lub skontaktuj się z pomocą techniczną.",
          status: "error",
          duration: 5000,
          isClosable: true,
        });
      }
    } catch {
      toast({
        title: "Wystąpił błąd",
        description: "Sprawdź połączenie internetowe i spróbuj ponownie.",
        status: "error",
        duration: 5000,
        isClosable: true,
      });
    } finally {
      setCreating(false);
    }
  };

  const handleModalClose = () => {
    if (!creating) {
      setOrgName("");
      setNameError("");
      onClose();
    }
  };

  return (
    <Flex minH="100vh" bg={pageBg} align="center" justify="center" p={4}>
      <Box
        bg={cardBg}
        rounded="2xl"
        shadow="lg"
        borderWidth="1px"
        borderColor={borderColor}
        p={{ base: 8, md: 12 }}
        maxW="480px"
        w="full"
        textAlign="center"
      >
        <VStack spacing={8}>
          {/* Icon */}
          <Flex
            w={20}
            h={20}
            bg={iconBg}
            borderRadius="full"
            align="center"
            justify="center"
            mx="auto"
          >
            <Building2 size={36} color="var(--chakra-colors-gray-500)" />
          </Flex>

          {/* Copy */}
          <VStack spacing={3}>
            <Heading size="lg">Brak dostępu do platformy</Heading>
            <Text color={mutedText} lineHeight="tall">
              Nie jesteś jeszcze członkiem żadnej organizacji. Aby korzystać z platformy, utwórz
              własną organizację lub poczekaj na zaproszenie od administratora istniejącej
              organizacji.
            </Text>
          </VStack>

          {/* Actions */}
          <VStack spacing={4} w="full">
            <Button
              leftIcon={<Plus size={18} />}
              colorScheme="primary"
              size="lg"
              w="full"
              onClick={onOpen}
            >
              Utwórz organizację
            </Button>

            <Box
              p={4}
              bg={hintBg}
              borderWidth="1px"
              borderColor={hintBorder}
              rounded="lg"
              w="full"
              textAlign="left"
            >
              <HStack spacing={3} align="start">
                <Box pt="2px" flexShrink={0}>
                  <Mail size={16} color="var(--chakra-colors-blue-500)" />
                </Box>
                <VStack align="start" spacing={1}>
                  <Text fontSize="sm" fontWeight="semibold" color={hintTextHeading}>
                    Oczekujesz na zaproszenie?
                  </Text>
                  <Text fontSize="xs" color={hintTextBody} lineHeight="tall">
                    Poproś administratora organizacji o wysłanie zaproszenia na adres e-mail
                    powiązany z Twoim kontem. Po otrzymaniu zaproszenia zaloguj się ponownie.
                  </Text>
                </VStack>
              </HStack>
            </Box>
          </VStack>
        </VStack>
      </Box>

      {/* Create organisation modal */}
      <Modal isOpen={isOpen} onClose={handleModalClose} isCentered size={{ base: "full", md: "md" }}>
        <ModalOverlay />
        <ModalContent>
          <ModalHeader>Utwórz organizację</ModalHeader>
          <ModalCloseButton isDisabled={creating} />
          <ModalBody>
            <FormControl isInvalid={!!nameError}>
              <FormLabel>Nazwa organizacji</FormLabel>
              <Input
                placeholder="np. Moja Firma Budowlana"
                value={orgName}
                onChange={(e) => {
                  setOrgName(e.target.value);
                  setNameError("");
                }}
                onKeyDown={(e) => {
                  if (e.key === "Enter") handleCreate();
                }}
                autoFocus
              />
              {nameError && <FormErrorMessage>{nameError}</FormErrorMessage>}
            </FormControl>
          </ModalBody>
          <ModalFooter gap={2}>
            <Button variant="ghost" onClick={handleModalClose} isDisabled={creating}>
              Anuluj
            </Button>
            <Button colorScheme="primary" onClick={handleCreate} isLoading={creating}>
              Utwórz
            </Button>
          </ModalFooter>
        </ModalContent>
      </Modal>
    </Flex>
  );
}

// ---------------------------------------------------------------------------
// TenantAccessGuard
// ---------------------------------------------------------------------------

/**
 * Sprawdza, czy zalogowany użytkownik ma prawo korzystać z platformy.
 *
 * Warunki dopuszczenia:
 * 1. user.activeTenantId jest ustawione → przepuszcza.
 * 2. Brak activeTenantId ale user jest w aktywnych tenantach → auto-select i przepuszcza.
 * 3. Brak tenantów ale są oczekujące zaproszenia → ekran akceptacji zaproszeń.
 * 4. Brak tenantów i zaproszeń → ekran informacyjny z możliwością stworzenia org.
 */
export default function TenantAccessGuard({ children }: { children: ReactNode }) {
  const { user, refreshUser } = useContext(AuthContext);
  const navigate = useNavigate();
  const [screen, setScreen] = useState<AccessScreen>("loading");
  const [pendingInvitations, setPendingInvitations] = useState<TenantInvitationWeb[]>([]);

  // Stable ref to refreshUser to avoid stale closures without re-triggering the effect
  const refreshUserRef = useRef(refreshUser);
  useEffect(() => {
    refreshUserRef.current = refreshUser;
  }, [refreshUser]);

  useEffect(() => {
    if (!user) return;

    // User already belongs to an active tenant — let them in immediately
    if (user.activeTenantId) {
      setScreen("allowed");
      return;
    }

    let cancelled = false;

    const check = async () => {
      setScreen("checking");
      try {
        const [tenants, invitations] = await Promise.all([
          getUserTenants(),
          getActiveInvitations(),
        ]);

        if (cancelled) return;

        const activeTenants = tenants.filter((t: UserTenant) => t.isActive);

        if (activeTenants.length > 0) {
          // Prefer tenant with active subscription; fall back to admin (can enter blocked);
          // last resort: first tenant in list
          const selectedTenant: UserTenant =
            activeTenants.find((t: UserTenant) => !isSubscriptionBlocked(t.subscriptionStatus)) ??
            activeTenants.find((t: UserTenant) => t.roleCode === RoleCodes.TENANT_ADMIN) ??
            activeTenants[0];

          const result = await changeActiveTenant(selectedTenant.id);
          await refreshUserRef.current();

          if (result.isSubscriptionBlocked && selectedTenant.roleCode === RoleCodes.TENANT_ADMIN) {
            navigate(`/tenants/${selectedTenant.id}/subscription`);
          }
          // After refreshUser, user.activeTenantId will be set → effect re-runs → "allowed"
        } else {
          const pending = invitations.filter(
            (i: TenantInvitationWeb) => i.status === InvitationStatus.Pending,
          );
          if (!cancelled) {
            if (pending.length > 0) {
              setPendingInvitations(pending);
              setScreen("invitations");
            } else {
              setScreen("no-access");
            }
          }
        }
      } catch {
        if (!cancelled) setScreen("no-access");
      }
    };

    check();

    return () => {
      cancelled = true;
    };
    // Zależy tylko od id i activeTenantId — nie od referencji refreshUser
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [user?.id, user?.activeTenantId]);

  if (screen === "loading" || screen === "checking") {
    return (
      <Flex justify="center" align="center" minH="100vh">
        <VStack spacing={4}>
          <Spinner size="xl" color="primary.500" thickness="4px" />
          <Text color="gray.500">Sprawdzanie dostępu...</Text>
        </VStack>
      </Flex>
    );
  }

  if (screen === "invitations") {
    return (
      <PendingInvitationsScreen
        invitations={pendingInvitations}
        onAccepted={async (tenantId) => {
          try {
            await changeActiveTenant(tenantId);
            await refreshUserRef.current();
            // user.activeTenantId now set → effect re-runs → setScreen("allowed")
          } catch {
            // changeActiveTenant failed — re-check from scratch
            await refreshUserRef.current();
          }
        }}
      />
    );
  }

  if (screen === "no-access") {
    return (
      <NoTenantAccessScreen
        onOrganizationCreated={async () => {
          await refreshUserRef.current();
          // user.activeTenantId now set → effect re-runs → setScreen("allowed")
        }}
      />
    );
  }

  // screen === "allowed"
  return <>{children}</>;
}
