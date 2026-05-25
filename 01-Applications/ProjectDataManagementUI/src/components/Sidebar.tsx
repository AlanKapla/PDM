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
  MessageSquare,
  ShieldAlert,
} from "lucide-react";

import { useLocation, Link as RouterLink } from "react-router-dom";
import { useContext } from "react";
import { InvitationStatus } from "../types/auth.types";
import { useActiveInvitations } from "../hooks/queries";
import { ChatUnreadContext } from "../context/ChatUnreadContext";
import { AuthContext } from "../context/AuthContext";

// ===== SIDEBAR CONTENT COMPONENT =====
export function SidebarContent() {
  const location = useLocation();
  const { totalUnread } = useContext(ChatUnreadContext);
  const { user } = useContext(AuthContext);
  const { data: invitations = [] } = useActiveInvitations({
    refetchInterval: 30000,
  });
  const invitationsCount = invitations.filter(
    (inv) => inv.status === InvitationStatus.Pending
  ).length;

  const activeBg = useColorModeValue("primary.100", "primary.700");
  const hoverBg = useColorModeValue("gray.200", "gray.600");
  const adminActiveBg = useColorModeValue("red.100", "red.800");
  const adminHoverBg = useColorModeValue("red.50", "red.700");

  return (
    <VStack align="stretch" w="100%" spacing={2} h="100%">
      {/* Przełącz organizację — przeniesione do strony Projekty (Select) */}
      {/* <Button
        as={RouterLink}
        to="/tenants/collaborating"
        variant="ghost"
        justifyContent="flex-start"
        leftIcon={<RefreshCw size={20} />}
        w="100%"
        bg={location.pathname === "/tenants/collaborating" ? activeBg : "transparent"}
        _hover={{ bg: hoverBg, textDecoration: "none" }}
        _activeLink={{ textDecoration: "none" }}
        textDecoration="none"
      >
        Przełącz organizację
      </Button> */}

      {/* Projekty */}
      <Button
        as={RouterLink}
        to="/projects"
        variant="ghost"
        justifyContent="flex-start"
        leftIcon={<FolderKanban size={20} />}
        w="100%"
        bg={location.pathname === "/projects" ? activeBg : "transparent"}
        _hover={{ bg: hoverBg, textDecoration: "none" }}
        textDecoration="none"
      >
        Projekty
      </Button>

      {/* Wiadomości */}
      <Button
        as={RouterLink}
        to="/chat"
        variant="ghost"
        justifyContent="flex-start"
        leftIcon={
          <Box position="relative" display="inline-flex">
            <MessageSquare size={20} color={totalUnread > 0 && !location.pathname.startsWith("/chat") ? "var(--chakra-colors-primary-500)" : undefined} />
            {totalUnread > 0 && !location.pathname.startsWith("/chat") && (
              <Box
                position="absolute"
                top="-3px"
                right="-3px"
                w="8px"
                h="8px"
                bg="primary.500"
                borderRadius="full"
                border="2px solid"
                borderColor="white"
              />
            )}
          </Box>
        }
        w="100%"
        bg={location.pathname.startsWith("/chat") ? activeBg : "transparent"}
        _hover={{ bg: hoverBg, textDecoration: "none" }}
        textDecoration="none"
      >
        Wiadomości
        {totalUnread > 0 && (
          <Badge colorScheme="primary" borderRadius="full" fontSize="xs" ml="auto">
            {totalUnread > 99 ? "99+" : totalUnread}
          </Badge>
        )}
      </Button>

      {/* Zarządzaj organizacjami */}
      <Button
        as={RouterLink}
        to="/tenants/managed"
        variant="ghost"
        justifyContent="flex-start"
        leftIcon={<Building2 size={20} />}
        w="100%"
        bg={location.pathname === "/tenants/managed" ? activeBg : "transparent"}
        _hover={{ bg: hoverBg, textDecoration: "none" }}
        textDecoration="none"
      >
        Zarządzanie
      </Button>

      {/* Zaproszenia do organizacji */}
      <Button
        as={RouterLink}
        to="/tenants/invitations"
        variant="ghost"
        justifyContent="flex-start"
        leftIcon={<Mail size={20} />}
        w="100%"
        bg={location.pathname === "/tenants/invitations" ? activeBg : "transparent"}
        _hover={{ bg: hoverBg, textDecoration: "none" }}
        textDecoration="none"
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
        as={RouterLink}
        to="/assigned-works"
        variant="ghost"
        justifyContent="flex-start"
        leftIcon={<Briefcase size={20} />}
        w="100%"
        bg={location.pathname === "/assigned-works" ? activeBg : "transparent"}
        _hover={{ bg: hoverBg, textDecoration: "none" }}
        textDecoration="none"
      >
        Zaplanowane prace
      </Button>

      {/* Szablony kosztorysów */}
      <Button
        as={RouterLink}
        to="/cost-estimate-templates"
        variant="ghost"
        justifyContent="flex-start"
        leftIcon={<FileText size={20} />}
        w="100%"
        bg={location.pathname === "/cost-estimate-templates" ? activeBg : "transparent"}
        _hover={{ bg: hoverBg, textDecoration: "none" }}
        textDecoration="none"
      >
        Szablony kosztorysów
      </Button>

      {/* Ustawienia */}
      <Button
        as={RouterLink}
        to="/profile"
        variant="ghost"
        justifyContent="flex-start"
        leftIcon={<Settings size={20} />}
        w="100%"
        bg={location.pathname === "/profile" ? activeBg : "transparent"}
        _hover={{ bg: hoverBg, textDecoration: "none" }}
        textDecoration="none"
      >
        Ustawienia
      </Button>

      {/* Admin — tylko dla SuperAdmin, przyklejony do dołu */}
      {user?.isSuperAdmin && (
        <Box mt="auto" pt={2}>
          <Button
            as={RouterLink}
            to="/admin"
            variant="ghost"
            justifyContent="flex-start"
            leftIcon={<ShieldAlert size={20} />}
            w="100%"
            bg={location.pathname.startsWith("/admin") ? adminActiveBg : "transparent"}
            color={location.pathname.startsWith("/admin") ? "red.600" : "red.500"}
            _hover={{ bg: adminHoverBg, textDecoration: "none", color: "red.600" }}
            textDecoration="none"
          >
            Admin
          </Button>
        </Box>
      )}
    </VStack>
  );
}

// ===== SIDEBAR COMPONENT =====
export default function Sidebar() {
  const { isOpen, onOpen, onClose } = useDisclosure();
  const bg = useColorModeValue("white", "gray.900");
  const border = useColorModeValue("gray.200", "gray.700");

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
        colorScheme="primary"
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
        display={{ base: "none", md: "flex" }}
        flexDirection="column"
      >
        <SidebarContent />
      </Box>
    </>
  );
}
