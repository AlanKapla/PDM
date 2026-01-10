import { useEffect, useState, useRef, memo } from "react";
import { useParams, useNavigate } from "react-router-dom";
import {
  Box,
  Heading,
  VStack,
  HStack,
  Text,
  Button,
  useColorModeValue,
  Tabs,
  TabList,
  TabPanels,
  Tab,
  TabPanel,
  Table,
  Thead,
  Tbody,
  Tr,
  Th,
  Td,
  IconButton,
  Input,
  Textarea,
  useDisclosure,
  Badge,
  Icon,
  Checkbox,
} from "@chakra-ui/react";
import { ArrowLeft, Plus, Share2, Edit2, Trash2, DollarSign, FileUp, X, Eye, Download } from "lucide-react";
import MainLayout from "../layout/MainLayout";
import { projectApi, ResourceScope } from "../api/projectApi";
import { AuthContext } from "../context/AuthContext";
import { useContext } from "react";
import { LoadingSpinner, EmptyState } from "../components/common";
import { useToastNotification } from "../hooks/useToastNotification";
import { formatDate, formatCurrency } from "../utils/formatters";
import ShareCostModal from "../components/ShareCostModal";
import { ManageCostShareModal } from "../components/ManageCostShareModal";
import ShareCostsModal from "../components/ShareCostsModal";
import type { ProjectCostListItemWeb } from "../types/project.types";
import { useResourcePermissions } from "../hooks/useResourcePermissions";
import { useTabCache } from "../hooks/useTabCache";
import { useGlobalCache } from "../hooks/useGlobalCache";

// === TAB COMPONENTS (poza głównym komponentem aby uniknąć re-tworzenia) ===

interface AllCostsTabProps {
  costs: ProjectCostListItemWeb[];
  loading: boolean;
  showNewCostRow: boolean;
  newCostData: any;
  documentFile: File | null;
  addingNewCost: boolean;
  resourcePerms: any;
  editingCostId: string | null;
  editingCostData: any;
  editDocumentFile: File | null;
  deletingCostId: string | null;
  editingClosedCostId: string | null;
  savingClosedCost: boolean;
  onShareCostsModalOpen: () => void;
  onShowNewCostRow: (show: boolean) => void;
  onNewCostDataChange: (data: any) => void;
  onDocumentFileChange: (file: File | null) => void;
  onAddCost: () => void;
  onManageShare: (cost: ProjectCostListItemWeb) => void;
  onEditCost: (cost: ProjectCostListItemWeb) => void;
  onEditingCostDataChange: (data: any) => void;
  onEditDocumentFileChange: (file: File | null) => void;
  onSaveEdit: () => void;
  onCancelEdit: () => void;
  onDeleteCost: (id: string) => void;
  onToggleCostClosed: (costId: string, currentIsClosed: boolean) => void;
}

const AllCostsTab = memo(function AllCostsTab({
  costs,
  loading,
  showNewCostRow,
  newCostData,
  documentFile,
  addingNewCost,
  resourcePerms,
  editingCostId,
  editingCostData,
  editDocumentFile,
  deletingCostId,
  editingClosedCostId,
  savingClosedCost,
  onShareCostsModalOpen,
  onShowNewCostRow,
  onNewCostDataChange,
  onDocumentFileChange,
  onAddCost,
  onManageShare,
  onEditCost,
  onEditingCostDataChange,
  onEditDocumentFileChange,
  onSaveEdit,
  onCancelEdit,
  onDeleteCost,
  onToggleCostClosed,
}: AllCostsTabProps) {
  const bgColor = useColorModeValue("white", "gray.800");
  const borderColor = useColorModeValue("gray.200", "gray.600");
  const newRowBg = useColorModeValue("blue.50", "blue.900");
  const editRowBg = useColorModeValue("yellow.50", "yellow.900");
  const hoverBg = useColorModeValue("gray.50", "gray.700");
  const canEditClosedCost = resourcePerms.mine.canEdit || resourcePerms.all.canEdit || resourcePerms.shared.canEdit;

  if (loading) {
    return <LoadingSpinner />;
  }

  return (
    <VStack spacing={4} align="stretch">
      <HStack justify="space-between">
        <Text fontSize="sm" color="gray.600">
          Wszystkie koszty w projekcie (admin)
        </Text>
        <HStack spacing={2}>
          {resourcePerms.all.canShare && (
            <Button
              leftIcon={<Share2 size={18} />}
              colorScheme="orange"
              size="sm"
              onClick={onShareCostsModalOpen}
            >
              Udostępnij grupowo
            </Button>
          )}
          {resourcePerms.all.canCreate && (
            <Button
              leftIcon={<Plus size={18} />}
              colorScheme="green"
              onClick={() => onShowNewCostRow(true)}
            >
              Dodaj koszt
            </Button>
          )}
        </HStack>
      </HStack>

      {costs.length === 0 && !showNewCostRow ? (
        <EmptyState
          icon={DollarSign}
          title="Brak kosztów"
          description="Nie ma jeszcze żadnych kosztów w tym projekcie"
        />
      ) : (
        <Box overflowX="auto" bg={bgColor} p={4} rounded="lg" borderWidth="1px" borderColor={borderColor}>
          <Table size="sm" variant="simple">
            <Thead>
              <Tr>
                <Th>Nazwa</Th>
                <Th>Właściciel</Th>
                <Th>Miejsce</Th>
                <Th>Data</Th>
                <Th>Opis</Th>
                <Th isNumeric>Netto</Th>
                <Th isNumeric>VAT %</Th>
                <Th isNumeric>Brutto</Th>
                <Th textAlign="center">Rozliczone</Th>
                <Th textAlign="center">Dokument</Th>
                {(resourcePerms.all.canEdit || resourcePerms.all.canDelete || resourcePerms.all.canManageShare) && <Th textAlign="center">Akcje</Th>}
              </Tr>
            </Thead>
            <Tbody>
              {showNewCostRow && (
                <Tr bg={newRowBg}>
                  <Td><Input size="sm" value={newCostData.name} onChange={(e) => onNewCostDataChange({ ...newCostData, name: e.target.value })} placeholder="Nazwa" /></Td>
                  <Td>
                    <Text fontSize="xs" color="gray.500">Ty</Text>
                  </Td>
                  <Td><Input size="sm" value={newCostData.place} onChange={(e) => onNewCostDataChange({ ...newCostData, place: e.target.value })} placeholder="Miejsce" /></Td>
                  <Td><Input size="sm" type="date" value={newCostData.date} onChange={(e) => onNewCostDataChange({ ...newCostData, date: e.target.value })} /></Td>
                  <Td><Textarea size="sm" value={newCostData.description} onChange={(e) => onNewCostDataChange({ ...newCostData, description: e.target.value })} placeholder="Opis" rows={2} /></Td>
                  <Td>
                    <Input 
                      size="sm" 
                      type="number" 
                      step="0.01" 
                      value={newCostData.netAmount} 
                      onChange={(e) => {
                        const updates: any = { netAmount: e.target.value };
                        if (!newCostData.vatRate) updates.vatRate = '0';
                        onNewCostDataChange({ ...newCostData, ...updates });
                      }} 
                      placeholder="0.00"
                    />
                  </Td>
                  <Td>
                    <Input 
                      size="sm" 
                      type="number" 
                      step="0.01" 
                      min="0"
                      max="100"
                      value={newCostData.vatRate} 
                      onChange={(e) => onNewCostDataChange({ ...newCostData, vatRate: e.target.value })} 
                      placeholder="0"
                    />
                  </Td>
                  <Td>
                    <Input 
                      size="sm" 
                      type="number" 
                      step="0.01" 
                      value={newCostData.grossAmount} 
                      onChange={(e) => onNewCostDataChange({ ...newCostData, grossAmount: e.target.value, netAmount: '', vatRate: '' })} 
                      placeholder="0.00"
                    />
                  </Td>
                  <Td textAlign="center">
                    {canEditClosedCost ? (
                      <Checkbox
                        isChecked={newCostData.isClosed}
                        onChange={(e) => onNewCostDataChange({ ...newCostData, isClosed: e.target.checked })}
                        colorScheme="green"
                      />
                    ) : (
                      <Badge colorScheme={newCostData.isClosed ? "green" : "gray"} fontSize="xs">
                        {newCostData.isClosed ? "Tak" : "Nie"}
                      </Badge>
                    )}
                  </Td>
                  <Td textAlign="center">
                    <VStack spacing={1}>
                      <Input
                        size="sm"
                        type="file"
                        accept=".pdf,.jpg,.jpeg,.png"
                        onChange={(e) => onDocumentFileChange(e.target.files?.[0] || null)}
                        display="none"
                        id="new-cost-file-all"
                      />
                      <Button
                        as="label"
                        htmlFor="new-cost-file-all"
                        size="xs"
                        leftIcon={<FileUp size={14} />}
                        variant="outline"
                        cursor="pointer"
                      >
                        {documentFile ? documentFile.name.substring(0, 15) : "Dodaj plik"}
                      </Button>
                      {documentFile && (
                        <IconButton
                          aria-label="Usuń plik"
                          icon={<X size={12} />}
                          size="xs"
                          variant="ghost"
                          colorScheme="red"
                          onClick={() => onDocumentFileChange(null)}
                        />
                      )}
                    </VStack>
                  </Td>
                  {(resourcePerms.all.canEdit || resourcePerms.all.canDelete || resourcePerms.all.canManageShare) && (
                    <Td textAlign="center">
                      <HStack spacing={1} justify="center">
                        <Button size="sm" colorScheme="green" onClick={onAddCost} isLoading={addingNewCost}>Zapisz</Button>
                        <Button size="sm" variant="ghost" onClick={() => { onShowNewCostRow(false); onDocumentFileChange(null); }}>Anuluj</Button>
                      </HStack>
                    </Td>
                  )}
                </Tr>
              )}
              {costs.map((cost) => editingCostId === cost.id ? (
                <Tr key={cost.id} bg={editRowBg}>
                  <Td><Input size="sm" value={editingCostData.name} onChange={(e) => onEditingCostDataChange({ ...editingCostData, name: e.target.value })} /></Td>
                  <Td colSpan={1}><Text fontSize="sm" color="gray.600">{cost.userName || "-"}</Text></Td>
                  <Td><Input size="sm" value={editingCostData.place} onChange={(e) => onEditingCostDataChange({ ...editingCostData, place: e.target.value })} /></Td>
                  <Td><Input size="sm" type="date" value={editingCostData.date} onChange={(e) => onEditingCostDataChange({ ...editingCostData, date: e.target.value })} /></Td>
                  <Td><Textarea size="sm" value={editingCostData.description} onChange={(e) => onEditingCostDataChange({ ...editingCostData, description: e.target.value })} rows={2} /></Td>
                  <Td>
                    <Input 
                      size="sm" 
                      type="number" 
                      step="0.01" 
                      value={editingCostData.netAmount} 
                      onChange={(e) => {
                        const updates: any = { netAmount: e.target.value };
                        if (!editingCostData.vatRate) updates.vatRate = '0';
                        onEditingCostDataChange({ ...editingCostData, ...updates });
                      }} 
                      placeholder="0.00"
                    />
                  </Td>
                  <Td>
                    <Input 
                      size="sm" 
                      type="number" 
                      step="0.01" 
                      min="0"
                      max="100"
                      value={editingCostData.vatRate} 
                      onChange={(e) => onEditingCostDataChange({ ...editingCostData, vatRate: e.target.value })} 
                      placeholder="0"
                    />
                  </Td>
                  <Td>
                    <Input 
                      size="sm" 
                      type="number" 
                      step="0.01" 
                      value={editingCostData.grossAmount} 
                      onChange={(e) => onEditingCostDataChange({ ...editingCostData, grossAmount: e.target.value, netAmount: '', vatRate: '' })} 
                      placeholder="0.00"
                    />
                  </Td>
                  <Td textAlign="center">
                    <Checkbox
                      isChecked={editingCostData.isClosed}
                      onChange={(e) => onEditingCostDataChange({ ...editingCostData, isClosed: e.target.checked })}
                      colorScheme="green"
                    />
                  </Td>
                  <Td textAlign="center">
                    <VStack spacing={1}>
                      <Input
                        size="sm"
                        type="file"
                        accept=".pdf,.jpg,.jpeg,.png"
                        onChange={(e) => onEditDocumentFileChange(e.target.files?.[0] || null)}
                        display="none"
                        id={`edit-cost-file-${cost.id}`}
                      />
                      <Button
                        as="label"
                        htmlFor={`edit-cost-file-${cost.id}`}
                        size="xs"
                        leftIcon={<FileUp size={14} />}
                        variant="outline"
                        cursor="pointer"
                      >
                        {editDocumentFile ? editDocumentFile.name.substring(0, 15) : (cost.hasDocument ? `${cost.documentFileName?.substring(0, 15)}` : "Dodaj plik")}
                      </Button>
                      {(editDocumentFile || cost.hasDocument) && (
                        <IconButton
                          aria-label="Usuń plik"
                          icon={<X size={12} />}
                          size="xs"
                          variant="ghost"
                          colorScheme="red"
                          onClick={() => {
                            onEditDocumentFileChange(null);
                            onEditingCostDataChange({ ...editingCostData, removeDocument: true });
                          }}
                        />
                      )}
                    </VStack>
                  </Td>
                  {(resourcePerms.all.canEdit || resourcePerms.all.canDelete || resourcePerms.all.canManageShare) && (
                    <Td textAlign="center">
                      <HStack spacing={1} justify="center">
                        <Button size="sm" colorScheme="green" onClick={onSaveEdit}>Zapisz</Button>
                        <Button size="sm" variant="ghost" onClick={onCancelEdit}>Anuluj</Button>
                      </HStack>
                    </Td>
                  )}
                </Tr>
              ) : (
                <Tr key={cost.id} _hover={{ bg: hoverBg }}>
                  <Td fontWeight="medium">{cost.name}</Td>
                  <Td fontSize="sm" color="gray.600">{cost.userName || "-"}</Td>
                  <Td>{cost.place || "-"}</Td>
                  <Td>{formatDate(cost.date)}</Td>
                  <Td>{cost.description || "-"}</Td>
                  <Td isNumeric>{formatCurrency(cost.netAmount ?? 0)}</Td>
                  <Td isNumeric>{cost.vatRate ?? 0}%</Td>
                  <Td isNumeric fontWeight="bold" color="green.600">{formatCurrency(cost.grossAmount)}</Td>
                  <Td textAlign="center">
                    {canEditClosedCost ? (
                      <Checkbox
                        isChecked={cost.isClosed}
                        onChange={() => onToggleCostClosed(cost.id, cost.isClosed)}
                        colorScheme="green"
                        isDisabled={editingClosedCostId === cost.id && savingClosedCost}
                      />
                    ) : (
                      <Badge colorScheme={cost.isClosed ? "green" : "gray"} fontSize="xs">
                        {cost.isClosed ? "Tak" : "Nie"}
                      </Badge>
                    )}
                  </Td>
                  <Td textAlign="center">
                    {cost.hasDocument && cost.previewSasUrl && cost.downloadSasUrl ? (
                      <HStack spacing={1} justify="center">
                        <IconButton
                          aria-label="Podgląd"
                          icon={<Eye size={14} />}
                          size="xs"
                          variant="ghost"
                          colorScheme="purple"
                          onClick={() => window.open(cost.previewSasUrl, '_blank')}
                          title={`Podgląd: ${cost.documentFileName}`}
                        />
                        <IconButton
                          aria-label="Pobierz"
                          icon={<Download size={14} />}
                          size="xs"
                          variant="ghost"
                          colorScheme="green"
                          onClick={() => window.open(cost.downloadSasUrl, '_blank')}
                          title={`Pobierz: ${cost.documentFileName}`}
                        />
                      </HStack>
                    ) : (
                      <Badge colorScheme="gray" fontSize="xs">Brak</Badge>
                    )}
                  </Td>
                  {(resourcePerms.all.canEdit || resourcePerms.all.canDelete || resourcePerms.all.canManageShare) && (
                    <Td textAlign="center">
                      <HStack spacing={1} justify="center">
                        {resourcePerms.all.canEdit && (
                          <IconButton
                            aria-label="Edytuj"
                            icon={<Edit2 size={14} />}
                            size="xs"
                            variant="ghost"
                            colorScheme="blue"
                            onClick={() => onEditCost(cost)}
                          />
                        )}
                        {resourcePerms.all.canDelete && (
                          <IconButton
                            aria-label="Usuń"
                            icon={<Trash2 size={14} />}
                            size="xs"
                            variant="ghost"
                            colorScheme="red"
                            onClick={() => onDeleteCost(cost.id)}
                            isLoading={deletingCostId === cost.id}
                          />
                        )}
                        {resourcePerms.all.canManageShare && (
                          <IconButton
                            aria-label="Zarządzaj udostępnieniem"
                            icon={<Share2 size={14} />}
                            size="xs"
                            variant="ghost"
                            colorScheme="orange"
                            onClick={() => onManageShare(cost)}
                          />
                        )}
                      </HStack>
                    </Td>
                  )}
                </Tr>
              ))}
            </Tbody>
          </Table>
        </Box>
      )}
    </VStack>
  );
});

interface MyCostsTabProps {
  costs: ProjectCostListItemWeb[];
  loading: boolean;
  showNewCostRow: boolean;
  newCostData: any;
  documentFile: File | null;
  addingNewCost: boolean;
  editingCostId: string | null;
  editingCostData: any;
  editDocumentFile: File | null;
  savingCost: boolean;
  deletingCostId: string | null;
  editingClosedCostId: string | null;
  savingClosedCost: boolean;
  resourcePerms: any;
  onShareCostsModalOpen: () => void;
  onShowNewCostRow: (show: boolean) => void;
  onNewCostDataChange: (data: any) => void;
  onDocumentFileChange: (file: File | null) => void;
  onAddCost: () => void;
  onEditCost: (cost: ProjectCostListItemWeb) => void;
  onEditingCostDataChange: (data: any) => void;
  onEditDocumentFileChange: (file: File | null) => void;
  onSaveEdit: () => void;
  onCancelEdit: () => void;
  onShareCost: (cost: ProjectCostListItemWeb) => void;
  onDeleteCost: (costId: string) => void;
  onToggleCostClosed: (costId: string, currentIsClosed: boolean) => void;
}

const MyCostsTab = memo(function MyCostsTab({
  costs,
  loading,
  showNewCostRow,
  newCostData,
  documentFile,
  addingNewCost,
  editingCostId,
  editingCostData,
  editDocumentFile,
  savingCost,
  deletingCostId,
  editingClosedCostId,
  savingClosedCost,
  resourcePerms,
  onShareCostsModalOpen,
  onShowNewCostRow,
  onNewCostDataChange,
  onDocumentFileChange,
  onAddCost,
  onEditCost,
  onEditingCostDataChange,
  onEditDocumentFileChange,
  onSaveEdit,
  onCancelEdit,
  onShareCost,
  onDeleteCost,
  onToggleCostClosed,
}: MyCostsTabProps) {
  const bgColor = useColorModeValue("white", "gray.800");
  const borderColor = useColorModeValue("gray.200", "gray.600");
  const newRowBg = useColorModeValue("blue.50", "blue.900");
  const editRowBg = useColorModeValue("yellow.50", "yellow.900");
  const hoverBg = useColorModeValue("gray.50", "gray.700");
  const canEditClosedCost = resourcePerms.mine.canEdit || resourcePerms.all.canEdit || resourcePerms.shared.canEdit;

  if (loading) {
    return <LoadingSpinner />;
  }

  return (
    <VStack spacing={4} align="stretch">
      <HStack justify="space-between">
        <Text fontSize="sm" color="gray.600">
          Twoje koszty w projekcie
        </Text>
        <HStack spacing={2}>
          {resourcePerms.mine.canShare && (
            <Button
              leftIcon={<Share2 size={18} />}
              colorScheme="orange"
              size="sm"
              onClick={onShareCostsModalOpen}
            >
              Udostępnij grupowo
            </Button>
          )}
          {resourcePerms.mine.canCreate && (
            <Button
              leftIcon={<Plus size={18} />}
              colorScheme="green"
              onClick={() => onShowNewCostRow(true)}
            >
              Dodaj koszt
            </Button>
          )}
        </HStack>
      </HStack>

      {costs.length === 0 && !showNewCostRow ? (
        <EmptyState
          icon={DollarSign}
          title="Brak kosztów"
          description="Dodaj pierwszy koszt do projektu"
        />
      ) : (
        <Box overflowX="auto" bg={bgColor} p={4} rounded="lg" borderWidth="1px" borderColor={borderColor}>
          <Table size="sm" variant="simple">
            <Thead>
              <Tr>
                <Th>Nazwa</Th>
                <Th>Miejsce</Th>
                <Th>Data</Th>
                <Th>Opis</Th>
                <Th isNumeric>Netto</Th>
                <Th isNumeric>VAT %</Th>
                <Th isNumeric>Brutto</Th>
                <Th textAlign="center">Rozliczone</Th>
                <Th textAlign="center">Dokument</Th>
                <Th textAlign="center">Akcje</Th>
              </Tr>
            </Thead>
            <Tbody>
              {showNewCostRow && (
                <Tr bg={newRowBg}>
                  <Td><Input size="sm" value={newCostData.name} onChange={(e) => onNewCostDataChange({ ...newCostData, name: e.target.value })} placeholder="Nazwa" /></Td>
                  <Td><Input size="sm" value={newCostData.place} onChange={(e) => onNewCostDataChange({ ...newCostData, place: e.target.value })} placeholder="Miejsce" /></Td>
                  <Td><Input size="sm" type="date" value={newCostData.date} onChange={(e) => onNewCostDataChange({ ...newCostData, date: e.target.value })} /></Td>
                  <Td><Textarea size="sm" value={newCostData.description} onChange={(e) => onNewCostDataChange({ ...newCostData, description: e.target.value })} placeholder="Opis" rows={2} /></Td>
                  <Td>
                    <Input 
                      size="sm" 
                      type="number" 
                      step="0.01" 
                      value={newCostData.netAmount} 
                      onChange={(e) => {
                        const updates: any = { netAmount: e.target.value };
                        if (!newCostData.vatRate) updates.vatRate = '0';
                        onNewCostDataChange({ ...newCostData, ...updates });
                      }} 
                      placeholder="0.00"
                    />
                  </Td>
                  <Td>
                    <Input 
                      size="sm" 
                      type="number" 
                      step="0.01" 
                      min="0"
                      max="100"
                      value={newCostData.vatRate} 
                      onChange={(e) => onNewCostDataChange({ ...newCostData, vatRate: e.target.value })} 
                      placeholder="0"
                    />
                  </Td>
                  <Td>
                    <Input 
                      size="sm" 
                      type="number" 
                      step="0.01" 
                      value={newCostData.grossAmount} 
                      onChange={(e) => onNewCostDataChange({ ...newCostData, grossAmount: e.target.value, netAmount: '', vatRate: '' })} 
                      placeholder="0.00"
                    />
                  </Td>
                  <Td textAlign="center">
                    {canEditClosedCost ? (
                      <Checkbox
                        isChecked={newCostData.isClosed}
                        onChange={(e) => onNewCostDataChange({ ...newCostData, isClosed: e.target.checked })}
                        colorScheme="green"
                      />
                    ) : (
                      <Badge colorScheme={newCostData.isClosed ? "green" : "gray"} fontSize="xs">
                        {newCostData.isClosed ? "Tak" : "Nie"}
                      </Badge>
                    )}
                  </Td>
                  <Td textAlign="center">
                    <VStack spacing={1}>
                      <Input
                        size="sm"
                        type="file"
                        accept=".pdf,.jpg,.jpeg,.png"
                        onChange={(e) => onDocumentFileChange(e.target.files?.[0] || null)}
                        display="none"
                        id="new-cost-file"
                      />
                      <Button
                        as="label"
                        htmlFor="new-cost-file"
                        size="xs"
                        leftIcon={<FileUp size={14} />}
                        variant="outline"
                        cursor="pointer"
                      >
                        {documentFile ? documentFile.name.substring(0, 15) : "Dodaj plik"}
                      </Button>
                      {documentFile && (
                        <IconButton
                          aria-label="Usuń plik"
                          icon={<X size={12} />}
                          size="xs"
                          variant="ghost"
                          colorScheme="red"
                          onClick={() => onDocumentFileChange(null)}
                        />
                      )}
                    </VStack>
                  </Td>
                  <Td textAlign="center">
                    <HStack spacing={1} justify="center">
                      <Button size="sm" colorScheme="green" onClick={onAddCost} isLoading={addingNewCost}>Zapisz</Button>
                      <Button size="sm" variant="ghost" onClick={() => { onShowNewCostRow(false); onDocumentFileChange(null); }}>Anuluj</Button>
                    </HStack>
                  </Td>
                </Tr>
              )}
              {costs.map((cost) => editingCostId === cost.id ? (
                <Tr key={cost.id} bg={editRowBg}>
                  <Td><Input size="sm" value={editingCostData.name} onChange={(e) => onEditingCostDataChange({ ...editingCostData, name: e.target.value })} /></Td>
                  <Td><Input size="sm" value={editingCostData.place} onChange={(e) => onEditingCostDataChange({ ...editingCostData, place: e.target.value })} /></Td>
                  <Td><Input size="sm" type="date" value={editingCostData.date} onChange={(e) => onEditingCostDataChange({ ...editingCostData, date: e.target.value })} /></Td>
                  <Td><Textarea size="sm" value={editingCostData.description} onChange={(e) => onEditingCostDataChange({ ...editingCostData, description: e.target.value })} rows={2} /></Td>
                  <Td>
                    <Input 
                      size="sm" 
                      type="number" 
                      step="0.01" 
                      value={editingCostData.netAmount} 
                      onChange={(e) => {
                        const updates: any = { netAmount: e.target.value };
                        if (!editingCostData.vatRate) updates.vatRate = '0';
                        onEditingCostDataChange({ ...editingCostData, ...updates });
                      }} 
                      placeholder="0.00"
                    />
                  </Td>
                  <Td>
                    <Input 
                      size="sm" 
                      type="number" 
                      step="0.01" 
                      min="0"
                      max="100"
                      value={editingCostData.vatRate} 
                      onChange={(e) => onEditingCostDataChange({ ...editingCostData, vatRate: e.target.value })} 
                      placeholder="0"
                    />
                  </Td>
                  <Td>
                    <Input 
                      size="sm" 
                      type="number" 
                      step="0.01" 
                      value={editingCostData.grossAmount} 
                      onChange={(e) => onEditingCostDataChange({ ...editingCostData, grossAmount: e.target.value, netAmount: '', vatRate: '' })} 
                      placeholder="0.00"
                    />
                  </Td>
                  <Td textAlign="center">
                    {canEditClosedCost ? (
                      <Checkbox
                        isChecked={editingCostData.isClosed}
                        onChange={(e) => onEditingCostDataChange({ ...editingCostData, isClosed: e.target.checked })}
                        colorScheme="green"
                      />
                    ) : (
                      <Badge colorScheme={editingCostData.isClosed ? "green" : "gray"} fontSize="xs">
                        {editingCostData.isClosed ? "Tak" : "Nie"}
                      </Badge>
                    )}
                  </Td>
                  <Td textAlign="center">
                    <VStack spacing={1}>
                      <Input
                        size="sm"
                        type="file"
                        accept=".pdf,.jpg,.jpeg,.png"
                        onChange={(e) => onEditDocumentFileChange(e.target.files?.[0] || null)}
                        display="none"
                        id={`edit-cost-file-${cost.id}`}
                      />
                      <Button
                        as="label"
                        htmlFor={`edit-cost-file-${cost.id}`}
                        size="xs"
                        leftIcon={<FileUp size={14} />}
                        variant="outline"
                        cursor="pointer"
                      >
                        {editDocumentFile ? editDocumentFile.name.substring(0, 15) : cost.documentFileName ? cost.documentFileName.substring(0, 15) : "Dodaj plik"}
                      </Button>
                      {(editDocumentFile || cost.hasDocument) && (
                        <IconButton
                          aria-label="Usuń plik"
                          icon={<X size={12} />}
                          size="xs"
                          variant="ghost"
                          colorScheme="red"
                          onClick={() => {
                            onEditDocumentFileChange(null);
                            onEditingCostDataChange({ ...editingCostData, removeDocument: true });
                          }}
                        />
                      )}
                    </VStack>
                  </Td>
                  <Td textAlign="center">
                    <HStack spacing={1} justify="center">
                      <Button size="sm" colorScheme="green" onClick={onSaveEdit} isLoading={savingCost}>Zapisz</Button>
                      <Button size="sm" variant="ghost" onClick={onCancelEdit}>Anuluj</Button>
                    </HStack>
                  </Td>
                </Tr>
              ) : (
                <Tr key={cost.id} _hover={{ bg: hoverBg }}>
                  <Td fontWeight="medium">{cost.name}</Td>
                  <Td>{cost.place || "-"}</Td>
                  <Td>{formatDate(cost.date)}</Td>
                  <Td>{cost.description || "-"}</Td>
                  <Td isNumeric>{formatCurrency(cost.netAmount ?? 0)}</Td>
                  <Td isNumeric>{cost.vatRate ?? 0}%</Td>
                  <Td isNumeric fontWeight="bold" color="green.600">{formatCurrency(cost.grossAmount)}</Td>
                  <Td textAlign="center">
                    {canEditClosedCost ? (
                      <Checkbox
                        isChecked={cost.isClosed}
                        onChange={() => onToggleCostClosed(cost.id, cost.isClosed)}
                        colorScheme="green"
                        isDisabled={editingClosedCostId === cost.id && savingClosedCost}
                      />
                    ) : (
                      <Badge colorScheme={cost.isClosed ? "green" : "gray"} fontSize="xs">
                        {cost.isClosed ? "Tak" : "Nie"}
                      </Badge>
                    )}
                  </Td>
                  <Td textAlign="center">
                    {cost.hasDocument && cost.previewSasUrl && cost.downloadSasUrl ? (
                      <HStack spacing={1} justify="center">
                        <IconButton
                          aria-label="Podgląd"
                          icon={<Eye size={14} />}
                          size="xs"
                          variant="ghost"
                          colorScheme="purple"
                          onClick={() => window.open(cost.previewSasUrl, '_blank')}
                          title={`Podgląd: ${cost.documentFileName}`}
                        />
                        <IconButton
                          aria-label="Pobierz"
                          icon={<Download size={14} />}
                          size="xs"
                          variant="ghost"
                          colorScheme="green"
                          onClick={() => window.open(cost.downloadSasUrl, '_blank')}
                          title={`Pobierz: ${cost.documentFileName}`}
                        />
                      </HStack>
                    ) : (
                      <Badge colorScheme="gray" fontSize="xs">Brak</Badge>
                    )}
                  </Td>
                  <Td textAlign="center">
                    <HStack spacing={1} justify="center">
                      <IconButton
                        aria-label="Edytuj"
                        icon={<Edit2 size={14} />}
                        size="xs"
                        variant="ghost"
                        colorScheme="blue"
                        onClick={() => onEditCost(cost)}
                      />
                      {resourcePerms.mine.canManageShare && (
                        <IconButton
                          aria-label="Udostępnij"
                          icon={<Share2 size={14} />}
                          size="xs"
                          variant="ghost"
                          colorScheme="orange"
                          onClick={() => onShareCost(cost)}
                        />
                      )}
                      <IconButton
                        aria-label="Usuń"
                        icon={<Trash2 size={14} />}
                        size="xs"
                        variant="ghost"
                        colorScheme="red"
                        onClick={() => onDeleteCost(cost.id)}
                        isLoading={deletingCostId === cost.id}
                      />
                    </HStack>
                  </Td>
                </Tr>
              ))}
            </Tbody>
          </Table>
        </Box>
      )}
    </VStack>
  );
});

interface SharedCostsTabProps {
  costs: ProjectCostListItemWeb[];
  loading: boolean;
  editingSharedCostId: string | null;
  savingSharedCost: boolean;
  resourcePerms: any;
  onToggleSharedCostClosed: (costId: string, currentIsClosed: boolean) => void;
}

const SharedCostsTab = memo(function SharedCostsTab({
  costs,
  loading,
  editingSharedCostId,
  savingSharedCost,
  resourcePerms,
  onToggleSharedCostClosed,
}: SharedCostsTabProps) {
  const bgColor = useColorModeValue("white", "gray.800");
  const borderColor = useColorModeValue("gray.200", "gray.600");
  const hoverBg = useColorModeValue("gray.50", "gray.700");
  const canEditClosedCost = resourcePerms.mine.canEdit || resourcePerms.all.canEdit || resourcePerms.shared.canEdit;

  if (loading) {
    return <LoadingSpinner />;
  }

  return (
    <VStack spacing={4} align="stretch">
      <Text fontSize="sm" color="gray.600">
        Koszty udostępnione przez innych członków projektu
      </Text>

      {costs.length === 0 ? (
        <EmptyState
          icon={Share2}
          title="Brak udostępnionych kosztów"
          description="Nikt jeszcze nie udostępnił Ci kosztów w tym projekcie"
        />
      ) : (
        <Box overflowX="auto" bg={bgColor} p={4} rounded="lg" borderWidth="1px" borderColor={borderColor}>
          <Table size="sm" variant="simple">
            <Thead>
              <Tr>
                <Th>Nazwa</Th>
                <Th>Miejsce</Th>
                <Th>Data</Th>
                <Th>Opis</Th>
                <Th isNumeric>Netto</Th>
                <Th isNumeric>VAT %</Th>
                <Th isNumeric>Brutto</Th>
                <Th textAlign="center">Rozliczone</Th>
                <Th textAlign="center">Dokument</Th>
                <Th>Udostępnione przez</Th>
              </Tr>
            </Thead>
            <Tbody>
              {costs.map((cost) => (
                <Tr key={cost.id} _hover={{ bg: hoverBg }}>
                  <Td fontWeight="medium">{cost.name}</Td>
                  <Td>{cost.place || "-"}</Td>
                  <Td>{formatDate(cost.date)}</Td>
                  <Td>{cost.description || "-"}</Td>
                  <Td isNumeric>{formatCurrency(cost.netAmount)}</Td>
                  <Td isNumeric>{cost.vatRate ?? 0}%</Td>
                  <Td isNumeric fontWeight="bold" color="green.600">{formatCurrency(cost.grossAmount)}</Td>
                  <Td textAlign="center">
                    {canEditClosedCost ? (
                      <Checkbox
                        isChecked={cost.isClosed}
                        onChange={() => onToggleSharedCostClosed(cost.id, cost.isClosed)}
                        colorScheme="green"
                        isDisabled={editingSharedCostId === cost.id && savingSharedCost}
                      />
                    ) : (
                      <Badge colorScheme={cost.isClosed ? "green" : "gray"} fontSize="xs">
                        {cost.isClosed ? "Tak" : "Nie"}
                      </Badge>
                    )}
                  </Td>
                  <Td textAlign="center">
                    {cost.hasDocument && cost.previewSasUrl && cost.downloadSasUrl ? (
                      <HStack spacing={1} justify="center">
                        <IconButton
                          aria-label="Podgląd"
                          icon={<Eye size={14} />}
                          size="xs"
                          variant="ghost"
                          colorScheme="purple"
                          onClick={() => window.open(cost.previewSasUrl, '_blank')}
                          title={`Podgląd: ${cost.documentFileName}`}
                        />
                        <IconButton
                          aria-label="Pobierz"
                          icon={<Download size={14} />}
                          size="xs"
                          variant="ghost"
                          colorScheme="green"
                          onClick={() => window.open(cost.downloadSasUrl, '_blank')}
                          title={`Pobierz: ${cost.documentFileName}`}
                        />
                      </HStack>
                    ) : (
                      <Badge colorScheme="gray" fontSize="xs">Brak</Badge>
                    )}
                  </Td>
                  <Td>{cost.userName}</Td>
                </Tr>
              ))}
            </Tbody>
          </Table>
        </Box>
      )}
    </VStack>
  );
});

// === MAIN COMPONENT ===

export default function ProjectSimpleCosts() {
  const { projectId } = useParams<{ projectId: string }>();
  const navigate = useNavigate();
  const { user } = useContext(AuthContext);
  const { showSuccess, showError } = useToastNotification();

  const [activeTabIndex, setActiveTabIndex] = useState(0);
  const [loading, setLoading] = useState(true);
  const [project, setProject] = useState<any | null>(null);
  const [projectName, setProjectName] = useState("");
  const hasFetchedProjectData = useRef(false);
  const [showNewCostRow, setShowNewCostRow] = useState(false);
  const [addingNewCost, setAddingNewCost] = useState(false);
  const [editingCostId, setEditingCostId] = useState<string | null>(null);
  const [editingCostData, setEditingCostData] = useState<any>(null);
  const [savingCost, setSavingCost] = useState(false);
  const [deletingCostId, setDeletingCostId] = useState<string | null>(null);
  const [costToShare, setCostToShare] = useState<ProjectCostListItemWeb | null>(null);
  const [costToManageShare, setCostToManageShare] = useState<ProjectCostListItemWeb | null>(null);
  const [documentFile, setDocumentFile] = useState<File | null>(null);
  const [editDocumentFile, setEditDocumentFile] = useState<File | null>(null);
  const [editingSharedCostId, setEditingSharedCostId] = useState<string | null>(null);
  const [savingSharedCost, setSavingSharedCost] = useState(false);
  const [editingClosedCostId, setEditingClosedCostId] = useState<string | null>(null);
  const [savingClosedCost, setSavingClosedCost] = useState(false);

  // Tab cache dla wszystkich kosztów
  const allCostsCache = useTabCache<ProjectCostListItemWeb[]>(
    async () => {
      if (!user?.activeTenantId || !projectId) return [];
      const res = await projectApi.getProjectCosts(user.activeTenantId, projectId, ResourceScope.All);
      return res.data;
    },
    `costs-all-${projectId}`
  );

  // Tab cache dla moich kosztów
  const myCostsCache = useTabCache<ProjectCostListItemWeb[]>(
    async () => {
      if (!user?.activeTenantId || !projectId) return [];
      const res = await projectApi.getProjectCosts(user.activeTenantId, projectId, ResourceScope.Mine);
      return res.data;
    },
    `costs-mine-${projectId}`
  );

  // Tab cache dla udostępnionych kosztów
  const sharedCostsCache = useTabCache<ProjectCostListItemWeb[]>(
    async () => {
      if (!user?.activeTenantId || !projectId) return [];
      const res = await projectApi.getProjectCosts(user.activeTenantId, projectId, ResourceScope.Shared);
      return res.data;
    },
    `costs-shared-${projectId}`
  );

  const { isOpen: isShareModalOpen, onOpen: onShareModalOpen, onClose: onShareModalClose } = useDisclosure();
  const { isOpen: isManageShareModalOpen, onOpen: onManageShareModalOpen, onClose: onManageShareModalClose } = useDisclosure();
  const { isOpen: isShareCostsModalOpen, onOpen: onShareCostsModalOpen, onClose: onShareCostsModalClose } = useDisclosure();

  const [newCostData, setNewCostData] = useState({
    name: '',
    place: '',
    date: new Date().toISOString().split('T')[0],
    description: '',
    netAmount: '',
    vatRate: '',
    grossAmount: '',
    isClosed: false,
  });

  const resourcePerms = useResourcePermissions(projectId);

  // Globalny cache dla project details (współdzielony między stronami projektu)
  const projectDetailsCache = useGlobalCache(
    `project-details-${projectId}`,
    async () => {
      if (!user?.activeTenantId || !projectId) throw new Error('Missing tenant or project ID');
      const res = await projectApi.getProjectDetails(user.activeTenantId, projectId);
      return res.data;
    }
  );

  // Globalny cache dla project members (współdzielony między stronami projektu)
  const projectMembersCache = useGlobalCache(
    `project-members-${projectId}`,
    async () => {
      if (!user?.activeTenantId || !projectId) throw new Error('Missing tenant or project ID');
      const res = await projectApi.getProjectMembers(user.activeTenantId, projectId);
      return res.data;
    }
  );

  useEffect(() => {
    if (resourcePerms.raw.loading) return;
    if (hasFetchedProjectData.current) return;
    
    hasFetchedProjectData.current = true;
    fetchProjectData();
  }, [projectId, resourcePerms.raw.loading]);

  // Automatyczne wyliczanie kwoty brutto dla nowego kosztu
  useEffect(() => {
    const netAmount = parseFloat(newCostData.netAmount);
    const vatRate = parseFloat(newCostData.vatRate);
    
    if (!isNaN(netAmount) && netAmount > 0 && !isNaN(vatRate) && vatRate >= 0) {
      const calculatedGross = netAmount * (1 + vatRate / 100);
      const roundedGross = Math.round(calculatedGross * 100) / 100;
      
      setNewCostData(prev => ({
        ...prev,
        grossAmount: roundedGross.toFixed(2)
      }));
    }
  }, [newCostData.netAmount, newCostData.vatRate]);

  // Automatyczne wyliczanie kwoty brutto dla edytowanego kosztu
  useEffect(() => {
    if (!editingCostData) return;
    
    const netAmount = parseFloat(editingCostData.netAmount);
    const vatRate = parseFloat(editingCostData.vatRate);
    
    if (!isNaN(netAmount) && netAmount > 0 && !isNaN(vatRate) && vatRate >= 0) {
      const calculatedGross = netAmount * (1 + vatRate / 100);
      const roundedGross = Math.round(calculatedGross * 100) / 100;
      
      setEditingCostData((prev: any) => ({
        ...prev,
        grossAmount: roundedGross.toFixed(2)
      }));
    }
  }, [editingCostData?.netAmount, editingCostData?.vatRate]);

  const fetchProjectData = async () => {
    if (!user?.activeTenantId || !projectId) return;

    setLoading(true);
    try {
      const projectData = await projectDetailsCache.fetch();
      
      setProject(projectData);
      setProjectName(projectData.name);

      // Pobierz wszystkie zakładki równolegle według uprawnień
      const fetchPromises = [];
      if (resourcePerms.tabs.showAll) {
        fetchPromises.push(allCostsCache.fetch());
      }
      if (resourcePerms.tabs.showMine) {
        fetchPromises.push(myCostsCache.fetch());
      }
      if (resourcePerms.tabs.showShared) {
        fetchPromises.push(sharedCostsCache.fetch());
      }
      
      await Promise.all(fetchPromises);
    } catch (error) {
      console.error("Błąd podczas pobierania projektu:", error);
    } finally {
      setLoading(false);
    }
  };

  const refreshData = () => {
    allCostsCache.clear();
    myCostsCache.clear();
    sharedCostsCache.clear();
    projectDetailsCache.clear();
    hasFetchedProjectData.current = false;
    fetchProjectData();
  };

  // Oblicz indeksy tabów - zapobiega niepotrzebnemu wywoływaniu useEffect
  const allCostsTabIndex = resourcePerms.tabs.showAll ? 0 : -1;
  const myCostsTabIndex = 
    resourcePerms.tabs.showAll && resourcePerms.tabs.showMine ? 1 :
    !resourcePerms.tabs.showAll && resourcePerms.tabs.showMine ? 0 : -1;
  const sharedCostsTabIndex =
    resourcePerms.tabs.showAll && resourcePerms.tabs.showMine && resourcePerms.tabs.showShared ? 2 :
    (resourcePerms.tabs.showAll || resourcePerms.tabs.showMine) && resourcePerms.tabs.showShared ? 1 :
    !resourcePerms.tabs.showAll && !resourcePerms.tabs.showMine && resourcePerms.tabs.showShared ? 0 : -1;

  const handleAddCost = async () => {
    if (!user?.activeTenantId || !projectId) return;
    if (!newCostData.name.trim() || !newCostData.grossAmount) {
      showError("Nazwa i kwota brutto są wymagane");
      return;
    }

    setAddingNewCost(true);
    try {
      await projectApi.createProjectCost(
        user.activeTenantId,
        projectId,
        {
          name: newCostData.name,
          place: newCostData.place || undefined,
          date: new Date(newCostData.date),
          description: newCostData.description || undefined,
          netAmount: newCostData.netAmount ? parseFloat(newCostData.netAmount) : undefined,
          vatRate: newCostData.vatRate ? parseFloat(newCostData.vatRate) : undefined,
          grossAmount: parseFloat(newCostData.grossAmount),
          isClosed: newCostData.isClosed,
          document: documentFile || undefined,
        }
      );

      showSuccess("Koszt został dodany");
      setNewCostData({
        name: '',
        place: '',
        date: new Date().toISOString().split('T')[0],
        description: '',
        netAmount: '',
        vatRate: '',
        grossAmount: '',
        isClosed: false,
      });
      setDocumentFile(null);
      setShowNewCostRow(false);
      refreshData();
    } catch (error) {
      console.error("Błąd podczas dodawania kosztu:", error);
      showError("Wystąpił błąd podczas dodawania kosztu");
    } finally {
      setAddingNewCost(false);
    }
  };

  const handleEditCost = (cost: ProjectCostListItemWeb) => {
    setEditingCostId(cost.id);
    setEditingCostData({
      name: cost.name,
      place: cost.place || '',
      date: cost.date.split('T')[0],
      description: cost.description || '',
      netAmount: (cost.netAmount ?? 0).toString(),
      vatRate: (cost.vatRate ?? 0).toString(),
      grossAmount: cost.grossAmount.toString(),
      isClosed: cost.isClosed,
    });
  };

  const handleSaveEdit = async () => {
    if (!user?.activeTenantId || !projectId || !editingCostId) return;

    setSavingCost(true);
    try {
      await projectApi.updateProjectCost(
        user.activeTenantId,
        projectId,
        editingCostId,
        {
          name: editingCostData.name,
          place: editingCostData.place || undefined,
          date: new Date(editingCostData.date),
          description: editingCostData.description || undefined,
          netAmount: editingCostData.netAmount ? parseFloat(editingCostData.netAmount) : undefined,
          vatRate: editingCostData.vatRate ? parseFloat(editingCostData.vatRate) : undefined,
          grossAmount: editingCostData.grossAmount ? parseFloat(editingCostData.grossAmount) : undefined,
          isClosed: editingCostData.isClosed,
          document: editDocumentFile || undefined,
          removeDocument: editingCostData?.removeDocument || false,
        }
      );

      showSuccess("Koszt został zaktualizowany");
      setEditingCostId(null);
      setEditingCostData(null);
      setEditDocumentFile(null);
      refreshData();
    } catch (error) {
      console.error("Błąd podczas aktualizacji kosztu:", error);
      showError("Wystąpił błąd podczas aktualizacji kosztu");
    } finally {
      setSavingCost(false);
    }
  };

  const handleCancelEdit = () => {
    setEditingCostId(null);
    setEditingCostData(null);
    setEditDocumentFile(null);
  };

  const handleDeleteCost = async (costId: string) => {
    if (!user?.activeTenantId || !projectId) return;
    if (!confirm("Czy na pewno chcesz usunąć ten koszt?")) return;

    setDeletingCostId(costId);
    try {
      await projectApi.deleteProjectCost(user.activeTenantId, projectId, costId);

      showSuccess("Koszt został usunięty");
      refreshData();
    } catch (error) {
      console.error("Błąd podczas usuwania kosztu:", error);
      showError("Wystąpił błąd podczas usuwania kosztu");
    } finally {
      setDeletingCostId(null);
    }
  };

  const handleShareCost = (cost: ProjectCostListItemWeb) => {
    setCostToShare(cost);
    onShareModalOpen();
  };

  const handleManageShare = (cost: ProjectCostListItemWeb) => {
    setCostToManageShare(cost);
    onManageShareModalOpen();
  };

  const handleShareUpdated = () => {
    refreshData();
    onManageShareModalClose();
  };

  const handleToggleCostClosed = async (costId: string, currentIsClosed: boolean) => {
    if (!user?.activeTenantId || !projectId) return;

    setEditingClosedCostId(costId);
    setSavingClosedCost(true);
    try {
      // Znajdujemy koszt w cache aby pobrać wszystkie dane
      const cost = allCostsCache.data?.find(c => c.id === costId) || myCostsCache.data?.find(c => c.id === costId);
      if (!cost) {
        showError("Nie znaleziono kosztu");
        return;
      }

      await projectApi.updateProjectCost(
        user.activeTenantId,
        projectId,
        costId,
        {
          name: cost.name,
          place: cost.place || undefined,
          date: new Date(cost.date),
          description: cost.description || undefined,
          netAmount: cost.netAmount ?? undefined,
          vatRate: cost.vatRate ?? undefined,
          grossAmount: cost.grossAmount ?? undefined,
          isClosed: !currentIsClosed,
          document: undefined,
          removeDocument: false,
        }
      );

      showSuccess("Status rozliczenia został zaktualizowany");
      refreshData();
    } catch (error) {
      console.error("Błąd podczas aktualizacji statusu:", error);
      showError("Wystąpił błąd podczas aktualizacji statusu");
    } finally {
      setEditingClosedCostId(null);
      setSavingClosedCost(false);
    }
  };

  const handleToggleSharedCostClosed = async (costId: string, currentIsClosed: boolean) => {
    if (!user?.activeTenantId || !projectId) return;
    if (!resourcePerms.shared.canEdit) return;

    setEditingSharedCostId(costId);
    setSavingSharedCost(true);
    try {
      // Znajdujemy koszt w cache aby pobrać wszystkie dane
      const cost = sharedCostsCache.data?.find(c => c.id === costId);
      if (!cost) {
        showError("Nie znaleziono kosztu");
        return;
      }

      await projectApi.updateProjectCost(
        user.activeTenantId,
        projectId,
        costId,
        {
          name: cost.name,
          place: cost.place || undefined,
          date: new Date(cost.date),
          description: cost.description || undefined,
          netAmount: cost.netAmount ?? undefined,
          vatRate: cost.vatRate ?? undefined,
          grossAmount: cost.grossAmount ?? undefined,
          isClosed: !currentIsClosed,
          document: undefined,
          removeDocument: false,
        }
      );

      showSuccess("Status rozliczenia został zaktualizowany");
      refreshData();
    } catch (error) {
      console.error("Błąd podczas aktualizacji statusu:", error);
      showError("Wystąpił błąd podczas aktualizacji statusu");
    } finally {
      setEditingSharedCostId(null);
      setSavingSharedCost(false);
    }
  };

  if (loading) {
    return (
      <MainLayout>
        <Box p={{ base: 4, md: 10 }} minH="100vh">
          <LoadingSpinner message="Ładowanie kosztów..." />
        </Box>
      </MainLayout>
    );
  }

  return (
    <MainLayout>
      <Box p={{ base: 4, md: 10 }} minH="100vh">
        <HStack justify="space-between" mb={8} flexWrap="wrap" gap={4}>
          <HStack spacing={3}>
            <Icon as={DollarSign} boxSize={8} color="red.600" />
            <VStack align="flex-start" spacing={0}>
              <Heading size="lg">Koszty projektu</Heading>
              {projectName && <Text fontSize="sm" color="gray.600">{projectName}</Text>}
            </VStack>
          </HStack>
        </HStack>

        {!project || !resourcePerms.hasAnyAccess ? (
          <Box p={8} textAlign="center">
            <EmptyState
              icon={DollarSign}
              title="Brak dostępu"
              description="Nie masz uprawnień do przeglądania kosztów w tym projekcie"
            />
          </Box>
        ) : (
        <Tabs colorScheme="blue" variant="enclosed" onChange={setActiveTabIndex}>
          <TabList>
            {resourcePerms.tabs.showAll && (
            <Tab fontWeight="bold">
              <HStack spacing={2}>
                <Icon as={DollarSign} boxSize={4} />
                <Text>Wszystkie koszty</Text>
                <Badge colorScheme="purple" ml={2}>{allCostsCache.data?.length || 0}</Badge>
              </HStack>
            </Tab>
            )}
            {resourcePerms.tabs.showMine && (
            <Tab fontWeight="bold">
              <HStack spacing={2}>
                <Icon as={DollarSign} boxSize={4} />
                <Text>Moje koszty</Text>
                <Badge colorScheme="blue" ml={2}>{myCostsCache.data?.length || 0}</Badge>
              </HStack>
            </Tab>
            )}
            {resourcePerms.tabs.showShared && (
            <Tab fontWeight="bold">
              <HStack spacing={2}>
                <Icon as={Share2} boxSize={4} />
                <Text>Udostępnione</Text>
                <Badge colorScheme="teal" ml={2}>{sharedCostsCache.data?.length || 0}</Badge>
              </HStack>
            </Tab>
            )}
          </TabList>

          <TabPanels>
            {resourcePerms.tabs.showAll && (
              <TabPanel>
                <AllCostsTab
                  costs={allCostsCache.data || []}
                  loading={allCostsCache.loading}
                  showNewCostRow={showNewCostRow}
                  newCostData={newCostData}
                  documentFile={documentFile}
                  addingNewCost={addingNewCost}
                  resourcePerms={resourcePerms}
                  editingCostId={editingCostId}
                  editingCostData={editingCostData}
                  editDocumentFile={editDocumentFile}
                  deletingCostId={deletingCostId}
                  editingClosedCostId={editingClosedCostId}
                  savingClosedCost={savingClosedCost}
                  onShareCostsModalOpen={onShareCostsModalOpen}
                  onShowNewCostRow={setShowNewCostRow}
                  onNewCostDataChange={setNewCostData}
                  onDocumentFileChange={setDocumentFile}
                  onAddCost={handleAddCost}
                  onManageShare={handleManageShare}
                  onEditCost={handleEditCost}
                  onEditingCostDataChange={setEditingCostData}
                  onEditDocumentFileChange={setEditDocumentFile}
                  onSaveEdit={handleSaveEdit}
                  onCancelEdit={handleCancelEdit}
                  onDeleteCost={handleDeleteCost}
                  onToggleCostClosed={handleToggleCostClosed}
                />
              </TabPanel>
            )}
            {resourcePerms.tabs.showMine && (
              <TabPanel>
                <MyCostsTab
                  costs={myCostsCache.data || []}
                  loading={myCostsCache.loading}
                  showNewCostRow={showNewCostRow}
                  newCostData={newCostData}
                  documentFile={documentFile}
                  addingNewCost={addingNewCost}
                  editingCostId={editingCostId}
                  editingCostData={editingCostData}
                  editDocumentFile={editDocumentFile}
                  savingCost={savingCost}
                  deletingCostId={deletingCostId}
                  editingClosedCostId={editingClosedCostId}
                  savingClosedCost={savingClosedCost}
                  resourcePerms={resourcePerms}
                  onShareCostsModalOpen={onShareCostsModalOpen}
                  onShowNewCostRow={setShowNewCostRow}
                  onNewCostDataChange={setNewCostData}
                  onDocumentFileChange={setDocumentFile}
                  onAddCost={handleAddCost}
                  onEditCost={handleEditCost}
                  onEditingCostDataChange={setEditingCostData}
                  onEditDocumentFileChange={setEditDocumentFile}
                  onSaveEdit={handleSaveEdit}
                  onCancelEdit={() => {
                    setEditingCostId(null);
                    setEditingCostData(null);
                    setEditDocumentFile(null);
                  }}
                  onShareCost={handleShareCost}
                  onDeleteCost={handleDeleteCost}
                  onToggleCostClosed={handleToggleCostClosed}
                />
              </TabPanel>
            )}
            {resourcePerms.tabs.showShared && (
              <TabPanel>
                <SharedCostsTab
                  costs={sharedCostsCache.data || []}
                  loading={sharedCostsCache.loading}
                  editingSharedCostId={editingSharedCostId}
                  savingSharedCost={savingSharedCost}
                  resourcePerms={resourcePerms}
                  onToggleSharedCostClosed={handleToggleSharedCostClosed}
                />
              </TabPanel>
            )}
          </TabPanels>
        </Tabs>
        )}

        <Box mt={6} p={4} bg="blue.50" rounded="md" borderWidth="1px" borderColor="blue.200">
          <Text fontSize="sm" color="blue.800">
            💡 <strong>Wskazówka:</strong> To są proste koszty projektu (faktury, paragony). Dla zaawansowanych kosztorysów według szablonów przejdź do zakładki "Kosztorysy".
          </Text>
        </Box>

        {/* MODAL: MANAGE COST SHARE (pojedynczy koszt) */}
        {costToManageShare && user?.activeTenantId && projectId && (
          <ManageCostShareModal
            isOpen={isManageShareModalOpen}
            onClose={() => {
              onManageShareModalClose();
              setCostToManageShare(null);
            }}
            tenantId={user.activeTenantId}
            projectId={projectId}
            costId={costToManageShare.id}
            costName={costToManageShare.name}
            sharedWithUserIds={costToManageShare.sharedWithUserIds || []}
            currentUserId={user?.id || ""}
            onShareUpdated={handleShareUpdated}
          />
        )}

        {/* MODAL: SHARE COSTS (grupowe udostępnianie) */}
        {user?.activeTenantId && projectId && (
          <ShareCostsModal
            isOpen={isShareCostsModalOpen}
            onClose={onShareCostsModalClose}
            tenantId={user.activeTenantId}
            projectId={projectId}
            onCostsShared={refreshData}
          />
        )}

        {/* MODAL: SHARE COST (stary komponent dla pojedynczego kosztu - backward compatibility) */}
        {costToShare && user?.activeTenantId && projectId && (
          <ShareCostModal
            isOpen={isShareModalOpen}
            onClose={() => {
              onShareModalClose();
              setCostToShare(null);
            }}
            tenantId={user.activeTenantId}
            projectId={projectId}
            cost={costToShare}
            onCostShared={() => {
              refreshData();
            }}
          />
        )}
      </Box>
    </MainLayout>
  );
}
