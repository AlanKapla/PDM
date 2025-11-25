# 🔐 Password Reset Flow - Dokumentacja

## Przegląd

Aplikacja obsługuje pełen flow resetowania hasła za pomocą tokenów wysyłanych emailem.

---

## 📋 Endpointy API

### 1. Request resetowania hasła
```
POST /api/User/reset-password-request
```

**Body:**
```json
{
  "email": "user@example.com"
}
```

**Response:**
- `200 OK` — email wysłany (lub konto nie istnieje, ale nie informujemy o tym)
- `400 Bad Request` — błąd walidacji

**Email zawiera:**
- Link: `http://frontend/reset-password?token=XXX`
- Token do ręcznego wpisania
- Data wygaśnięcia tokenu

---

### 2. Reset hasła
```
POST /api/User/reset-password
```

**Body:**
```json
{
  "token": "XXX",
  "password": "newpassword123"
}
```

**Response:**
- `200 OK` — hasło zmienione
- `400 Bad Request` — token nieprawidłowy/wygasły

---

## 🎨 UI Flow

### 1. Strona logowania (`/login`)

```
┌─────────────────────────┐
│   Logowanie             │
├─────────────────────────┤
│ Email: [_____________]  │
│ Hasło: [_____________]  │
│                         │
│ [  Zaloguj się  ]       │
│                         │
│ Nie pamiętam hasła ←─┐  │
│ Utwórz konto          │  │
└─────────────────────────┘  │
                             │
                             ↓
```

---

### 2. Forgot Password (`/forgot-password`)

**Krok 1: Formularz**
```
┌─────────────────────────┐
│  Resetowanie hasła      │
├─────────────────────────┤
│ Podaj email przypisany  │
│ do konta.               │
│                         │
│ Email: [_____________]  │
│                         │
│ [Wyślij link resetujący]│
│                         │
│ Powrót do logowania     │
└─────────────────────────┘
```

**Krok 2: Potwierdzenie**
```
┌─────────────────────────┐
│    Email wysłany        │
├─────────────────────────┤
│ Jeśli konto istnieje,   │
│ otrzymasz wiadomość z   │
│ linkiem resetującym.    │
│                         │
│ Sprawdź folder spam.    │
│                         │
│ [Powrót do logowania]   │
└─────────────────────────┘
```

---

### 3. Email z backendu

```html
Subject: Resetowanie hasła

Jeśli to Ty zażądałeś resetowania hasła, kliknij link:

[Reset Password] ← Link: /reset-password?token=ABC123

Lub użyj tokenu ręcznie:
Token: ABC123
Wygasa: 2025-11-24 15:30:00 UTC

Jeśli nie prosiłeś o reset, zignoruj tę wiadomość.
```

---

### 4. Reset Password (`/reset-password`)

**Po kliknięciu linku (token automatycznie wczytany):**
```
┌─────────────────────────┐
│  Ustaw nowe hasło       │
├─────────────────────────┤
│ Token: [ABC123_______]  │
│ (automatycznie wczytany)│
│                         │
│ Nowe hasło:             │
│ [_____________________] │
│                         │
│ Potwierdź hasło:        │
│ [_____________________] │
│                         │
│ [   Zmień hasło   ]     │
│                         │
│ Powrót do logowania     │
└─────────────────────────┘
```

**Po sukcesie:**
- Toast: "Hasło zmienione"
- Redirect: `/login`

---

## 🔧 Implementacja

### 1. Typy TypeScript

```typescript
// types/auth.types.ts

export interface PasswordResetRequest {
  email: string;
}

export interface ResetPasswordRequest {
  token: string;
  password: string;
}
```

---

### 2. API Client

```typescript
// api/authApi.ts

export const authApi = {
  requestPasswordReset: async (data: PasswordResetRequest) => {
    return fetch(`/api/User/reset-password-request`, {
      method: "POST",
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(data),
    });
  },

  resetPassword: async (data: ResetPasswordRequest) => {
    return fetch(`/api/User/reset-password`, {
      method: "POST",
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(data),
    });
  },
};
```

---

### 3. Serwisy

```typescript
// services/authService.ts

export const requestPasswordReset = async (email: string): Promise<boolean> => {
  const res = await authApi.requestPasswordReset({ email });
  return res.ok;
};

export const resetPassword = async (
  token: string, 
  password: string
): Promise<boolean> => {
  const res = await authApi.resetPassword({ token, password });
  return res.ok;
};
```

---

### 4. Komponenty

#### ForgotPassword.tsx
```tsx
const handleSubmit = async (email: string) => {
  const success = await requestPasswordReset(email);
  
  if (success) {
    setSent(true); // Pokazuje ekran potwierdzenia
  }
};
```

#### ResetPassword.tsx
```tsx
const [searchParams] = useSearchParams();
const token = searchParams.get("token"); // Automatyczne wczytanie

const handleSubmit = async (password: string) => {
  const success = await resetPassword(token, password);
  
  if (success) {
    navigate("/login");
  }
};
```

---

## 🚀 Routing

```tsx
// routes/AppRouter.tsx

<Routes>
  <Route path="/forgot-password" element={
    <PublicRoute>
      <ForgotPassword />
    </PublicRoute>
  } />
  
  <Route path="/reset-password" element={
    <PublicRoute>
      <ResetPassword />
    </PublicRoute>
  } />
</Routes>
```

---

## ✅ Walidacja

### Frontend
- Email: format `xxx@yyy.zzz`
- Hasło: minimum 6 znaków
- Potwierdzenie hasła: musi być identyczne
- Token: nie może być pusty

### Backend
- Token: weryfikacja sygnatury i expiry
- Hasło: zgodnie z polityką bezpieczeństwa

---

## 🛡️ Bezpieczeństwo

### ✅ Dobre praktyki zastosowane:
1. **Nie ujawniamy czy email istnieje** — zawsze zwracamy sukces
2. **Token jednokrotnego użytku** — po użyciu jest nieważny
3. **Expiry** — token wygasa po określonym czasie
4. **HTTPS** — cały flow przez szyfrowane połączenie
5. **Walidacja po stronie backendu** — frontend to tylko UI

### ⚠️ Uwagi:
- Token w URL może zostać zapisany w historii przeglądarki
- Token może być przechwycony jeśli email jest niezabezpieczony
- Zawsze używaj HTTPS w produkcji

---

## 📊 User Journey

```
┌─────────────┐
│   /login    │
│             │
│ "Nie pamię-│
│  tam hasła" │
└──────┬──────┘
       │
       ↓
┌──────────────────┐
│ /forgot-password │
│                  │
│ Wpisujesz email  │
└────────┬─────────┘
         │
         ↓
    ┌────────┐
    │ Email  │ ← Backend wysyła
    └────┬───┘
         │
         ↓
┌─────────────────────┐
│ /reset-password?... │
│                     │
│ Wpisujesz hasło     │
└──────────┬──────────┘
           │
           ↓
      ┌────────┐
      │ Sukces │
      └────┬───┘
           │
           ↓
      ┌────────┐
      │ /login │ ← Redirect
      └────────┘
```

---

## 🧪 Testowanie

### Ręczne testy:
1. ✅ Request z istniejącym emailem → email wysłany
2. ✅ Request z nieistniejącym emailem → "email wysłany" (nie ujawniamy)
3. ✅ Kliknięcie linku z emaila → token wczytany
4. ✅ Ręczne wpisanie tokenu → działa
5. ✅ Reset z prawidłowym tokenem → hasło zmienione
6. ✅ Reset z wygasłym tokenem → błąd
7. ✅ Reset z użytym tokenem → błąd
8. ✅ Walidacja hasła < 6 znaków → błąd
9. ✅ Różne hasła → błąd
10. ✅ Po sukcesie można zalogować nowym hasłem

---

## 📝 Przykłady

### Sukces flow
```
1. User: /login → "Nie pamiętam hasła"
2. User: /forgot-password → email: "user@example.com"
3. Backend: wysyła email z tokenem
4. User: klika link → /reset-password?token=ABC123
5. User: wpisuje hasło "newpass123"
6. Backend: weryfikuje token, zmienia hasło
7. Frontend: redirect → /login
8. User: loguje się nowym hasłem ✅
```

### Błąd — token wygasł
```
1. User: klika link po 24h
2. Frontend: /reset-password?token=EXPIRED
3. User: wpisuje hasło
4. Backend: 400 "Token wygasł"
5. Frontend: Toast "Token wygasł"
6. User: wraca do /forgot-password i prosi o nowy token
```

---

## 🔜 Możliwe rozszerzenia

1. **Rate limiting** — max 3 requesty/godzinę na email
2. **2FA** — dodatkowa weryfikacja
3. **Historia haseł** — nie pozwalaj na ostatnie 3 hasła
4. **Strength meter** — wskaźnik siły hasła
5. **Magic link** — logowanie bez hasła
6. **Captcha** — ochrona przed botami

---

**Status:** ✅ Gotowe do produkcji  
**Data:** 24.11.2025  
**Autor:** GitHub Copilot
