import React from "react";
import {
  Box,
  Button,
  Heading,
  HStack,
  SimpleGrid,
  Text,
  useColorModeValue,
} from "@chakra-ui/react";
import { ArrowLeft, Shield } from "lucide-react";
import { useNavigate } from "react-router-dom";
import MainLayout from "../layout/MainLayout";
import { ActivityLogsAdminPanel } from "../components/admin/ActivityLogsAdminPanel";
import { ColdMailsAdminPanel } from "../components/admin/ColdMailsAdminPanel";
import { DemoModePanel } from "../components/admin/DemoModePanel";
import { UsersAdminPanel } from "../components/admin/UsersAdminPanel";

export default function AdminPage(): React.ReactElement {
  const navigate = useNavigate();
  const pageBg = useColorModeValue("gray.50", "gray.900");
  const mutedText = useColorModeValue("gray.600", "gray.400");

  return (
    <MainLayout>
      <Box bg={pageBg} minH="calc(100vh - 60px)" p={{ base: 4, md: 8 }}>
        <Box maxW="900px" mx="auto">
          <HStack mb={6}>
            <Button
              leftIcon={<ArrowLeft size={16} aria-hidden />}
              variant="ghost"
              size="sm"
              onClick={() => navigate("/dashboard")}
            >
              Wróć
            </Button>
          </HStack>

          <HStack spacing={3} mb={2}>
            <Shield size={24} aria-hidden />
            <Heading size="lg">Panel administratora</Heading>
          </HStack>

          <Text color={mutedText} mb={8}>
            Narzędzia systemowe dostępne wyłącznie dla SuperAdmin.
          </Text>

          <SimpleGrid columns={{ base: 1, lg: 2 }} spacing={6}>
            <DemoModePanel />
            <UsersAdminPanel />
            <ColdMailsAdminPanel />
            <ActivityLogsAdminPanel />
          </SimpleGrid>
        </Box>
      </Box>
    </MainLayout>
  );
}
