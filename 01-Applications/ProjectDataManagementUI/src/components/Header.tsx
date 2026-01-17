import { useEffect, useState, useContext } from "react";
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
import { AuthContext } from "../context/AuthContext";
import { tenantApi } from "../api/tenantApi";
import NotificationBell from "./NotificationBell";
import { useGlobalCache } from "../hooks/useGlobalCache";

interface HeaderProps {
  onMenuOpen?: () => void;
}

export default function Header({ onMenuOpen }: HeaderProps) {
  const navigate = useNavigate();
  const location = useLocation();
  const { user, logout, isAuthenticated } = useContext(AuthContext);
  const [activeTenantName, setActiveTenantName] = useState<string | null>(null);

  const bg = useColorModeValue("white", "gray.800");
  const border = useColorModeValue("gray.200", "gray.700");
  const textColor = useColorModeValue("gray.700", "gray.200");
  const mutedColor = useColorModeValue("gray.600", "gray.400");
  const initials = user ? `${user.firstName[0]}${user.lastName[0]}`.toUpperCase() : "U";

  const tenantsCache = useGlobalCache(
    "my-tenants",
    async () => {
      const res = await tenantApi.getUserTenants();
      return res.data;
    }
  );

  useEffect(() => {
    if (!isAuthenticated) return;

    const fetchActiveTenant = async () => {
      try {
        const tenants = await tenantsCache.fetch();

        if (user?.activeTenantId) {
          const activeTenant = tenants.find((t: any) => t.id === user.activeTenantId);
          setActiveTenantName(activeTenant?.name || null);
        } else {
          setActiveTenantName(null);
        }
      } catch (err) {
        console.error("Błąd pobierania aktywnego tenanta:", err);
        setActiveTenantName(null);
      }
    };

    fetchActiveTenant();
  }, [isAuthenticated, user?.activeTenantId]);

  return (
    <Box
      bg={bg}
      borderBottom="1px solid"
      borderColor={border}
      px={{ base: 2, sm: 3, md: 6 }}
      py={{ base: 1.5, md: 3 }}
      position="fixed"
      top={0}
      left={0}
      right={0}
      zIndex={1000}
      height="60px"
      display="flex"
      alignItems="center"
    >
      <HStack
        maxW="100%"
        mx="auto"
        justify="space-between"
        spacing={{ base: 1, sm: 2, md: 4 }}
        w="100%"
      >
        {/* Logo + nazwa aplikacji (widoczne również na mobile) */}
        <HStack
          spacing={1}
          cursor="pointer"
          _hover={{ opacity: 0.8 }}
          onClick={() => navigate("/dashboard")}
          flex={1}
        >
          <Icon as={Database} boxSize={{ base: 5, md: 5 }} color="blue.600" flexShrink={0} />

          <Text
            fontSize={{ base: "sm", md: "lg" }}
            fontWeight="bold"
            color={textColor}
            display="block"
            whiteSpace="nowrap"
          >
            Brickly
          </Text>
        </HStack>

        {/* Menu użytkownika */}
        {isAuthenticated && user ? (
          <HStack spacing={{ base: 0.5, md: 2 }}>
            <VStack align="flex-end" spacing={0}>
              <Text
                fontSize={{ base: "10px", md: "sm" }}
                fontWeight="medium"
                color={textColor}
                whiteSpace="nowrap"
              >
                {user.firstName} {user.lastName}
              </Text>

              {activeTenantName && (
                <HStack spacing={1} fontSize={{ base: "10px", md: "10px" }} color={mutedColor}>
                  <Icon as={Building2} boxSize={{ base: 3.5, md: 3 }} />
                  <Text whiteSpace="nowrap">{activeTenantName}</Text>
                </HStack>
              )}
            </VStack>

            <NotificationBell />

            <Menu placement="bottom-end" strategy="fixed">
              <MenuButton cursor="pointer">
                <Avatar
                  size={{ base: "xs", md: "sm" }}
                  bg="blue.600"
                  color="white"
                  ignoreFallback
                  css={{ "& svg": { display: "none" } }}
                >
                  {initials}
                </Avatar>
              </MenuButton>

              <MenuList zIndex={1001}>
                <MenuItem icon={<UserIcon size={16} />} onClick={() => navigate("/profile")}>
                  Ustawienia profilu
                </MenuItem>

                <MenuDivider />

                <MenuItem
                  icon={<RefreshCw size={16} />}
                  onClick={() => navigate("/tenants/collaborating")}
                >
                  Zmień aktywnego tenanta
                </MenuItem>

                <MenuDivider />

                <MenuItem
                  color="red.500"
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
          <Text fontSize="sm" color="gray.500">
            Nie zalogowano
          </Text>
        )}
      </HStack>
    </Box>
  );
}
