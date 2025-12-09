import { Box, SimpleGrid, Card, CardBody, Heading, Text, Icon, VStack } from "@chakra-ui/react";
import { Building2, FolderKanban, Settings } from "lucide-react";
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
      path: "/tenants/collaborating",
      subItems: [
        { label: "Aktywne zaproszenia", path: "/tenants/invitations" },
        { label: "Z którymi współpracujesz", path: "/tenants/collaborating" },
        { label: "Którymi zarządzasz", path: "/tenants/managed" },
      ]
    },
    {
      title: "Projekty",
      description: "Przeglądaj i zarządzaj swoimi projektami",
      icon: FolderKanban,
      color: "green.500",
      path: "/projects",
    },
    {
      title: "Ustawienia",
      description: "Personalizuj swoje konto i preferencje",
      icon: Settings,
      color: "gray.500",
      path: "/profile",
      subItems: [
        { label: "Profil", path: "/profile" },
      ]
    },
  ];

  return (
    <MainLayout>
      <Box p={{ base: 4, md: 10 }} minH="100vh">
        <Heading mb={8} size={{ base: "lg", md: "xl" }}>
          Panel główny
        </Heading>

        <SimpleGrid columns={{ base: 1, md: 2, lg: 3 }} spacing={6}>
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
              <CardBody>
                <VStack align="flex-start" spacing={4}>
                  <Icon as={card.icon} boxSize={10} color={card.color} />
                  <VStack align="flex-start" spacing={2}>
                    <Heading size="md">{card.title}</Heading>
                    <Text color="gray.600" fontSize="sm">
                      {card.description}
                    </Text>
                  </VStack>
                  
                  {card.subItems && (
                    <VStack align="flex-start" spacing={1} mt={2} w="100%">
                      {card.subItems.map((item) => (
                        <Text
                          key={item.path}
                          fontSize="xs"
                          color="gray.500"
                          _hover={{ color: card.color }}
                          onClick={(e) => {
                            e.stopPropagation();
                            navigate(item.path);
                          }}
                        >
                          • {item.label}
                        </Text>
                      ))}
                    </VStack>
                  )}
                </VStack>
              </CardBody>
            </Card>
          ))}
        </SimpleGrid>
      </Box>
    </MainLayout>
  );
}
