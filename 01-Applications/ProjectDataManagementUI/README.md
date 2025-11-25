# Project Data Management UI

## 📋 Opis

Nowoczesna aplikacja React SPA (Single Page Application) do zarządzania danymi projektów. Aplikacja wykorzystuje TypeScript, React Router, Chakra UI oraz komunikuje się z backendem API poprzez cookie-based authentication.

## 🏗 Architektura

### Struktura katalogów

```
src/
├── api/              # Wywołania API (fetch)
├── components/       # Komponenty UI wielokrotnego użytku
├── context/          # Contexty React (AuthContext)
├── hooks/            # Własne hooki (useAuth)
├── layout/           # Layouty stron (MainLayout)
├── pages/            # Strony aplikacji
├── routes/           # Routing i guardy (ProtectedRoute, PublicRoute)
├── services/         # Logika biznesowa i serwisy
├── types/            # TypeScript types i interfaces
└── utils/            # Funkcje pomocnicze
```

### Warstwa komunikacji

```
Komponent → Hook (useAuth) → Serwis → API → Backend
```

**Przykład:**
```
Login.tsx → useAuth() → authService.loginUser() → authApi.login() → /api/User/login
```

## 🔐 Autentykacja

Aplikacja używa **cookie-based authentication** z ciasteczkami HttpOnly.

### Przepływ logowania:
1. Użytkownik wypełnia formularz logowania
2. `Login.tsx` wywołuje `useAuth().login()`
3. `AuthContext.login()` używa `authService.loginUser()`
4. `authService` wywołuje `authApi.login()`
5. Backend zwraca ciasteczko sesyjne
6. `AuthContext` pobiera profil użytkownika i aktualizuje stan

### Resetowanie hasła:
1. **Request** (`/forgot-password`)
   - Użytkownik podaje email
   - Backend wysyła email z linkiem + tokenem
   - Link: `/reset-password?token=xxx`

2. **Reset** (`/reset-password`)
   - Użytkownik klika link lub wpisuje token
   - Wprowadza nowe hasło
   - Backend weryfikuje token i zmienia hasło

### Ochrona tras:
- `ProtectedRoute` — wymaga autoryzacji
- `PublicRoute` — dostępne tylko dla niezalogowanych

## 📦 Technologie

| Biblioteka | Wersja | Cel |
|-----------|--------|-----|
| React | 18.2.0 | Framework UI |
| TypeScript | ~5.9.3 | Typowanie |
| React Router | ^7.9.5 | Routing |
| Chakra UI | 2.10.9 | Biblioteka komponentów |
| Axios | ^1.13.2 | Klient HTTP (backup) |
| Vite | ^7.2.2 | Build tool |

## 🚀 Uruchomienie

### Instalacja zależności
```bash
npm install
```

### Uruchomienie dev serwera
```bash
npm run dev
```

### Build produkcyjny
```bash
npm run build
```

### Preview buildu
```bash
npm run preview
```

## 📝 Konwencje kodowania

### Komponenty
- Małe, jednozadaniowe komponenty
- Oddzielenie UI od logiki biznesowej
- Używaj własnych hooków dla logiki (np. `useAuth`)

### Typy
- **Zawsze** definiuj typy dla:
  - Propsów komponentów
  - Requestów/response API
  - Stanów komponentów
- **Unikaj** `any`, `as unknown as`

### API
- Wszystkie wywołania API w katalogu `api/`
- Logika biznesowa w `services/`
- Typowanie requestów/response w `types/`

### Obsługa błędów
- **Zawsze loguj błędy w catch**: `console.error("Kontekst:", error)`
- Nie zjadaj wyjątków: `catch {}`
- Używaj `handleApiError` dla response API

### Nazewnictwo
- Komponenty: `PascalCase` (np. `UserProfile.tsx`)
- Hooki: `use` prefix (np. `useAuth.ts`)
- Serwisy: `camelCase` + `Service` suffix (np. `authService.ts`)
- Typy: `PascalCase` + typ suffix (np. `UserProfile`, `LoginRequest`)

## 🔄 Przepływ danych

### Login
```
Login.tsx
  → useAuth().login()
    → authService.loginUser()
      → authApi.login() [POST /api/User/login]
        → Backend ustawia cookie
    → authService.getUserProfile()
      → authApi.getProfile() [GET /api/User/me]
        → Pobiera dane użytkownika
  → AuthContext aktualizuje stan
  → Przekierowanie na Dashboard
```

### Logout
```
Sidebar.tsx
  → useAuth().logout()
    → authApi.logout() [POST /api/User/logout]
      → Backend kasuje cookie
    → AuthContext czyści stan
  → navigate("/login")
```

## ⚠️ Najczęstsze błędy

### ❌ Bezpośrednie `fetch` w komponentach
```tsx
// ZŁE
const handleLogin = async () => {
  const res = await fetch("/api/User/login", {...});
};
```

```tsx
// DOBRE
const handleLogin = async () => {
  const success = await useAuth().login(email, password);
};
```

### ❌ Używanie `any`
```tsx
// ZŁE
const data: any = await res.json();
```

```tsx
// DOBRE
const data: UserProfile = await res.json();
```

### ❌ Zjadanie błędów
```tsx
// ZŁE
try {
  await login();
} catch {}
```

```tsx
// DOBRE
try {
  await login();
} catch (error) {
  console.error("Błąd logowania:", error);
}
```

## 📚 Przydatne linki

- [React Documentation](https://react.dev/)
- [TypeScript Handbook](https://www.typescriptlang.org/docs/)
- [Chakra UI](https://chakra-ui.com/)
- [React Router](https://reactrouter.com/)
- [Vite](https://vitejs.dev/)

## 🤝 Wkład

Przed wprowadzeniem zmian:
1. Przeczytaj `.github/copilot-instructions.md`
2. Upewnij się, że kod spełnia konwencje projektu
3. Dodaj odpowiednie typy TypeScript
4. Przetestuj lokalnie

---

**Autor:** Team PDM  
**Licencja:** MIT
