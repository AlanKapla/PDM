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

  const cardBg = useColorModeValue("white", "#101010");
  const cardHover = useColorModeValue("gray.50", "#181818");
  const border = useColorModeValue("gray.200", "#1e1e1e");
  const textColor = useColorModeValue("gray.800", "gray.100");
  const mutedColor = useColorModeValue("gray.600", "gray.400");
  const iconColor = useColorModeValue("gray.700", "gray.300");

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
          color={textColor}
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
              boxShadow="sm"
              _hover={{
                bg: cardHover,
                borderColor: useColorModeValue("gray.300", "#3a3a3a"),
                transform: "translateY(-2px)",
                boxShadow: "md",
              }}
              onClick={() => navigate(card.path)}
            >
              <Icon
                as={card.icon}
                color={iconColor}
                boxSize={7}
                mb={4}
              />

              <VStack align="flex-start" spacing={1} flex="1">
                <Text fontSize="lg" fontWeight="semibold" color={textColor}>
                  {card.title}
                </Text>

                <Text fontSize="sm" color={mutedColor}>
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
