# Raport — Naprawa TPH `UserProfileBase`

## Diagnoza

Duplikat nie był między nieistniejącym plikiem konfiguracyjnym a `AppDbContext`.
Duplikat był między dwoma konfiguracjami tej samej relacji `User → UserProfileBase`:

| Konfiguracja | Lokalizacja | `WithMany` | Discriminator |
|---|---|---|---|
| `UserProfileConfiguration` | `UserConfiguration.cs` | `.WithMany(u => u.Profiles)` ✅ | tylko `TenantPreferences` ❌ |
| Inline | `AppDbContext.OnModelCreating` | `.WithMany()` bez nawigacji ❌ | oba typy ✅ |

EF Core widział dwie niezależne relacje i tworzył shadow FK `UserId1` — nullable kolumna w tabeli `UserProfiles`, zawsze `NULL`.

---

## Wykonane zmiany

### 1. `src\Entities\Configurations\UserConfiguration.cs`

Uzupełniono istniejący `UserProfileConfiguration`: dodano `.IsRequired()` i brakującą wartość discriminatora dla `PermissionsVersionProfile`.

```csharp
public class UserProfileConfiguration : IEntityTypeConfiguration<UserProfileBase>
{
    public void Configure(EntityTypeBuilder<UserProfileBase> builder)
    {
        builder.HasKey(p => p.Id);
        builder.HasOne(p => p.User)
               .WithMany(u => u.Profiles)
               .HasForeignKey(p => p.UserId)
               .IsRequired()
               .OnDelete(DeleteBehavior.Cascade);
        builder.HasDiscriminator<string>("ProfileType")
               .HasValue<TenantPreferencesProfile>("TenantPreferences")
               .HasValue<PermissionsVersionProfile>("PermissionsVersion");
    }
}
```

### 2. `src\Entities\Context\AppDbContext.cs`

Usunięto cały blok inline. `OnModelCreating` zawiera teraz tylko:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
}
```

### 3. Nowa migracja EF Core

`src\Entities\Migrations\20260505..._remove-user-profile-shadow-property.cs`

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    // Usunięcie shadow property — bezpieczne, kolumna zawsze NULL
    migrationBuilder.DropForeignKey(
        name: "FK_UserProfiles_Users_UserId1",
        table: "UserProfiles");
    migrationBuilder.DropIndex(
        name: "IX_UserProfiles_UserId1",
        table: "UserProfiles");
    migrationBuilder.DropColumn(
        name: "UserId1",
        table: "UserProfiles");

    // Pre-existing drift — usunięcie zbędnego defaultValue: false
    migrationBuilder.AlterColumn<bool>(
        name: "IsDeleted", table: "CostEstimateTemplates", type: "bit", nullable: false,
        oldClrType: typeof(bool), oldType: "bit", oldDefaultValue: false);
    migrationBuilder.AlterColumn<bool>(
        name: "IsDeleted", table: "CostEstimateFieldFiles", type: "bit", nullable: false,
        oldClrType: typeof(bool), oldType: "bit", oldDefaultValue: false);
}
```

---

## Weryfikacja snapshot po zmianach

| Typ | Wartość discriminatora | Status |
|-----|------------------------|--------|
| `TenantPreferencesProfile` | `"TenantPreferences"` | ✅ zachowana |
| `PermissionsVersionProfile` | `"PermissionsVersion"` | ✅ zachowana |
| Shadow property `UserId1` | — | ✅ zniknęła ze snapshot |

---

## Kompilacja

| Status | Błędy |
|--------|-------|
| ✅ Build successful | 0 |

---

## Zmodyfikowane pliki

| Plik | Zmiana |
|------|--------|
| `src\Entities\Configurations\UserConfiguration.cs` | Dodano `.IsRequired()` i `HasValue<PermissionsVersionProfile>("PermissionsVersion")` |
| `src\Entities\Context\AppDbContext.cs` | Usunięto blok inline `modelBuilder.Entity<UserProfileBase>(...)` |
| `src\Entities\Migrations\..._remove-user-profile-shadow-property.cs` | Nowa migracja: DROP `UserId1` + pre-existing `IsDeleted` drift |

---

## ⚠️ Wymagane przed deploymentem

```bash
dotnet ef database update --project src\Entities --startup-project src\WebApi
```

Kolumna `UserId1` w tabeli `UserProfiles` jest nullable i zawsze pusta — DROP bezpieczny.

---

## Następny krok

C5 — `TenantInvitation` bez `BaseEntity` i konfiguracji EF
