import React, { useMemo } from "react";
import {
  Box,
  Heading,
  Spinner,
  Text,
  useColorModeValue,
  VStack,
} from "@chakra-ui/react";
import { useColdMailTemplate } from "../../hooks/useColdMailTemplate";
import { fillColdMailTemplate } from "../../utils/fillColdMailTemplate";
import { getApiErrorMessage } from "../../utils/apiErrorUtils";
import { isColdMailBodyEmpty } from "./ColdMailBodyEditor";

export interface ColdMailTemplatePreviewProps {
  subject: string;
  body: string;
}

export function ColdMailTemplatePreview({
  subject,
  body,
}: ColdMailTemplatePreviewProps): React.ReactElement {
  const cardBg = useColorModeValue("white", "gray.800");
  const borderColor = useColorModeValue("gray.200", "gray.700");
  const mutedText = useColorModeValue("gray.600", "gray.400");

  const { data: template, isPending, isError, error } = useColdMailTemplate();

  const previewHtml: string | null = useMemo(() => {
    if (!template) {
      return null;
    }
    return fillColdMailTemplate(
      template.htmlTemplate,
      template.appUrl,
      template.ctaLabel,
      subject,
      body
    );
  }, [template, subject, body]);

  const hasUserContent: boolean =
    subject.trim().length > 0 || !isColdMailBodyEmpty(body);

  return (
    <Box
      as="section"
      aria-labelledby="cold-mail-preview-heading"
      bg={cardBg}
      borderWidth="1px"
      borderColor={borderColor}
      borderRadius="xl"
      p={{ base: 4, md: 6 }}
      h="100%"
    >
      <Heading id="cold-mail-preview-heading" size="md" mb={1}>
        Podgląd maila
      </Heading>
      <Text fontSize="sm" color={mutedText} mb={4}>
        {hasUserContent
          ? "Szablon z serwera (cold-mail.html) — podgląd lokalny, bez odpytywania API przy pisaniu."
          : "Wpisz temat i treść, aby zobaczyć podgląd ze szablonem Brickly."}
      </Text>

      {isError && (
        <Text fontSize="sm" color="red.600" role="alert" mb={3}>
          Nie udało się pobrać szablonu: {getApiErrorMessage(error)}
        </Text>
      )}

      <Box
        borderWidth="1px"
        borderColor={borderColor}
        borderRadius="md"
        overflow="hidden"
        bg="neutral.100"
        position="relative"
      >
        {isPending && (
          <VStack
            position="absolute"
            inset={0}
            justify="center"
            bg="blackAlpha.50"
            zIndex={1}
          >
            <Spinner size="sm" color="primary.600" />
            <Text fontSize="xs" color={mutedText}>
              Ładowanie szablonu…
            </Text>
          </VStack>
        )}

        {previewHtml ? (
          <Box
            as="iframe"
            title="Podgląd cold maila ze szablonem Brickly"
            srcDoc={previewHtml}
            sandbox=""
            w="100%"
            h={{ base: "420px", md: "560px" }}
            border="0"
            bg="white"
          />
        ) : (
          <Box h={{ base: "420px", md: "560px" }} />
        )}
      </Box>
    </Box>
  );
}
