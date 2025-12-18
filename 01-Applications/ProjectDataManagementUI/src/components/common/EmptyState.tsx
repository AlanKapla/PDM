import { memo } from "react";
import { Box, Text, VStack, Icon } from "@chakra-ui/react";
import type { LucideIcon } from "lucide-react";

interface EmptyStateProps {
  icon?: LucideIcon;
  title: string;
  description?: string;
  action?: React.ReactNode;
}

const EmptyState = memo(function EmptyState({ 
  icon, 
  title, 
  description, 
  action 
}: EmptyStateProps) {
  return (
    <Box textAlign="center" py={10}>
      <VStack spacing={4}>
        {icon && <Icon as={icon} boxSize={12} color="gray.400" />}
        <VStack spacing={2}>
          <Text fontSize="lg" fontWeight="medium" color="gray.700">
            {title}
          </Text>
          {description && (
            <Text color="gray.500" fontSize="sm">
              {description}
            </Text>
          )}
        </VStack>
        {action && <Box mt={4}>{action}</Box>}
      </VStack>
    </Box>
  );
});

export default EmptyState;
