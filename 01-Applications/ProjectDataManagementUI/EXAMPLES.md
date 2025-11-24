# 📚 Przykłady użycia

## 🔐 Autentykacja

### Użycie hooka useAuth w komponencie

```tsx
import { useAuth } from "../hooks/useAuth";

export default function MyComponent() {
  const { user, isAuthenticated, loading, login, logout } = useAuth();

  if (loading) return <Spinner />;

  if (!isAuthenticated) {
    return <Text>Zaloguj się aby kontynuować</Text>;
  }

  return (
    <Box>
      <Text>Witaj, {user?.firstName}!</Text>
      <Button onClick={logout}>Wyloguj</Button>
    </Box>
  );
}
```

### Resetowanie hasła

#### 1. Request resetowania (ForgotPassword.tsx)

```tsx
import { requestPasswordReset } from "../services/authService";

const handleSubmit = async (email: string) => {
  const success = await requestPasswordReset(email);
  
  if (success) {
    toast({ title: "Email wysłany", status: "success" });
  }
};
```

#### 2. Reset hasła z tokenem (ResetPassword.tsx)

```tsx
import { resetPassword } from "../services/authService";
import { useSearchParams } from "react-router-dom";

const [searchParams] = useSearchParams();
const token = searchParams.get("token"); // z URL

const handleSubmit = async (password: string) => {
  const success = await resetPassword(token, password);
  
  if (success) {
    toast({ title: "Hasło zmienione", status: "success" });
    navigate("/login");
  }
};
```

#### 3. Link z emaila

Email zawiera link w formacie:
```
http://localhost:5173/reset-password?token=ABC123XYZ
```

Token jest automatycznie wczytywany z query params.

## 🌐 Wywołania API

### Tworzenie nowego endpointu

#### 1. Dodaj typ w `types/`

```typescript
// types/project.types.ts
export interface Project {
  id: string;
  name: string;
  description: string;
  createdAt: string;
}

export interface CreateProjectRequest {
  name: string;
  description: string;
}
```

#### 2. Dodaj API w `api/`

```typescript
// api/projectApi.ts
import type { Project, CreateProjectRequest } from "../types/project.types";

const API_URL = "/api/Project";

export const projectApi = {
  getAll: async (): Promise<Response> => {
    return fetch(`${API_URL}`, {
      method: "GET",
      credentials: "include",
    });
  },

  create: async (data: CreateProjectRequest): Promise<Response> => {
    return fetch(`${API_URL}`, {
      method: "POST",
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(data),
    });
  },

  delete: async (id: string): Promise<Response> => {
    return fetch(`${API_URL}/${id}`, {
      method: "DELETE",
      credentials: "include",
    });
  },
};
```

#### 3. Dodaj serwis w `services/`

```typescript
// services/projectService.ts
import { projectApi } from "../api/projectApi";
import type { Project, CreateProjectRequest } from "../types/project.types";

export const getProjects = async (): Promise<Project[]> => {
  const res = await projectApi.getAll();
  
  if (!res.ok) {
    throw new Error("Błąd pobierania projektów");
  }
  
  return res.json();
};

export const createProject = async (data: CreateProjectRequest): Promise<Project> => {
  const res = await projectApi.create(data);
  
  if (!res.ok) {
    throw new Error("Błąd tworzenia projektu");
  }
  
  return res.json();
};

export const deleteProject = async (id: string): Promise<boolean> => {
  const res = await projectApi.delete(id);
  return res.ok;
};
```

#### 4. Użyj w komponencie

```tsx
// pages/Projects.tsx
import { useEffect, useState } from "react";
import { Box, Button, VStack, useToast } from "@chakra-ui/react";
import { getProjects, createProject } from "../services/projectService";
import type { Project } from "../types/project.types";

export default function Projects() {
  const [projects, setProjects] = useState<Project[]>([]);
  const [loading, setLoading] = useState(true);
  const toast = useToast();

  useEffect(() => {
    loadProjects();
  }, []);

  const loadProjects = async () => {
    try {
      const data = await getProjects();
      setProjects(data);
    } catch (error) {
      console.error("Błąd ładowania projektów:", error);
      toast({
        title: "Błąd ładowania projektów",
        status: "error",
        duration: 3000,
      });
    } finally {
      setLoading(false);
    }
  };

  const handleCreate = async () => {
    try {
      const newProject = await createProject({
        name: "Nowy projekt",
        description: "Opis",
      });
      
      setProjects([...projects, newProject]);
      
      toast({
        title: "Projekt utworzony",
        status: "success",
        duration: 3000,
      });
    } catch (error) {
      console.error("Błąd tworzenia projektu:", error);
      toast({
        title: "Błąd tworzenia projektu",
        status: "error",
        duration: 3000,
      });
    }
  };

  if (loading) return <Spinner />;

  return (
    <VStack>
      <Button onClick={handleCreate}>Dodaj projekt</Button>
      {projects.map((project) => (
        <Box key={project.id}>{project.name}</Box>
      ))}
    </VStack>
  );
}
```

## 🎨 Własne hooki

### Hook do zarządzania formularzami

```typescript
// hooks/useForm.ts
import { useState } from "react";

export const useForm = <T extends Record<string, any>>(initialValues: T) => {
  const [values, setValues] = useState<T>(initialValues);
  const [errors, setErrors] = useState<Partial<Record<keyof T, string>>>({});

  const handleChange = (name: keyof T, value: any) => {
    setValues({ ...values, [name]: value });
    setErrors({ ...errors, [name]: undefined });
  };

  const validate = (rules: Partial<Record<keyof T, (value: any) => string | undefined>>) => {
    const newErrors: Partial<Record<keyof T, string>> = {};
    
    for (const key in rules) {
      const error = rules[key]?.(values[key]);
      if (error) newErrors[key] = error;
    }
    
    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const reset = () => {
    setValues(initialValues);
    setErrors({});
  };

  return { values, errors, handleChange, validate, reset };
};
```

### Użycie w komponencie

```tsx
import { useForm } from "../hooks/useForm";

export default function CreateProjectForm() {
  const { values, errors, handleChange, validate, reset } = useForm({
    name: "",
    description: "",
  });

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    const isValid = validate({
      name: (v) => (!v ? "Nazwa jest wymagana" : undefined),
      description: (v) => (v.length < 10 ? "Minimum 10 znaków" : undefined),
    });

    if (!isValid) return;

    try {
      await createProject(values);
      reset();
      toast({ title: "Projekt utworzony", status: "success" });
    } catch (error) {
      console.error("Błąd:", error);
    }
  };

  return (
    <form onSubmit={handleSubmit}>
      <FormControl isInvalid={!!errors.name}>
        <FormLabel>Nazwa</FormLabel>
        <Input
          value={values.name}
          onChange={(e) => handleChange("name", e.target.value)}
        />
        <FormErrorMessage>{errors.name}</FormErrorMessage>
      </FormControl>

      <FormControl isInvalid={!!errors.description}>
        <FormLabel>Opis</FormLabel>
        <Textarea
          value={values.description}
          onChange={(e) => handleChange("description", e.target.value)}
        />
        <FormErrorMessage>{errors.description}</FormErrorMessage>
      </FormControl>

      <Button type="submit">Utwórz</Button>
    </form>
  );
}
```

## 🛡️ Protected routes z uprawnieniami

```tsx
// routes/RoleProtectedRoute.tsx
import { ReactNode } from "react";
import { Navigate } from "react-router-dom";
import { useAuth } from "../hooks/useAuth";

interface Props {
  children: ReactNode;
  requiredRole: string;
}

export default function RoleProtectedRoute({ children, requiredRole }: Props) {
  const { user, loading } = useAuth();

  if (loading) return <Spinner />;

  if (!user) {
    return <Navigate to="/login" replace />;
  }

  if (user.role !== requiredRole) {
    return <Navigate to="/unauthorized" replace />;
  }

  return <>{children}</>;
}
```

## 🎯 Best practices

### ✅ Dobre praktyki

```tsx
// ✅ Centralizacja logiki w serwisach
const data = await getUserProfile();

// ✅ Właściwe typowanie
const [user, setUser] = useState<UserProfile | null>(null);

// ✅ Obsługa błędów z logowaniem
try {
  await login(email, password);
} catch (error) {
  console.error("Błąd logowania:", error);
  toast({ title: "Błąd", status: "error" });
}

// ✅ Używanie własnych hooków
const { isAuthenticated } = useAuth();

// ✅ Separacja concerns
// Komponent zajmuje się tylko UI
// Hook/serwis zajmuje się logiką
```

### ❌ Złe praktyki

```tsx
// ❌ Bezpośrednie fetch w komponencie
const res = await fetch("/api/User/me");

// ❌ Typowanie any
const data: any = await res.json();

// ❌ Zjadanie błędów
try {
  await login();
} catch {} // ❌ brak obsługi

// ❌ Duplikacja logiki
// Ten sam kod w wielu komponentach zamiast hooka

// ❌ Komponent robi wszystko
// Logika, UI, wywołania API w jednym miejscu
```
