import { useEffect, useState } from "react";
import { Box, Spinner, VStack } from "@chakra-ui/react";
import { useNavigate } from "react-router-dom";
import { authApi } from "../api/authApi";
import MainLayout from "../layout/MainLayout";

export default function Dashboard() {
  const navigate = useNavigate();
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    async function loadUser() {
      try {
        const res = await authApi.getProfile();
        if (!res.ok) {
          navigate("/login");
          return;
        }
      } catch (err) {
        console.error("Błąd pobierania danych użytkownika", err);
        navigate("/login");
      } finally {
        setLoading(false);
      }
    }

    loadUser();
  }, [navigate]);

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
      <Box p={10} minH="100vh">
        {/* content */}
      </Box>
    </MainLayout>
  );
}
