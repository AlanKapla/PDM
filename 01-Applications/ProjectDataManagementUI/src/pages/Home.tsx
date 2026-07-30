import { Button, Flex, Spinner, Text, VStack, useToast } from "@chakra-ui/react";
import { LogIn } from "lucide-react";
import { useCallback, useEffect, useRef, useState } from "react";
import { Link as RouterLink } from "react-router-dom";
import { useMsal } from "@azure/msal-react";
import { DemoModeHomeToggle } from "../components/DemoModeHomeToggle";
import {
  AuthPageHeading,
  AuthPageShell,
} from "../features/auth/components/AuthPageShell";
import { getCustomAuthClient } from "../auth/customAuthInstance";
import { clearStaleMsalInteraction } from "../auth/clearStaleMsalInteraction";
import { getRememberedSignInEmail } from "../auth/rememberedSignIn";
import { tryResumeNativeSession } from "../auth/tryResumeNativeSession";
import { useAuth } from "../context/AuthContext";

const HOME_SPINNER_STUCK_MS = 12_000;

function resetSessionAndReload(): void {
  clearStaleMsalInteraction();
  try {
    Object.keys(localStorage)
      .filter((key) => key.startsWith("msal."))
      .forEach((key) => localStorage.removeItem(key));
    sessionStorage.clear();
  } catch {
    // ignore
  }
  window.location.reload();
}

export default function Home() {
  const { accounts, inProgress } = useMsal();
  const { isAuthenticated, user, loading: authLoading } = useAuth();
  const toast = useToast();

  const rememberedEmail: string | null = getRememberedSignInEmail();
  const cacheEmail: string | null =
    accounts[0]?.username ?? rememberedEmail;
  const canContinueAs: boolean =
    !isAuthenticated && Boolean(cacheEmail) && accounts.length > 0;

  const [isResuming, setIsResuming] = useState(false);
  const [spinnerStuck, setSpinnerStuck] = useState(false);
  const spinnerSinceRef = useRef<number | null>(null);
  const isLoading = inProgress === "login" || inProgress === "acquireToken";
  const showBootSpinner =
    isLoading || isResuming || (isAuthenticated && authLoading);

  useEffect(() => {
    if (isAuthenticated && user) {
      window.location.assign("/dashboard");
    }
  }, [isAuthenticated, user]);

  useEffect(() => {
    if (!showBootSpinner) {
      spinnerSinceRef.current = null;
      setSpinnerStuck(false);
      return;
    }

    if (spinnerSinceRef.current === null) {
      spinnerSinceRef.current = Date.now();
    }

    const elapsed: number = Date.now() - spinnerSinceRef.current;
    const remaining: number = Math.max(0, HOME_SPINNER_STUCK_MS - elapsed);
    const timer: ReturnType<typeof setTimeout> = setTimeout(() => {
      setSpinnerStuck(true);
    }, remaining);

    return () => {
      clearTimeout(timer);
    };
  }, [showBootSpinner]);

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

  if (showBootSpinner) {
    if (spinnerStuck) {
      return (
        <Flex minH="100vh" align="center" justify="center" bg="white" p={6}>
          <VStack spacing={4} maxW="md" textAlign="center">
            <Text fontWeight="semibold">Nie można potwierdzić sesji</Text>
            <Text color="neutral.600" fontSize="sm">
              Logowanie trwa zbyt długo. Zresetuj sesję, aby kontynuować.
            </Text>
            <Button colorScheme="primary" onClick={resetSessionAndReload}>
              Zresetuj sesję i odśwież
            </Button>
          </VStack>
        </Flex>
      );
    }

    return (
      <Flex minH="100vh" align="center" justify="center" bg="white">
        <VStack spacing={4}>
          <Spinner size="xl" color="primary.500" thickness="3px" />
          <Text color="neutral.600">
            {isResuming ? "Wznawianie sesji..." : "Przetwarzanie logowania..."}
          </Text>
        </VStack>
      </Flex>
    );
  }

  if (isAuthenticated && user) {
    return (
      <Flex minH="100vh" align="center" justify="center" bg="white">
        <VStack spacing={4}>
          <Spinner size="xl" color="primary.500" thickness="3px" />
          <Text color="neutral.600">Przekierowywanie do aplikacji...</Text>
        </VStack>
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
          title="Witaj w Brickly"
          hint="Zaloguj się, żeby kontynuować pracę."
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
          {canContinueAs ? "Zaloguj się na inne konto" : "Zaloguj się"}
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
