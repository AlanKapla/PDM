import { useEffect, useState } from "react";
import { useSearchParams, useNavigate } from "react-router-dom";
import {
  Box,
  Button,
  Flex,
  Heading,
  Text,
  Spinner,
  Alert,
  AlertIcon,
  AlertTitle,
  AlertDescription,
  VStack,
  useColorModeValue,
  Icon,
} from "@chakra-ui/react";
import { CheckCircle, XCircle } from "lucide-react";
import { activateAccount } from "../services/authService";

export default function ActivateAccount() {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const token = searchParams.get("token");

  const [loading, setLoading] = useState(true);
  const [success, setSuccess] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    if (!token) {
      setLoading(false);
      setError("Invalid activation link. No token provided.");
      return;
    }

    const activate = async () => {
      try {
        const result = await activateAccount(token);
        if (result) {
          setSuccess(true);
        } else {
          setError("Activation failed. The link may be invalid or expired.");
        }
      } catch (err) {
        setError("An error occurred during activation. Please try again later.");
      } finally {
        setLoading(false);
      }
    };

    activate();
  }, [token]);

  const cardBg = useColorModeValue("white", "gray.800");
  const pageBg = useColorModeValue("gray.50", "gray.900");

  return (
    <Flex justify="center" align="center" minH="100vh" bg={pageBg}>
      <Box bg={cardBg} p={8} rounded="lg" shadow="lg" maxW="500px" width="100%">
        <VStack spacing={6} textAlign="center">
          {loading && (
            <>
              <Spinner size="xl" color="blue.500" thickness="4px" />
              <Heading size="lg">Activating your account...</Heading>
              <Text color="gray.600">
                Please wait while we verify your activation link.
              </Text>
            </>
          )}

          {!loading && success && (
            <>
              <Icon as={CheckCircle} boxSize={20} color="green.500" />
              <Heading size="xl">Account Activated!</Heading>
              <Text color="gray.600">
                Your account has been successfully activated. You can now log in.
              </Text>
              <Button
                colorScheme="blue"
                size="lg"
                width="100%"
                onClick={() => navigate("/login")}
              >
                Go to Login
              </Button>
            </>
          )}

          {!loading && error && (
            <>
              <Icon as={XCircle} boxSize={20} color="red.500" />
              <Heading size="lg">Activation Failed</Heading>
              <Alert status="error" rounded="md">
                <AlertIcon />
                <Box>
                  <AlertTitle>Error</AlertTitle>
                  <AlertDescription>{error}</AlertDescription>
                </Box>
              </Alert>
              <Button
                variant="outline"
                colorScheme="blue"
                size="lg"
                width="100%"
                onClick={() => navigate("/login")}
              >
                Back to Login
              </Button>
            </>
          )}
        </VStack>
      </Box>
    </Flex>
  );
}
