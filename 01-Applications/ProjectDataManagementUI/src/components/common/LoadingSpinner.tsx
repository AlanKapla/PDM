import { memo } from "react";
import { Spinner, VStack, Text, Box } from "@chakra-ui/react";

interface LoadingSpinnerProps {
  message?: string;
  size?: "xs" | "sm" | "md" | "lg" | "xl";
  fullScreen?: boolean;
}

const LoadingSpinner = memo(function LoadingSpinner({ 
  message, 
  size = "xl", 
  fullScreen = false 
}: LoadingSpinnerProps) {
  const content = (
    <VStack spacing={4}>
      <Spinner size={size} color="primary.500" thickness="4px" />
      {message && <Text color="neutral.600">{message}</Text>}
    </VStack>
  );

  if (fullScreen) {
    return (
      <VStack justify="center" align="center" minH="100vh">
        {content}
      </VStack>
    );
  }

  return (
    <Box textAlign="center" py={10}>
      {content}
    </Box>
  );
});

export default LoadingSpinner;
