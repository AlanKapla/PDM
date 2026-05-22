# Raport — C5: `TenantInvitation` — BaseEntity + konfiguracja EF

## Krok 1 — Analiza

### Encja TenantInvitation

| Właściwość | Wartość |
|------------|---------|
| Plik | `src\Entities\Models\TenantInvitation.cs` |
| Obecne dziedziczenie | brak (plain class) |
| Jawne Id | ✅ tak — `public Guid Id { get; set; }` |
| IsDeleted | ❌ nie |
| DeletedAt | ❌ nie |

### Pola skalarne

| Pole | Typ | Nullable |
|------|-----|---------|
| `Id` | `Guid` | nie (usunięte po zmianie) |
| `TenantId` | `Guid` | nie |
| `Email` | `string` | nie |
| `Token` | `string` | nie |
| `CreatedAt` | `DateTime` | nie |
| `InvitedByUserId` | `Guid` | nie |
| `ExpiresAt` | `DateTime` | nie |
| `AcceptedAt` | `DateTime?` | ✅ tak |
| `IsActive` | `bool` | nie |
| `Status` | `InvitationStatus` (enum) | nie |

### Nawigacje

| Nawigacja | Typ | FK pole |
|-----------|-----|---------|
| `InvitedByUser` | `User` | `InvitedByUserId` |
| brak Tenant navigation | — | `TenantId` (tylko FK) |

### Użycia w solution

| Plik | Operacja | Wyszukiwanie po |
|------|---------|----------------|
| `InviteTenantMemberCommandHandler.cs` | Tworzenie | — (insert) |
| `AcceptTenantInvitationCommandHandler.cs` | Odczyt + aktualizacja | `Token == request.Token && IsActive` |

### DbSet i konfiguracja

| Aspekt | Status |
|--------|--------|
| `DbSet<TenantInvitation>` | ✅ istnieje w `AppDbContext` |
| Konfiguracja inline | ❌ brak |
| `TenantInvitationConfiguration.cs` | ❌ brak (przed zmianą) |

---

## Krok 2 — Decyzja o dziedziczeniu

### Wybrana opcja

**`BaseEntity`**

### Uzasadnienie

- Brak `IsDeleted` / `DeletedAt` — encja nie używa soft-delete
- Lifecycle zarządzany przez `IsActive` (bool) + `Status` (enum: Pending/Accepted/Revoked) + `ExpiresAt`
- Zaproszenia wygasają lub są akceptowane — nie są "usuwane" logicznie

---

## Krok 3 — Aktualizacja encji

### Status: ✅ WYKONANO

### Co zrobiono

- Usunięto `using System;` (niepotrzebny)
- Dodano `using Entities.Models.Base;`
- Zmieniono `public class TenantInvitation` → `public class TenantInvitation : BaseEntity`
- Usunięto jawne `public Guid Id { get; set; }` (dziedziczone z `BaseEntity`)

### Plik po zmianie

```csharp
using Entities.Models.Base;

namespace Entities.Models
{
    public class TenantInvitation : BaseEntity
    {
        public Guid TenantId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public Guid InvitedByUserId { get; set; }
        public User InvitedByUser { get; set; } = default!;
        public DateTime ExpiresAt { get; set; }
        public DateTime? AcceptedAt { get; set; }
        public bool IsActive { get; set; }
        public InvitationStatus Status { get; set; }
    }

    public enum InvitationStatus
    {
        Pending = 0,
        Accepted = 1,
        Revoked = 2
    }
}
```

> **Uwaga:** Handler `InviteTenantMemberCommandHandler` ustawia `Id = Guid.NewGuid()` explicite.
> Po dodaniu `BaseEntity` (które samo inicjalizuje `Id = Guid.NewGuid()`) jest to redundantne ale bezpieczne.

---

## Krok 4 — `TenantInvitationConfiguration.cs`

### Status: ✅ WYKONANO

### Utworzony plik

```csharp
// src\Entities\Configurations\TenantInvitationConfiguration.cs

using Entities.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entities.Configurations
{
    public class TenantInvitationConfiguration : IEntityTypeConfiguration<TenantInvitation>
    {
        public void Configure(EntityTypeBuilder<TenantInvitation> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Email)
                .IsRequired()
                .HasMaxLength(256);

            builder.Property(x => x.Token)
                .IsRequired()
                .HasMaxLength(512);

            builder.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(50);

            builder.HasIndex(x => x.Token)
                .IsUnique();

            builder.HasIndex(x => new { x.TenantId, x.Email });

            builder.HasIndex(x => new { x.TenantId, x.Status });

            builder.HasIndex(x => x.ExpiresAt);

            builder.HasOne(x => x.InvitedByUser)
                .WithMany()
                .HasForeignKey(x => x.InvitedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
```

---

## Krok 5 — Nawigacje w `Tenant`

### Status: ✅ POMINIĘTO ŚWIADOMIE

`Tenant` nie ma kolekcji `Invitations` — nie dodano jej, ponieważ żaden handler
nie używa nawigacji `tenant.Invitations`. Konfiguracja FK ustawiona przez
`HasOne<Tenant>().WithMany()` — bez nawigacji po stronie `Tenant`.
`TenantConfiguration` nie zawiera żadnej konfiguracji dotyczącej `TenantInvitation` — brak duplikatu.

---

## Krok 6 — Migracja EF

### Status: ✅ WYGENEROWANA

### Ocena migracji

| Zmiana | Oczekiwana | Status |
|--------|-----------|--------|
| Indeks `Token` (unique) | tak | ✅ |
| Indeks `(TenantId, Email)` | tak | ✅ |
| Indeks `(TenantId, Status)` | tak | ✅ |
| Indeks `ExpiresAt` | tak | ✅ |
| `MaxLength` Email → 256 | tak | ✅ |
| `MaxLength` Token → 512 | tak | ✅ |
| FK → Tenants (Cascade) | tak | ✅ |
| FK → Users/InvitedByUserId (Restrict) | tak | ✅ |
| Status: `int` → `nvarchar(50)` | ⚠️ zmiana typu | patrz uwaga |

### ⚠️ Zmiana typu kolumny `Status`

Migracja zmienia `Status` z `int` na `nvarchar(50)` (string enum conversion).
Jeśli tabela `TenantInvitations` zawiera **istniejące rekordy** — SQL Server
nie wykona automatycznej konwersji `int` → `nvarchar`.

**Przed uruchomieniem `database update` należy:**

```sql
-- Opcja 1: tabela jest pusta lub środowisko dev — bezpośredni update
UPDATE TenantInvitations SET Status = CASE Status
    WHEN 0 THEN 'Pending'
    WHEN 1 THEN 'Accepted'
    WHEN 2 THEN 'Revoked'
    ELSE 'Pending'
END;
```

Lub dodać kroki `Sql()` w metodzie `Up()` migracji przed `AlterColumn`.

---

## Podsumowanie końcowe

### Kompilacja

| Status | Błędy |
|--------|-------|
| ✅ Build successful | 0 |

### Zmodyfikowane pliki

| Plik | Zmiana |
|------|--------|
| `src\Entities\Models\TenantInvitation.cs` | Dodano `: BaseEntity`, usunięto jawne `Id` i `using System` |
| `src\Entities\Configurations\TenantInvitationConfiguration.cs` | Nowy plik — pełna konfiguracja EF |
| `src\Entities\Migrations\..._add-tenant-invitation-configuration.cs` | Nowa migracja: indeksy, MaxLength, FK, zmiana typu Status |

### Blokery

| # | Opis | Wymaga |
|---|------|--------|
| 1 | Zmiana typu `Status`: `int` → `nvarchar(50)` | Skrypt SQL przed `database update` jeśli są dane w tabeli |

### Następny krok

Kolejny problem z listy audytu encji.
