# Skill: UI / Formularze i modale

## Opis
Budowanie formularzy i modali z użyciem AppModal, DeleteAlertDialog i Chakra UI FormControl.

## Kiedy używać
Użyj tego skilla gdy tworzysz modal, formularz lub dialog potwierdzenia.

---

## AppModal — zawsze używaj gotowego komponentu

```tsx
// src/components/ui/AppModal.tsx — gotowy komponent projektu
import AppModal from '../components/ui/AppModal';
import { useModal } from '../hooks/useModal';

function ProjectActionsPanel(): React.ReactElement {
    const createModal = useModal();
    const [name, setName] = useState('');
    const [isLoading, setIsLoading] = useState(false);

    const handleCreate = async (): Promise<void> => {
        setIsLoading(true);
        try {
            await projectApi.create(tenantId, { name });
            createModal.onClose();
            setName('');
        } catch {
            // obsługa błędu
        } finally {
            setIsLoading(false);
        }
    };

    return (
        <>
            <Button onClick={createModal.onOpen}>Nowy projekt</Button>

            <AppModal
                isOpen={createModal.isOpen}
                onClose={createModal.onClose}
                title="Utwórz projekt"
                actionLabel="Utwórz"
                actionColorScheme="green"
                onAction={handleCreate}
                isActionLoading={isLoading}
            >
                <FormControl isRequired>
                    <FormLabel>Nazwa projektu</FormLabel>
                    <Input
                        value={name}
                        onChange={(e) => setName(e.target.value)}
                        placeholder="Wpisz nazwę..."
                    />
                </FormControl>
            </AppModal>
        </>
    );
}
```

## DeleteAlertDialog — potwierdzenie usunięcia

```tsx
import { DeleteAlertDialog } from '../components/ui/DeleteAlertDialog';

const deleteDialog = useModal();

<DeleteAlertDialog
    isOpen={deleteDialog.isOpen}
    onClose={deleteDialog.onClose}
    onConfirm={handleDelete}
    title="Usuń projekt"
    description="Tej operacji nie można cofnąć."
    isLoading={isDeleting}
/>
```

## Pola formularza — Chakra UI FormControl

```tsx
<FormControl isRequired isInvalid={!!error}>
    <FormLabel>Nazwa</FormLabel>
    <Input
        value={value}
        onChange={(e) => setValue(e.target.value)}
        placeholder="Wpisz nazwę..."
    />
    {error && <FormErrorMessage>{error}</FormErrorMessage>}
</FormControl>

<FormControl>
    <FormLabel>Opis</FormLabel>
    <Textarea
        value={description}
        onChange={(e) => setDescription(e.target.value)}
        placeholder="Opcjonalny opis..."
        rows={3}
    />
</FormControl>

<FormControl>
    <FormLabel>Data</FormLabel>
    <Input
        type="date"
        value={date}
        onChange={(e) => setDate(e.target.value)}
    />
</FormControl>

<FormControl>
    <FormLabel>Kwota netto</FormLabel>
    <NumberInput
        value={net ?? ''}
        onChange={(_, value) => setNet(isNaN(value) ? null : value)}
        min={0}
        precision={2}
    >
        <NumberInputField />
    </NumberInput>
</FormControl>
```

## Reset formularza przy zamknięciu

```tsx
const handleClose = (): void => {
    setName('');
    setDescription('');
    setError(null);
    onClose();
};

<AppModal onClose={handleClose} ...>
```

## Walidacja lokalna (prosta)

```tsx
const [nameError, setNameError] = useState<string | null>(null);

const validate = (): boolean => {
    if (!name.trim()) {
        setNameError('Nazwa jest wymagana');
        return false;
    }
    if (name.length > 200) {
        setNameError('Nazwa nie może przekraczać 200 znaków');
        return false;
    }
    setNameError(null);
    return true;
};

const handleSubmit = async (): Promise<void> => {
    if (!validate()) {
        return;
    }
    // ...
};
```

## Zasady

- Zawsze używaj `AppModal` — zakaz tworzenia własnych modali
- Zawsze używaj `DeleteAlertDialog` dla potwierdzeń usunięcia
- Reset stanu formularza przy `onClose`
- `isLoading` podczas akcji async — przekaż do `AppModal.isActionLoading`
- Walidacja lokalna dla prostych przypadków, walidacja API dla reguł biznesowych
- `FormControl` z `isRequired` i `isInvalid` dla czytelności
- Zakaz `form` elementów — używaj przycisków z `onClick`
