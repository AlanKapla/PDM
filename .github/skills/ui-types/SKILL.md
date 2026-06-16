---
name: ui-types
description: "Definiowanie typów TypeScript dla odpowiedzi API, props komponentów i wyników hooków. Użyj gdy tworzysz lub modyfikujesz typy TypeScript (*types.ts)."
---

# Skill: UI / Typy TypeScript

## Opis
Definiowanie typów TypeScript dla odpowiedzi API, props komponentów i wyników hooków.

## Kiedy używać
Użyj tego skilla gdy tworzysz lub modyfikujesz typy TypeScript (*types.ts).

---

## Lokalizacja

```
src/types/{domain}.types.ts           ← typy globalne per domena API
src/features/{domain}/types/          ← typy domenowe feature
```

## Typy odpowiedzi API

```typescript
// src/types/project.types.ts
export interface ProjectDetailsWeb {
    id: string;
    tenantId: string;
    name: string;
    isActive: boolean;
    createdAt: string;
    createdByUserId: string;
    userRoleCode: string;
    membersCount: number;
    userPermissions: string[];
}

export interface ProjectMemberWeb {
    userId: string;
    email: string;
    firstName: string;
    lastName: string;
    roleCode: string;
    joinedAt: string;
}
```

## Enum jako const object

```typescript
// src/constants/costStatus.ts
export const FinancialStatus = {
    NoBudget: 0,
    NoCosts: 1,
    InProgress: 2,
    NearLimit: 3,
    OverBudget: 4,
} as const;

export type FinancialStatus = typeof FinancialStatus[keyof typeof FinancialStatus];
```

## Request typy (body do API)

```typescript
export interface CreateProjectRequest {
    name: string;
    description?: string;
}

export interface UpdateProjectRequest {
    name: string;
    description?: string;
}
```

## Typy dla hooków

```typescript
export interface UseProjectDetailsResult {
    data: ProjectDetailsWeb | null;
    isLoading: boolean;
    error: string | null;
    refetch: () => void;
}
```

## Utility types

```typescript
// Opcjonalne pole
type PartialProject = Partial<ProjectDetailsWeb>;

// Wymagane pole
type RequiredName = Required<Pick<ProjectDetailsWeb, 'name'>>;

// Nullable
type NullableId = string | null;

// ID jako branded type (opcjonalnie)
type ProjectId = string & { readonly brand: 'ProjectId' };
```

## Zasady

- Zakaz `any` — zawsze explicit type
- Zakaz `object` jako typ — definiuj interfejs
- Interfejsy nazwy z sufiksem: `*Web` (odpowiedź API), `*Props` (props komponentu), `*Result` (wynik hooka), `*Request` (body do API)
- `string` dla wszystkich GUID (nie `Guid` — to C#)
- `string` dla dat, nie `Date` (konwersja przy użyciu)
- Optional fields przez `?` nie przez `| undefined`
- Export wszystkich publicznych typów
