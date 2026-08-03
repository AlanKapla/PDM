import React from "react";
import { Box, Container, Flex, Link, Text, VStack } from "@chakra-ui/react";
import { Link as RouterLink } from "react-router-dom";
import { LegalLinks } from "../../../components/legal/LegalLinks";

export interface AuthPageShellProps {
  children: React.ReactNode;
  footer?: React.ReactNode;
  cardTextAlign?: "left" | "center";
  showLegalLinks?: boolean;
}

/**
 * Wspólny layout stron auth (login / logout / register / reset) —
 * te same tokeny primary/neutral, karta i logo.
 */
export function AuthPageShell({
  children,
  footer,
  cardTextAlign = "left",
  showLegalLinks = true,
}: AuthPageShellProps): React.ReactElement {
  return (
    <Flex minH="100vh" bg="white" align="flex-start" justify="center" pt="12vh" px={4}>
      <Container maxW="440px">
        <VStack spacing={8} align="center" textAlign="center">
          <Link as={RouterLink} to="/" aria-label="Brickly — strona startowa">
            <Box as="img" src="/logo.png" alt="Brickly" h="64px" w="auto" />
          </Link>

          <Box
            w="full"
            bg="white"
            border="1px solid"
            borderColor="neutral.200"
            borderRadius="16px"
            p={8}
            textAlign={cardTextAlign}
          >
            {children}
          </Box>

          {footer}
        </VStack>
      </Container>

      {showLegalLinks && (
        <Box position="fixed" bottom={4} left={0} right={0} textAlign="center">
          <LegalLinks size="xs" variant="footer" />
        </Box>
      )}
    </Flex>
  );
}

export interface AuthPageHeadingProps {
  title: string;
  hint?: string | null;
}

export function AuthPageHeading({
  title,
  hint,
}: AuthPageHeadingProps): React.ReactElement {
  return (
    <Box>
      <Text fontSize="lg" fontWeight="semibold" color="neutral.800">
        {title}
      </Text>
      {hint ? (
        <Text fontSize="sm" color="neutral.600" mt={1}>
          {hint}
        </Text>
      ) : null}
    </Box>
  );
}
