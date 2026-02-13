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
      px={{ base: 1, sm: 3, md: 4 }}
      py={{ base: 1, md: 2 }}
      position="fixed"
      top={0}
      left={0}
      right={0}
      zIndex={1000}
      minH={{ base: "auto", md: "56px" }}
      display="flex"
      alignItems="center"
      justifyContent="center"
    >
      <HStack
        maxW="100%"
        mx="auto"
        justify="space-between"
        spacing={{ base: 1, sm: 2, md: 3 }}
        w="100%"
        alignItems="center"
      >
        {/* Logo + nazwa aplikacji */}
        <HStack
          spacing={{ base: 0.5, md: 1 }}
          cursor="pointer"
          _hover={{ opacity: 0.8 }}
          onClick={() => navigate("/dashboard")}
          flexShrink={0}
        >
          <Icon as={Database} boxSize={{ base: 5, md: 5 }} color="blue.600" flexShrink={0} />

          <Text
            fontSize={{ base: "xs", md: "md" }}
            fontWeight="bold"
            color={textColor}
            whiteSpace="nowrap"
          >
            Brickly
          </Text>
        </HStack>

        {/* Nazwa tenanta + imię i nazwisko - wycentrowane na mobilach */}
        {isAuthenticated && user && (
          <VStack
            align="center"
            spacing={0}
            flex={{ base: 1, md: "unset" }}
            display={{ base: "flex", md: "none" }}
          >
            <Text
              fontSize={{ base: "10px", md: "sm" }}
              fontWeight="medium"
              color={textColor}
              whiteSpace="nowrap"
              lineHeight="1"
            >
              {user.firstName} {user.lastName}
            </Text>

            {activeTenantName && (
              <HStack spacing={0.5} fontSize={{ base: "8px", md: "xs" }} color={mutedColor}>
                <Icon as={Building2} boxSize={{ base: 3, md: 3 }} flexShrink={0} />
                <Text whiteSpace="nowrap" noOfLines={1}>{activeTenantName}</Text>
              </HStack>
            )}
          </VStack>
        )}

        {/* Menu użytkownika - notifications + avatar + tenant (na PC) */}
        {isAuthenticated && user ? (
          <HStack spacing={{ base: 1, md: 1.5 }} flexShrink={0}>
            {/* Nazwa tenanta + imię i nazwisko - tylko na PC */}
            <VStack
              align="flex-end"
              spacing={0}
              display={{ base: "none", md: "flex" }}
            >
              <Text
                fontSize={{ base: "10px", md: "sm" }}
                fontWeight="medium"
                color={textColor}
                whiteSpace="nowrap"
                lineHeight="1"
              >
                {user.firstName} {user.lastName}
              </Text>

              {activeTenantName && (
                <HStack spacing={0.5} fontSize={{ base: "8px", md: "xs" }} color={mutedColor}>
                  <Icon as={Building2} boxSize={{ base: 3, md: 3 }} flexShrink={0} />
                  <Text whiteSpace="nowrap" noOfLines={1}>{activeTenantName}</Text>
                </HStack>
              )}
            </VStack>

            <NotificationBell />

            <Menu placement="bottom-end" strategy="fixed">
              <MenuButton cursor="pointer">
                <Avatar
                  size={{ base: "sm", md: "sm" }}
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
