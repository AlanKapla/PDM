import {
  Box,
  VStack,
  Button,
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
  Menu as MenuIcon,
  Building2,
  ChevronDown,
  ChevronUp,
  FolderKanban,
  FileText,
  Calculator,
} from "lucide-react";

import { useNavigate, useLocation } from "react-router-dom";
import { useState, useEffect } from "react";

export default function Sidebar() {
  const navigate = useNavigate();
  const location = useLocation();
  const { isOpen, onOpen, onClose } = useDisclosure();

  // Przywróć stan z localStorage lub ustaw na false
  const [tenantsExpanded, setTenantsExpanded] = useState(() => {
    const saved = localStorage.getItem("sidebar_tenants_expanded");
    return saved === "true";
  });

  // Automatycznie rozwiń sekcję jeśli użytkownik jest na danej ścieżce
  useEffect(() => {
    if (location.pathname.startsWith("/tenants") && !tenantsExpanded) {
      setTenantsExpanded(true);
    }
  }, [location.pathname]);

  // Zapisz stan do localStorage przy każdej zmianie
  useEffect(() => {
    localStorage.setItem("sidebar_tenants_expanded", String(tenantsExpanded));
  }, [tenantsExpanded]);

  const bg = useColorModeValue("white", "gray.900");
  const border = useColorModeValue("gray.200", "gray.700");
  const activeBg = useColorModeValue("blue.100", "blue.700");
  const hoverBg = useColorModeValue("gray.200", "gray.600");

  const SidebarContent = () => (
    <VStack align="flex-start" spacing={6} h="100%" overflow="auto">
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
        </VStack>
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
        top="60px"
        w="250px"
        h="calc(100vh - 60px)"
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
