import {
  Box,
  VStack,
  Text,
  Avatar,
  Button,
  HStack,
  useColorMode,
  useColorModeValue,
  Drawer,
  DrawerBody,
  DrawerHeader,
  DrawerOverlay,
  DrawerContent,
  DrawerCloseButton,
  useDisclosure,
  IconButton,
  Collapse,
} from "@chakra-ui/react";

import {
  User as UserIcon,
  LogOut,
  Moon,
  Sun,
  LayoutDashboard,
  Menu as MenuIcon,
  Building2,
  ChevronDown,
  ChevronUp,
  Mail,
} from "lucide-react";

import { useNavigate, useLocation } from "react-router-dom";
import { useEffect, useState } from "react";
import { useAuth } from "../hooks/useAuth";
import { getUserProfile } from "../services/authService";
import type { UserProfile } from "../types/auth.types";

export default function Sidebar() {
  const navigate = useNavigate();
  const location = useLocation();
  const { logout } = useAuth();
  const { isOpen, onOpen, onClose } = useDisclosure();

  const [user, setUser] = useState<UserProfile | null>(null);
  const [tenantsExpanded, setTenantsExpanded] = useState(false);

  useEffect(() => {
    async function loadUser() {
      try {
        const profile = await getUserProfile();
        setUser(profile);
      } catch (error) {
        console.error("Błąd ładowania użytkownika:", error);
        setUser(null);
      }
    }

    loadUser();
  }, []);

  const { colorMode, toggleColorMode } = useColorMode();

  const bg = useColorModeValue("white", "gray.900");
  const border = useColorModeValue("gray.200", "gray.700");
  const activeBg = useColorModeValue("blue.100", "blue.700");
  const hoverBg = useColorModeValue("gray.200", "gray.600");

  const menuItems = [
    { label: "Panel główny", icon: <LayoutDashboard size={20} />, path: "/dashboard" },
    { label: "Profil", icon: <UserIcon size={20} />, path: "/profile" },
    { label: "Organizacje", icon: <Building2 size={20} />, path: "/tenants" },
  ];

  const initials = user
    ? `${user.firstName[0]}${user.lastName[0]}`.toUpperCase()
    : "U";

  const SidebarContent = () => (
    <VStack align="flex-start" spacing={6} h="100%" overflow="auto">
        <HStack spacing={3}>
          <Avatar
            size="sm"
            bg="blue.600"
            color="white"
            src=""
            ignoreFallback
            css={{
              "& svg": { display: "none" }
            }}
          >
            {initials}
          </Avatar>

          <VStack align="flex-start" spacing={0}>
            <Text fontSize="sm" fontWeight="bold">
              {user?.firstName} {user?.lastName}
            </Text>
            <Text fontSize="xs" color="gray.500">
              {user?.email}
            </Text>
          </VStack>
        </HStack>

        <VStack align="stretch" w="100%" spacing={2}>
          {menuItems.filter(item => item.path !== "/tenants").map((item) => {
            const isActive = location.pathname === item.path;

            return (
              <Button
                key={item.label}
                variant="ghost"
                justifyContent="flex-start"
                leftIcon={item.icon}
                w="100%"
                bg={isActive ? activeBg : "transparent"}
                _hover={{ bg: hoverBg }}
                onClick={() => navigate(item.path)}
              >
                {item.label}
              </Button>
            );
          })}

          {/* Przycisk Organizacje z rozwinięciem */}
          <Button
            variant="ghost"
            justifyContent="space-between"
            leftIcon={<Building2 size={20} />}
            rightIcon={tenantsExpanded ? <ChevronUp size={16} /> : <ChevronDown size={16} />}
            w="100%"
            bg={location.pathname.startsWith("/tenants") ? activeBg : "transparent"}
            _hover={{ bg: hoverBg }}
            onClick={() => setTenantsExpanded(!tenantsExpanded)}
          >
            Organizacje
          </Button>

          {/* Panel rozwijany z trzema opcjami */}
          <Collapse in={tenantsExpanded} animateOpacity>
            <VStack align="stretch" w="100%" spacing={2} pl={4} pt={2}>
              <Button
                variant="ghost"
                size="sm"
                justifyContent="flex-start"
                leftIcon={<Mail size={16} />}
                w="100%"
                fontSize="sm"
                bg={location.pathname === "/tenants/invitations" ? activeBg : "transparent"}
                _hover={{ bg: hoverBg }}
                onClick={() => navigate("/tenants/invitations")}
              >
                Aktywne zaproszenia
              </Button>

              <Button
                variant="ghost"
                size="sm"
                justifyContent="flex-start"
                w="100%"
                fontSize="sm"
                bg={location.pathname === "/tenants/collaborating" ? activeBg : "transparent"}
                _hover={{ bg: hoverBg }}
                onClick={() => navigate("/tenants/collaborating")}
              >
                Z którymi współpracujesz
              </Button>

              <Button
                variant="ghost"
                size="sm"
                justifyContent="flex-start"
                w="100%"
                fontSize="sm"
                bg={location.pathname === "/tenants/managed" ? activeBg : "transparent"}
                _hover={{ bg: hoverBg }}
                onClick={() => navigate("/tenants/managed")}
              >
                Którymi zarządzasz
              </Button>
            </VStack>
          </Collapse>
        </VStack>

        <Box flex="1" />

        <Button
          leftIcon={colorMode === "light" ? <Moon size={20} /> : <Sun size={20} />}
          w="100%"
          variant="outline"
          onClick={toggleColorMode}
        >
          {colorMode === "light" ? "Tryb ciemny" : "Tryb jasny"}
        </Button>

        <Button
          leftIcon={<LogOut size={20} />}
          colorScheme="red"
          w="100%"
          onClick={async () => {
            await logout();
            navigate("/");
          }}
        >
          Wyloguj się
        </Button>
      </VStack>
  );

  return (
    <>
      {/* Mobile Menu Button */}
      <IconButton
        aria-label="Open menu"
        icon={<MenuIcon size={24} />}
        onClick={onOpen}
        position="fixed"
        top={3}
        left={4}
        zIndex={20}
        display={{ base: "flex", md: "none" }}
        colorScheme="blue"
        size="sm"
      />

      {/* Mobile Drawer */}
      <Drawer isOpen={isOpen} placement="left" onClose={onClose}>
        <DrawerOverlay />
        <DrawerContent bg={bg}>
          <DrawerCloseButton />
          <DrawerHeader>Menu</DrawerHeader>
          <DrawerBody>
            <Box onClick={onClose}>
              <SidebarContent />
            </Box>
          </DrawerBody>
        </DrawerContent>
      </Drawer>

      {/* Desktop Sidebar */}
      <Box
        position="fixed"
        left="0"
        top="0"
        w="250px"
        h="100vh"
        bg={bg}
        borderRight="1px solid"
        borderColor={border}
        p={5}
        display={{ base: "none", md: "block" }}
      >
        <SidebarContent />
      </Box>
    </>
  );
}
