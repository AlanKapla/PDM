import {
  Box,
  VStack,
  Text,
  Avatar,
  Button,
  HStack,
  useColorMode,
  useColorModeValue,
} from "@chakra-ui/react";

import {
  User as UserIcon,
  LogOut,
  Moon,
  Sun,
  LayoutDashboard,
} from "lucide-react";

import { useNavigate, useLocation } from "react-router-dom";
import { useContext, useEffect, useState } from "react";
import { AuthContext } from "../context/AuthContext";
import { authApi } from "../api/authApi";

export interface User {
  email: string;
  firstName: string;
  lastName: string;
}


export default function Sidebar() {
  const navigate = useNavigate();
  const location = useLocation();
  const { logout } = useContext(AuthContext);

  const [user, setUser] = useState<User | null>(null);

  useEffect(() => {
    async function loadUser() {
      try {
        const res = await authApi.getProfile();
        if (res.ok) {
          const json = await res.json();
          setUser(json);
        }
      } catch {
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
    { label: "Dashboard", icon: <LayoutDashboard size={20} />, path: "/dashboard" },
    { label: "Profil", icon: <UserIcon size={20} />, path: "/profile" },
  ];

  const initials = user
    ? `${user.firstName[0]}${user.lastName[0]}`.toUpperCase()
    : "U";

  return (
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
    >
      <VStack align="flex-start" spacing={6} h="100%">
        <HStack spacing={3}>
          <Avatar
            size="sm"
            bg="blue.600"
            color="white"
            src=""
            ignoreFallback
            css={{
              "& svg": { display: "none" }   // <<< ukrywa domyślną ikonę
            }}
          >
            {initials}
          </Avatar>



          <Text fontSize="lg" fontWeight="bold">
            Panel użytkownika
          </Text>
        </HStack>

        <VStack align="stretch" w="100%" spacing={2}>
          {menuItems.map((item) => {
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
          onClick={logout}
        >
          Wyloguj się
        </Button>
      </VStack>
    </Box>
  );
}
