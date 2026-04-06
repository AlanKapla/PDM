/**
 * Komponent do renderowania pól typu pliki (ItemSystemFiles, fieldType = 105)
 * 
 * Obsługuje:
 * - Wyświetlanie miniaturki/ikony plików w komórce tabeli
 * - Modal do zarządzania plikami (podgląd, dodawanie, usuwanie)
 * - Strategia Replace All - przy zatwierdzeniu wysyła pełną listę plików
 */

import React, { useRef, useState, useCallback, useEffect } from 'react';
import {
  Box,
  Button,
  Flex,
  HStack,
  VStack,
  Text,
  IconButton,
  Image,
  Tooltip,
  useToast,
  Progress,
  Badge,
  Modal,
  ModalOverlay,
  ModalContent,
  ModalHeader,
  ModalBody,
  ModalFooter,
  ModalCloseButton,
  useDisclosure,
  Divider,
  Alert,
  AlertIcon,
  useColorModeValue,
} from '@chakra-ui/react';
import {
  Upload,
  Download,
  Eye,
  FileText,
  ImageIcon,
  X,
  Plus,
  Trash2,
  Paperclip,
  Save,
} from 'lucide-react';
import type { CostEstimateFieldFileWeb } from '../../types/costEstimate.types.new';

// Dozwolone formaty plików
const ALLOWED_EXTENSIONS = ['.pdf', '.jpg', '.jpeg'];
const ALLOWED_MIME_TYPES = ['application/pdf', 'image/jpeg'];
const MAX_FILE_SIZE = 52_428_800; // 50 MB
const MAX_FILES_PER_REQUEST = 10;

interface FileFieldRendererProps {
  /** Lista plików dołączonych do pola (z serwera) */
  files: CostEstimateFieldFileWeb[] | null | undefined;
  /** Callback do uploadu plików - zwraca Promise z ID utworzonych plików */
  onUpload?: (files: File[]) => Promise<string[]>;
  /** Callback po pomyślnym uploadzie - do odświeżenia danych */
  onUploadSuccess?: () => void;
  /** Czy pole jest readonly */
  readOnly?: boolean;
  /** Czy komponent jest w trybie kompaktowym (do wyświetlania w komórce tabeli) */
  compact?: boolean;
  /** Etykieta pola */
  label?: string;
}

/** Plik do wyświetlenia - może być istniejący z serwera lub nowy lokalny */
interface DisplayFile {
  id: string;
  originalFileName: string;
  contentType: string;
  fileSize: number;
  sasUriPreview?: string | null;
  sasUriDownload?: string | null;
  /** Czy to nowy plik (jeszcze nie wysłany na serwer) */
  isNew: boolean;
  /** Obiekt File dla nowych plików */
  localFile?: File;
  /** Lokalny URL do podglądu nowych plików */
  localPreviewUrl?: string;
}

/** Formatuje rozmiar pliku do czytelnej postaci */
const formatFileSize = (bytes: number): string => {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
};

/** Sprawdza czy plik jest obrazem */
const isImageFile = (contentType: string): boolean => {
  return contentType.startsWith('image/');
};

/** Sprawdza czy plik jest PDF-em */
const isPdfFile = (contentType: string): boolean => {
  return contentType === 'application/pdf';
};

/** Waliduje plik przed dodaniem */
const validateFile = (file: File): string | null => {
  const extension = '.' + file.name.split('.').pop()?.toLowerCase();
  if (!ALLOWED_EXTENSIONS.includes(extension)) {
    return `Niedozwolone rozszerzenie: ${extension}. Dozwolone: ${ALLOWED_EXTENSIONS.join(', ')}`;
  }
  
  if (!ALLOWED_MIME_TYPES.includes(file.type)) {
    return `Niedozwolony typ pliku: ${file.type}. Dozwolone: PDF, JPG`;
  }
  
  if (file.size > MAX_FILE_SIZE) {
    return `Plik ${file.name} jest za duży (${formatFileSize(file.size)}). Maksymalny rozmiar: 50 MB`;
  }
  
  if (file.size === 0) {
    return `Plik ${file.name} jest pusty`;
  }
  
  return null;
};

/** Konwertuje File do DisplayFile */
const fileToDisplayFile = (file: File): DisplayFile => {
  return {
    id: `new_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`,
    originalFileName: file.name,
    contentType: file.type,
    fileSize: file.size,
    isNew: true,
    localFile: file,
    localPreviewUrl: URL.createObjectURL(file),
  };
};

/** Konwertuje CostEstimateFieldFileWeb do DisplayFile */
const serverFileToDisplayFile = (file: CostEstimateFieldFileWeb): DisplayFile => {
  return {
    id: file.id,
    originalFileName: file.originalFileName,
    contentType: file.contentType,
    fileSize: file.fileSize,
    sasUriPreview: file.sasUriPreview,
    sasUriDownload: file.sasUriDownload,
    isNew: false,
  };
};

// ============================================================================
// Modal podglądu pliku
// ============================================================================

const FilePreviewModal: React.FC<{
  file: DisplayFile | null;
  isOpen: boolean;
  onClose: () => void;
}> = ({ file, isOpen, onClose }) => {
  if (!file) return null;

  const isImage = isImageFile(file.contentType);
  const previewUrl = file.isNew ? file.localPreviewUrl : file.sasUriPreview;

  return (
    <Modal isOpen={isOpen} onClose={onClose} size="4xl">
      <ModalOverlay />
      <ModalContent>
        <ModalHeader>
          <HStack>
            {isImage ? <ImageIcon size={20} /> : <FileText size={20} />}
            <Text noOfLines={1}>{file.originalFileName}</Text>
            {file.isNew && (
              <Badge colorScheme="green" fontSize="xs">Nowy</Badge>
            )}
          </HStack>
        </ModalHeader>
        <ModalCloseButton />
        <ModalBody pb={6}>
          {isImage && previewUrl ? (
            <Image
              src={previewUrl}
              alt={file.originalFileName}
              maxH="70vh"
              mx="auto"
            />
          ) : previewUrl && !file.isNew ? (
            <Box
              as="iframe"
              src={previewUrl}
              w="100%"
              h="70vh"
              border="none"
              borderRadius="md"
            />
          ) : (
            <Flex
              direction="column"
              align="center"
              justify="center"
              py={12}
              color="gray.500"
            >
              <FileText size={64} />
              <Text mt={4}>
                {file.isNew ? 'Podgląd PDF dostępny po zapisaniu' : 'Podgląd niedostępny'}
              </Text>
            </Flex>
          )}
        </ModalBody>
      </ModalContent>
    </Modal>
  );
};

// ============================================================================
// Modal zarządzania plikami
// ============================================================================

const FileManagerModal: React.FC<{
  isOpen: boolean;
  onClose: () => void;
  initialFiles: CostEstimateFieldFileWeb[];
  onSave: (filesToUpload: File[]) => Promise<void>;
  readOnly?: boolean;
}> = ({ isOpen, onClose, initialFiles, onSave, readOnly }) => {
  const toast = useToast();
  const fileInputRef = useRef<HTMLInputElement>(null);
  
  const [displayFiles, setDisplayFiles] = useState<DisplayFile[]>([]);
  const [removedServerFileIds, setRemovedServerFileIds] = useState<Set<string>>(new Set());
  const [previewFile, setPreviewFile] = useState<DisplayFile | null>(null);
  const [isSaving, setIsSaving] = useState(false);
  const [saveProgress, setSaveProgress] = useState(0);
  
  const { isOpen: isPreviewOpen, onOpen: onPreviewOpen, onClose: onPreviewClose } = useDisclosure();

  const cardBg = useColorModeValue('white', 'gray.700');
  const hoverBg = useColorModeValue('gray.50', 'gray.600');

  // Reset stanu przy otwarciu modala
  useEffect(() => {
    if (isOpen) {
      setDisplayFiles(initialFiles.map(serverFileToDisplayFile));
      setRemovedServerFileIds(new Set());
    }
  }, [isOpen, initialFiles]);

  // Cleanup URL.createObjectURL przy zamknięciu
  useEffect(() => {
    return () => {
      displayFiles.forEach(f => {
        if (f.localPreviewUrl) {
          URL.revokeObjectURL(f.localPreviewUrl);
        }
      });
    };
  }, [displayFiles]);

  const hasChanges = useCallback(() => {
    const hasNewFiles = displayFiles.some(f => f.isNew);
    const hasRemovedFiles = removedServerFileIds.size > 0;
    return hasNewFiles || hasRemovedFiles;
  }, [displayFiles, removedServerFileIds.size]);

  const handleAddFiles = useCallback((event: React.ChangeEvent<HTMLInputElement>) => {
    const selectedFiles = Array.from(event.target.files ?? []);
    if (selectedFiles.length === 0) return;

    const totalFiles = displayFiles.length + selectedFiles.length;
    if (totalFiles > MAX_FILES_PER_REQUEST) {
      toast({
        title: 'Za dużo plików',
        description: `Maksymalna liczba plików: ${MAX_FILES_PER_REQUEST}. Aktualnie: ${displayFiles.length}`,
        status: 'error',
        duration: 5000,
      });
      if (fileInputRef.current) fileInputRef.current.value = '';
      return;
    }

    const errors: string[] = [];
    const newDisplayFiles: DisplayFile[] = [];

    for (const file of selectedFiles) {
      const error = validateFile(file);
      if (error) {
        errors.push(error);
      } else {
        newDisplayFiles.push(fileToDisplayFile(file));
      }
    }

    if (errors.length > 0) {
      toast({
        title: 'Błąd walidacji',
        description: errors.join('\n'),
        status: 'error',
        duration: 8000,
        isClosable: true,
      });
    }

    if (newDisplayFiles.length > 0) {
      setDisplayFiles(prev => [...prev, ...newDisplayFiles]);
    }

    if (fileInputRef.current) fileInputRef.current.value = '';
  }, [displayFiles.length, toast]);

  const handleRemoveFile = useCallback((fileId: string) => {
    setDisplayFiles(prev => {
      const file = prev.find(f => f.id === fileId);
      if (file) {
        // Cleanup URL dla nowych plików
        if (file.localPreviewUrl) {
          URL.revokeObjectURL(file.localPreviewUrl);
        }
        // Zaznacz usunięcie pliku z serwera
        if (!file.isNew) {
          setRemovedServerFileIds(prevSet => new Set(prevSet).add(fileId));
        }
      }
      return prev.filter(f => f.id !== fileId);
    });
  }, []);

  const handlePreview = useCallback((file: DisplayFile) => {
    setPreviewFile(file);
    onPreviewOpen();
  }, [onPreviewOpen]);

  const handleDownload = useCallback((file: DisplayFile) => {
    if (file.isNew && file.localFile) {
      // Dla nowych plików - pobierz lokalnie
      const url = URL.createObjectURL(file.localFile);
      const a = document.createElement('a');
      a.href = url;
      a.download = file.originalFileName;
      a.click();
      URL.revokeObjectURL(url);
    } else if (file.sasUriDownload) {
      window.open(file.sasUriDownload, '_blank');
    }
  }, []);

  const handleSave = useCallback(async () => {
    if (!hasChanges()) {
      onClose();
      return;
    }

    setIsSaving(true);
    setSaveProgress(0);

    try {
      // Zbierz wszystkie pliki do wysłania (strategia Replace All)
      // Musimy wysłać WSZYSTKIE pliki - zarówno nowe jak i istniejące z serwera
      // Ale istniejące pliki z serwera nie mamy jako File, więc:
      // - jeśli usunęliśmy jakieś pliki z serwera lub dodaliśmy nowe, wysyłamy TYLKO nowe pliki
      // - backend usunie stare i doda nowe
      
      const newFiles = displayFiles
        .filter(f => f.isNew && f.localFile)
        .map(f => f.localFile!);

      // Symulacja postępu
      const progressInterval = setInterval(() => {
        setSaveProgress(prev => Math.min(prev + 10, 90));
      }, 200);

      await onSave(newFiles);

      clearInterval(progressInterval);
      setSaveProgress(100);

      toast({
        title: 'Zapisano',
        description: 'Zmiany w plikach zostały zapisane',
        status: 'success',
        duration: 3000,
      });

      onClose();
    } catch (error: any) {
      toast({
        title: 'Błąd zapisu',
        description: error?.response?.data?.message || error?.message || 'Wystąpił błąd podczas zapisywania',
        status: 'error',
        duration: 5000,
        isClosable: true,
      });
    } finally {
      setIsSaving(false);
      setSaveProgress(0);
    }
  }, [displayFiles, hasChanges, onClose, onSave, toast]);

  const handleClose = useCallback(() => {
    if (hasChanges() && !isSaving) {
      // Można dodać potwierdzenie, ale na razie po prostu zamykamy
    }
    // Cleanup URLs
    displayFiles.forEach(f => {
      if (f.localPreviewUrl) {
        URL.revokeObjectURL(f.localPreviewUrl);
      }
    });
    onClose();
  }, [displayFiles, hasChanges, isSaving, onClose]);

  return (
    <>
      <Modal isOpen={isOpen} onClose={handleClose} size="2xl" closeOnOverlayClick={!isSaving}>
        <ModalOverlay />
        <ModalContent>
          <ModalHeader>
            <HStack>
              <Paperclip size={20} />
              <Text>Zarządzanie plikami</Text>
            </HStack>
          </ModalHeader>
          <ModalCloseButton isDisabled={isSaving} />
          
          <ModalBody>
            {/* Info o zmianach */}
            {hasChanges() && (
              <Alert status="info" mb={4} borderRadius="md">
                <AlertIcon />
                <Text fontSize="sm">
                  Masz niezapisane zmiany. Kliknij "Zapisz" aby je zachować.
                </Text>
              </Alert>
            )}

            {/* Lista plików */}
            {displayFiles.length > 0 ? (
              <VStack spacing={2} align="stretch" maxH="400px" overflowY="auto">
                {displayFiles.map((file) => {
                  const isImage = isImageFile(file.contentType);
                  const previewUrl = file.isNew ? file.localPreviewUrl : file.sasUriPreview;
                  
                  return (
                    <Flex
                      key={file.id}
                      p={3}
                      borderRadius="md"
                      border="1px solid"
                      borderColor={file.isNew ? 'green.300' : 'gray.200'}
                      bg={cardBg}
                      align="center"
                      gap={3}
                      _hover={{ bg: hoverBg }}
                    >
                      {/* Miniaturka */}
                      <Box
                        w="48px"
                        h="48px"
                        borderRadius="md"
                        overflow="hidden"
                        bg="gray.100"
                        display="flex"
                        alignItems="center"
                        justifyContent="center"
                        flexShrink={0}
                        cursor="pointer"
                        onClick={() => handlePreview(file)}
                      >
                        {isImage && previewUrl ? (
                          <Image
                            src={previewUrl}
                            alt={file.originalFileName}
                            objectFit="cover"
                            w="100%"
                            h="100%"
                          />
                        ) : isPdfFile(file.contentType) ? (
                          <FileText size={24} color="#E53E3E" />
                        ) : (
                          <FileText size={24} color="#718096" />
                        )}
                      </Box>

                      {/* Nazwa i info */}
                      <VStack align="start" spacing={0} flex={1} minW={0}>
                        <Text 
                          fontSize="sm" 
                          fontWeight="medium" 
                          noOfLines={1} 
                          title={file.originalFileName}
                        >
                          {file.originalFileName}
                        </Text>
                        <HStack spacing={2}>
                          <Text fontSize="xs" color="gray.500">
                            {formatFileSize(file.fileSize)}
                          </Text>
                          <Badge 
                            size="sm" 
                            colorScheme={isImage ? 'green' : 'red'} 
                            fontSize="2xs"
                          >
                            {isImage ? 'JPG' : 'PDF'}
                          </Badge>
                          {file.isNew && (
                            <Badge colorScheme="primary" fontSize="2xs">Nowy</Badge>
                          )}
                        </HStack>
                      </VStack>

                      {/* Akcje */}
                      <HStack spacing={1}>
                        <Tooltip label="Podgląd">
                          <IconButton
                            aria-label="Podgląd"
                            icon={<Eye size={16} />}
                            size="sm"
                            variant="ghost"
                            onClick={() => handlePreview(file)}
                          />
                        </Tooltip>
                        <Tooltip label="Pobierz">
                          <IconButton
                            aria-label="Pobierz"
                            icon={<Download size={16} />}
                            size="sm"
                            variant="ghost"
                            onClick={() => handleDownload(file)}
                            isDisabled={!file.isNew && !file.sasUriDownload}
                          />
                        </Tooltip>
                        {!readOnly && (
                          <Tooltip label="Usuń">
                            <IconButton
                              aria-label="Usuń"
                              icon={<Trash2 size={16} />}
                              size="sm"
                              variant="ghost"
                              colorScheme="red"
                              onClick={() => handleRemoveFile(file.id)}
                              isDisabled={isSaving}
                            />
                          </Tooltip>
                        )}
                      </HStack>
                    </Flex>
                  );
                })}
              </VStack>
            ) : (
              <Flex
                direction="column"
                align="center"
                justify="center"
                py={8}
                color="gray.500"
                borderRadius="md"
                border="2px dashed"
                borderColor="gray.200"
              >
                <Paperclip size={32} />
                <Text mt={2}>Brak załączonych plików</Text>
                {!readOnly && (
                  <Text fontSize="sm">Kliknij "Dodaj pliki" aby dodać</Text>
                )}
              </Flex>
            )}

            {/* Przycisk dodawania */}
            {!readOnly && (
              <>
                <input
                  type="file"
                  ref={fileInputRef}
                  onChange={handleAddFiles}
                  accept={ALLOWED_EXTENSIONS.join(',')}
                  multiple
                  style={{ display: 'none' }}
                />
                
                <Divider my={4} />
                
                <Button
                  leftIcon={<Plus size={16} />}
                  variant="outline"
                  colorScheme="primary"
                  onClick={() => fileInputRef.current?.click()}
                  isDisabled={isSaving || displayFiles.length >= MAX_FILES_PER_REQUEST}
                  w="100%"
                >
                  Dodaj pliki
                </Button>
                
                <Text fontSize="xs" color="gray.500" mt={2} textAlign="center">
                  Dozwolone formaty: PDF, JPG • Maks. rozmiar: 50 MB/plik • Maks. {MAX_FILES_PER_REQUEST} plików
                </Text>
              </>
            )}

            {/* Progress bar */}
            {isSaving && (
              <Box mt={4}>
                <Progress
                  value={saveProgress}
                  size="sm"
                  colorScheme="primary"
                  borderRadius="md"
                />
                <Text fontSize="xs" color="gray.500" mt={1} textAlign="center">
                  Zapisywanie zmian... {saveProgress}%
                </Text>
              </Box>
            )}
          </ModalBody>

          <ModalFooter>
            <HStack spacing={3}>
              <Button
                variant="ghost"
                onClick={handleClose}
                isDisabled={isSaving}
              >
                Anuluj
              </Button>
              {!readOnly && (
                <Button
                  colorScheme="primary"
                  leftIcon={<Save size={16} />}
                  onClick={handleSave}
                  isLoading={isSaving}
                  loadingText="Zapisuję..."
                  isDisabled={!hasChanges()}
                >
                  Zapisz
                </Button>
              )}
            </HStack>
          </ModalFooter>
        </ModalContent>
      </Modal>

      {/* Modal podglądu */}
      <FilePreviewModal
        file={previewFile}
        isOpen={isPreviewOpen}
        onClose={() => {
          onPreviewClose();
          setPreviewFile(null);
        }}
      />
    </>
  );
};

// ============================================================================
// Główny komponent
// ============================================================================

export const FileFieldRenderer: React.FC<FileFieldRendererProps> = ({
  files,
  onUpload,
  onUploadSuccess,
  readOnly = false,
  compact = false,
  label,
}) => {
  const { isOpen, onOpen, onClose } = useDisclosure();
  const fileList = files ?? [];

  const handleSave = useCallback(async (filesToUpload: File[]) => {
    if (!onUpload) return;
    
    // Wywołaj upload (strategia Replace All)
    await onUpload(filesToUpload);
    
    // Odśwież dane
    if (onUploadSuccess) {
      onUploadSuccess();
    }
  }, [onUpload, onUploadSuccess]);

  // Tryb kompaktowy dla komórki tabeli
  if (compact) {
    const hasFiles = fileList.length > 0;
    const firstFile = fileList[0];
    const isImage = firstFile ? isImageFile(firstFile.contentType) : false;
    
    // Określ tooltip w zależności od stanu
    const getTooltipLabel = () => {
      if (readOnly && !onUpload) {
        return 'Zapisz pozycję aby móc dodać pliki';
      }
      if (hasFiles) {
        return `${fileList.length} plik(ów) - kliknij aby zarządzać`;
      }
      if (readOnly) {
        return 'Podgląd plików';
      }
      return 'Kliknij aby dodać pliki';
    };

    return (
      <>
        <Tooltip label={getTooltipLabel()}>
          <Box
            as="button"
            onClick={onOpen}
            p={1}
            borderRadius="md"
          bg={hasFiles ? 'primary.50' : 'transparent'}
          _hover={{ bg: hasFiles ? 'primary.100' : 'gray.100' }}
            display="flex"
            alignItems="center"
            gap={1}
            cursor="pointer"
            transition="all 0.2s"
            opacity={readOnly && !onUpload && !hasFiles ? 0.5 : 1}
          >
            {hasFiles ? (
              <>
                {/* Miniaturka pierwszego pliku */}
                <Box
                  w="24px"
                  h="24px"
                  borderRadius="sm"
                  overflow="hidden"
                  bg="gray.100"
                  display="flex"
                  alignItems="center"
                  justifyContent="center"
                >
                  {isImage && firstFile.sasUriPreview ? (
                    <Image
                      src={firstFile.sasUriPreview}
                      alt={firstFile.originalFileName}
                      objectFit="cover"
                      w="100%"
                      h="100%"
                    />
                  ) : (
                    <FileText size={14} color="#718096" />
                  )}
                </Box>
                {/* Liczba plików */}
                <Badge colorScheme="primary" fontSize="2xs" borderRadius="full">
                  {fileList.length}
                </Badge>
              </>
            ) : (
              <HStack spacing={1} color={readOnly && !onUpload ? 'gray.300' : 'gray.400'}>
                <Paperclip size={14} />
                {!readOnly && onUpload && <Plus size={12} />}
              </HStack>
            )}
          </Box>
        </Tooltip>

        <FileManagerModal
          isOpen={isOpen}
          onClose={onClose}
          initialFiles={fileList}
          onSave={handleSave}
          readOnly={readOnly || !onUpload}
        />
      </>
    );
  }

  // Tryb normalny (pełny)
  return (
    <Box w="100%">
      {label && (
        <Text fontWeight="medium" mb={2}>
          {label}
        </Text>
      )}

      <Button
        leftIcon={<Paperclip size={16} />}
        variant="outline"
        size="sm"
        onClick={onOpen}
        rightIcon={
          fileList.length > 0 ? (
            <Badge colorScheme="primary" borderRadius="full">
              {fileList.length}
            </Badge>
          ) : undefined
        }
      >
        {fileList.length > 0 ? 'Zarządzaj plikami' : 'Dodaj pliki'}
      </Button>

      <FileManagerModal
        isOpen={isOpen}
        onClose={onClose}
        initialFiles={fileList}
        onSave={handleSave}
        readOnly={readOnly || !onUpload}
      />
    </Box>
  );
};

export default FileFieldRenderer;
