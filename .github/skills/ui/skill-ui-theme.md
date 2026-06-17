# Skill: UI / Theme i design tokens

## Opis
Używanie palet kolorów, spacing i typografii z Chakra UI i appColors.

## Kiedy używać
Użyj tego skilla gdy stosujesz kolory, spacing lub dodajesz nowy token designu.

---

## Jedyne źródło prawdy dla kolorów

```typescript
// src/theme/tokens/colors.ts — nie modyfikuj bezpośrednio
import { appColors } from '@/theme/tokens/colors';
```

## Palety kolorów (zarejestrowane w theme.ts)

| Paleta | Znaczenie | Kiedy używać |
|--------|-----------|-------------|
| `primary` | niebieski | nagłówki, CTA, aktywne stany, focus ring |
| `level1` | zielony | kosztorys, sukces, Dodaj |
| `level2` | fioletowy | opcje, sumowania, obliczenia |
| `action` | teal | akcje drugorzędne, zapis, udostępnij |

## Użycie kolorów w komponentach

```tsx
// Przez Chakra token (preferuj)
<Box bg="primary.600" color="white">
<Badge colorScheme="level1">
<Button colorScheme="action">

// Przez import (gdy poza Chakra)
import { appColors } from '@/theme/tokens/colors';
const color = appColors.primary[600];
```

## Zakaz hardkodowania kolorów

```tsx
// ZAKAZANE
<Box bg="#2B6CB0" color="#276749">
<Box style={{ backgroundColor: 'blue' }}>

// POPRAWNIE
<Box bg="primary.600" color="level1.700">
```

## Spacing i sizing — przez Chakra tokens

```tsx
// ZAKAZANE
<Box style={{ margin: '16px', padding: '8px' }}>
<Text style={{ fontSize: '14px' }}>

// POPRAWNIE
<Box mt={4} p={2}>
<Text fontSize="sm">

// Chakra spacing scale: 1 = 4px
// 1=4px, 2=8px, 3=12px, 4=16px, 5=20px, 6=24px, 8=32px, 10=40px, 12=48px
```

## Rozmiary czcionek

```tsx
<Text fontSize="xs">    // 12px
<Text fontSize="sm">    // 14px
<Text fontSize="md">    // 16px (domyślny)
<Text fontSize="lg">    // 18px
<Text fontSize="xl">    // 20px
<Text fontSize="2xl">   // 24px
```

## Typowe wzorce UI

```tsx
// Karta
<Box
    bg="white"
    borderRadius="lg"
    border="1px solid"
    borderColor="gray.200"
    p={4}
    shadow="sm"
>

// Badge statusu
<Badge colorScheme="level1" px={2} py={1} borderRadius="full" fontSize="xs">
    Aktywny
</Badge>

// Sekcja z nagłówkiem
<Box mb={6}>
    <Text fontSize="lg" fontWeight="semibold" color="gray.800" mb={3}>
        Tytuł sekcji
    </Text>
    {/* zawartość */}
</Box>

// Wiersz akcji
<HStack spacing={2} justify="flex-end">
    <Button variant="ghost" onClick={onCancel}>Anuluj</Button>
    <Button colorScheme="primary" onClick={onSave} isLoading={isLoading}>
        Zapisz
    </Button>
</HStack>
```

## Dodawanie nowego tokenu koloru

```typescript
// src/theme/tokens/colors.ts — dodaj do istniejącej struktury
// src/theme/theme.ts — zarejestruj paletę w extendTheme({ colors: { ... } })
```

## Zasady

- Zakaz `style={{}}` inline — zawsze Chakra props
- Zakaz hardkodowanych hex — zawsze token
- Spacing przez liczby (`mt={4}`), nie przez `px` stringi
- `colorScheme` zamiast ręcznego `bg` + `color` dla przycisków/badge
- Ciemniejsze odcienie (600-800) dla tekstu, jaśniejsze (50-200) dla tła
