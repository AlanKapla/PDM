import React, { useEffect, useRef, useState } from 'react';
import { createPortal } from 'react-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import {
  X,
  Upload,
  Paperclip,
  Trash2,
  Loader2,
  Plus,
  Pencil,
  ChevronDown,
  ChevronUp,
} from 'lucide-react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { costTrackerApi } from '../../api/costTrackerApi';
import type {
  TrackedCostWeb,
  CreateCostRequest,
  UpdateCostRequest,
} from '../../types/costTracker.types';
import { MoneyCell } from './MoneyCell';

// ---------------------------------------------------------------------------
// Zod schema
// ---------------------------------------------------------------------------

const schema = z
  .object({
    name: z
      .string()
      .min(1, 'Nazwa jest wymagana')
      .max(300, 'Nazwa może mieć max 300 znaków'),
    description: z.string().max(2000, 'Opis może mieć max 2000 znaków').optional(),
    net: z.string().optional(),
    gross: z.string().optional(),
    contractor: z.string().max(300).optional(),
    date: z.string().optional(),
  })
  .refine(
    (data) => {
      const net = parseFloat(data.net ?? '');
      const gross = parseFloat(data.gross ?? '');
      if (!isNaN(net) && !isNaN(gross)) return gross >= net;
      return true;
    },
    {
      message: 'Brutto musi być większe lub równe netto',
      path: ['gross'],
    }
  );

type FormValues = z.infer<typeof schema>;

// ---------------------------------------------------------------------------
// Props
// ---------------------------------------------------------------------------

export interface CostItemModalProps {
  isOpen: boolean;
  onClose: () => void;
  tenantId: string;
  projectId: string;
  trackerId: string;
  costEstimateId: string;
  costEstimateItemId: string;
  /** Czytelna nazwa pozycji wyświetlana w nagłówku */
  itemName?: string;
  /** Nazwa etapu wyświetlana jako podtytuł */
  groupName?: string;
  /** Czy otworzyć od razu w trybie nowego kosztu */
  startInAddMode?: boolean;
}

type ModalMode = 'list' | 'add' | 'edit';

// ---------------------------------------------------------------------------
// Formularz (add / edit)
// ---------------------------------------------------------------------------

interface CostFormProps {
  mode: 'add' | 'edit';
  editCost?: TrackedCostWeb | null;
  tenantId: string;
  projectId: string;
  trackerId: string;
  costEstimateId: string;
  costEstimateItemId: string;
  invalidateKeys: string[][];
  onBack: () => void;
  onSaveSuccess?: () => void;
}

const CostForm: React.FC<CostFormProps> = ({
  mode,
  editCost,
  tenantId,
  projectId,
  trackerId,
  costEstimateId,
  costEstimateItemId,
  invalidateKeys,
  onBack,
  onSaveSuccess,
}) => {
  const queryClient = useQueryClient();
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [newFiles, setNewFiles] = useState<File[]>([]);
  const [keepAttachmentIds, setKeepAttachmentIds] = useState<string[]>([]);
  const [dragOver, setDragOver] = useState(false);

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      name: '',
      description: '',
      net: '',
      gross: '',
      contractor: '',
      date: new Date().toISOString().split('T')[0],
    },
  });

  useEffect(() => {
    if (mode === 'edit' && editCost) {
      reset({
        name: editCost.name,
        description: editCost.description ?? '',
        net: editCost.net != null ? String(editCost.net) : '',
        gross: editCost.gross != null ? String(editCost.gross) : '',
        contractor: editCost.contractor ?? '',
        date: editCost.date ? editCost.date.split('T')[0] : '',
      });
      setKeepAttachmentIds(editCost.attachments.map((a) => a.id));
    } else {
      reset({
        name: '',
        description: '',
        net: '',
        gross: '',
        contractor: '',
        date: new Date().toISOString().split('T')[0],
      });
      setKeepAttachmentIds([]);
      setNewFiles([]);
    }
  }, [mode, editCost, reset]);

  const mutation = useMutation({
    mutationFn: async (values: FormValues) => {
      const netVal = values.net ? parseFloat(values.net) || undefined : undefined;
      const grossVal = values.gross ? parseFloat(values.gross) || undefined : undefined;

      if (mode === 'edit' && editCost) {
        const req: UpdateCostRequest = {
          costEstimateId: costEstimateId,
          costEstimateItemId: costEstimateItemId,
          name: values.name,
          description: values.description || undefined,
          net: netVal,
          gross: grossVal,
          contractor: values.contractor || undefined,
          date: values.date || undefined,
          existingAttachmentIds: keepAttachmentIds,
          newFiles: newFiles.length > 0 ? newFiles : undefined,
        };
        return costTrackerApi.updateCost(tenantId, projectId, editCost.id, req);
      } else {
        const req: CreateCostRequest = {
          costEstimateId: costEstimateId,
          costEstimateItemId: costEstimateItemId,
          name: values.name,
          description: values.description || undefined,
          net: netVal,
          gross: grossVal,
          contractor: values.contractor || undefined,
          date: values.date || undefined,
          newFiles: newFiles.length > 0 ? newFiles : undefined,
        };
        return costTrackerApi.createCost(tenantId, projectId, req);
      }
    },
    onSuccess: () => {
      invalidateKeys.forEach((key) => queryClient.invalidateQueries({ queryKey: key }));
      if (onSaveSuccess) {
        onSaveSuccess();
      } else {
        onBack();
      }
    },
  });

  const handleFiles = (files: FileList | null) => {
    if (!files) return;
    setNewFiles((prev) => [...prev, ...Array.from(files)]);
  };

  return (
    <form onSubmit={handleSubmit((v) => mutation.mutate(v))} className="px-6 py-5 space-y-4">
      {/* Nazwa */}
      <div>
        <label className="block text-sm font-medium text-gray-700 mb-1">
          Nazwa <span className="text-red-500">*</span>
        </label>
        <input
          {...register('name')}
          type="text"
          placeholder="Np. Faktura VAT nr 123"
          className="w-full px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
        />
        {errors.name && (
          <p className="mt-1 text-xs text-red-500">{errors.name.message}</p>
        )}
      </div>

      {/* Netto / Brutto */}
      <div className="grid grid-cols-2 gap-3">
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">Netto</label>
          <input
            {...register('net')}
            type="number"
            step="0.01"
            placeholder="0,00"
            className="w-full px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
        </div>
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">Brutto</label>
          <input
            {...register('gross')}
            type="number"
            step="0.01"
            placeholder="0,00"
            className="w-full px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
          {errors.gross && (
            <p className="mt-1 text-xs text-red-500">{errors.gross.message}</p>
          )}
        </div>
      </div>
      <p className="text-xs text-gray-500 -mt-2">Podaj netto, brutto lub oba</p>

      {/* Kontrahent */}
      <div>
        <label className="block text-sm font-medium text-gray-700 mb-1">Kontrahent</label>
        <input
          {...register('contractor')}
          type="text"
          placeholder="Nazwa firmy lub osoby"
          className="w-full px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
        />
      </div>

      {/* Data */}
      <div>
        <label className="block text-sm font-medium text-gray-700 mb-1">Data</label>
        <input
          {...register('date')}
          type="date"
          className="w-full px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
        />
      </div>

      {/* Opis */}
      <div>
        <label className="block text-sm font-medium text-gray-700 mb-1">Opis</label>
        <textarea
          {...register('description')}
          rows={3}
          placeholder="Opcjonalny opis kosztu..."
          className="w-full px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 resize-none"
        />
        {errors.description && (
          <p className="mt-1 text-xs text-red-500">{errors.description.message}</p>
        )}
      </div>

      {/* Istniejące załączniki (tryb edycji) */}
      {mode === 'edit' && editCost && editCost.attachments.length > 0 && (
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-2">
            Istniejące załączniki
          </label>
          <div className="space-y-1">
            {editCost.attachments.map((att) => {
              const kept = keepAttachmentIds.includes(att.id);
              return (
                <div
                  key={att.id}
                  className={`flex items-center justify-between px-3 py-1.5 rounded-md text-sm border ${
                    kept
                      ? 'bg-gray-50 border-gray-200 text-gray-700'
                      : 'bg-red-50 border-red-200 text-red-500 line-through'
                  }`}
                >
                  <span className="flex items-center gap-1.5 truncate">
                    <Paperclip size={12} />
                    {att.originalFileName}
                  </span>
                  <button
                    type="button"
                    onClick={() =>
                      kept
                        ? setKeepAttachmentIds((prev) => prev.filter((id) => id !== att.id))
                        : setKeepAttachmentIds((prev) => [...prev, att.id])
                    }
                    className="ml-2 shrink-0 text-gray-400 hover:text-red-500 transition-colors"
                    aria-label={kept ? 'Usuń załącznik' : 'Przywróć załącznik'}
                  >
                    {kept ? <X size={14} /> : <span className="text-xs">przywróć</span>}
                  </button>
                </div>
              );
            })}
          </div>
        </div>
      )}

      {/* Dropzone nowych plików */}
      <div>
        <label className="block text-sm font-medium text-gray-700 mb-2">Załączniki</label>
        <div
          className={`border-2 border-dashed rounded-lg p-4 text-center transition-colors cursor-pointer ${
            dragOver
              ? 'border-blue-400 bg-blue-50'
              : 'border-gray-300 hover:border-gray-400 hover:bg-gray-50'
          }`}
          onDragOver={(e) => { e.preventDefault(); setDragOver(true); }}
          onDragLeave={() => setDragOver(false)}
          onDrop={(e) => { e.preventDefault(); setDragOver(false); handleFiles(e.dataTransfer.files); }}
          onClick={() => fileInputRef.current?.click()}
        >
          <Upload size={20} className="mx-auto mb-1 text-gray-400" />
          <p className="text-sm text-gray-500">
            Przeciągnij pliki lub <span className="text-blue-600">kliknij aby wybrać</span>
          </p>
          <p className="text-xs text-gray-400 mt-1">PDF, obrazy, dokumenty Office</p>
          <input
            ref={fileInputRef}
            type="file"
            multiple
            accept=".pdf,.png,.jpg,.jpeg,.gif,.webp,.doc,.docx,.xls,.xlsx,.odt,.ods"
            className="hidden"
            onChange={(e) => handleFiles(e.target.files)}
          />
        </div>
        {newFiles.length > 0 && (
          <div className="mt-2 space-y-1">
            {newFiles.map((file, i) => (
              <div
                key={i}
                className="flex items-center justify-between px-3 py-1.5 bg-blue-50 border border-blue-200 rounded-md text-sm text-blue-700"
              >
                <span className="flex items-center gap-1.5 truncate">
                  <Paperclip size={12} />
                  {file.name}
                </span>
                <button
                  type="button"
                  onClick={() => setNewFiles((prev) => prev.filter((_, j) => j !== i))}
                  className="ml-2 shrink-0 text-blue-400 hover:text-red-500 transition-colors"
                >
                  <Trash2 size={14} />
                </button>
              </div>
            ))}
          </div>
        )}
      </div>

      {mutation.isError && (
        <div className="px-3 py-2 bg-red-50 border border-red-200 rounded-md text-sm text-red-600">
          Wystąpił błąd podczas zapisywania. Spróbuj ponownie.
        </div>
      )}

      <div className="flex justify-between gap-3 pt-2">
        <button
          type="button"
          onClick={onBack}
          className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50 transition-colors"
        >
          ← Wróć do listy
        </button>
        <button
          type="submit"
          disabled={mutation.isPending}
          className="px-4 py-2 text-sm font-medium text-white bg-blue-600 rounded-md hover:bg-blue-700 disabled:opacity-60 flex items-center gap-2 transition-colors"
        >
          {mutation.isPending && <Loader2 size={14} className="animate-spin" />}
          {mode === 'edit' ? 'Zapisz zmiany' : 'Dodaj koszt'}
        </button>
      </div>
    </form>
  );
};

// ---------------------------------------------------------------------------
// CostRow — pojedynczy koszt na liście
// ---------------------------------------------------------------------------

interface CostRowProps {
  cost: TrackedCostWeb;
  tenantId: string;
  projectId: string;
  trackerId: string;
  invalidateKeys: string[][];
  onEdit: (cost: TrackedCostWeb) => void;
}

const CostRow: React.FC<CostRowProps> = ({
  cost,
  tenantId,
  projectId,
  trackerId,
  invalidateKeys,
  onEdit,
}) => {
  const queryClient = useQueryClient();
  const [attachmentsOpen, setAttachmentsOpen] = useState(false);
  const [confirmDelete, setConfirmDelete] = useState(false);

  const deleteMutation = useMutation({
    mutationFn: () =>
      costTrackerApi.deleteCost(tenantId, projectId, cost.id),
    onSuccess: () => {
      invalidateKeys.forEach((key) => queryClient.invalidateQueries({ queryKey: key }));
    },
  });

  const formattedDate = cost.date
    ? new Date(cost.date).toLocaleDateString('pl-PL')
    : null;

  return (
    <div className="border border-gray-200 rounded-lg overflow-hidden">
      <div className="flex items-start gap-3 px-4 py-3 bg-white hover:bg-gray-50 transition-colors">
        {/* Treść */}
        <div className="flex-1 min-w-0">
          <p className="text-sm font-semibold text-gray-900 truncate">{cost.name}</p>
          <div className="flex flex-wrap gap-x-3 gap-y-0.5 mt-0.5">
            {cost.contractor && (
              <span className="text-xs text-gray-500">{cost.contractor}</span>
            )}
            {formattedDate && (
              <span className="text-xs text-gray-400">{formattedDate}</span>
            )}
          </div>
        </div>

        {/* Kwoty */}
        <div className="text-right shrink-0">
          {cost.net != null && (
            <div className="text-xs text-gray-500">
              netto: <MoneyCell value={cost.net} currency="PLN" className="font-medium text-gray-700" />
            </div>
          )}
          {cost.gross != null && (
            <div className="text-xs text-gray-500">
              brutto: <MoneyCell value={cost.gross} currency="PLN" className="font-semibold text-gray-900" />
            </div>
          )}
        </div>

        {/* Akcje */}
        <div className="flex items-center gap-1 shrink-0">
          {cost.attachments.length > 0 && (
            <button
              type="button"
              title={`${cost.attachments.length} załącznik(i)`}
              onClick={() => setAttachmentsOpen((v) => !v)}
              className="p-1.5 rounded text-gray-400 hover:text-blue-600 hover:bg-blue-50 transition-colors"
            >
              <Paperclip size={14} />
            </button>
          )}
          <button
            type="button"
            title="Edytuj koszt"
            onClick={() => onEdit(cost)}
            className="p-1.5 rounded text-gray-400 hover:text-blue-600 hover:bg-blue-50 transition-colors"
          >
            <Pencil size={14} />
          </button>
          {confirmDelete ? (
            <span className="flex items-center gap-1">
              <button
                type="button"
                onClick={() => deleteMutation.mutate()}
                disabled={deleteMutation.isPending}
                className="px-2 py-0.5 text-xs text-white bg-red-600 rounded hover:bg-red-700 disabled:opacity-50"
              >
                {deleteMutation.isPending ? <Loader2 size={10} className="animate-spin" /> : 'Usuń'}
              </button>
              <button
                type="button"
                onClick={() => setConfirmDelete(false)}
                className="px-2 py-0.5 text-xs text-gray-600 bg-gray-100 rounded hover:bg-gray-200"
              >
                Anuluj
              </button>
            </span>
          ) : (
            <button
              type="button"
              title="Usuń koszt"
              onClick={() => setConfirmDelete(true)}
              className="p-1.5 rounded text-gray-400 hover:text-red-600 hover:bg-red-50 transition-colors"
            >
              <Trash2 size={14} />
            </button>
          )}
        </div>
      </div>

      {/* Lista załączników */}
      {attachmentsOpen && cost.attachments.length > 0 && (
        <div className="border-t border-gray-100 bg-gray-50 px-4 py-2 space-y-1">
          {cost.attachments.map((att) => (
            <a
              key={att.id}
              href={att.fileUrl}
              target="_blank"
              rel="noopener noreferrer"
              className="flex items-center gap-1.5 text-xs text-blue-600 hover:text-blue-800 hover:underline"
            >
              <Paperclip size={11} />
              {att.originalFileName}
            </a>
          ))}
        </div>
      )}
    </div>
  );
};

// ---------------------------------------------------------------------------
// Główny komponent CostItemModal
// ---------------------------------------------------------------------------

export const CostItemModal: React.FC<CostItemModalProps> = ({
  isOpen,
  onClose,
  tenantId,
  projectId,
  trackerId,
  costEstimateId,
  costEstimateItemId,
  itemName,
  groupName,
  startInAddMode = false,
}) => {
  const [mode, setMode] = useState<ModalMode>(startInAddMode ? 'add' : 'list');
  const [editCost, setEditCost] = useState<TrackedCostWeb | null>(null);

  const invalidateKeys: string[][] = [
    ['tracker', 'item-costs', costEstimateId, costEstimateItemId],
    ['tracker', 'by-estimate', costEstimateId],
  ];

  const { data: costs = [], isLoading } = useQuery({
    queryKey: ['tracker', 'item-costs', costEstimateId, costEstimateItemId],
    queryFn: () =>
      costTrackerApi.getItemCosts(tenantId, projectId, costEstimateId, costEstimateItemId),
    enabled: isOpen,
    staleTime: 30_000,
  });

  // Reset trybu przy otwarciu
  useEffect(() => {
    if (isOpen) {
      setMode(startInAddMode ? 'add' : 'list');
      setEditCost(null);
    }
  }, [isOpen, startInAddMode]);

  const handleEdit = (cost: TrackedCostWeb) => {
    setEditCost(cost);
    setMode('edit');
  };

  const handleBack = () => {
    setEditCost(null);
    setMode('list');
  };

  const totalNet = costs.reduce(
    (sum, c) => (c.net != null ? sum + c.net : sum),
    0
  );
  const totalGross = costs.reduce(
    (sum, c) => (c.gross != null ? sum + c.gross : sum),
    0
  );

  if (!isOpen) return null;

  const title =
    mode === 'add'
      ? 'Dodaj koszt'
      : mode === 'edit'
      ? 'Edytuj koszt'
      : itemName ?? 'Koszty pozycji';

  return createPortal(
    <div className="fixed inset-0 z-[9999] flex items-center justify-center p-4">
      {/* Backdrop */}
      <div
        className="absolute inset-0 bg-black/40 backdrop-blur-sm"
        onClick={onClose}
      />

      {/* Dialog */}
      <div className="relative z-10 bg-white rounded-xl shadow-2xl w-full max-w-xl max-h-[90vh] flex flex-col">
        {/* Nagłówek */}
        <div className="flex items-start justify-between px-6 py-4 border-b border-gray-200 shrink-0">
          <div className="min-w-0">
            <h2 className="text-base font-semibold text-gray-900 truncate">{title}</h2>
            {mode === 'list' && groupName && (
              <p className="text-xs text-gray-500 mt-0.5 truncate">{groupName}</p>
            )}
          </div>
          <div className="flex items-center gap-2 ml-4 shrink-0">
            {mode === 'list' && (
              <button
                type="button"
                onClick={() => setMode('add')}
                className="inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-medium text-white bg-blue-600 rounded-md hover:bg-blue-700 transition-colors"
              >
                <Plus size={13} />
                Dodaj koszt
              </button>
            )}
            <button
              type="button"
              onClick={onClose}
              className="text-gray-400 hover:text-gray-600 transition-colors"
              aria-label="Zamknij"
            >
              <X size={18} />
            </button>
          </div>
        </div>

        {/* Treść — przewijalna */}
        <div className="flex-1 overflow-y-auto">
          {mode === 'list' ? (
            <div className="px-6 py-4 space-y-3">
              {isLoading ? (
                <div className="flex items-center justify-center py-12">
                  <Loader2 size={24} className="animate-spin text-gray-400" />
                </div>
              ) : costs.length === 0 ? (
                <div className="text-center py-10">
                  <p className="text-sm text-gray-500 mb-4">Brak kosztów dla tej pozycji.</p>
                  <button
                    type="button"
                    onClick={() => setMode('add')}
                    className="inline-flex items-center gap-1.5 px-4 py-2 text-sm font-medium text-blue-600 border border-blue-300 rounded-md hover:bg-blue-50 transition-colors"
                  >
                    <Plus size={14} />
                    Dodaj pierwszy koszt
                  </button>
                </div>
              ) : (
                costs.map((cost) => (
                  <CostRow
                    key={cost.id}
                    cost={cost}
                    tenantId={tenantId}
                    projectId={projectId}
                    trackerId={trackerId}
                    invalidateKeys={invalidateKeys}
                    onEdit={handleEdit}
                  />
                ))
              )}
            </div>
          ) : (
            <CostForm
              mode={mode}
              editCost={editCost}
              tenantId={tenantId}
              projectId={projectId}
              trackerId={trackerId}
              costEstimateId={costEstimateId}
              costEstimateItemId={costEstimateItemId}
              invalidateKeys={invalidateKeys}
              onBack={handleBack}
              onSaveSuccess={mode === 'add' ? onClose : undefined}
            />
          )}
        </div>

        {/* Footer — tylko w trybie listy */}
        {mode === 'list' && costs.length > 0 && (
          <div className="px-6 py-3 border-t border-gray-200 bg-gray-50 shrink-0">
            <div className="flex items-center justify-between">
              <span className="text-xs text-gray-500">
                Razem ({costs.length} {costs.length === 1 ? 'koszt' : 'koszty/kosztów'})
              </span>
              <div className="flex items-center gap-4 text-sm">
                <span className="text-gray-500">
                  netto:{' '}
                  <MoneyCell value={totalNet} currency="PLN" className="font-medium text-gray-700" />
                </span>
                <span className="text-gray-700 font-semibold">
                  brutto:{' '}
                  <MoneyCell value={totalGross} currency="PLN" className="font-bold text-gray-900" />
                </span>
              </div>
            </div>
          </div>
        )}
      </div>
    </div>,
    document.body
  );
};
