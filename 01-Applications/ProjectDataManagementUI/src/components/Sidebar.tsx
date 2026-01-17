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
  Badge,
} from "@chakra-ui/react";

import {
  Menu as MenuIcon,
  Building2,
  FolderKanban,
  Briefcase,
  FileText,
  Settings,
  RefreshCw,
  Mail,
} from "lucide-react";

import { useNavigate, useLocation } from "react-router-dom";
import { useState, useEffect } from "react";
import { getActiveInvitations } from "../services/tenantService";
import { InvitationStatus } from "../types/auth.types";
import { useGlobalCache } from "../hooks/useGlobalCache";

export default function Sidebar() {
  const navigate = useNavigate();
  const location = useLocation();
  const { isOpen, onOpen, onClose } = useDisclosure();

  const [invitationsCount, setInvitationsCount] = useState(0);

  // Globalny cache dla invitations (współdzielony z ActiveInvitations page)
  const invitationsCache = useGlobalCache(
    'invitations',
    async () => {
      return await getActiveInvitations();
    }
  );

  // Pobierz liczbę aktywnych zaproszeń
  useEffect(() => {
    const fetchInvitations = async () => {
      try {
        const invitations = await invitationsCache.fetch();
        const pending = invitations.filter((inv: { status: number }) => inv.status === InvitationStatus.Pending);
        setInvitationsCount(pending.length);
      } catch (error) {
        console.error("Błąd pobierania zaproszeń:", error);
      }
    };

    fetchInvitations();
    // Odświeżaj co 30 sekund
    const interval = setInterval(fetchInvitations, 30000);
    return () => clearInterval(interval);
  }, []);

  const bg = useColorModeValue("white", "gray.900");
  const border = useColorModeValue("gray.200", "gray.700");
  const activeBg = useColorModeValue("blue.100", "blue.700");
  const hoverBg = useColorModeValue("gray.200", "gray.600");

  const SidebarContent = () => (
    <VStack align="stretch" w="100%" spacing={2}>
      {/* Przełącz organizację */}
      <Button
        variant="ghost"
        justifyContent="flex-start"
        leftIcon={<RefreshCw size={20} />}
        w="100%"
        bg={location.pathname === "/tenants/collaborating" ? activeBg : "transparent"}
        _hover={{ bg: hoverBg }}
        onClick={() => navigate("/tenants/collaborating")}
      >
        Przełącz organizację
      </Button>

      {/* Projekty */}
      <Button
        variant="ghost"
        justifyContent="flex-start"
        leftIcon={<FolderKanban size={20} />}
        w="100%"
        bg={location.pathname === "/projects" ? activeBg : "transparent"}
        _hover={{ bg: hoverBg }}
        onClick={() => navigate("/projects")}
      >
        Projekty
      </Button>

      {/* Zarządzaj organizacjami */}
      <Button
        variant="ghost"
        justifyContent="flex-start"
        leftIcon={<Building2 size={20} />}
        w="100%"
        bg={location.pathname === "/tenants/managed" ? activeBg : "transparent"}
        _hover={{ bg: hoverBg }}
        onClick={() => navigate("/tenants/managed")}
      >
        Zarządzanie
      </Button>

      {/* Zaproszenia do organizacji */}
      <Button
        variant="ghost"
        justifyContent="flex-start"
        leftIcon={<Mail size={20} />}
        w="100%"
        bg={location.pathname === "/tenants/invitations" ? activeBg : "transparent"}
        _hover={{ bg: hoverBg }}
        onClick={() => navigate("/tenants/invitations")}
      >
        Zaproszenia
        {invitationsCount > 0 && (
          <Badge colorScheme="red" borderRadius="full" fontSize="xs" ml="auto">
            {invitationsCount}
          </Badge>
        )}
      </Button>

      {/* Zaplanowane prace */}
      <Button
        variant="ghost"
        justifyContent="flex-start"
        leftIcon={<Briefcase size={20} />}
        w="100%"
        bg={location.pathname === "/assigned-works" ? activeBg : "transparent"}
        _hover={{ bg: hoverBg }}
        onClick={() => navigate("/assigned-works")}
      >
        Zaplanowane prace
      </Button>

      {/* Szablony kosztorysów */}
      <Button
        variant="ghost"
        justifyContent="flex-start"
        leftIcon={<FileText size={20} />}
        w="100%"
        bg={location.pathname === "/cost-estimate-templates" ? activeBg : "transparent"}
        _hover={{ bg: hoverBg }}
        onClick={() => navigate("/cost-estimate-templates")}
      >
        Szablony kosztorysów
      </Button>

      {/* Ustawienia */}
      <Button
        variant="ghost"
        justifyContent="flex-start"
        leftIcon={<Settings size={20} />}
        w="100%"
        bg={location.pathname === "/profile" ? activeBg : "transparent"}
        _hover={{ bg: hoverBg }}
        onClick={() => navigate("/profile")}
      >
        Ustawienia
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
