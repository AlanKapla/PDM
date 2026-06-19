import React from "react";
import {
  Box,
  Button,
  Flex,
  Heading,
  Text,
  VStack,
  useColorModeValue,
} from "@chakra-ui/react";
import { AlertTriangle } from "lucide-react";

interface AppErrorBoundaryProps {
  children: React.ReactNode;
}

interface AppErrorBoundaryState {
  hasError: boolean;
}

export class AppErrorBoundary extends React.Component<
  AppErrorBoundaryProps,
  AppErrorBoundaryState
> {
  constructor(props: AppErrorBoundaryProps) {
    super(props);
    this.state = { hasError: false };
  }

  static getDerivedStateFromError(): AppErrorBoundaryState {
    return { hasError: true };
  }

  componentDidCatch(error: Error, errorInfo: React.ErrorInfo): void {
    if (import.meta.env.DEV) {
      console.error("AppErrorBoundary:", error, errorInfo);
    }
  }

  private handleReload = (): void => {
    window.location.reload();
  };

  render(): React.ReactNode {
    if (this.state.hasError) {
      return <AppErrorFallback onReload={this.handleReload} />;
    }

    return this.props.children;
  }
}

interface AppErrorFallbackProps {
  onReload: () => void;
}

function AppErrorFallback({ onReload }: AppErrorFallbackProps): React.ReactElement {
  const pageBg = useColorModeValue("gray.50", "gray.900");
  const cardBg = useColorModeValue("white", "gray.800");
  const mutedText = useColorModeValue("gray.600", "gray.400");

  return (
    <Flex minH="100vh" bg={pageBg} align="center" justify="center" p={4}>
      <Box
        bg={cardBg}
        rounded="xl"
        shadow="md"
        p={8}
        maxW="md"
        w="full"
        textAlign="center"
      >
        <VStack spacing={4}>
          <AlertTriangle size={48} color="var(--chakra-colors-red-400)" aria-hidden="true" />
          <Heading size="md">Coś poszło nie tak</Heading>
          <Text color={mutedText}>
            Wystąpił nieoczekiwany błąd aplikacji. Odśwież stronę lub spróbuj ponownie później.
          </Text>
          <Button colorScheme="primary" onClick={onReload}>
            Odśwież stronę
          </Button>
        </VStack>
      </Box>
    </Flex>
  );
}
