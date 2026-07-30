import { useCallback, useContext, useEffect, useState } from "react";
import { Link as RouterLink } from "react-router-dom";
import { Button, Flex, Spinner, Text, VStack, useToast } from "@chakra-ui/react";
import { LogIn } from "lucide-react";
import { useMsal } from "@azure/msal-react";
import { InteractionStatus } from "@azure/msal-browser";
import { AuthContext } from "../context/AuthContext";
import { DemoModeHomeToggle } from "../components/DemoModeHomeToggle";
import {
  AuthPageHeading,
  AuthPageShell,
} from "../features/auth/components/AuthPageShell";
import { getCustomAuthClient } from "../auth/customAuthInstance";
import { clearAppStoragePreservingMsal } from "../auth/logoutSession";
import { getRememberedSignInEmail } from "../auth/rememberedSignIn";
import { tryResumeNativeSession } from "../auth/tryResumeNativeSession";

export default function LoggedOut() {
  const { accounts, inProgress } = useMsal();
  const { isAuthenticated, user, loading: authLoading } = useContext(AuthContext);
  const toast = useToast();
  const [isResuming, setIsResuming] = useState(false);

  useEffect(() => {
    // Nie czyść całego sessionStorage — tam siedzi cache MSAL pod „Kontynuuj jako…”.
    clearAppStoragePreservingMsal();
  }, []);

  const isLoading = inProgress !== InteractionStatus.None;
  const rememberedEmail: string | null = getRememberedSignInEmail();
  const cacheEmail: string | null = accounts[0]?.username ?? rememberedEmail;
  const canContinueAs: boolean =
    !isAuthenticated && Boolean(cacheEmail) && accounts.length > 0;

  const handleContinueAs = useCallback(async () => {
    setIsResuming(true);
    try {
      const client = await getCustomAuthClient();
      const resume = await tryResumeNativeSession(client);
      if (!resume.resumed) {
        toast({
          title: "Sesja wygasła",
          description: "Zaloguj się ponownie hasłem.",
          status: "info",
          duration: 4000,
          isClosable: true,
        });
        window.location.assign("/login");
      }
    } catch {
      toast({
        title: "Nie udało się wznowić sesji",
        description: "Zaloguj się ponownie.",
        status: "error",
        duration: 4000,
        isClosable: true,
      });
      window.location.assign("/login");
    } finally {
      setIsResuming(false);
    }
  }, [toast]);

  if (isLoading || isResuming) {
    return (
      <Flex minH="100vh" align="center" justify="center" bg="white">
        <VStack spacing={4}>
          <Spinner size="xl" color="primary.500" thickness="3px" />
          <Text color="neutral.600">
            {isResuming ? "Wznawianie sesji..." : "Przetwarzanie wylogowania..."}
          </Text>
        </VStack>
      </Flex>
    );
  }

  if (isAuthenticated && authLoading) {
    return (
      <Flex minH="100vh" align="center" justify="center" bg="white">
        <VStack spacing={4}>
          <Spinner size="xl" color="primary.500" thickness="3px" />
          <Text color="neutral.600">Ładowanie profilu użytkownika...</Text>
        </VStack>
      </Flex>
    );
  }

  if (isAuthenticated && user) {
    window.location.assign("/dashboard");
    return (
      <Flex minH="100vh" align="center" justify="center" bg="white">
        <Spinner size="xl" color="primary.500" thickness="3px" />
      </Flex>
    );
  }

  return (
    <AuthPageShell
      footer={
        <Text fontSize="sm" color="neutral.600">
          Kosztorysy · Harmonogramy · Pliki · Komunikacja
        </Text>
      }
    >
      <VStack spacing={5} align="stretch">
        <AuthPageHeading
          title="Wylogowano"
          hint="Zostałeś wylogowany z systemu. Możesz zalogować się ponownie."
        />

        {canContinueAs && cacheEmail ? (
          <Button
            size="lg"
            w="full"
            h="auto"
            minH="12"
            py={3}
            colorScheme="primary"
            fontWeight={700}
            borderRadius="10px"
            whiteSpace="normal"
            leftIcon={<LogIn size={18} aria-hidden="true" />}
            onClick={() => {
              void handleContinueAs();
            }}
            isLoading={isResuming}
          >
            <VStack spacing={0} align="start" maxW="100%" overflow="hidden">
              <Text as="span" fontSize="md" fontWeight={700} lineHeight="short">
                Kontynuuj jako
              </Text>
              <Text
                as="span"
                fontSize="sm"
                fontWeight={600}
                lineHeight="short"
                wordBreak="break-all"
                noOfLines={2}
              >
                {cacheEmail}
              </Text>
            </VStack>
          </Button>
        ) : null}

        <Button
          as={RouterLink}
          to="/login"
          size="lg"
          w="full"
          colorScheme="primary"
          fontWeight={700}
          borderRadius="10px"
          variant={canContinueAs ? "outline" : "solid"}
          leftIcon={
            canContinueAs ? undefined : <LogIn size={18} aria-hidden="true" />
          }
        >
          {canContinueAs ? "Zaloguj się na inne konto" : "Zaloguj się ponownie"}
        </Button>
        <Button
          as={RouterLink}
          to="/register"
          size="md"
          w="full"
          variant="outline"
          colorScheme="primary"
        >
          Utwórz konto
        </Button>
        <DemoModeHomeToggle />
      </VStack>
    </AuthPageShell>
  );
}
