import { useContext, useEffect, useState } from "react";
import {
  Box,
  Button,
  Heading,
  Text,
  Spinner,
  VStack,
  useColorModeValue,
} from "@chakra-ui/react";

import { AuthContext } from "../context/AuthContext";
import { useNavigate } from "react-router-dom";
import { authApi } from "../api/authApi";
import MainLayout from "../layout/MainLayout";

interface UserDetails {
  email: string;
  lastTenantId?: string | null;
}

export default function Dashboard() {
  const { logout } = useContext(AuthContext);
  const navigate = useNavigate();

  const [user, setUser] = useState<UserDetails | null>(null);
  const [loading, setLoading] = useState(true);

  const cardBg = useColorModeValue("white", "gray.800");
  const cardTextColor = useColorModeValue("gray.600", "gray.300");
  const pageBg = useColorModeValue("gray.50", "gray.900");
  const headingColor = useColorModeValue("black", "white");

  useEffect(() => {
    async function loadUser() {
      try {
        const res = await authApi.getProfile();

        if (!res.ok) {
          navigate("/login");
          return;
        }

        const data = await res.json();
        setUser(data);
      } catch (err) {
        console.error("Błąd pobierania danych użytkownika", err);
        navigate("/login");
      } finally {
        setLoading(false);
      }
    }

    loadUser();
  }, [navigate]);

  const handleLogout = async () => {
    try {
      await logout();
      navigate("/login");
    } catch {
      navigate("/login");
    }
  };

  if (loading) {
    return (
      <MainLayout>
        <VStack minH="100vh" justify="center">
          <Spinner size="xl" />
        </VStack>
      </MainLayout>
    );
  }

  return (
    <MainLayout>
      <Box p={10} bg={pageBg} minH="100vh">

      </Box>
    </MainLayout>
  );
}
