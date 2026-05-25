-- ============================================================
-- TEST: Subskrypcja wygasła — dostęp do tenanta zablokowany
-- Status PastDue (2) — płatność wymagana, zasoby niedostępne.
--
-- Ustaw @tenantId na Id tenanta który chcesz przetestować.
-- Uruchamiaj wielokrotnie — UPDATE jest idempotentny.
-- ============================================================

DECLARE @tenantId UNIQUEIDENTIFIER = '00000000-0000-0000-0000-000000000000'; -- <-- zmień na swój TenantId
DECLARE @now      DATETIME2        = SYSUTCDATETIME();

-- Pobierz limity planu Standard (Plan = 1)
DECLARE @maxProjects INT;
DECLARE @maxUsers    INT;

SELECT @maxProjects = MaxProjects, @maxUsers = MaxUsers
FROM SubscriptionPlanDefinitions
WHERE [Plan] = 1 AND IsActive = 1;  -- Standard

IF @maxProjects IS NULL
BEGIN
    -- Fallback — jeśli brak planu Standard, użyj dowolnych limitów
    SET @maxProjects = 10;
    SET @maxUsers    = 20;
END

-- Utwórz subskrypcję jeśli nie istnieje, lub zaktualizuj istniejącą
IF NOT EXISTS (SELECT 1 FROM TenantSubscriptions WHERE TenantId = @tenantId)
BEGIN
    INSERT INTO TenantSubscriptions
    (
        Id, TenantId, [Plan], [Status],
        MaxProjects, MaxUsers,
        IsFullAccess, FullAccessGrantedByAdminId, FullAccessGrantedAt,
        CurrentPeriodStart, CurrentPeriodEnd,
        NextPaymentDue, LastPaidAt, LastPaidAmount,
        GracePeriodDays, GracePeriodEndsAt,
        TrialEndsAt, CanceledAt, CreatedAt, UpdatedAt
    )
    VALUES
    (
        NEWID(), @tenantId, 1, 2,           -- Plan=Standard, Status=PastDue
        @maxProjects, @maxUsers,
        0, NULL, NULL,
        DATEADD(DAY, -35, @now),            -- CurrentPeriodStart: 35 dni temu
        DATEADD(DAY,  -5, @now),            -- CurrentPeriodEnd:    5 dni temu (wygasła)
        DATEADD(DAY,  -5, @now),            -- NextPaymentDue:      5 dni temu (przeterminowana)
        DATEADD(DAY, -65, @now), 99.00,     -- LastPaidAt: 65 dni temu (zapłacono za POPRZEDNI okres, nie bieżący)
        7, NULL,                            -- GracePeriodDays=7, GracePeriodEndsAt=NULL (karencja minęła)
        NULL, NULL, @now, NULL
    );
    PRINT 'Utworzono nową subskrypcję PastDue.';
END
ELSE
BEGIN
    UPDATE TenantSubscriptions
    SET
        [Plan]             = 1,                      -- Standard
        [Status]           = 2,                      -- PastDue
        MaxProjects        = @maxProjects,
        MaxUsers           = @maxUsers,
        CurrentPeriodStart = DATEADD(DAY, -35, @now),
        CurrentPeriodEnd   = DATEADD(DAY,  -5, @now),
        NextPaymentDue     = DATEADD(DAY,  -5, @now),
        GracePeriodDays    = 7,
        GracePeriodEndsAt  = NULL,                   -- karencja już minęła
        LastPaidAt         = DATEADD(DAY, -65, @now),  -- zapłacono za POPRZEDNI okres (przed CurrentPeriodStart)
        LastPaidAmount     = 99.00,
        CanceledAt         = NULL,
        UpdatedAt          = @now
    WHERE TenantId = @tenantId;
    PRINT 'Zaktualizowano istniejącą subskrypcję na PastDue.';
END

-- Podgląd wyniku
SELECT
    ts.TenantId,
    t.Name                  AS TenantName,
    CASE ts.[Plan]
        WHEN 0 THEN 'Free'
        WHEN 1 THEN 'Standard'
        WHEN 2 THEN 'Premium'
        WHEN 3 THEN 'Enterprise'
    END                     AS [Plan],
    CASE ts.[Status]
        WHEN 0 THEN 'Active'
        WHEN 1 THEN 'Trialing'
        WHEN 2 THEN 'PastDue      ← ZABLOKOWANY'
        WHEN 3 THEN 'Canceled     ← ZABLOKOWANY'
        WHEN 4 THEN 'GracePeriod  ← ZABLOKOWANY'
    END                     AS [Status],
    ts.CurrentPeriodEnd,
    ts.NextPaymentDue,
    ts.GracePeriodEndsAt,
    ts.LastPaidAt
FROM TenantSubscriptions ts
JOIN Tenants t ON t.Id = ts.TenantId
WHERE ts.TenantId = @tenantId;
