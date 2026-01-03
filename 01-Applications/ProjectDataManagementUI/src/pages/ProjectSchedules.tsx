import { useEffect, useState, useContext } from "react";
import { useParams, useNavigate } from "react-router-dom";
import {
  Box,
  Heading,
  VStack,
  HStack,
  Text,
  Icon,
  Button,
  useColorModeValue,
  useDisclosure,
} from "@chakra-ui/react";
import { ArrowLeft, Calendar, Clock, User } from "lucide-react";
import MainLayout from "../layout/MainLayout";
import CreateWorkScheduleModal from "../components/CreateWorkScheduleModal";
import { AuthContext } from "../context/AuthContext";
import { LoadingSpinner, EmptyState } from "../components/common";
import { useToastNotification } from "../hooks/useToastNotification";
import { formatDate } from "../utils/formatters";
import { projectApi } from "../api/projectApi";
import { useProjectPermissions } from "../hooks/useProjectPermissions";
import type { WorkScheduleSummaryWeb } from "../types/workSchedule.types";

export default function ProjectSchedules() {
  const { projectId } = useParams<{ projectId: string }>();
  const navigate = useNavigate();
  const { user } = useContext(AuthContext);
  const { showError } = useToastNotification();
  const { isOpen, onOpen, onClose } = useDisclosure();

  const [loading, setLoading] = useState(true);
  const [workSchedules, setWorkSchedules] = useState<WorkScheduleSummaryWeb[]>([]);
  const [project, setProject] = useState<any | null>(null);
  const [members, setMembers] = useState<any[]>([]);

  const cardBg = useColorModeValue("white", "gray.800");
  const borderColor = useColorModeValue("gray.200", "gray.700");
  const hoverBg = useColorModeValue("gray.50", "gray.700");

  const permissions = useProjectPermissions(projectId);

  useEffect(() => {
    fetchData();
  }, [projectId]);

  const fetchData = async () => {
    if (!user?.activeTenantId || !projectId) return;

    setLoading(true);
    try {
      const [projectRes, schedulesRes, membersRes] = await Promise.all([
        projectApi.getProjectDetails(user.activeTenantId, projectId),
        projectApi.getMyWorkSchedules(user.activeTenantId, projectId),
        projectApi.getProjectMembers(user.activeTenantId, projectId),
      ]);

      setProject(projectRes.data);
      setWorkSchedules(schedulesRes.data);
      setMembers(membersRes.data);
    } catch (error) {
      showError("Nie udało się pobrać danych");
    } finally {
      setLoading(false);
    }
  };

  if (loading) {
    return (
      <MainLayout>
        <Box p={{ base: 4, md: 10 }} minH="100vh">
          <LoadingSpinner message="Ładowanie harmonogramów..." />
        </Box>
      </MainLayout>
    );
  }

  return (
    <MainLayout>
      <Box p={{ base: 4, md: 10 }} minH="100vh">
        <HStack justify="space-between" mb={8} flexWrap="wrap" gap={4}>
          <HStack spacing={3}>
            <Icon as={Calendar} boxSize={8} color="purple.600" />
            <VStack align="flex-start" spacing={0}>
              <Heading size="lg">Harmonogramy prac</Heading>
              {project && <Text fontSize="sm" color="gray.600">{project.name}</Text>}
            </VStack>
          </HStack>
          {permissions.canWriteResources && (
            <Button
              leftIcon={<Calendar size={18} />}
              colorScheme="purple"
              onClick={onOpen}
            >
              Utwórz harmonogram
            </Button>
          )}
        </HStack>

        {!permissions.canWriteResources ? (
          <Box p={8} textAlign="center">
            <EmptyState
              icon={Calendar}
              title="Brak dostępu"
              description="Harmonogramy są dostępne tylko dla edytorów i administratorów projektu"
            />
          </Box>
        ) : (
        <>
        {workSchedules.length === 0 ? (
          <EmptyState
            icon={Calendar}
            title="Brak harmonogramów"
            description="Utwórz pierwszy harmonogram prac dla tego projektu"
          />
        ) : (
          <VStack spacing={4} align="stretch">
            {workSchedules.map((schedule) => (
              <Box
                key={schedule.id}
                bg={cardBg}
                p={6}
                borderWidth="1px"
                borderColor={borderColor}
                rounded="lg"
                _hover={{ bg: hoverBg, transform: "translateY(-2px)", shadow: "md" }}
                transition="all 0.2s"
                cursor="pointer"
                onClick={() => navigate(`/projects/${projectId}/schedules/${schedule.id}`)}
                shadow="sm"
              >
                <VStack align="flex-start" spacing={3}>
                  <Text fontWeight="bold" fontSize="xl">{schedule.name}</Text>
                  <HStack spacing={6} fontSize="sm" color="gray.600">
                    <HStack spacing={2}>
                      <Icon as={User} boxSize={4} />
                      <Text>{schedule.createdByUserName}</Text>
                    </HStack>
                    <HStack spacing={2}>
                      <Icon as={Clock} boxSize={4} />
                      <Text>{formatDate(schedule.createdAt)}</Text>
                    </HStack>
                  </HStack>
                </VStack>
              </Box>
            ))}
          </VStack>
        )}
        </>
        )}

        <CreateWorkScheduleModal
          isOpen={isOpen}
          onClose={onClose}
          projectId={projectId || ""}
          tenantId={user?.activeTenantId || ""}
          projectName={project?.name || ""}
          members={members}
          onScheduleCreated={fetchData}
        />
      </Box>
    </MainLayout>
  );
}
