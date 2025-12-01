import { useEffect, useState } from "react";
import {
  Text,
  HStack,
  Avatar,
  Menu,
  MenuButton,
  MenuList,
  MenuItem,
  MenuDivider,
  useColorModeValue,
  Icon,
  Flex,
} from "@chakra-ui/react";
import { useNavigate, useLocation } from "react-router-dom";
import { User as UserIcon, RefreshCw, Building2 } from "lucide-react";
import { useAuth } from "../hooks/useAuth";
import { tenantApi } from "../api/tenantApi";
import NotificationBell from "./NotificationBell";

export default function Header() {
  const navigate = useNavigate();
  const location = useLocation();
  const { user, logout, isAuthenticated } = useAuth();

  const [activeTenantName, setActiveTenantName] = useState<string | null>(null);

  const bg = useColorModeValue("#0f0f0f", "#0f0f0f");
  const border = useColorModeValue("#1d1d1d", "#1d1d1d");

  const initials =
    user?.firstName && user?.lastName
      ? `${user.firstName[0]}${user.lastName[0]}`.toUpperCase()
      : "U";

  const pageTitle = getPageTitle(location.pathname);

  /* Pobranie aktywnego tenanta */
  useEffect(() => {
    if (!isAuthenticated) return;

    const loadTenant = async () => {
      try {
        const activeRes = await tenantApi.getActiveTenant();
        const listRes = await tenantApi.getUserTenants();

        if (!activeRes.ok || !listRes.ok) return;

        const active = await activeRes.json();
        const tenants = await listRes.json();

        const found = tenants.find((t: any) => t.id === active.activeTenantId);
        setActiveTenantName(found?.name ?? null);
      } catch (err) {
        console.error("Błąd pobierania aktywnego tenanta:", err);
      }
    };

    loadTenant();
  }, [isAuthenticated, location.pathname]);

  return (
    <Flex
      position="fixed"
      top="0"
      left="240px"
      right="0"
      height="64px"
      align="center"
      justify="space-between"
      px={10}
      bg={bg}
      borderBottom={`1px solid ${border}`}
      zIndex={999}
    >
      {/* LEWY: tytuł strony */}
      <Text fontSize="lg" fontWeight="semibold" color="gray.200">
        {pageTitle}
      </Text>

      {/* PRAWY: tenant + powiadomienia + avatar */}
      {isAuthenticated && user ? (
        <HStack spacing={6}>
          <NotificationBell />

          {activeTenantName && (
            <HStack spacing={1} cursor="pointer">
              <Icon as={Building2} boxSize={4} color="gray.400" />
              <Text fontSize="sm" color="gray.300">
                {activeTenantName}
              </Text>
            </HStack>
          )}

          <Menu placement="bottom-end">
            <MenuButton>
              <Avatar size="sm" bg="gray.700" color="white">
                {initials}
              </Avatar>
            </MenuButton>

            <MenuList bg="#1a1a1a" borderColor="#2a2a2a" color="gray.200">
              <MenuItem
                icon={<UserIcon size={16} />}
                bg="#1a1a1a"
                _hover={{ bg: "#222" }}
                onClick={() => navigate("/profile")}
              >
                Ustawienia profilu
              </MenuItem>

              <MenuDivider />

              <MenuItem
                icon={<RefreshCw size={16} />}
                bg="#1a1a1a"
                _hover={{ bg: "#222" }}
                onClick={() => navigate("/tenants/collaborating")}
              >
                Zmień aktywnego tenanta
              </MenuItem>

              <MenuDivider />

              <MenuItem
                color="red.400"
                bg="#1a1a1a"
                _hover={{ bg: "#220000" }}
                onClick={async () => {
                  await logout();
                  navigate("/");
                }}
              >
                Wyloguj się
              </MenuItem>
            </MenuList>
          </Menu>
        </HStack>
      ) : (
        <Text color="gray.500">Nie zalogowano</Text>
      )}
    </Flex>
  );
}

/* Automatyczne tytuły widoków */
function getPageTitle(path: string): string {
  if (path.startsWith("/projects")) return "Projekty";
  if (path.startsWith("/files")) return "Pliki";
  if (path.startsWith("/costs")) return "Kosztorysy";
  if (path.startsWith("/tenants")) return "Organizacje";
  if (path.includes("profile")) return "Profil użytkownika";
  return "Project Data Management";
}
