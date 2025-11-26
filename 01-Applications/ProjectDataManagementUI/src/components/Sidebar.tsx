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
  Menu as MenuIcon,
  Building2,
  ChevronDown,
  ChevronUp,
  Settings,
  FolderKanban,
  FileText,
  Calculator,
} from "lucide-react";

import { useNavigate, useLocation } from "react-router-dom";
import { useState, useEffect } from "react";
import { useAuth } from "../hooks/useAuth";

export default function Sidebar() {
  const navigate = useNavigate();
  const location = useLocation();
  const { logout, user } = useAuth();
  const { isOpen, onOpen, onClose } = useDisclosure();

  // Przywróć stan z localStorage lub ustaw na false
  const [tenantsExpanded, setTenantsExpanded] = useState(() => {
    const saved = localStorage.getItem("sidebar_tenants_expanded");
    return saved === "true";
  });
  const [settingsExpanded, setSettingsExpanded] = useState(() => {
    const saved = localStorage.getItem("sidebar_settings_expanded");
    return saved === "true";
  });

  // Automatycznie rozwiń sekcję jeśli użytkownik jest na danej ścieżce
  useEffect(() => {
    if (location.pathname.startsWith("/tenants") && !tenantsExpanded) {
      setTenantsExpanded(true);
    }
    if (location.pathname.startsWith("/profile") && !settingsExpanded) {
      setSettingsExpanded(true);
    }
  }, [location.pathname]);

  // Zapisz stan do localStorage przy każdej zmianie
  useEffect(() => {
    localStorage.setItem("sidebar_tenants_expanded", String(tenantsExpanded));
  }, [tenantsExpanded]);

  useEffect(() => {
    localStorage.setItem("sidebar_settings_expanded", String(settingsExpanded));
  }, [settingsExpanded]);

  const { colorMode, toggleColorMode } = useColorMode();

  const bg = useColorModeValue("white", "gray.900");
  const border = useColorModeValue("gray.200", "gray.700");
  const activeBg = useColorModeValue("blue.100", "blue.700");
  const hoverBg = useColorModeValue("gray.200", "gray.600");

  const initials = user
    ? `${user.firstName[0]}${user.lastName[0]}`.toUpperCase()
    : "U";

  const SidebarContent = () => (
    <VStack align="flex-start" spacing={6} h="100%" overflow="auto">
        {/* Logo/Nazwa aplikacji */}
        <Box 
          w="100%" 
          py={3} 
          px={4}
          bg="linear-gradient(135deg, #4F46E5 0%, #06B6D4 100%)"
          borderRadius="xl"
          cursor="pointer"
          _hover={{ 
            transform: "translateY(-2px)",
            boxShadow: "lg"
          }}
          transition="all 0.3s"
          onClick={() => navigate("/")}
          mb={2}
          boxShadow="md"
        >
          <Text 
            fontSize="sm" 
            fontWeight="bold" 
            color="white"
            letterSpacing="wide"
            textAlign="center"
            whiteSpace="nowrap"
          >
            Project Data Management
          </Text>
        </Box>

        {/* Profil użytkownika */}
        <HStack 
          spacing={3} 
          cursor="pointer" 
          _hover={{ opacity: 0.8 }}
          onClick={() => navigate("/profile")}
          w="100%"
        >
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

          <VStack align="flex-start" spacing={0} flex="1" minW="0">
            <Text fontSize="sm" fontWeight="bold" isTruncated w="100%">
              {user?.firstName} {user?.lastName}
            </Text>
            <Text fontSize="xs" color="gray.500" isTruncated w="100%">
              {user?.email}
            </Text>
          </VStack>
        </HStack>

        <VStack align="stretch" w="100%" spacing={2}>
          {/* Organizacje na samej górze */}
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

          {/* Panel rozwijany organizacji */}
          <Collapse in={tenantsExpanded} animateOpacity>
            <VStack align="stretch" w="100%" spacing={2} pl={4} pt={2}>
              <Button
                variant="ghost"
                size="sm"
                justifyContent="flex-start"
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

          {/* Projekty */}
          <Button
            variant="ghost"
            justifyContent="flex-start"
            leftIcon={<FolderKanban size={20} />}
            w="100%"
            bg={location.pathname.startsWith("/projects") ? activeBg : "transparent"}
            _hover={{ bg: hoverBg }}
            onClick={() => navigate("/projects")}
          >
            Projekty
          </Button>

          {/* Pliki */}
          <Button
            variant="ghost"
            justifyContent="flex-start"
            leftIcon={<FileText size={20} />}
            w="100%"
            bg={location.pathname.startsWith("/files") ? activeBg : "transparent"}
            _hover={{ bg: hoverBg }}
            onClick={() => navigate("/files")}
          >
            Pliki
          </Button>

          {/* Kosztorysy */}
          <Button
            variant="ghost"
            justifyContent="flex-start"
            leftIcon={<Calculator size={20} />}
            w="100%"
            bg={location.pathname.startsWith("/estimates") ? activeBg : "transparent"}
            _hover={{ bg: hoverBg }}
            onClick={() => navigate("/estimates")}
          >
            Kosztorysy
          </Button>

          {/* Sekcja Ustawienia */}
          <Button
            variant="ghost"
            justifyContent="space-between"
            leftIcon={<Settings size={20} />}
            rightIcon={settingsExpanded ? <ChevronUp size={16} /> : <ChevronDown size={16} />}
            w="100%"
            bg={location.pathname.startsWith("/profile") ? activeBg : "transparent"}
            _hover={{ bg: hoverBg }}
            onClick={() => setSettingsExpanded(!settingsExpanded)}
          >
            Ustawienia
          </Button>

          {/* Panel rozwijany ustawień */}
          <Collapse in={settingsExpanded} animateOpacity>
            <VStack align="stretch" w="100%" spacing={2} pl={4} pt={2}>
              <Button
                variant="ghost"
                size="sm"
                justifyContent="flex-start"
                leftIcon={<UserIcon size={16} />}
                w="100%"
                fontSize="sm"
                bg={location.pathname === "/profile" ? activeBg : "transparent"}
                _hover={{ bg: hoverBg }}
                onClick={() => navigate("/profile")}
              >
                Profil
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
            <SidebarContent />
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
