import { Button, HStack, Text, useColorModeValue } from "@chakra-ui/react";
import { useState, type ReactElement } from "react";
import { PrivacyPolicyContent, TermsOfServiceContent } from "../../content/legalContent";
import { LegalModal } from "./LegalModal";

export interface LegalLinksProps {
  size?: "xs" | "sm";
  variant?: "footer" | "inline";
}

export function LegalLinks({
  size = "sm",
  variant = "inline",
}: LegalLinksProps): ReactElement {
  const [privacyOpen, setPrivacyOpen] = useState(false);
  const [termsOpen, setTermsOpen] = useState(false);

  const linkColor = useColorModeValue("neutral.600", "neutral.400");
  const sepColor = useColorModeValue("neutral.400", "neutral.600");

  return (
    <>
      <HStack
        spacing={variant === "footer" ? 2 : 3}
        justify="center"
        flexWrap="wrap"
      >
        <Button
          variant="link"
          size={size}
          fontWeight="normal"
          color={linkColor}
          onClick={() => setPrivacyOpen(true)}
        >
          Polityka prywatności
        </Button>
        <Text fontSize={size} color={sepColor} aria-hidden="true">
          ·
        </Text>
        <Button
          variant="link"
          size={size}
          fontWeight="normal"
          color={linkColor}
          onClick={() => setTermsOpen(true)}
        >
          Regulamin
        </Button>
      </HStack>

      <LegalModal
        isOpen={privacyOpen}
        onClose={() => setPrivacyOpen(false)}
        title="Polityka prywatności"
      >
        <PrivacyPolicyContent />
      </LegalModal>

      <LegalModal
        isOpen={termsOpen}
        onClose={() => setTermsOpen(false)}
        title="Regulamin"
      >
        <TermsOfServiceContent />
      </LegalModal>
    </>
  );
}
