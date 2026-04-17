# User API — Dokumentacja

## Przegląd

**Kontroler:** `UserController`  
**Base route:** `api/user`

---

## Endpoint: GET `/assigned-works`

Zwraca wszystkie zakresy prac przypisane do zalogowanego użytkownika, pogrupowane hierarchicznie:  
**Tenant → Project → WorkSchedule → Stage → Work**

### Warunki filtrowania

- Tylko przypisania należące do **aktywnych tenantów** (`Tenant.IsActive = true`)
- Tylko przypisania należące do **aktywnych projektów** (`Project.IsActive = true`)
- Tylko przypisania w **niesuniętych harmonogramach** (`WorkSchedule.IsDeleted = false`)

### Sortowanie

| Poziom | Pole |
|--------|------|
| Tenants | `TenantName` ASC |
| Projects | `ProjectName` ASC |
| WorkSchedules | `WorkScheduleCreatedAt` DESC |
| Stages | `StageOrder` ASC |
| Works | `WorkOrder` ASC |
| Periods | `StartDate` ASC |
| Comments | `CreatedAt` ASC |

### Struktura odpowiedzi

```json
[
  {
    "tenantId": "guid",
    "tenantName": "string",
    "projects": [
      {
        "projectId": "guid",
        "projectName": "string",
        "workSchedules": [
          {
            "workScheduleId": "guid",
            "workScheduleName": "string",
            "workScheduleCreatedAt": "datetime",
            "stages": [
              {
                "stageId": "guid",
                "stageName": "string",
                "stageOrder": 0,
                "works": [
                  {
                    "workId": "guid",
                    "workName": "string",
                    "workOrder": 0,
                    "colorRgb": "string",
                    "isClosed": false,
                    "periods": [
                      {
                        "id": "guid",
                        "startDate": "datetime",
                        "endDate": "datetime",
                        "isClosed": false
                      }
                    ],
                    "comments": [
                      {
                        "id": "guid",
                        "content": "string",
                        "createdByUserId": "guid",
                        "createdByUserName": "string",
                        "createdAt": "datetime"
                      }
                    ]
                  }
                ]
              }
            ]
          }
        ]
      }
    ]
  }
]
```

### Modele C#

#### `UserAssignedWorksByTenantWeb`
| Pole | Typ | Opis |
|------|-----|------|
| `TenantId` | `Guid` | Identyfikator tenanta |
| `TenantName` | `string` | Nazwa tenanta |
| `Projects` | `List<UserAssignedWorksGroupedWeb>` | Projekty w ramach tenanta |

#### `UserAssignedWorksGroupedWeb`
| Pole | Typ | Opis |
|------|-----|------|
| `ProjectId` | `Guid` | Identyfikator projektu |
| `ProjectName` | `string` | Nazwa projektu |
| `WorkSchedules` | `List<UserAssignedWorkScheduleWeb>` | Harmonogramy w ramach projektu |

#### `UserAssignedWorkScheduleWeb`
| Pole | Typ | Opis |
|------|-----|------|
| `WorkScheduleId` | `Guid` | Identyfikator harmonogramu |
| `WorkScheduleName` | `string` | Nazwa harmonogramu |
| `WorkScheduleCreatedAt` | `DateTime` | Data utworzenia harmonogramu |
| `Stages` | `List<UserAssignedStageWeb>` | Etapy harmonogramu |

#### `UserAssignedStageWeb`
| Pole | Typ | Opis |
|------|-----|------|
| `StageId` | `Guid` | Identyfikator etapu |
| `StageName` | `string` | Nazwa etapu |
| `StageOrder` | `int` | Kolejność etapu |
| `Works` | `List<UserAssignedWorkWeb>` | Zakresy prac w etapie |

#### `UserAssignedWorkWeb`
| Pole | Typ | Opis |
|------|-----|------|
| `WorkId` | `Guid` | Identyfikator zakresu pracy |
| `WorkName` | `string` | Nazwa zakresu pracy |
| `WorkOrder` | `int` | Kolejność w etapie |
| `ColorRgb` | `string` | Kolor zakresu (RGB) |
| `IsClosed` | `bool` | `true` jeśli wszystkie okresy są zamknięte i istnieje co najmniej jeden okres |
| `Periods` | `List<WorkScheduleStageWorkPeriodWeb>` | Okresy realizacji |
| `Comments` | `List<WorkScheduleStageWorkCommentWeb>` | Komentarze |

#### `WorkScheduleStageWorkPeriodWeb`
| Pole | Typ | Opis |
|------|-----|------|
| `Id` | `Guid` | Identyfikator okresu |
| `StartDate` | `DateTime` | Data rozpoczęcia |
| `EndDate` | `DateTime` | Data zakończenia |
| `IsClosed` | `bool` | Czy okres jest zamknięty |

#### `WorkScheduleStageWorkCommentWeb`
| Pole | Typ | Opis |
|------|-----|------|
| `Id` | `Guid` | Identyfikator komentarza |
| `Content` | `string` | Treść komentarza |
| `CreatedByUserId` | `Guid` | Identyfikator autora |
| `CreatedByUserName` | `string` | Imię i nazwisko autora (`FirstName LastName`) |
| `CreatedAt` | `DateTime` | Data i godzina dodania komentarza |
