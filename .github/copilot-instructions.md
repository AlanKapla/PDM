# 🧭 Kontekst projektu

Nowoczesna aplikacja webowa:

| Warstwa | Technologia |
|--------|-------------|
| Backend | API (np. .NET / Node) |
| Frontend | React SPA (preferowany TypeScript) |

Cele projektu: **czytelny, prosty i łatwy w utrzymaniu kod**  
Priorytety: **architektura • bezpieczeństwo • testowalność • spójność**

**Zasady komentarzy Copilota:** pisz **po polsku**, podawaj **powód**, proponuj **gotowe poprawki (`suggested change`)**.

---

# 🔍 Ogólne praktyki programistyczne

### ✔ Czytelność ponad “spryt”
- preferuj prosty, jednoznaczny kod
- zgłaszaj nadmiernie złożone warunki i długie metody

### ✔ Nazewnictwo
- nazwy mają wskazywać **co to jest** i **po co istnieje**
- unikaj skrótów typu `svc`, `mgr`, `obj`

### ✔ DRY
- reaguj na duplikację logiki
- nie proponuj nadmiernej abstrakcji przy małej skali powtórzeń

### ✔ Obsługa błędów
- zgłaszaj `catch` bez logowania
- unikaj zjadania wyjątków

### ✔ Komentarze
- dobre komentarze tłumaczą **dlaczego**, a nie **co**
- jeśli komentarz wyjaśnia **co robi kod**, preferuj uproszczenie kodu

---

# 🏗 Architektura API (backend)

### Warstwy
`Controller → Command/Query → Serwisy → Repozytoria`  
⚠ zgłaszaj logikę biznesową w kontrolerach i w repozytoriach

### Kontrakty API
- RESTful nazewnictwo endpointów
- spójne statusy HTTP (200/201/204/400/401/403/404/422/500)  
⚠ zwracanie `200` z tekstem błędu — do poprawy

### Modele / DTO
- API zwraca **web modele**, nie encje bazy  
⚠ zgłaszaj eksport encji bazodanowych do JSON

### Walidacja i async
- walidacja na wejściu do API
- operacje IO powinny używać `async/await`  
⚠ zgłaszaj `.Result`, `.Wait()`, blokujące wywołania

---

# 💻 Architektura frontendu (React)

### Komponenty
- małe, jednozadaniowe komponenty
- oddziel UI od logiki i pobierania danych  
⚠ komponent robiący „wszystko naraz” — do podziału

### Stan i hooki
- promuj własne hooki (`useUser`, `useOrders`, …)
⚠ zbyt duży `useEffect` — do rozbicia

### API po stronie frontu
- wywołania API **centralnie** (np. `apiClient`, `services`)
⚠ rozproszone `fetch/axios` w komponentach

### TypeScript
- jawne typowanie modeli i propsów
⚠ nadużywanie `any` lub `as unknown as`

---

# 🔗 Komunikacja API ↔ Frontend

- typy API i frontu muszą być spójne (`UserDto` → `User`)
- zawsze obsługuj: `loading`, `error`, `null/undefined`  
⚠ zakładanie, że dane zawsze istnieją (`data!`) jest błędem

---

# 🔐 Bezpieczeństwo

| Obszar | Sprawdzaj |
|--------|----------|
| Wejście | walidacja danych, unikanie injection |
| Autoryzacja | modyfikujące endpointy muszą wymagać uprawnień |
| Dane wrażliwe | nie logować haseł/tokenów, nie eksponować pól technicznych |

---

# ⚙ CQRS — Commands • Queries • Web modele

### Commands → **zmieniają stan**
- powinny zwracać prosty wynik (`Id` / `Success`)
- mogą odczytywać dane jedynie w celu **walidacji lub logiki biznesowej**
⚠ rozbudowane projekcje danych pod UI w Command → przenieść do Query

### Queries → **odczyt danych**
- wyłącznie **read-only**
- zwracają **web modele dopasowane do potrzeb ekranu/API**
⚠ jakakolwiek zmiana stanu w Query = błąd

### Web modele (DTO / ViewModel)
- reprezentują **kontrakt API**
- mogą różnić się od modeli domenowych  
⚠ web model nie powinien zawierać pól technicznych ani poufnych

### Serwisy / helpery
- serwisy = logika domenowa / integracje
- helpery = prostsze operacje techniczne  
⚠ „god-service” z wieloma odpowiedzialnościami → do podziału

---

# 🔄 Poprawny przepływ żądania

