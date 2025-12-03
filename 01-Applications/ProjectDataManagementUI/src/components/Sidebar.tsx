import {
  Box,
  VStack,
  Text,
  HStack,
  Avatar,
  Divider,
  useColorModeValue,
} from "@chakra-ui/react";
import { Link as RouterLink, useLocation, useNavigate } from "react-router-dom";
import {
  FolderKanban,
  FileText,
  Users,
  Settings,
  ChevronRight,
} from "lucide-react";
import { useAuth } from "../hooks/useAuth";

export default function Sidebar() {
  const bg = useColorModeValue("white", "#0f0f0f");
  const activeBg = useColorModeValue("gray.100", "#1a1a1a");
  const textColor = useColorModeValue("gray.800", "gray.200");
  const mutedColor = useColorModeValue("gray.600", "gray.500");
  const border = useColorModeValue("gray.200", "#1e1e1e");

  const location = useLocation();
  const navigate = useNavigate();
  const { user, logout } = useAuth();

  /* --- UNIWERSALNY NAV ITEM (obsługuje Link i onClick) --- */
  const NavItem = ({
    icon,
    label,
    to,
    onClick,
  }: {
    icon: React.ReactNode;
    label: string;
    to?: string;            // opcjonalnie
    onClick?: () => void;   // opcjonalnie
  }) => {
    const isActive = to ? location.pathname.startsWith(to) : false;

    // jeśli podano "to", używamy RouterLink
    const Component: any = to ? RouterLink : "div";

    return (
      <HStack
        as={Component}
        to={to || undefined}
        onClick={onClick}
        spacing={3}
        px={3}
        py={2}
        borderRadius="md"
        bg={isActive ? activeBg : "transparent"}
        color={isActive ? textColor : mutedColor}
        _hover={{
          bg: activeBg,
          color: textColor,
          cursor: "pointer",
        }}
        transition="all 0.15s ease"
      >
        <Box opacity={0.8}>{icon}</Box>
        <Text fontSize="sm" fontWeight={isActive ? "semibold" : "normal"}>
          {label}
        </Text>
      </HStack>
    );
  };

  return (
    <Box
      position="fixed"
      left="0"
      top="0"
      w="240px"
      h="100vh"
      bg={bg}
      borderRight="1px solid"
      borderColor={border}
      p={4}
      color={textColor}
    >
      {/* LOGO */}
      <Text 
        fontSize="lg" 
        fontWeight="bold" 
        mb={6}
        cursor="pointer"
        onClick={() => navigate("/dashboard")}
        _hover={{ opacity: 0.8 }}
      >
        Project Data Management
      </Text>

      {/* USER */}
      <HStack spacing={3} mb={6}>
        <Avatar size="sm" bg="gray.300" />
        <Box>
          <Text fontSize="sm" fontWeight="semibold">
            {user?.firstName} {user?.lastName}
          </Text>
          <Text fontSize="xs" color={mutedColor}>
            {user?.email}
          </Text>
        </Box>
      </HStack>

      <VStack align="stretch" spacing={5}>
        {/* ORGANIZACJE */}
        <Box>
          <Text fontSize="xs" mb={2} color={mutedColor}>
            ORGANIZACJE
          </Text>
          <VStack align="stretch" spacing={1}>
            <NavItem
              icon={<Users size={16} />}
              label="Aktywne zaproszenia"
              to="/tenants/invitations"
            />
            <NavItem
              icon={<Users size={16} />}
              label="Współpracujesz"
              to="/tenants/collaborating"
            />
            <NavItem
              icon={<Users size={16} />}
              label="Zarządzasz"
              to="/tenants/managed"
            />
          </VStack>
        </Box>

        <Divider borderColor={border} />

        {/* PROJEKTY */}
        <Box>
          <Text fontSize="xs" mb={2} color={mutedColor}>
            PROJEKTY
          </Text>
          <VStack align="stretch" spacing={1}>
            <NavItem
              icon={<FolderKanban size={16} />}
              label="Projekty"
              to="/projects"
            />
            <NavItem
              icon={<FileText size={16} />}
              label="Pliki"
              to="/files"
            />
            <NavItem
              icon={<FileText size={16} />}
              label="Kosztorysy"
              to="/estimates"
            />
          </VStack>
        </Box>

        <Divider borderColor={border} />

        {/* USTAWIENIA */}
        <Box>
          <Text fontSize="xs" mb={2} color={mutedColor}>
            USTAWIENIA
          </Text>
          <VStack align="stretch" spacing={1}>
            <NavItem
              icon={<Settings size={16} />}
              label="Profil"
              to="/profile"
            />
          </VStack>
        </Box>
      </VStack>

      {/* DOLE – LOGOUT */}
      <Box position="absolute" bottom="20px" w="calc(100% - 2rem)">
        <Divider borderColor={border} mb={3} />

        <NavItem
          icon={<ChevronRight size={16} />}
          label="Wyloguj się"
          onClick={async () => {
            await logout();
            navigate("/");
          }}
        />
      </Box>
    </Box>
  );
}
