import {
  Box,
  SimpleGrid,
  VStack,
  Text,
  Icon,
  Flex,
  useColorModeValue,
} from "@chakra-ui/react";
import {
  Building2,
  FolderKanban,
  FileText,
  Calculator,
  Settings,
} from "lucide-react";
import { useNavigate } from "react-router-dom";
import MainLayout from "../layout/MainLayout";

export default function Dashboard() {
  const navigate = useNavigate();

  const cardBg = useColorModeValue("#1a1a1a", "#1a1a1a");
  const cardHover = useColorModeValue("#232323", "#232323");
  const border = useColorModeValue("#2a2a2a", "#2a2a2a");

  const menuCards = [
    {
      title: "Organizacje",
      desc: "Zarządzaj organizacjami i członkostwem",
      icon: Building2,
      path: "/tenants/collaborating",
    },
    {
      title: "Projekty",
      desc: "Twórz i przeglądaj projekty",
      icon: FolderKanban,
      path: "/projects",
    },
    {
      title: "Pliki",
      desc: "Dokumentacja, rysunki i załączniki",
      icon: FileText,
      path: "/files",
    },
    {
      title: "Kosztorysy",
      desc: "Zarządzanie kosztami i wycenami",
      icon: Calculator,
      path: "/estimates",
    },
    {
      title: "Ustawienia",
      desc: "Profil oraz preferencje konta",
      icon: Settings,
      path: "/profile",
    },
  ];

  return (
    <MainLayout>
      <Box px={12} py={10} mt="20px">
        <Text
          fontSize="2xl"
          fontWeight="semibold"
          mb={8}
          color="gray.200"
        >
          Panel główny
        </Text>

        <SimpleGrid columns={{ base: 1, md: 2, lg: 3 }} spacing={8}>
          {menuCards.map((card) => (
            <Flex
              key={card.title}
              direction="column"
              bg={cardBg}
              border="1px solid"
              borderColor={border}
              borderRadius="lg"
              p={6}
              cursor="pointer"
              transition="0.2s"
              _hover={{
                bg: cardHover,
                borderColor: "#3a3a3a",
                transform: "translateY(-2px)",
              }}
              onClick={() => navigate(card.path)}
            >
              <Icon
                as={card.icon}
                color="gray.300"
                boxSize={7}
                mb={4}
              />

              <VStack align="flex-start" spacing={1} flex="1">
                <Text fontSize="lg" fontWeight="semibold" color="gray.100">
                  {card.title}
                </Text>

                <Text fontSize="sm" color="gray.400">
                  {card.desc}
                </Text>
              </VStack>
            </Flex>
          ))}
        </SimpleGrid>
      </Box>
    </MainLayout>
  );
}
