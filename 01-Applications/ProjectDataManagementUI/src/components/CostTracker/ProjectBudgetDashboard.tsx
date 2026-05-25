import { useState } from "react";
import {
  Box,
  VStack,
  HStack,
  Text,
  Button,
  Badge,
  Tabs,
  TabList,
  TabPanels,
  Tab,
  TabPanel,
  Divider,
  Skeleton,
  SkeletonText,
  Alert,
  AlertIcon,
  AlertTitle,
  AlertDescription,
  Tooltip,
  useDisclosure,
  useBreakpointValue,
} from "@chakra-ui/react";
import { Plus, RefreshCw } from "lucide-react";
import { formatCurrency } from "../../utils/formatters";
import ProjectSummaryHeader from "./ProjectSummaryHeader";
import EstimateCard from "./EstimateCard";
import ProjectAdditionalCostsSection from "./ProjectAdditionalCostsSection";
import AllCostsSection from "./AllCostsSection";
import CostFormModal from "./CostFormModal";
import BudgetSummarySection from "./BudgetSummarySection";
import { useProjectCostTracker } from "../../hooks/useProjectCostTracker";

interface ProjectBudgetDashboardProps {
  tenantId: string;
  projectId: string;
}

function DashboardSkeleton() {
  return (
    <VStack spacing={4} align="stretch">
      <Skeleton height="40px" />
      <HStack spacing={4}>
        {[1, 2, 3, 4].map((i) => (
          <Skeleton key={i} height="80px" flex={1} borderRadius="md" />
        ))}
      </HStack>
      {[1, 2].map((i) => (
        <Box key={i} borderWidth={1} borderRadius="lg" p={4}>
          <Skeleton height="20px" mb={3} />
          <Skeleton height="8px" mb={2} />
          <SkeletonText noOfLines={3} spacing={2} />
        </Box>
      ))}
    </VStack>
  );
}

export default function ProjectBudgetDashboard({
  tenantId,
  projectId,
}: ProjectBudgetDashboardProps) {
  const { data, isLoading, error, refetch } = useProjectCostTracker(tenantId, projectId);
  const { isOpen: isModalOpen, onOpen: onModalOpen, onClose: onModalClose } = useDisclosure();
  const isMobile = useBreakpointValue({ base: true, md: false });
  const [lastRefreshed, setLastRefreshed] = useState(() => new Date());

  const handleRefetch = () => {
    void refetch();
    setLastRefreshed(new Date());
  };

  if (isLoading) {
    return (
      <Box px={{ base: 3, md: 6 }} py={{ base: 4, md: 8 }}>
        <DashboardSkeleton />
      </Box>
    );
  }

  if (error) {
    return (
      <Box px={{ base: 3, md: 6 }} py={{ base: 4, md: 8 }}>
        <Alert status="error" borderRadius="md">
          <AlertIcon />
          <Box>
            <AlertTitle>Błąd ładowania danych</AlertTitle>
            <AlertDescription>{error}</AlertDescription>
          </Box>
          <Button size="sm" ml="auto" onClick={handleRefetch} leftIcon={<RefreshCw size={14} />}>
            Ponów
          </Button>
        </Alert>
      </Box>
    );
  }

  if (!data) return null;

  return (
    <Box px={{ base: 3, md: 6 }} py={{ base: 4, md: 8 }} position="relative">
      <VStack align="stretch" spacing={6}>
        {/* Nagłówek sekcji */}
        <HStack justify="space-between" flexWrap="wrap" gap={2}>
          <Text fontSize={{ base: "lg", md: "2xl" }} fontWeight="bold">
            Realizacja budżetu
          </Text>
          <HStack spacing={2}>
            <HStack spacing={1} align="center">
              {!isMobile && (
                <Text fontSize="xs" color="neutral.400">
                  {lastRefreshed.toLocaleTimeString('pl-PL', { hour: '2-digit', minute: '2-digit' })}
                </Text>
              )}
              <Button
                size="sm"
                variant="ghost"
                leftIcon={<RefreshCw size={14} />}
                onClick={handleRefetch}
                minH="44px"
              >
                {isMobile ? undefined : "Odśwież"}
              </Button>
            </HStack>
            {!isMobile && (
              <Button
                colorScheme="primary"
                size="sm"
                leftIcon={<Plus size={14} />}
                onClick={onModalOpen}
                minH="44px"
              >
                Dodaj koszt
              </Button>
            )}
          </HStack>
        </HStack>

        {/* Podsumowanie projektu */}
        <ProjectSummaryHeader
          summary={data.summary}
          estimates={data.costEstimateSummaries}
        />

        {/* Budżet trackera — koszty dodatkowe projektu */}
        <BudgetSummarySection
          trackerId={data.id}
          tenantId={tenantId}
          projectId={projectId}
          budgetSummary={data.budgetSummary}
          onMutated={refetch}
        />

        <Divider />

        {/* Tabs: Wszystkie koszty / Kosztorysy / Koszty dodatkowe projektu */}
        <Tabs variant="enclosed" isLazy>
          <TabList overflowX="auto">
            <Tab fontSize={{ base: "xs", md: "sm" }} minH="44px">
              Wszystkie koszty
              <Tooltip label={formatCurrency(data.summary.totalCostsNet)} hasArrow placement="top">
                <Badge ml={2} colorScheme="gray" borderRadius="full" fontSize="xs">
                  {data.summary.costCount}
                </Badge>
              </Tooltip>
            </Tab>
            <Tab fontSize={{ base: "xs", md: "sm" }} minH="44px">
              Kosztorysy
              <Tooltip label={formatCurrency(data.costEstimateSummaries.reduce((s, e) => s + (e.totalCostsNet ?? 0), 0))} hasArrow placement="top">
                <Badge ml={2} colorScheme="primary" borderRadius="full" fontSize="xs">
                  {data.costEstimateSummaries.length}
                </Badge>
              </Tooltip>
            </Tab>
            <Tab fontSize={{ base: "xs", md: "sm" }} minH="44px">
              Koszty dodatkowe
              <Tooltip label={formatCurrency(data.projectAdditionalCosts.totalNet)} hasArrow placement="top">
                <Badge ml={2} colorScheme="gray" borderRadius="full" fontSize="xs">
                  {data.projectAdditionalCosts.costsCount}
                </Badge>
              </Tooltip>
            </Tab>
          </TabList>

          <TabPanels>
            {/* Wszystkie koszty */}
            <TabPanel px={0}>
              <AllCostsSection
                data={data}
                tenantId={tenantId}
                projectId={projectId}
                onCostMutated={handleRefetch}
              />
            </TabPanel>

            {/* Kosztorysy */}
            <TabPanel px={0}>
              <VStack spacing={4} align="stretch">
                {data.costEstimateSummaries.length === 0 ? (
                  <VStack spacing={3} py={8}>
                    <Text color="neutral.500" fontSize="sm" textAlign="center">
                      Brak kosztorysów powiązanych z tym projektem.
                    </Text>
                    <Text color="neutral.400" fontSize="xs" textAlign="center">
                      Utwórz kosztorys w module Kosztorysy, aby śledzić koszty pozycji.
                    </Text>
                  </VStack>
                ) : (
                  data.costEstimateSummaries.map((est) => (
                    <EstimateCard
                      key={est.costEstimateId}
                      estimate={est}
                      tenantId={tenantId}
                      projectId={projectId}
                      onCostMutated={handleRefetch}
                      allEstimates={data.costEstimateSummaries}
                    />
                  ))
                )}
              </VStack>
            </TabPanel>

            {/* Koszty dodatkowe projektu */}
            <TabPanel px={0}>
              <ProjectAdditionalCostsSection
                projectAdditionalCosts={data.projectAdditionalCosts}
                tenantId={tenantId}
                projectId={projectId}
                onCostMutated={handleRefetch}
                estimates={data.costEstimateSummaries}
              />
            </TabPanel>
          </TabPanels>
        </Tabs>
      </VStack>

      {/* Floating button — mobile */}
      {isMobile && (
        <Tooltip label="Dodaj koszt" hasArrow placement="left">
          <Button
            position="fixed"
            bottom={4}
            right={4}
            colorScheme="primary"
            borderRadius="full"
            shadow="lg"
            onClick={onModalOpen}
            zIndex={10}
            minH="44px"
            minW="44px"
            aria-label="Dodaj koszt"
          >
            <Plus size={20} />
          </Button>
        </Tooltip>
      )}

      {/* Modal dodawania kosztu */}
      <CostFormModal
        isOpen={isModalOpen}
        onClose={onModalClose}
        onSuccess={() => { onModalClose(); handleRefetch(); }}
        tenantId={tenantId}
        projectId={projectId}
        costEstimateSummaries={data.costEstimateSummaries}
      />
    </Box>
  );
}
