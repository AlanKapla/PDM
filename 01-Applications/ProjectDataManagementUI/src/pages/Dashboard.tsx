import { Box, SimpleGrid, Card, CardBody, Heading, Text, Icon, VStack } from "@chakra-ui/react";
import { Building2, FolderKanban, Settings, Briefcase, FileText } from "lucide-react";
import { useNavigate } from "react-router-dom";
import MainLayout from "../layout/MainLayout";

export default function Dashboard() {
  const navigate = useNavigate();

  const menuCards = [
    {
      title: "Organizacje",
      description: "Zarządzaj swoimi organizacjami i współpracuj z innymi",
      icon: Building2,
      color: "blue.500",
      path: "/tenants",
    },
    {
      title: "Projekty",
      description: "Przeglądaj i zarządzaj swoimi projektami",
      icon: FolderKanban,
      color: "green.500",
      path: "/projects",
    },
    {
      title: "Zaplanowane prace",
      description: "Zobacz przydzielone do Ciebie zadania",
      icon: Briefcase,
      color: "orange.500",
      path: "/assigned-works",
    },
    {
      title: "Szablony kosztorysów",
      description: "Zarządzaj szablonami kosztorysów",
      icon: FileText,
      color: "teal.500",
      path: "/cost-estimate-templates",
    },
    {
      title: "Ustawienia",
      description: "Personalizuj swoje konto i preferencje",
      icon: Settings,
      color: "gray.500",
      path: "/profile",
    },
  ];

  return (
    <MainLayout>
      <Box p={{ base: 3, sm: 4, md: 10 }} minH="100vh">
        <Heading mb={{ base: 4, md: 8 }} size={{ base: "md", sm: "lg", md: "xl" }}>
          Panel główny
        </Heading>

        <SimpleGrid columns={{ base: 1, sm: 2, md: 3 }} spacing={{ base: 3, md: 6 }}>
          {menuCards.map((card) => (
            <Card
              key={card.title}
              cursor="pointer"
              transition="all 0.2s"
              _hover={{ 
                transform: "translateY(-4px)", 
                shadow: "xl",
                borderColor: card.color 
              }}
              onClick={() => navigate(card.path)}
              borderWidth="2px"
              borderColor="transparent"
            >
              <CardBody p={{ base: 3, md: 6 }}>
                <VStack align="flex-start" spacing={{ base: 3, md: 4 }}>
                  <Icon as={card.icon} boxSize={{ base: 8, md: 10 }} color={card.color} />
                  <VStack align="flex-start" spacing={1}>
                    <Heading size={{ base: "sm", md: "md" }}>{card.title}</Heading>
                    <Text color="gray.600" fontSize={{ base: "xs", md: "sm" }}>
                      {card.description}
                    </Text>
                  </VStack>
                </VStack>
              </CardBody>
            </Card>
          ))}
        </SimpleGrid>
      </Box>
    </MainLayout>
  );
}
