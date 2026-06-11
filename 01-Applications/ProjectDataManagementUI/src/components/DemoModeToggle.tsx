// ============================================
//   DemoModeToggle — przycisk w headerze
//   Widoczny tylko dla SYSTEM.SUPERADMIN
// ============================================

import { HStack, Text, Tooltip, Badge, Icon } from "@chakra-ui/react";
import { Monitor } from "lucide-react";
import { useDemoMode } from "../context/DemoContext";
import { useAuth } from "../context/AuthContext";

export default function DemoModeToggle(): React.ReactElement | null {
  const { isDemoMode, toggleDemoMode } = useDemoMode();
  const { user } = useAuth();

  // Pokaż tylko dla SuperAdmina
  if (!user?.isSuperAdmin) {
    return null;
  }

  return (
    <Tooltip
      label={
        isDemoMode
          ? "Demo Mode AKTYWNY — dane mockowane. Kliknij, aby wyłączyć."
          : "Demo Mode nieaktywny. Kliknij, aby włączyć mockowane dane."
      }
      placement="bottom"
      hasArrow
    >
      <HStack
        as="button"
        type="button"
        spacing={1.5}
        px={2.5}
        py={1}
        borderRadius="md"
        cursor="pointer"
        bg={isDemoMode ? "orange.100" : "gray.100"}
        _hover={{ bg: isDemoMode ? "orange.200" : "gray.200" }}
        onClick={toggleDemoMode}
        aria-label={
          isDemoMode ? "Wyłącz Demo Mode" : "Włącz Demo Mode"
        }
      >
        <Icon
          as={Monitor}
          boxSize={4}
          color={isDemoMode ? "orange.500" : "gray.500"}
        />
        <Text fontSize="xs" fontWeight="medium" color={isDemoMode ? "orange.700" : "gray.600"}>
          Demo
        </Text>
        <Badge
          fontSize="2xs"
          colorScheme={isDemoMode ? "orange" : "gray"}
          variant="solid"
          px={1.5}
          py={0.5}
          borderRadius="full"
        >
          {isDemoMode ? "ON" : "OFF"}
        </Badge>
      </HStack>
    </Tooltip>
  );
}
