// ============================================
//   DemoModeMenuItem — przełącznik w menu użytkownika
//   Widoczny tylko dla SYSTEM.SUPERADMIN
// ============================================

import { Fragment } from "react";
import { HStack, Text, MenuItem, MenuDivider, Switch } from "@chakra-ui/react";
import { Monitor } from "lucide-react";
import { useDemoMode } from "../context/DemoContext";
import { useAuth } from "../context/AuthContext";

export default function DemoModeMenuItem(): React.ReactElement | null {
  const { isDemoMode, toggleDemoMode } = useDemoMode();
  const { user } = useAuth();

  if (!user?.isSuperAdmin) {
    return null;
  }

  return (
    <Fragment>
      <MenuDivider />
      <MenuItem
      closeOnSelect={false}
      icon={<Monitor size={16} />}
      onClick={(event) => {
        event.preventDefault();
        toggleDemoMode();
      }}
      aria-label={isDemoMode ? "Wyłącz tryb demo" : "Włącz tryb demo"}
    >
      <HStack justify="space-between" w="full" spacing={3}>
        <Text>Tryb demo</Text>
        <Switch
          size="sm"
          colorScheme="orange"
          isChecked={isDemoMode}
          pointerEvents="none"
          aria-hidden
        />
      </HStack>
    </MenuItem>
    </Fragment>
  );
}
