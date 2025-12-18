# Przykłady użycia nowych komponentów

## Komponenty wspólne

### LoadingSpinner

```tsx
import { LoadingSpinner } from "../components/common";

// Podstawowe użycie
<LoadingSpinner />

// Z wiadomością
<LoadingSpinner message="Ładowanie danych..." />

// Pełny ekran
<LoadingSpinner fullScreen message="Trwa przetwarzanie..." />

// Mały rozmiar
<LoadingSpinner size="sm" />

// W komponencie
function MyComponent() {
  const [loading, setLoading] = useState(true);

  if (loading) {
    return <LoadingSpinner fullScreen />;
  }

  return <Content />;
}
```

### EmptyState

```tsx
import { EmptyState } from "../components/common";
import { FolderKanban, Plus } from "lucide-react";
import { Button, Icon } from "@chakra-ui/react";

// Podstawowe użycie
<EmptyState 
  title="Brak projektów"
  description="Nie masz jeszcze żadnych projektów"
/>

// Z ikoną
<EmptyState 
  icon={FolderKanban}
  title="Brak projektów"
  description="Stwórz swój pierwszy projekt"
/>

// Z akcją
<EmptyState 
  icon={FolderKanban}
  title="Brak projektów"
  description="Stwórz swój pierwszy projekt"
  action={
    <Button 
      leftIcon={<Icon as={Plus} />}
      colorScheme="blue"
      onClick={handleCreate}
    >
      Utwórz projekt
    </Button>
  }
/>

// W liście
function ProjectsList() {
  if (projects.length === 0) {
    return (
      <EmptyState 
        icon={FolderKanban}
        title="Brak projektów"
        action={<Button onClick={onCreate}>Dodaj projekt</Button>}
      />
    );
  }

  return <List items={projects} />;
}
```

### ErrorAlert

```tsx
import { ErrorAlert } from "../components/common";

// Podstawowe użycie
<ErrorAlert description="Nie udało się pobrać danych" />

// Z własnym tytułem
<ErrorAlert 
  title="Błąd serwera" 
  description="Spróbuj ponownie później"
/>

// Różne warianty
<ErrorAlert variant="subtle" description="Błąd" />
<ErrorAlert variant="solid" description="Błąd" />
<ErrorAlert variant="left-accent" description="Błąd" />
<ErrorAlert variant="top-accent" description="Błąd" />

// W komponencie
function DataView() {
  const [error, setError] = useState<string | null>(null);

  if (error) {
    return <ErrorAlert description={error} />;
  }

  return <Data />;
}
```

### UserAvatar

```tsx
import { UserAvatar } from "../components/common";

// Podstawowe użycie
<UserAvatar firstName="Jan" lastName="Kowalski" />

// Różne rozmiary
<UserAvatar firstName="Jan" lastName="Kowalski" size="xs" />
<UserAvatar firstName="Jan" lastName="Kowalski" size="sm" />
<UserAvatar firstName="Jan" lastName="Kowalski" size="md" />
<UserAvatar firstName="Jan" lastName="Kowalski" size="lg" />
<UserAvatar firstName="Jan" lastName="Kowalski" size="xl" />

// Własne kolory
<UserAvatar 
  firstName="Jan" 
  lastName="Kowalski" 
  bg="green.600"
  color="white"
/>

// W liście
{members.map(member => (
  <HStack key={member.id}>
    <UserAvatar 
      firstName={member.firstName}
      lastName={member.lastName}
    />
    <Text>{member.firstName} {member.lastName}</Text>
  </HStack>
))}
```

### DataCard

```tsx
import { DataCard } from "../components/common";

// Podstawowe użycie
<DataCard>
  <Text>Zawartość karty</Text>
</DataCard>

// Z efektem hover
<DataCard hoverable>
  <Text>Najedź na mnie</Text>
</DataCard>

// Z padding
<DataCard p={4} hoverable>
  <VStack align="flex-start">
    <Heading size="sm">Tytuł</Heading>
    <Text>Opis</Text>
  </VStack>
</DataCard>

// W liście
<VStack spacing={2}>
  {items.map(item => (
    <DataCard key={item.id} hoverable p={3}>
      <HStack justify="space-between">
        <Text>{item.name}</Text>
        <Badge>{item.status}</Badge>
      </HStack>
    </DataCard>
  ))}
</VStack>
```

### ConfirmDialog

```tsx
import { ConfirmDialog } from "../components/common";
import { useModal } from "../hooks/useModal";

function MyComponent() {
  const confirmModal = useModal();
  const [deleting, setDeleting] = useState(false);

  const handleDelete = async () => {
    setDeleting(true);
    try {
      await deleteItem();
      confirmModal.onClose();
    } finally {
      setDeleting(false);
    }
  };

  return (
    <>
      <Button onClick={confirmModal.onOpen} colorScheme="red">
        Usuń
      </Button>

      <ConfirmDialog
        isOpen={confirmModal.isOpen}
        onClose={confirmModal.onClose}
        onConfirm={handleDelete}
        title="Usuń element"
        message="Czy na pewno chcesz usunąć ten element?"
        confirmText="Usuń"
        cancelText="Anuluj"
        isLoading={deleting}
        colorScheme="red"
      />
    </>
  );
}
```

---

## Custom Hooki

### useToastNotification

```tsx
import { useToastNotification } from "../hooks/useToastNotification";

function MyComponent() {
  const { showSuccess, showError, showWarning, showInfo } = useToastNotification();

  const handleSubmit = async () => {
    try {
      await submitData();
      showSuccess("Zapisano pomyślnie");
    } catch (error) {
      showError("Błąd", "Nie udało się zapisać danych");
    }
  };

  const handleValidate = () => {
    if (!isValid) {
      showWarning("Uwaga", "Niektóre pola są puste");
    }
  };

  const handleInfo = () => {
    showInfo("Informacja", "To jest informacja dla użytkownika");
  };

  return <Button onClick={handleSubmit}>Zapisz</Button>;
}
```

### useModal

```tsx
import { useModal } from "../hooks/useModal";
import { Modal, ModalOverlay, ModalContent, Button } from "@chakra-ui/react";

function MyComponent() {
  const createModal = useModal();
  const editModal = useModal();

  return (
    <>
      <Button onClick={createModal.onOpen}>Utwórz</Button>
      <Button onClick={editModal.onOpen}>Edytuj</Button>
      <Button onClick={createModal.toggle}>Toggle</Button>

      <Modal isOpen={createModal.isOpen} onClose={createModal.onClose}>
        <ModalOverlay />
        <ModalContent>
          {/* Zawartość */}
        </ModalContent>
      </Modal>

      <Modal isOpen={editModal.isOpen} onClose={editModal.onClose}>
        <ModalOverlay />
        <ModalContent>
          {/* Zawartość */}
        </ModalContent>
      </Modal>
    </>
  );
}
```

### useForm

```tsx
import { useForm } from "../hooks/useForm";
import { useToastNotification } from "../hooks/useToastNotification";

function CreateProjectForm() {
  const { values, errors, handleChange, validate, reset } = useForm({
    name: "",
    description: "",
    isActive: true
  });

  const { showSuccess, showError } = useToastNotification();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    const isValid = validate({
      name: (v) => (!v ? "Nazwa jest wymagana" : undefined),
      description: (v) => (v.length < 10 ? "Minimum 10 znaków" : undefined)
    });

    if (!isValid) return;

    try {
      await createProject(values);
      showSuccess("Projekt utworzony");
      reset();
    } catch (error) {
      showError("Błąd", "Nie udało się utworzyć projektu");
    }
  };

  return (
    <form onSubmit={handleSubmit}>
      <FormControl isInvalid={!!errors.name}>
        <FormLabel>Nazwa projektu</FormLabel>
        <Input
          value={values.name}
          onChange={(e) => handleChange("name", e.target.value)}
        />
        {errors.name && <FormErrorMessage>{errors.name}</FormErrorMessage>}
      </FormControl>

      <FormControl isInvalid={!!errors.description}>
        <FormLabel>Opis</FormLabel>
        <Textarea
          value={values.description}
          onChange={(e) => handleChange("description", e.target.value)}
        />
        {errors.description && <FormErrorMessage>{errors.description}</FormErrorMessage>}
      </FormControl>

      <Button type="submit">Utwórz</Button>
    </form>
  );
}
```

### useFetch

```tsx
import { useFetch } from "../hooks/useFetch";
import { useEffect } from "react";
import { userApi } from "../api/userApi";

function UsersList() {
  const { data: users, loading, error, execute } = useFetch<User[]>({
    onSuccess: (data) => console.log("Pobrano użytkowników:", data.length),
    onError: (error) => console.error("Błąd:", error)
  });

  useEffect(() => {
    execute(() => userApi.getUsers());
  }, []);

  if (loading) return <LoadingSpinner />;
  if (error) return <ErrorAlert description={error} />;
  if (!users || users.length === 0) return <EmptyState title="Brak użytkowników" />;

  return (
    <VStack>
      {users.map(user => (
        <Text key={user.id}>{user.name}</Text>
      ))}
    </VStack>
  );
}
```

---

## Funkcje formatujące

### formatters.ts

```tsx
import { 
  formatFileSize, 
  formatDate, 
  formatDateShort,
  formatDateForInput,
  getRelativeTime,
  truncateText,
  getFileExtension,
  isImageFile,
  isPdfFile
} from "../utils/formatters";

// Formatowanie rozmiaru pliku
<Text>{formatFileSize(file.size)}</Text>
// Wynik: "1.5 MB"

// Formatowanie daty
<Text>{formatDate(file.createdAt)}</Text>
// Wynik: "12 grudnia 2025, 14:30"

<Text>{formatDate(file.createdAt, false)}</Text>
// Wynik: "12 grudnia 2025"

// Krótka data
<Text>{formatDateShort(file.createdAt)}</Text>
// Wynik: "12.12.2025"

// Data dla inputa
<Input type="date" value={formatDateForInput(new Date())} />
// Wynik: "2025-12-12"

// Względny czas
<Text>{getRelativeTime(file.createdAt)}</Text>
// Wynik: "2 godz. temu"

// Skracanie tekstu
<Text>{truncateText(longText, 50)}</Text>
// Wynik: "To jest długi tekst który zostanie skrócon..."

// Rozszerzenie pliku
const ext = getFileExtension("dokument.pdf");
// Wynik: "pdf"

// Sprawdzanie typu pliku
if (isImageFile(filename)) {
  return <Image src={url} />;
}

if (isPdfFile(filename)) {
  return <PdfViewer url={url} />;
}
```

### constants.ts

```tsx
import { 
  getProjectRoleName, 
  getProjectRoleColor,
  getTenantRoleName,
  getTenantRoleColor,
  FILE_UPLOAD,
  WORK_SCHEDULE_COLORS,
  TOAST_DURATION
} from "../utils/constants";

// Role projektu
<Badge colorScheme={getProjectRoleColor(member.role)}>
  {getProjectRoleName(member.role)}
</Badge>

// Role organizacji
<Badge colorScheme={getTenantRoleColor(member.role)}>
  {getTenantRoleName(member.role)}
</Badge>

// Konfiguracja uploadu
const validateFile = (file: File) => {
  if (!FILE_UPLOAD.ALLOWED_TYPES.includes(file.type)) {
    return `Dozwolone typy: ${FILE_UPLOAD.ALLOWED_TYPES_DISPLAY}`;
  }
  
  if (file.size > FILE_UPLOAD.MAX_FILE_SIZE) {
    return `Maksymalny rozmiar: ${formatFileSize(FILE_UPLOAD.MAX_FILE_SIZE)}`;
  }
  
  return null;
};

// Kolory harmonogramu
<Select>
  {WORK_SCHEDULE_COLORS.map((color, index) => (
    <option key={index} value={color}>
      {color}
    </option>
  ))}
</Select>

// Czasy toastów
showSuccess("Sukces", { duration: TOAST_DURATION.SHORT });
showError("Błąd", { duration: TOAST_DURATION.LONG });
```

---

## Kompletny przykład komponentu

```tsx
import { useState, useEffect } from "react";
import { Box, Heading, Button, VStack, HStack, Icon } from "@chakra-ui/react";
import { Plus } from "lucide-react";
import { LoadingSpinner, EmptyState, ErrorAlert, DataCard, UserAvatar } from "../components/common";
import { useToastNotification } from "../hooks/useToastNotification";
import { useModal } from "../hooks/useModal";
import { useFetch } from "../hooks/useFetch";
import { formatDate } from "../utils/formatters";
import { getProjectRoleName, getProjectRoleColor } from "../utils/constants";
import { projectApi } from "../api/projectApi";

interface Project {
  id: string;
  name: string;
  createdAt: string;
  memberRole: number;
  owner: {
    firstName: string;
    lastName: string;
  };
}

export default function ProjectsPage() {
  const { showSuccess, showError } = useToastNotification();
  const createModal = useModal();
  const { data: projects, loading, error, execute } = useFetch<Project[]>();

  useEffect(() => {
    execute(() => projectApi.getProjects());
  }, []);

  const handleCreate = async () => {
    try {
      await projectApi.createProject(/* ... */);
      showSuccess("Projekt utworzony");
      execute(() => projectApi.getProjects());
      createModal.onClose();
    } catch (error) {
      showError("Błąd", "Nie udało się utworzyć projektu");
    }
  };

  if (loading) return <LoadingSpinner fullScreen />;
  if (error) return <ErrorAlert description={error} />;
  
  if (!projects || projects.length === 0) {
    return (
      <EmptyState 
        title="Brak projektów"
        description="Stwórz swój pierwszy projekt aby rozpocząć"
        action={
          <Button 
            leftIcon={<Icon as={Plus} />}
            colorScheme="blue"
            onClick={createModal.onOpen}
          >
            Utwórz projekt
          </Button>
        }
      />
    );
  }

  return (
    <Box p={6}>
      <HStack justify="space-between" mb={6}>
        <Heading>Projekty</Heading>
        <Button 
          leftIcon={<Icon as={Plus} />}
          colorScheme="blue"
          onClick={createModal.onOpen}
        >
          Nowy projekt
        </Button>
      </HStack>

      <VStack spacing={3} align="stretch">
        {projects.map(project => (
          <DataCard key={project.id} hoverable p={4}>
            <HStack justify="space-between">
              <VStack align="flex-start" spacing={1}>
                <Heading size="sm">{project.name}</Heading>
                <Text fontSize="sm" color="gray.600">
                  Utworzono: {formatDate(project.createdAt, false)}
                </Text>
              </VStack>
              
              <HStack>
                <UserAvatar 
                  firstName={project.owner.firstName}
                  lastName={project.owner.lastName}
                />
                <Badge colorScheme={getProjectRoleColor(project.memberRole)}>
                  {getProjectRoleName(project.memberRole)}
                </Badge>
              </HStack>
            </HStack>
          </DataCard>
        ))}
      </VStack>

      {/* Create Modal */}
      <Modal isOpen={createModal.isOpen} onClose={createModal.onClose}>
        {/* ... */}
      </Modal>
    </Box>
  );
}
```

---

## Migracja istniejących komponentów

### Przed:
```tsx
import { useToast, Spinner, Box } from "@chakra-ui/react";

const toast = useToast();
const [loading, setLoading] = useState(false);

// Loading
if (loading) {
  return (
    <Box textAlign="center" py={10}>
      <Spinner size="xl" color="blue.500" />
    </Box>
  );
}

// Toast
toast({
  title: "Sukces",
  status: "success",
  duration: 3000,
  isClosable: true,
});
```

### Po:
```tsx
import { LoadingSpinner } from "../components/common";
import { useToastNotification } from "../hooks/useToastNotification";

const { showSuccess } = useToastNotification();
const [loading, setLoading] = useState(false);

// Loading
if (loading) {
  return <LoadingSpinner />;
}

// Toast
showSuccess("Sukces");
```
