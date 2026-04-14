import { memo } from "react";
import {
  Box,
  VStack,
  Text,
  Button,
  Icon,
  useColorModeValue,
} from "@chakra-ui/react";
import { AlertTriangle } from "lucide-react";

interface ErrorStateProps {
  title?: string;
  description?: string;
  onRetry?: () => void;
  retryLabel?: string;
}

/**
 * Komponent stanu błędu dla tabel i list.
 * Wyświetla komunikat błędu i opcjonalny przycisk "Spróbuj ponownie".
 */
const ErrorState = memo(function ErrorState({
  title = "Wystąpił błąd",
  description = "Nie udało się załadować danych.",
  onRetry,
  retryLabel = "Spróbuj ponownie",
}: ErrorStateProps) {
  const iconColor = useColorModeValue("red.400", "red.300");
  const titleColor = useColorModeValue("gray.700", "gray.200");
  const descColor = useColorModeValue("gray.500", "gray.400");

  return (
    <Box textAlign="center" py={12} px={4}>
      <VStack spacing={3}>
        <Icon as={AlertTriangle} boxSize={12} color={iconColor} />
        <VStack spacing={1}>
          <Text fontSize="md" fontWeight="medium" color={titleColor}>
            {title}
          </Text>
          <Text color={descColor} fontSize="sm" maxW="sm">
            {description}
          </Text>
        </VStack>
        {onRetry && (
          <Button
            variant="outline"
            colorScheme="red"
            size="sm"
            onClick={onRetry}
            mt={2}
          >
            {retryLabel}
          </Button>
        )}
      </VStack>
    </Box>
  );
});

export default ErrorState;
