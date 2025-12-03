import { useEffect, useState } from "react";
import {
  Box,
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
  VStack,
} from "@chakra-ui/react";
import { useNavigate, useLocation } from "react-router-dom";
import { Database, User as UserIcon, RefreshCw, Building2 } from "lucide-react";
import { useAuth } from "../hooks/useAuth";
import { tenantApi } from "../api/tenantApi";
import NotificationBell from "./NotificationBell";

export default function Header() {
  const navigate = useNavigate();
  const location = useLocation();
  const { user, logout, isAuthenticated } = useAuth();
  const [activeTenantName, setActiveTenantName] = useState<string | null>(null);

  const bg = useColorModeValue("white", "gray.800");
  const border = useColorModeValue("gray.200", "gray.700");
  const textColor = useColorModeValue("gray.700", "gray.200");
  const muted = useColorModeValue("gray.600", "gray.400");

  const initials = user
    ? `${user.firstName[0]}${user.lastName[0]}`.toUpperCase()
    : "U";

  // ACTIVE TENANT
  useEffect(() => {
    if (!isAuthenticated) return;

    const load = async () => {
      try {
        const [active, all] = await Promise.all([
          tenantApi.getActiveTenant(),
          tenantApi.getUserTenants(),
        ]);

        if (active.ok && all.ok) {
          const activeData = await active.json();
          const tenants = await all.json();
          const match = tenants.find((t: any) => t.id === activeData.activeTenantId);
          setActiveTenantName(match?.name || null);
        }
      } catch (_) {}
    };

    load();
  }, [isAuthenticated, location.pathname]);

  return (
    <Box
      bg={bg}
      borderBottom="1px solid"
      borderColor={border}
      px={6}
      py={3}
      position="fixed"
      top={0}
      left={0}
      right={0}
      zIndex={1000}
    >
      <HStack justify="space-between">
        {/* Logo */}
        <HStack
          spacing={2}
          cursor="pointer"
          onClick={() => navigate("/dashboard")}
        >
          <Icon as={Database} boxSize={5} color="blue.600" />
          <Text fontSize="lg" fontWeight="bold" color={textColor}>
            Project Data Management
          </Text>
        </HStack>

        {/* USER */}
        {isAuthenticated && user ? (
          <HStack spacing={4}>
            <NotificationBell />

            <VStack align="flex-end" spacing={0}>
              <Text fontSize="sm" fontWeight="medium" color={textColor}>
                {user.firstName} {user.lastName}
              </Text>
              {activeTenantName && (
                <HStack spacing={1} fontSize="xs" color={muted}>
                  <Icon as={Building2} boxSize={3} />
                  <Text>{activeTenantName}</Text>
                </HStack>
              )}
            </VStack>

            <Menu>
              <MenuButton cursor="pointer">
                <Avatar bg="blue.600" color="white">
                  {initials}
                </Avatar>
              </MenuButton>
              <MenuList>
                <MenuItem icon={<UserIcon size={16} />} onClick={() => navigate("/profile")}>
                  Ustawienia profilu
                </MenuItem>
                <MenuDivider />
                <MenuItem icon={<RefreshCw size={16} />} onClick={() => navigate("/tenants/collaborating")}>
                  Zmień tenanta
                </MenuItem>
                <MenuDivider />
                <MenuItem color="red.500" onClick={async () => {
                  await logout();
                  navigate("/");
                }}>
                  Wyloguj się
                </MenuItem>
              </MenuList>
            </Menu>
          </HStack>
        ) : null}
      </HStack>
    </Box>
  );
}
