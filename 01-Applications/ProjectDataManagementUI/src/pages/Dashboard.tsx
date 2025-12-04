import {
  Box,
  SimpleGrid,
  Card,
  CardBody,
  Text,
  Icon,
  VStack,
  useColorModeValue,
  Badge,
} from "@chakra-ui/react";
import {
  Building2,
  FolderKanban,
  Settings,
  FileText,
  Calculator,
} from "lucide-react";
import { useNavigate } from "react-router-dom";
import MainLayout from "../layout/MainLayout";
import { motion } from "framer-motion";

const MotionCard = motion(Card);

export default function Dashboard() {
  const navigate = useNavigate();

  const pastel = {
    blue: "#5B8DEF",
    green: "#4FD1C5",
    purple: "#B794F4",
    orange: "#F6AD55",
    gray: "#A0AEC0",
  };

  const cardBg = useColorModeValue("white", "gray.800");
  const cardBorder = useColorModeValue("rgba(0,0,0,0.08)", "rgba(255,255,255,0.1)");
  const textSecondary = useColorModeValue("gray.600", "gray.400");

  const menuCards = [
    {
      title: "Organizacje",
      desc: "Zarządzaj strukturą, zaproszeniami i współpracownikami",
      icon: Building2,
      color: pastel.blue,
      gradient: "linear(to-br, blue.50, white)",
      path: "/tenants/collaborating",
    },
    {
      title: "Projekty",
      desc: "Przeglądaj i zarządzaj strukturą projektową",
      icon: FolderKanban,
      color: pastel.green,
      gradient: "linear(to-br, teal.50, white)",
      path: "/projects",
    },
    {
      title: "Pliki",
      desc: "Zarządzaj dokumentami oraz załącznikami projektów",
      icon: FileText,
      color: pastel.purple,
      gradient: "linear(to-br, purple.50, white)",
      path: "/files",
    },
    {
      title: "Kosztorysy",
      desc: "Twórz i przeglądaj kosztorysy i wyceny",
      icon: Calculator,
      color: pastel.orange,
      gradient: "linear(to-br, orange.50, white)",
      path: "/cost-editor",
    },
    {
      title: "Ustawienia",
      desc: "Personalizuj swoje konto i preferencje",
      icon: Settings,
      color: pastel.gray,
      gradient: "linear(to-br, gray.50, white)",
      path: "/profile",
    },
  ];

  return (
    <MainLayout>
      <Box p={12} minH="100vh" bg="white">
        <Text
          fontSize="3xl"
          fontWeight="700"
          mb={12}
          letterSpacing="-0.5px"
          color="black"
        >
          Panel główny
        </Text>

        <SimpleGrid columns={{ base: 1, md: 2, lg: 3 }} spacing={10}>
          {menuCards.map((card, i) => (
            <MotionCard
              key={card.title}
              bgGradient={card.gradient}
              bg={cardBg}
              border={`1px solid ${cardBorder}`}
              rounded="2xl"
              shadow="sm"
              cursor="pointer"
              onClick={() => navigate(card.path)}
              initial={{ opacity: 0, y: 30, scale: 0.98 }}
              animate={{ opacity: 1, y: 0, scale: 1 }}
              transition={{ duration: 0.45, delay: i * 0.05 }}
              _hover={{ shadow: "xl", transform: "translateY(-6px)" }}
            >
              <CardBody p={7}>
                <VStack align="flex-start" spacing={5}>
                  <Badge
                    px={3}
                    py={2}
                    rounded="full"
                    bg={card.color + "20"}
                    color={card.color}
                    fontSize="0.8rem"
                    display="flex"
                    alignItems="center"
                    gap={2}
                  >
                    <Icon as={card.icon} size={18} strokeWidth={1.8} />
                    {card.title}
                  </Badge>

                  <Text fontSize="sm" color={textSecondary} lineHeight="1.5">
                    {card.desc}
                  </Text>
                </VStack>
              </CardBody>
            </MotionCard>
          ))}
        </SimpleGrid>
      </Box>
    </MainLayout>
  );
}
