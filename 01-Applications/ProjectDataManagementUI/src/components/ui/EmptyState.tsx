import { memo } from "react";
import {
  Box,
  Text,
  VStack,
  Icon,
  useColorModeValue,
} from "@chakra-ui/react";
import type { LucideIcon } from "lucide-react";
import { Inbox } from "lucide-react";

interface EmptyStateProps {
  icon?: LucideIcon;
  title?: string;
  description?: string;
  action?: React.ReactNode;
}

/**
 * Wyśrodkowany komponent pustego stanu dla tabel i list.
 * Używaj gdy lista danych jest pusta.
 */
const EmptyState = memo(function EmptyState({
  icon = Inbox,
  title = "Brak danych do wyświetlenia",
  description,
  action,
}: EmptyStateProps) {
  const iconColor = useColorModeValue("gray.400", "gray.500");
  const titleColor = useColorModeValue("gray.600", "gray.300");
  const descColor = useColorModeValue("gray.500", "gray.400");

  return (
    <Box textAlign="center" py={12} px={4}>
      <VStack spacing={3}>
        <Icon as={icon} boxSize={12} color={iconColor} />
        <VStack spacing={1}>
          <Text fontSize="md" fontWeight="medium" color={titleColor}>
            {title}
          </Text>
          {description && (
            <Text color={descColor} fontSize="sm" maxW="sm">
              {description}
            </Text>
          )}
        </VStack>
        {action && <Box mt={2}>{action}</Box>}
      </VStack>
    </Box>
  );
});

export default EmptyState;
