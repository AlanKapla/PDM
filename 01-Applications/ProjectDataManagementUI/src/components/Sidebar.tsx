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
  Settings,
  RefreshCw,
  Mail,
  MessageSquare,
} from "lucide-react";

import { useLocation, Link as RouterLink } from "react-router-dom";
import { useContext } from "react";
import { InvitationStatus } from "../types/auth.types";
import { useActiveInvitations, useActiveProjectInvitations } from "../hooks/queries";
import { ChatUnreadContext } from "../context/ChatUnreadContext";

// ===== SIDEBAR CONTENT COMPONENT =====
export function SidebarContent() {
  const location = useLocation();
  const { totalUnread } = useContext(ChatUnreadContext);
  const { data: invitations = [] } = useActiveInvitations({
    refetchInterval: 30000,
    refetchIntervalInBackground: false,
  });
  const { data: projectInvitations = [] } = useActiveProjectInvitations({
    refetchInterval: 30000,
    refetchIntervalInBackground: false,
  });
  const invitationsCount =
    invitations.filter((inv) => inv.status === InvitationStatus.Pending).length +
    projectInvitations.filter((inv) => inv.status === InvitationStatus.Pending).length;

  const activeBg = useColorModeValue("primary.100", "primary.700");
  const hoverBg = useColorModeValue("gray.200", "gray.600");

  return (
    <VStack align="stretch" w="100%" spacing={2}>
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

      {/* Zaplanowane prace */}
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
        display={{ base: "none", md: "block" }}
      >
        <SidebarContent />
      </Box>
    </>
  );
}
