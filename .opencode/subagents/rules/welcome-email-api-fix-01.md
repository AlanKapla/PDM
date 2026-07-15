# Refactor — welcome-email-api-fix-01

**Cel:** Wysyłka maila powitalnego do nowo zarejestrowanych użytkowników oraz jednorazowa wysyłka do istniejących użytkowników w systemie.

---

## Audyt (stan obecny)

| Obszar | Stan |
|--------|------|
| Rejestracja użytkownika | Azure AD B2C → frontend `POST /api/user/sync-b2c` → `UserSyncFromB2CCommandHandler` tworzy użytkownika przy pierwszym logowaniu |
| Infrastruktura email | `IEmailSender` → `QueuedEmailSender` → kolejka → `EmailWorker` → `SmtpEmailSender` |
| Szablony HTML | `Business/Resources/EmailTemplates/*.html` + `EmailTemplateLoader` |
| Wzorce wysyłki | `InviteProjectMemberCommandHandler`, `InviteTenantMemberCommandHandler` |
| Mail powitalny | **Brak** |
| Śledzenie wysyłki | **Brak pola** na encji `User` |

---

## Plan implementacji

### Krok 1 — Encja i migracja

**Plik:** `src/Entities/Models/Users/User.cs`
- Dodaj pole: `public DateTime? WelcomeEmailSentAt { get; set; }`

**Plik:** `src/Entities/Configurations/UserConfiguration.cs`
- Brak specjalnej konfiguracji wymaganej (nullable DateTime jest OK domyślnie)

**Migracja EF Core:**
```powershell
cd 02-ApplicationServices/ProductDataManagementWebAPI/src/Entities
dotnet ef migrations add add-user-welcome-email-sent-at --startup-project ../WebApi
```

---

### Krok 2 — Szablon HTML maila powitalnego

**Nowy plik:** `src/Business/Resources/EmailTemplates/welcome-email.html`

Wzoruj się na `tenant-invitation.html` (Brickly branding, język polski, kolory #0047AB, #F1EFE8).

Placeholdery:
- `{firstName}` — imię użytkownika (fallback: "Użytkowniku" gdy puste)
- `{appUrl}` — link do aplikacji (FrontendSettings.BaseUrl + HomePath)
- `{bodyText}` — treść powitalna
- `{ctaLabel}` — tekst przycisku CTA (np. "Przejdź do Brickly")

Treść (PL):
- Nagłówek: "Witaj w Brickly!"
- Body: krótkie powitanie, info że konto zostało utworzone, zachęta do rozpoczęcia pracy z projektami budowlanymi
- CTA: link do aplikacji
- Footer: standardowy jak w tenant-invitation.html

Upewnij się, że plik jest objęty przez `<EmbeddedResource Include="Resources\EmailTemplates\*.html" />` w `Business.csproj` (już istnieje).

---

### Krok 3 — Serwis domenowy

**Nowy plik:** `src/Business/Interfaces/Services/IWelcomeEmailService.cs`
```csharp
public interface IWelcomeEmailService
{
    Task SendWelcomeEmailAsync(User user, CancellationToken cancellationToken = default);
}
```

**Nowy plik:** `src/Business/Implementation/Services/WelcomeEmailService.cs`
- `sealed` klasa
- Zależności: `IEmailSender`, `IOptions<FrontendSettings>`, `ILogger<WelcomeEmailService>`
- Metoda `SendWelcomeEmailAsync`:
  - Buduje `appUrl` z `FrontendSettings.BaseUrl.TrimEnd('/') + FrontendSettings.HomePath`
  - `firstName` = user.FirstName jeśli niepuste, inaczej "Użytkowniku"
  - Ładuje szablon `welcome-email.html` przez `EmailTemplateLoader`
  - Wysyła przez `IEmailSender.SendEmailAsync` z Subject: `"Witaj w Brickly!"`
  - Loguje sukces/błąd; **nie rzuca wyjątku** przy błędzie wysyłki (fire-and-forget jak w invitation handlers)

**Rejestracja DI** w `src/WebApi/Extensions/ServiceCollectionExtensions.cs`:
```csharp
services.AddScoped<IWelcomeEmailService, WelcomeEmailService>();
```

---

### Krok 4 — Mail powitalny przy rejestracji (nowi użytkownicy)

**Plik:** `src/CQRS/Users/UserSyncFromB2C/UserSyncFromB2CCommandHandler.cs`

Zmiany:
1. Dodaj zależność `IWelcomeEmailService welcomeEmailService` i `IRepository<User> userRepo` (userRepo już jest)
2. Po `await userRepo.Insert(newUser)` i przed `return newUser.Id`:
   - Wywołaj `await welcomeEmailService.SendWelcomeEmailAsync(newUser, cancellationToken)`
   - Ustaw `newUser.WelcomeEmailSentAt = DateTime.UtcNow`
   - Wywołaj `await userRepo.Update(newUser)` (lub SaveChanges jeśli Insert nie zapisuje od razu — sprawdź wzorzec w projekcie)
3. **NIE wysyłaj** maila gdy:
   - Użytkownik już istnieje po B2C Object ID (early return)
   - Użytkownik już istnieje po email i jest linkowany (migration scenario)

---

### Krok 5 — Jednorazowa wysyłka do istniejących użytkowników

**Nowy plik:** `src/Business/Interfaces/WebModels/Users/SendWelcomeEmailsResultWeb.cs`
```csharp
public sealed record SendWelcomeEmailsResultWeb(int SentCount, int SkippedCount);
```

**Nowy folder:** `src/CQRS/Users/SendWelcomeEmailsToExistingUsers/`

**SendWelcomeEmailsToExistingUsersCommand.cs:**
```csharp
public sealed record SendWelcomeEmailsToExistingUsersCommand : IRequestCommand<SendWelcomeEmailsResultWeb>;
```

**SendWelcomeEmailsToExistingUsersCommandHandler.cs:**
- `sealed` handler
- Zależności: `IReadRepository<User>`, `IRepository<User>`, `ICurrentUser`, `IWelcomeEmailService`, `ILogger<...>`
- W `Handle`:
  1. Jeśli `!currentUser.IsSuperAdmin` → `throw new ForbiddenApiException("Only SuperAdmin can send bulk welcome emails.");`
  2. Pobierz użytkowników: `IsActive == true && WelcomeEmailSentAt == null && Email != ""`
     - Użyj `GetBySearch` lub `GetPagedBySearchAsync` (batch po 50 jeśli dużo użytkowników)
  3. Dla każdego użytkownika:
     - Wywołaj `welcomeEmailService.SendWelcomeEmailAsync(user, ct)`
     - Ustaw `user.WelcomeEmailSentAt = DateTime.UtcNow`
     - `await userRepo.Update(user)`
  4. Po pętli: `await userRepo.SaveChangesAsync(ct)` jeśli wymagane
  5. Zwróć `new SendWelcomeEmailsResultWeb(sentCount, skippedCount)`

---

### Krok 6 — Endpoint API

**Plik:** `src/WebApi/Controllers/UserController.cs`

Dodaj endpoint (SuperAdmin-only, autoryzacja przez handler):
```csharp
/// <summary>
/// Sends welcome emails to all existing users who haven't received one yet. SuperAdmin only.
/// </summary>
[Authorize]
[HttpPost("send-welcome-emails")]
[ProducesResponseType(typeof(SendWelcomeEmailsResultWeb), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public async Task<IActionResult> SendWelcomeEmailsToExistingUsers(CancellationToken cancellationToken)
{
    SendWelcomeEmailsToExistingUsersCommand command = new();
    SendWelcomeEmailsResultWeb result = await Send(command, cancellationToken);
    return Ok(result);
}
```

Dodaj import: `CQRS.Users.SendWelcomeEmailsToExistingUsers`, `Business.Interfaces.WebModels.Users`

---

### Krok 7 — Testy jednostkowe

**Nowy plik:** `tests/Business.Tests/Services/WelcomeEmailServiceTests.cs`
- Test: `SendWelcomeEmailAsync_WhenUserIsValid_EnqueuesEmail`
- Mock `IEmailSender`, verify `SendEmailAsync` called once with correct To/Subject

**Nowy plik:** `tests/CQRS.Tests/Users/UserSyncFromB2CCommandHandlerTests.cs` (lub rozszerz jeśli istnieje)
- Test: `Handle_WhenNewUserCreated_SendsWelcomeEmailAndSetsSentAt`
- Test: `Handle_WhenUserAlreadyExists_DoesNotSendWelcomeEmail`

**Nowy plik:** `tests/CQRS.Tests/Users/SendWelcomeEmailsToExistingUsersCommandHandlerTests.cs`
- Test: `Handle_WhenNotSuperAdmin_ThrowsForbiddenApiException`
- Test: `Handle_WhenSuperAdmin_SendsToUsersWithoutSentAt`
- Test: `Handle_WhenNoPendingUsers_ReturnsZeroSent`

Wzoruj się na `InviteTenantMemberCommandHandlerTests.cs` (mocki IEmailSender, ICurrentUser, repozytoria).

---

### Krok 8 — Build i testy

```powershell
cd 02-ApplicationServices/ProductDataManagementWebAPI
dotnet build --configuration Release
dotnet test tests/Business.Tests --configuration Release --no-build --filter "FullyQualifiedName~WelcomeEmail"
dotnet test tests/CQRS.Tests --configuration Release --no-build --filter "FullyQualifiedName~WelcomeEmail|FullyQualifiedName~UserSyncFromB2C"
```

Napraw wszystkie błędy kompilacji i testów przed zakończeniem.

---

## Konwencje (obowiązkowe)

- Zakaz `var` — explicit types
- `is null` / `is not null`
- Handlery `sealed`
- Wyjątki domenowe: `ForbiddenApiException`, `NotFoundApiException` (nie `InvalidOperationException`)
- Klamry `{}` przy każdym bloku
- Metody max ~20 linii w Handle — logika w prywatnych metodach

---

## Kryterium done

- [ ] Pole `WelcomeEmailSentAt` na encji User + migracja EF
- [ ] Szablon `welcome-email.html` (PL, Brickly branding)
- [ ] `IWelcomeEmailService` + implementacja + DI
- [ ] Nowy użytkownik (UserSyncFromB2C) dostaje mail powitalny tuż po rejestracji
- [ ] Endpoint `POST /api/user/send-welcome-emails` dla SuperAdmin wysyła maile do istniejących użytkowników bez `WelcomeEmailSentAt`
- [ ] Brak duplikatów — pole `WelcomeEmailSentAt` zapobiega ponownej wysyłce
- [ ] Testy jednostkowe przechodzą
- [ ] Build Release successful
