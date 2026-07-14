import React, { useState } from 'react';
import { Button, HStack } from '@chakra-ui/react';
import { Plus, Sparkles } from 'lucide-react';
import { AICostImportModal } from '../../../components/CostTracker/AICostImportModal';
import { AICostPendingBadge } from '../../../components/AICostReview/AICostPendingBadge';
import type { ParsedCostDto } from '../../../types/ai.types';
import { CostModal } from './CostModal';
import type { TrackedCostWeb } from '../types/projectDashboard.types';

export interface DashboardAddCostToolbarProps {
  tenantId: string;
  projectId: string;
  onRefetch: () => void;
}

/**
 * Globalny pasek akcji — dodawanie kosztu dostępne z każdej zakładki dashboardu.
 */
export function DashboardAddCostToolbar({
  tenantId,
  projectId,
  onRefetch,
}: DashboardAddCostToolbarProps): React.ReactElement {
  const [createModalOpen, setCreateModalOpen] = useState(false);
  const [aiImportOpen, setAiImportOpen] = useState(false);
  const [aiPrefillData, setAiPrefillData] = useState<{ parsedData: ParsedCostDto; file: File } | null>(null);

  const handleAiSuccess = (parsedData: ParsedCostDto, file: File): void => {
    setAiPrefillData({ parsedData, file });
    setAiImportOpen(false);
    setCreateModalOpen(true);
  };

  const handleCostCreated = (_cost: TrackedCostWeb): void => {
    setCreateModalOpen(false);
    setAiPrefillData(null);
    onRefetch();
  };

  return (
    <>
      <HStack spacing={2} flexWrap="wrap">
        <AICostPendingBadge tenantId={tenantId} projectId={projectId} context="dashboard" />
        <Button
          size="sm"
          leftIcon={<Sparkles size={18} aria-hidden="true" />}
          colorScheme="purple"
          variant="outline"
          onClick={() => setAiImportOpen(true)}
        >
          Importuj z AI
        </Button>
        <Button
          size="sm"
          leftIcon={<Plus size={18} aria-hidden="true" />}
          colorScheme="primary"
          onClick={() => {
            setAiPrefillData(null);
            setCreateModalOpen(true);
          }}
        >
          Dodaj koszt
        </Button>
      </HStack>

      {aiImportOpen && (
        <AICostImportModal
          isOpen
          onClose={() => setAiImportOpen(false)}
          tenantId={tenantId}
          projectId={projectId}
          costType="TrackedCost"
          onParsed={handleAiSuccess}
        />
      )}

      {createModalOpen && (
        <CostModal
          type="tracked"
          mode="create"
          tenantId={tenantId}
          projectId={projectId}
          onClose={() => {
            setCreateModalOpen(false);
            setAiPrefillData(null);
          }}
          onSuccess={handleCostCreated}
          aiPrefill={aiPrefillData ?? undefined}
        />
      )}
    </>
  );
}

export default DashboardAddCostToolbar;
