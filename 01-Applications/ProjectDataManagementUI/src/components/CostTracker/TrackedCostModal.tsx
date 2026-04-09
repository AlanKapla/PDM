import React, { useEffect, useRef, useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { X, Upload, Paperclip, Trash2, Loader2 } from 'lucide-react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { costTrackerApi } from '../../api/costTrackerApi';
import type {
  TrackedCostWeb,
  CreateCostRequest,
  UpdateCostRequest,
} from '../../types/costTracker.types';

// ---------------------------------------------------------------------------
// Walidacja Zod
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

export interface TrackedCostModalProps {
  isOpen: boolean;
  onClose: () => void;
  tenantId: string;
  projectId: string;
  trackerId: string;
  /** Prefillowane ID kosztorysu (opcjonalne) */
  costEstimateId?: string | null;
  /** Prefillowane ID pozycji kosztorysu (opcjonalne) */
  costEstimateItemId?: string | null;
  /** Czytelna etykieta kontekstu wyświetlana w formularzu */
  contextLabel?: string;
  /** Jeśli podany — tryb edycji */
  editCost?: TrackedCostWeb | null;
  /** React Query key do inwalidacji po zapisie */
  invalidateKeys?: string[][];
}

// ---------------------------------------------------------------------------
// Komponent
// ---------------------------------------------------------------------------

export const TrackedCostModal: React.FC<TrackedCostModalProps> = ({
  isOpen,
  onClose,
  tenantId,
  projectId,
  trackerId,
  costEstimateId,
  costEstimateItemId,
  contextLabel,
  editCost,
  invalidateKeys = [],
}) => {
  const queryClient = useQueryClient();
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [newFiles, setNewFiles] = useState<File[]>([]);
  const [keepAttachmentIds, setKeepAttachmentIds] = useState<string[]>([]);
  const [dragOver, setDragOver] = useState(false);

  const isEdit = Boolean(editCost);

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
      date: '',
    },
  });

  // Wypełnij formularz danymi do edycji
  useEffect(() => {
    if (isOpen) {
      if (editCost) {
        reset({
          name: editCost.name,
          description: editCost.description ?? '',
          net: editCost.net !== null && editCost.net !== undefined ? String(editCost.net) : '',
          gross:
            editCost.gross !== null && editCost.gross !== undefined
              ? String(editCost.gross)
              : '',
          contractor: editCost.contractor ?? '',
          date: editCost.date ? editCost.date.split('T')[0] : '',
        });
        setKeepAttachmentIds(editCost.attachments.map((a) => a.id));
        setNewFiles([]);
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
    }
  }, [isOpen, editCost, reset]);

  const mutation = useMutation({
    mutationFn: async (values: FormValues) => {
      const netVal = values.net && values.net !== '' ? parseFloat(values.net) : undefined;
      const grossVal =
        values.gross && values.gross !== '' ? parseFloat(values.gross) : undefined;

      if (isEdit && editCost) {
        const req: UpdateCostRequest = {
          costEstimateId: costEstimateId ?? undefined,
          costEstimateItemId: costEstimateItemId ?? undefined,
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
          costEstimateId: costEstimateId ?? undefined,
          costEstimateItemId: costEstimateItemId ?? undefined,
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
      onClose();
    },
  });

  const onSubmit = handleSubmit((values) => mutation.mutate(values));

  const handleFiles = (files: FileList | null) => {
    if (!files) return;
    setNewFiles((prev) => [...prev, ...Array.from(files)]);
  };

  const removeNewFile = (index: number) => {
    setNewFiles((prev) => prev.filter((_, i) => i !== index));
  };

  const removeExistingAttachment = (id: string) => {
    setKeepAttachmentIds((prev) => prev.filter((a) => a !== id));
  };

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
      {/* Backdrop */}
      <div
        className="absolute inset-0 bg-black/40 backdrop-blur-sm"
        onClick={onClose}
      />

      {/* Dialog */}
      <div className="relative z-10 bg-white rounded-xl shadow-2xl w-full max-w-lg max-h-[90vh] overflow-y-auto">
        {/* Nagłówek */}
        <div className="flex items-center justify-between px-6 py-4 border-b border-gray-200">
          <h2 className="text-base font-semibold text-gray-900">
            {isEdit ? 'Edytuj koszt' : 'Dodaj koszt'}
          </h2>
          <button
            type="button"
            onClick={onClose}
            className="text-gray-400 hover:text-gray-600 transition-colors"
            aria-label="Zamknij"
          >
            <X size={18} />
          </button>
        </div>

        {/* Treść formularza */}
        <form onSubmit={onSubmit} className="px-6 py-5 space-y-4">
          {/* Kontekst */}
          {contextLabel && (
            <div>
              <label className="block text-xs font-medium text-gray-500 mb-1">Powiązanie</label>
              <div className="px-3 py-2 bg-gray-50 rounded-md text-sm text-gray-700 border border-gray-200">
                {contextLabel}
              </div>
            </div>
          )}

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
          <p className="text-xs text-gray-500 -mt-2">
            Podaj netto, brutto lub oba — resztę wyliczymy automatycznie.
          </p>

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
          {isEdit && editCost && editCost.attachments.length > 0 && (
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
                            ? removeExistingAttachment(att.id)
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

          {/* Nowe pliki — dropzone */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Załączniki
            </label>
            <div
              className={`border-2 border-dashed rounded-lg p-4 text-center transition-colors cursor-pointer ${
                dragOver
                  ? 'border-blue-400 bg-blue-50'
                  : 'border-gray-300 hover:border-gray-400 hover:bg-gray-50'
              }`}
              onDragOver={(e) => {
                e.preventDefault();
                setDragOver(true);
              }}
              onDragLeave={() => setDragOver(false)}
              onDrop={(e) => {
                e.preventDefault();
                setDragOver(false);
                handleFiles(e.dataTransfer.files);
              }}
              onClick={() => fileInputRef.current?.click()}
            >
              <Upload size={20} className="mx-auto mb-1 text-gray-400" />
              <p className="text-sm text-gray-500">
                Przeciągnij pliki lub <span className="text-blue-600">kliknij aby wybrać</span>
              </p>
              <input
                ref={fileInputRef}
                type="file"
                multiple
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
                      onClick={() => removeNewFile(i)}
                      className="ml-2 shrink-0 text-blue-400 hover:text-red-500 transition-colors"
                      aria-label="Usuń plik"
                    >
                      <Trash2 size={14} />
                    </button>
                  </div>
                ))}
              </div>
            )}
          </div>

          {/* Błąd mutacji */}
          {mutation.isError && (
            <div className="px-3 py-2 bg-red-50 border border-red-200 rounded-md text-sm text-red-600">
              Wystąpił błąd podczas zapisywania. Spróbuj ponownie.
            </div>
          )}

          {/* Przyciski */}
          <div className="flex justify-end gap-3 pt-2">
            <button
              type="button"
              onClick={onClose}
              className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50 transition-colors"
            >
              Anuluj
            </button>
            <button
              type="submit"
              disabled={mutation.isPending}
              className="px-4 py-2 text-sm font-medium text-white bg-blue-600 rounded-md hover:bg-blue-700 disabled:opacity-60 disabled:cursor-not-allowed flex items-center gap-2 transition-colors"
            >
              {mutation.isPending && <Loader2 size={14} className="animate-spin" />}
              {isEdit ? 'Zapisz zmiany' : 'Dodaj koszt'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
