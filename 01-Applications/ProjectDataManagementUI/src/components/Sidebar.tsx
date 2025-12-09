import {
  Box,
  VStack,
  Button,
  useColorModeValue,
  Drawer,
  DrawerBody,
  DrawerHeader,
  DrawerOverlay,
  DrawerContent,
  DrawerCloseButton,
  useDisclosure,
  IconButton,
  Collapse,
  Badge,
} from "@chakra-ui/react";

import {
  Menu as MenuIcon,
  Building2,
  ChevronDown,
  ChevronUp,
  FolderKanban,
} from "lucide-react";

import { useNavigate, useLocation } from "react-router-dom";
import { useState, useEffect } from "react";
import { getActiveInvitations } from "../services/tenantService";
import { InvitationStatus } from "../types/auth.types";
import { tenantApi } from "../api/tenantApi";

export default function Sidebar() {
  const navigate = useNavigate();
  const location = useLocation();
  const { isOpen, onOpen, onClose } = useDisclosure();

  const [invitationsCount, setInvitationsCount] = useState(0);
  const [projects, setProjects] = useState<any[]>([]);
  const [activeTenantId, setActiveTenantId] = useState<string | null>(null);

  // Przywróć stan z localStorage lub ustaw na false
  const [tenantsExpanded, setTenantsExpanded] = useState(() => {
    const saved = localStorage.getItem("sidebar_tenants_expanded");
    return saved === "true";
  });

  const [projectsExpanded, setProjectsExpanded] = useState(() => {
    const saved = localStorage.getItem("sidebar_projects_expanded");
    return saved === "true";
  });

  // Pobierz liczbę aktywnych zaproszeń
  useEffect(() => {
    const fetchInvitations = async () => {
      try {
        const invitations = await getActiveInvitations();
        const pending = invitations.filter((inv: { status: number }) => inv.status === InvitationStatus.Pending);
        setInvitationsCount(pending.length);
      } catch (error) {
        console.error("Błąd pobierania zaproszeń:", error);
      }
    };

    fetchInvitations();
    // Odświeżaj co 30 sekund
    const interval = setInterval(fetchInvitations, 30000);
    return () => clearInterval(interval);
  }, []);

  // Pobierz projekty aktywnego tenanta
  useEffect(() => {
    const fetchProjects = async () => {
      try {
        const activeTenantResponse = await tenantApi.getActiveTenant();
        if (activeTenantResponse.ok) {
          const activeTenantData = await activeTenantResponse.json();
          
          if (activeTenantData.activeTenantId && activeTenantData.activeTenantId !== "00000000-0000-0000-0000-000000000000") {
            setActiveTenantId(activeTenantData.activeTenantId);
            
            const projectsResponse = await tenantApi.getTenantProjects(activeTenantData.activeTenantId);
            if (projectsResponse.ok) {
              const projectsData = await projectsResponse.json();
              setProjects(projectsData);
            }
          }
        }
      } catch (error) {
        console.error("Błąd pobierania projektów w sidebar:", error);
      }
    };

    fetchProjects();
  }, []);

  // Automatycznie rozwiń sekcję jeśli użytkownik jest na danej ścieżce
  useEffect(() => {
    if (location.pathname.startsWith("/tenants") && !tenantsExpanded) {
      setTenantsExpanded(true);
    }
    if (location.pathname.startsWith("/projects") && !projectsExpanded) {
      setProjectsExpanded(true);
    }
  }, [location.pathname]);

  // Zapisz stan do localStorage przy każdej zmianie
  useEffect(() => {
    localStorage.setItem("sidebar_tenants_expanded", String(tenantsExpanded));
  }, [tenantsExpanded]);

  useEffect(() => {
    localStorage.setItem("sidebar_projects_expanded", String(projectsExpanded));
  }, [projectsExpanded]);

  const bg = useColorModeValue("white", "gray.900");
  const border = useColorModeValue("gray.200", "gray.700");
  const activeBg = useColorModeValue("blue.100", "blue.700");
  const hoverBg = useColorModeValue("gray.200", "gray.600");

  const SidebarContent = () => (
    <VStack align="flex-start" spacing={6} h="100%" overflow="auto">
        <VStack align="stretch" w="100%" spacing={2}>
          {/* Organizacje na samej górze */}
          <Button
            variant="ghost"
            justifyContent="space-between"
            leftIcon={<Building2 size={20} />}
            rightIcon={tenantsExpanded ? <ChevronUp size={16} /> : <ChevronDown size={16} />}
            w="100%"
            bg={location.pathname.startsWith("/tenants") ? activeBg : "transparent"}
            _hover={{ bg: hoverBg }}
            onClick={() => setTenantsExpanded(!tenantsExpanded)}
          >
            Organizacje
          </Button>

          {/* Panel rozwijany organizacji */}
          <Collapse in={tenantsExpanded} animateOpacity>
            <VStack align="stretch" w="100%" spacing={2} pl={4} pt={2}>
              <Button
                variant="ghost"
                size="sm"
                justifyContent="space-between"
                w="100%"
                fontSize="sm"
                bg={location.pathname === "/tenants/invitations" ? activeBg : "transparent"}
                _hover={{ bg: hoverBg }}
                onClick={() => navigate("/tenants/invitations")}
              >
                Aktywne zaproszenia
                {invitationsCount > 0 && (
                  <Badge colorScheme="red" borderRadius="full" fontSize="xs" ml={2}>
                    {invitationsCount}
                  </Badge>
                )}
              </Button>

              <Button
                variant="ghost"
                size="sm"
                justifyContent="flex-start"
                w="100%"
                fontSize="sm"
                bg={location.pathname === "/tenants/collaborating" ? activeBg : "transparent"}
                _hover={{ bg: hoverBg }}
                onClick={() => navigate("/tenants/collaborating")}
              >
                Z którymi współpracujesz
              </Button>

              <Button
                variant="ghost"
                size="sm"
                justifyContent="flex-start"
                w="100%"
                fontSize="sm"
                bg={location.pathname === "/tenants/managed" ? activeBg : "transparent"}
                _hover={{ bg: hoverBg }}
                onClick={() => navigate("/tenants/managed")}
              >
                Którymi zarządzasz
              </Button>
            </VStack>
          </Collapse>

          {/* Projekty */}
          <Button
            variant="ghost"
            justifyContent="space-between"
            leftIcon={<FolderKanban size={20} />}
            rightIcon={projectsExpanded ? <ChevronUp size={16} /> : <ChevronDown size={16} />}
            w="100%"
            bg={location.pathname === "/projects" ? activeBg : "transparent"}
            _hover={{ bg: hoverBg }}
            onClick={() => setProjectsExpanded(!projectsExpanded)}
          >
            Projekty
          </Button>

          {/* Panel rozwijany projektów */}
          <Collapse in={projectsExpanded} animateOpacity>
            <VStack align="stretch" w="100%" spacing={2} pl={4} pt={2}>
              {projects.length > 0 ? (
                projects.map((project) => (
                  <Button
                    key={project.id}
                    variant="ghost"
                    size="sm"
                    justifyContent="flex-start"
                    w="100%"
                    fontSize="sm"
                    bg={location.pathname === `/projects/${project.id}` ? activeBg : "transparent"}
                    _hover={{ bg: hoverBg }}
                    onClick={() => navigate(`/projects/${project.id}`)}
                    title={project.name}
                    overflow="hidden"
                    textOverflow="ellipsis"
                    whiteSpace="nowrap"
                  >
                    {project.name}
                  </Button>
                ))
              ) : (
                <Box pl={2} py={1} fontSize="xs" color="gray.500">
                  Brak projektów
                </Box>
              )}
            </VStack>
          </Collapse>
        </VStack>
      </VStack>
  );

  return (
    <>
      {/* Mobile Menu Button */}
      <IconButton
        aria-label="Open menu"
        icon={<MenuIcon size={24} />}
        onClick={onOpen}
        position="fixed"
        top={3}
        left={4}
        zIndex={20}
        display={{ base: "flex", md: "none" }}
        colorScheme="blue"
        size="sm"
      />

      {/* Mobile Drawer */}
      <Drawer isOpen={isOpen} placement="left" onClose={onClose}>
        <DrawerOverlay />
        <DrawerContent bg={bg}>
          <DrawerCloseButton />
          <DrawerHeader>Menu</DrawerHeader>
          <DrawerBody>
            <SidebarContent />
          </DrawerBody>
        </DrawerContent>
      </Drawer>

      {/* Desktop Sidebar */}
      <Box
        position="fixed"
        left="0"
        top="60px"
        w="250px"
        h="calc(100vh - 60px)"
        bg={bg}
        borderRight="1px solid"
        borderColor={border}
        p={5}
        display={{ base: "none", md: "block" }}
      >
        <SidebarContent />
      </Box>
    </>
  );
}
