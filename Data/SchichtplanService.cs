using Dapper;
using Microsoft.Data.SqlClient;

namespace QIN_Production_Web.Data;

public class SchichtplanService
{
    private readonly string _fertigungConnectionString = SqlManager.FertigungConnectionString;
    private readonly string _mainConnectionString = SqlManager.connectionString;
    private static readonly TimeSpan PlanDateRolloverTime = new(21, 45, 0);
    private static readonly TimeZoneInfo PlanTimeZone = ResolvePlanTimeZone();

    public async Task<SchichtplanBoardModel> GetBoardAsync(DateTime planDate)
    {
        await SchichtplanSchemaService.EnsureZielSnapshotColumnsAsync(_fertigungConnectionString);

        var normalizedDate = planDate.Date;

        using var connection = new SqlConnection(_fertigungConnectionString);
        await connection.OpenAsync();

        var plan = await connection.QuerySingleOrDefaultAsync<PlanRow>(
            @"SELECT TOP (1) ID, PlanDatum, Kalenderwoche, ZuletztGeaendertAm, ZuletztGeaendertVon
              FROM dbo.SchichtplanPlan
              WHERE PlanDatum = @PlanDatum;",
            new { PlanDatum = normalizedDate });

        var workplaces = (await connection.QueryAsync<WorkplaceRow>(
            @"SELECT ID, Bereich, BereichSortierung, ArbeitsplatzNr, ArbeitsplatzName, ArbeitsplatzSortierung
              FROM dbo.SchichtplanArbeitsplatz
              WHERE Aktiv = 1
              ORDER BY BereichSortierung, ArbeitsplatzSortierung, ArbeitsplatzName;")).ToList();

        var entries = (await connection.QueryAsync<EntryRow>(
            @"SELECT
                    e.ID,
                    e.ArbeitsplatzID,
                    e.Schicht,
                    e.MaterialStammID,
                    COALESCE(m1.Material, e.Material) AS Material,
                    COALESCE(e.MaterialZielMenge, m1.TagesMenge) AS MaterialTagesMenge,
                    e.MaterialZielMenge,
                    e.MaterialStammID2,
                    COALESCE(m2.Material, e.Material2) AS Material2,
                    COALESCE(e.Material2ZielMenge, m2.TagesMenge) AS Material2TagesMenge,
                    e.Material2ZielMenge,
                    e.FA_Nr,
                    e.Bemerkung,
                    e.ZuletztGeaendertAm
              FROM dbo.SchichtplanEintrag e
              INNER JOIN dbo.SchichtplanPlan p ON p.ID = e.SchichtplanPlanID
              LEFT JOIN dbo.SchichtplanMaterialStamm m1 ON m1.ID = e.MaterialStammID
              LEFT JOIN dbo.SchichtplanMaterialStamm m2 ON m2.ID = e.MaterialStammID2
              WHERE p.PlanDatum = @PlanDatum;",
            new { PlanDatum = normalizedDate })).ToList();

        var assignments = (await connection.QueryAsync<AssignmentRow>(
            @"SELECT ben.ID, ben.SchichtplanEintragID, ben.Benutzer, ben.BenutzerQuelle, ben.BenutzerSchluessel, ben.Personalnummer, ben.Sortierung
              FROM dbo.SchichtplanEintragBenutzer ben
              INNER JOIN dbo.SchichtplanPlan p ON p.ID = ben.SchichtplanPlanID
              WHERE p.PlanDatum = @PlanDatum
              ORDER BY ben.Sortierung, ben.ID;",
            new { PlanDatum = normalizedDate })).ToList();

        return BuildBoard(normalizedDate, plan, workplaces, entries, assignments);
    }

    public async Task<SchichtplanManagementModel> GetManagementModelAsync(DateTime planDate)
    {
        var normalizedDate = planDate.Date;
        var boardTask = GetBoardAsync(normalizedDate);
        var usersTask = GetAvailableUsersAsync();
        var materialsTask = GetMaterialsAsync();

        await Task.WhenAll(boardTask, usersTask, materialsTask);

        return new SchichtplanManagementModel
        {
            Board = await boardTask,
            AvailableUsers = await usersTask,
            Materials = await materialsTask
        };
    }

    public async Task<int> CopyPlanAsync(DateTime sourceDate, DateTime targetDate, string changedBy)
    {
        await SchichtplanSchemaService.EnsureZielSnapshotColumnsAsync(_fertigungConnectionString);

        var normalizedSourceDate = sourceDate.Date;
        var normalizedTargetDate = targetDate.Date;

        if (normalizedSourceDate == normalizedTargetDate)
        {
            throw new InvalidOperationException("Quell- und Zieldatum dürfen nicht identisch sein.");
        }

        using var connection = new SqlConnection(_fertigungConnectionString);
        await connection.OpenAsync();
        using var transaction = connection.BeginTransaction();

        var sourcePlan = await connection.QuerySingleOrDefaultAsync<PlanCopyRow>(
            @"SELECT TOP (1) ID, Titel, Bemerkung
              FROM dbo.SchichtplanPlan
              WHERE PlanDatum = @PlanDatum;",
            new { PlanDatum = normalizedSourceDate },
            transaction);

        if (sourcePlan is null)
        {
            throw new InvalidOperationException($"Am {normalizedSourceDate:dd.MM.yyyy} gibt es keinen Schichtplan zum Übernehmen.");
        }

        var sourceEntryCount = await connection.ExecuteScalarAsync<int>(
            @"SELECT COUNT(*)
              FROM dbo.SchichtplanEintrag
              WHERE SchichtplanPlanID = @PlanId;",
            new { PlanId = sourcePlan.ID },
            transaction);

        if (sourceEntryCount == 0)
        {
            throw new InvalidOperationException($"Am {normalizedSourceDate:dd.MM.yyyy} gibt es keine Schichtplan-Einträge zum Übernehmen.");
        }

        var targetPlanId = await EnsurePlanAsync(connection, transaction, normalizedTargetDate, changedBy);

        await connection.ExecuteAsync(
            @"UPDATE dbo.SchichtplanPlan
              SET Titel = @Titel,
                  Bemerkung = @Bemerkung
              WHERE ID = @PlanId;",
            new
            {
                PlanId = targetPlanId,
                sourcePlan.Titel,
                sourcePlan.Bemerkung
            },
            transaction);

        await connection.ExecuteAsync(
            @"DELETE FROM dbo.SchichtplanEintrag
              WHERE SchichtplanPlanID = @PlanId;",
            new { PlanId = targetPlanId },
            transaction);

        await connection.ExecuteAsync(
            @"
DECLARE @EntryMap TABLE
(
    SourceEntryId INT NOT NULL,
    TargetEntryId INT NOT NULL
);

MERGE dbo.SchichtplanEintrag AS target
USING
(
    SELECT
        source.ID AS SourceEntryId,
        source.ArbeitsplatzID,
        source.Schicht,
        source.MaterialStammID,
        source.Material,
        source.MaterialZielMenge,
        source.MaterialStammID2,
        source.Material2,
        source.Material2ZielMenge,
        source.FA_Nr,
        source.Bemerkung
    FROM dbo.SchichtplanEintrag source
    WHERE source.SchichtplanPlanID = @SourcePlanId
) AS source
    ON 1 = 0
WHEN NOT MATCHED THEN
    INSERT
    (
        SchichtplanPlanID,
        ArbeitsplatzID,
        Schicht,
        MaterialStammID,
        Material,
        MaterialZielMenge,
        MaterialStammID2,
        Material2,
        Material2ZielMenge,
        FA_Nr,
        Bemerkung,
        CreatedBy,
        ZuletztGeaendertVon
    )
    VALUES
    (
        @TargetPlanId,
        source.ArbeitsplatzID,
        source.Schicht,
        source.MaterialStammID,
        source.Material,
        source.MaterialZielMenge,
        source.MaterialStammID2,
        source.Material2,
        source.Material2ZielMenge,
        source.FA_Nr,
        source.Bemerkung,
        @ChangedBy,
        @ChangedBy
    )
OUTPUT source.SourceEntryId, INSERTED.ID
    INTO @EntryMap (SourceEntryId, TargetEntryId);

INSERT INTO dbo.SchichtplanEintragBenutzer
(
    SchichtplanEintragID,
    SchichtplanPlanID,
    BenutzerQuelle,
    BenutzerSchluessel,
    Personalnummer,
    Benutzer,
    Sortierung
)
SELECT
    map.TargetEntryId,
    @TargetPlanId,
    source.BenutzerQuelle,
    source.BenutzerSchluessel,
    source.Personalnummer,
    source.Benutzer,
    source.Sortierung
FROM dbo.SchichtplanEintragBenutzer source
INNER JOIN @EntryMap map
    ON map.SourceEntryId = source.SchichtplanEintragID
WHERE source.SchichtplanPlanID = @SourcePlanId;",
            new
            {
                SourcePlanId = sourcePlan.ID,
                TargetPlanId = targetPlanId,
                ChangedBy = NormalizeAuditName(changedBy)
            },
            transaction);

        await TouchPlanAsync(connection, transaction, targetPlanId, normalizedTargetDate, changedBy);
        transaction.Commit();


        return sourceEntryCount;
    }

    public async Task<int> ClearPlanAsync(DateTime planDate, string changedBy)
    {
        var normalizedDate = planDate.Date;

        using var connection = new SqlConnection(_fertigungConnectionString);
        await connection.OpenAsync();
        using var transaction = connection.BeginTransaction();

        var planId = await connection.ExecuteScalarAsync<int?>(
            @"SELECT TOP (1) ID
              FROM dbo.SchichtplanPlan
              WHERE PlanDatum = @PlanDatum;",
            new { PlanDatum = normalizedDate },
            transaction);

        if (!planId.HasValue)
        {
            transaction.Commit();
            return 0;
        }

        var deletedEntryCount = await connection.ExecuteScalarAsync<int>(
            @"SELECT COUNT(*)
              FROM dbo.SchichtplanEintrag
              WHERE SchichtplanPlanID = @PlanId;",
            new { PlanId = planId.Value },
            transaction);

        await connection.ExecuteAsync(
            @"DELETE FROM dbo.SchichtplanEintrag
              WHERE SchichtplanPlanID = @PlanId;",
            new { PlanId = planId.Value },
            transaction);

        await connection.ExecuteAsync(
            @"DELETE FROM dbo.SchichtplanPlan
              WHERE ID = @PlanId;",
            new { PlanId = planId.Value },
            transaction);

        transaction.Commit();


        return deletedEntryCount;
    }

    public async Task<SchichtplanAvailableUserModel?> CreateManualUserAsync(string displayName, string changedBy)
    {
        var normalizedName = NormalizeNullable(displayName);
        if (normalizedName is null)
        {
            return null;
        }

        using var connection = new SqlConnection(_fertigungConnectionString);
        await connection.OpenAsync();

        var user = await connection.QuerySingleAsync<SchichtplanAvailableUserModel>(
            @"
DECLARE @ExistingId INT = (
    SELECT TOP (1) ID
    FROM dbo.SchichtplanZusatzBenutzer
    WHERE Benutzer = @Benutzer
);

IF @ExistingId IS NOT NULL
BEGIN
    UPDATE dbo.SchichtplanZusatzBenutzer
    SET Aktiv = 1,
        UpdatedAt = SYSDATETIME()
    WHERE ID = @ExistingId;

    SELECT
        Benutzer AS DisplayName,
        CAST(N'Manuell' AS NVARCHAR(20)) AS Source,
        CAST(ID AS NVARCHAR(150)) AS [Key],
        CAST(NULL AS NVARCHAR(50)) AS Personalnummer,
        CAST(1 AS bit) AS IsManual
    FROM dbo.SchichtplanZusatzBenutzer
    WHERE ID = @ExistingId;
END
ELSE
BEGIN
    DECLARE @Inserted TABLE
    (
        ID INT,
        Benutzer NVARCHAR(150)
    );

    INSERT INTO dbo.SchichtplanZusatzBenutzer (Benutzer, Aktiv, CreatedBy)
    OUTPUT INSERTED.ID, INSERTED.Benutzer INTO @Inserted (ID, Benutzer)
    VALUES (@Benutzer, 1, @ChangedBy);

    SELECT
        Benutzer AS DisplayName,
        CAST(N'Manuell' AS NVARCHAR(20)) AS Source,
        CAST(ID AS NVARCHAR(150)) AS [Key],
        CAST(NULL AS NVARCHAR(50)) AS Personalnummer,
        CAST(1 AS bit) AS IsManual
    FROM @Inserted;
END;",
            new
            {
                Benutzer = normalizedName,
                ChangedBy = NormalizeAuditName(changedBy)
            });

        return user;
    }

    public async Task<SchichtplanMaterialModel?> CreateMaterialAsync(string materialName, string changedBy)
    {
        var normalizedMaterial = NormalizeNullable(materialName);
        if (normalizedMaterial is null)
        {
            return null;
        }

        using var connection = new SqlConnection(_fertigungConnectionString);
        await connection.OpenAsync();

        var material = await connection.QuerySingleAsync<SchichtplanMaterialModel>(
            @"
DECLARE @ExistingId INT = (
    SELECT TOP (1) ID
    FROM dbo.SchichtplanMaterialStamm
    WHERE Material = @Material
);

IF @ExistingId IS NOT NULL
BEGIN
    UPDATE dbo.SchichtplanMaterialStamm
    SET Aktiv = 1,
        UpdatedAt = SYSDATETIME()
    WHERE ID = @ExistingId;

    SELECT ID, Material, TagesMenge, Sortierung, IstStandard
    FROM dbo.SchichtplanMaterialStamm
    WHERE ID = @ExistingId;
END
ELSE
BEGIN
    DECLARE @Inserted TABLE
    (
        ID INT,
        Material NVARCHAR(200),
        TagesMenge INT NULL,
        Sortierung INT,
        IstStandard BIT
    );

    INSERT INTO dbo.SchichtplanMaterialStamm (Material, TagesMenge, Sortierung, IstStandard, Aktiv, CreatedBy)
    OUTPUT INSERTED.ID, INSERTED.Material, INSERTED.TagesMenge, INSERTED.Sortierung, INSERTED.IstStandard
        INTO @Inserted (ID, Material, TagesMenge, Sortierung, IstStandard)
    VALUES (
        @Material,
        NULL,
        ISNULL((SELECT MAX(Sortierung) + 10 FROM dbo.SchichtplanMaterialStamm), 10),
        0,
        1,
        @ChangedBy
    );

    SELECT ID, Material, TagesMenge, Sortierung, IstStandard
    FROM @Inserted;
END;",
            new
            {
                Material = normalizedMaterial,
                ChangedBy = NormalizeAuditName(changedBy)
            });

        return material;
    }

    public async Task<SchichtplanMaterialModel?> UpdateMaterialAsync(int materialId, string materialName, int? tagesMenge, string changedBy)
    {
        var normalizedMaterial = NormalizeNullable(materialName);
        if (normalizedMaterial is null)
        {
            return null;
        }

        using var connection = new SqlConnection(_fertigungConnectionString);
        await connection.OpenAsync();

        var updated = await connection.QuerySingleOrDefaultAsync<SchichtplanMaterialModel>(
            @"
IF EXISTS
(
    SELECT 1
    FROM dbo.SchichtplanMaterialStamm
    WHERE ID <> @Id
      AND Aktiv = 1
      AND Material = @Material
)
BEGIN
    THROW 51000, N'Ein Material mit diesem Namen existiert bereits.', 1;
END;

UPDATE dbo.SchichtplanMaterialStamm
SET Material = @Material,
    TagesMenge = @TagesMenge,
    UpdatedAt = SYSDATETIME()
WHERE ID = @Id
  AND Aktiv = 1;

SELECT ID, Material, TagesMenge, Sortierung, IstStandard
FROM dbo.SchichtplanMaterialStamm
WHERE ID = @Id
  AND Aktiv = 1;",
            new
            {
                Id = materialId,
                Material = normalizedMaterial,
                TagesMenge = tagesMenge,
                ChangedBy = NormalizeAuditName(changedBy)
            });

        return updated;
    }

    public async Task<bool> DeleteManualUserAsync(string? userKey, string changedBy)
    {
        if (!int.TryParse(userKey, out var userId))
        {
            return false;
        }

        using var connection = new SqlConnection(_fertigungConnectionString);
        await connection.OpenAsync();

        var userName = await connection.ExecuteScalarAsync<string?>(
            @"SELECT TOP (1) Benutzer
              FROM dbo.SchichtplanZusatzBenutzer
              WHERE ID = @Id
                AND Aktiv = 1;",
            new { Id = userId });

        if (string.IsNullOrWhiteSpace(userName))
        {
            return false;
        }

        var rows = await connection.ExecuteAsync(
            @"UPDATE dbo.SchichtplanZusatzBenutzer
              SET Aktiv = 0,
                  UpdatedAt = SYSDATETIME()
              WHERE ID = @Id
                AND Aktiv = 1;",
            new { Id = userId });

        return rows > 0;
    }

    public async Task<bool> DeleteMaterialAsync(int materialId, string changedBy)
    {
        using var connection = new SqlConnection(_fertigungConnectionString);
        await connection.OpenAsync();

        var material = await connection.QuerySingleOrDefaultAsync<SchichtplanMaterialModel>(
            @"SELECT TOP (1) ID, Material, TagesMenge, Sortierung, IstStandard
              FROM dbo.SchichtplanMaterialStamm
              WHERE ID = @Id
                AND Aktiv = 1;",
            new { Id = materialId });

        if (material is null)
        {
            return false;
        }

        var rows = await connection.ExecuteAsync(
            @"UPDATE dbo.SchichtplanMaterialStamm
              SET Aktiv = 0,
                  UpdatedAt = SYSDATETIME()
              WHERE ID = @Id
                AND Aktiv = 1;",
            new { Id = materialId });

        return rows > 0;
    }

    public async Task<bool> AssignUserAsync(DateTime planDate, int workplaceId, string shift, SchichtplanAvailableUserModel user, bool allowMultipleAssignments, string changedBy)
    {
        ValidateShift(shift);

        if (user is null)
        {
            throw new InvalidOperationException("Der gezogene Benutzer ist ungültig.");
        }

        var displayName = NormalizeNullable(user.DisplayName);
        if (displayName is null)
        {
            throw new InvalidOperationException("Der gezogene Benutzer hat keinen Namen.");
        }

        var source = NormalizeNullable(user.Source) ?? "LoginDaten";
        var userKey = NormalizeNullable(user.Key)
            ?? NormalizeNullable(user.Personalnummer)
            ?? $"MANUELL:{displayName}";

        using var connection = new SqlConnection(_fertigungConnectionString);
        await connection.OpenAsync();
        using var transaction = connection.BeginTransaction();

        var planId = await EnsurePlanAsync(connection, transaction, planDate.Date, changedBy);

        if (!allowMultipleAssignments)
        {
            var existingPlanAssignment = await connection.ExecuteScalarAsync<int?>(
                @"SELECT TOP (1) ID
                  FROM dbo.SchichtplanEintragBenutzer
                  WHERE SchichtplanPlanID = @PlanId
                    AND BenutzerQuelle = @BenutzerQuelle
                    AND BenutzerSchluessel = @BenutzerSchluessel;",
                new
                {
                    PlanId = planId,
                    BenutzerQuelle = source,
                    BenutzerSchluessel = userKey
                },
                transaction);

            if (existingPlanAssignment.HasValue)
            {
                transaction.Commit();
                return false;
            }
        }

        var entryId = await EnsureEntryAsync(connection, transaction, planId, workplaceId, shift, changedBy);
        var existingEntryAssignment = await connection.ExecuteScalarAsync<int?>(
            @"SELECT TOP (1) ID
              FROM dbo.SchichtplanEintragBenutzer
              WHERE SchichtplanEintragID = @EntryId
                AND BenutzerQuelle = @BenutzerQuelle
                AND BenutzerSchluessel = @BenutzerSchluessel;",
            new
            {
                EntryId = entryId,
                BenutzerQuelle = source,
                BenutzerSchluessel = userKey
            },
            transaction);

        if (existingEntryAssignment.HasValue)
        {
            await CleanupEntryIfEmptyAsync(connection, transaction, entryId);
            transaction.Commit();
            return false;
        }

        var usedSlots = (await connection.QueryAsync<byte>(
            @"SELECT Sortierung
              FROM dbo.SchichtplanEintragBenutzer
              WHERE SchichtplanEintragID = @EntryId;",
            new { EntryId = entryId },
            transaction)).ToHashSet();

        var nextSlot = Enumerable.Range(1, 4)
            .Select(value => (byte)value)
            .FirstOrDefault(value => !usedSlots.Contains(value));

        if (nextSlot == 0)
        {
            throw new InvalidOperationException("Auf einer Karte können maximal 4 Mitarbeiter zugeordnet werden.");
        }

        await connection.ExecuteAsync(
            @"INSERT INTO dbo.SchichtplanEintragBenutzer
                (SchichtplanEintragID, SchichtplanPlanID, BenutzerQuelle, BenutzerSchluessel, Personalnummer, Benutzer, Sortierung)
              VALUES
                (@EntryId, @PlanId, @BenutzerQuelle, @BenutzerSchluessel, @Personalnummer, @Benutzer, @Sortierung);",
            new
            {
                EntryId = entryId,
                PlanId = planId,
                BenutzerQuelle = source,
                BenutzerSchluessel = userKey,
                Personalnummer = NormalizeNullable(user.Personalnummer),
                Benutzer = displayName,
                Sortierung = nextSlot
            },
            transaction);

        await TouchEntryAsync(connection, transaction, entryId, changedBy);
        await TouchPlanAsync(connection, transaction, planId, planDate.Date, changedBy);
        transaction.Commit();

        return true;
    }

    public async Task RemoveUserAsync(int assignmentId, string changedBy)
    {
        using var connection = new SqlConnection(_fertigungConnectionString);
        await connection.OpenAsync();
        using var transaction = connection.BeginTransaction();

        var assignment = await connection.QuerySingleOrDefaultAsync<AssignmentRemoveRow>(
            @"SELECT TOP (1)
                    ben.ID,
                    ben.Benutzer,
                    ben.SchichtplanEintragID,
                    ben.SchichtplanPlanID,
                    sp.PlanDatum
              FROM dbo.SchichtplanEintragBenutzer ben
              INNER JOIN dbo.SchichtplanPlan sp ON sp.ID = ben.SchichtplanPlanID
              WHERE ben.ID = @AssignmentId;",
            new { AssignmentId = assignmentId },
            transaction);

        if (assignment is null)
        {
            transaction.Commit();
            return;
        }

        await connection.ExecuteAsync(
            "DELETE FROM dbo.SchichtplanEintragBenutzer WHERE ID = @AssignmentId;",
            new { AssignmentId = assignmentId },
            transaction);

        await CleanupEntryIfEmptyAsync(connection, transaction, assignment.SchichtplanEintragID);
        await TouchPlanAsync(connection, transaction, assignment.SchichtplanPlanID, assignment.PlanDatum, changedBy);
        transaction.Commit();

    }

    public async Task<SchichtplanMaterialAssignResult> AssignMaterialAsync(DateTime planDate, int workplaceId, string shift, int materialId, string changedBy)
    {
        await SchichtplanSchemaService.EnsureZielSnapshotColumnsAsync(_fertigungConnectionString);

        ValidateShift(shift);

        using var connection = new SqlConnection(_fertigungConnectionString);
        await connection.OpenAsync();
        using var transaction = connection.BeginTransaction();

        var material = await connection.QuerySingleOrDefaultAsync<MaterialRow>(
            @"SELECT TOP (1) ID, Material, TagesMenge
              FROM dbo.SchichtplanMaterialStamm
              WHERE ID = @MaterialId
                AND Aktiv = 1;",
            new { MaterialId = materialId },
            transaction);

        if (material is null)
        {
            transaction.Commit();
            return SchichtplanMaterialAssignResult.MaterialNotFound;
        }

        var planId = await EnsurePlanAsync(connection, transaction, planDate.Date, changedBy);
        var entryId = await EnsureEntryAsync(connection, transaction, planId, workplaceId, shift, changedBy);

        var entry = await connection.QuerySingleAsync<EntryRow>(
            @"SELECT TOP (1) ID, ArbeitsplatzID, Schicht, MaterialStammID, Material, MaterialZielMenge, MaterialStammID2, Material2, Material2ZielMenge, FA_Nr, Bemerkung, ZuletztGeaendertAm
              FROM dbo.SchichtplanEintrag
              WHERE ID = @EntryId;",
            new { EntryId = entryId },
            transaction);

        if (entry.MaterialStammID == material.Id || entry.MaterialStammID2 == material.Id)
        {
            transaction.Commit();
            return SchichtplanMaterialAssignResult.AlreadyAssigned;
        }

        string updateSql;
        SchichtplanMaterialAssignResult result;

        if (!entry.MaterialStammID.HasValue && string.IsNullOrWhiteSpace(entry.Material))
        {
            updateSql =
                @"UPDATE dbo.SchichtplanEintrag
                  SET MaterialStammID = @MaterialId,
                      Material = @Material,
                      MaterialZielMenge = @MaterialZielMenge,
                      ZuletztGeaendertAm = SYSDATETIME(),
                      ZuletztGeaendertVon = @ChangedBy
                  WHERE ID = @EntryId;";
            result = SchichtplanMaterialAssignResult.AddedPrimary;
        }
        else if (!entry.MaterialStammID2.HasValue && string.IsNullOrWhiteSpace(entry.Material2))
        {
            updateSql =
                @"UPDATE dbo.SchichtplanEintrag
                  SET MaterialStammID2 = @MaterialId,
                      Material2 = @Material,
                      Material2ZielMenge = @MaterialZielMenge,
                      ZuletztGeaendertAm = SYSDATETIME(),
                      ZuletztGeaendertVon = @ChangedBy
                  WHERE ID = @EntryId;";
            result = SchichtplanMaterialAssignResult.AddedSecondary;
        }
        else
        {
            transaction.Commit();
            return SchichtplanMaterialAssignResult.NoFreeSlot;
        }

        await connection.ExecuteAsync(
            updateSql,
            new
            {
                EntryId = entryId,
                MaterialId = material.Id,
                Material = material.Material,
                MaterialZielMenge = material.TagesMenge,
                ChangedBy = NormalizeAuditName(changedBy)
            },
            transaction);

        await TouchPlanAsync(connection, transaction, planId, planDate.Date, changedBy);
        transaction.Commit();

        return result;
    }

    public async Task ClearMaterialAsync(DateTime planDate, int workplaceId, string shift, int materialSlot, string changedBy)
    {
        await SchichtplanSchemaService.EnsureZielSnapshotColumnsAsync(_fertigungConnectionString);

        ValidateShift(shift);
        ValidateMaterialSlot(materialSlot);

        using var connection = new SqlConnection(_fertigungConnectionString);
        await connection.OpenAsync();
        using var transaction = connection.BeginTransaction();

        var entryId = await GetEntryIdAsync(connection, transaction, planDate.Date, workplaceId, shift);
        if (!entryId.HasValue)
        {
            transaction.Commit();
            return;
        }

        var entry = await connection.QuerySingleAsync<EntryRow>(
            @"SELECT TOP (1) ID, ArbeitsplatzID, Schicht, MaterialStammID, Material, MaterialZielMenge, MaterialStammID2, Material2, Material2ZielMenge, FA_Nr, Bemerkung, ZuletztGeaendertAm
              FROM dbo.SchichtplanEintrag
              WHERE ID = @EntryId;",
            new { EntryId = entryId.Value },
            transaction);

        var primaryMaterialId = entry.MaterialStammID;
        var primaryMaterial = NormalizeNullable(entry.Material);
        var primaryMaterialZielMenge = entry.MaterialZielMenge;
        var secondaryMaterialId = entry.MaterialStammID2;
        var secondaryMaterial = NormalizeNullable(entry.Material2);
        var secondaryMaterialZielMenge = entry.Material2ZielMenge;

        if (materialSlot == 1)
        {
            primaryMaterialId = secondaryMaterialId;
            primaryMaterial = secondaryMaterial;
            primaryMaterialZielMenge = secondaryMaterialZielMenge;
            secondaryMaterialId = null;
            secondaryMaterial = null;
            secondaryMaterialZielMenge = null;
        }
        else
        {
            secondaryMaterialId = null;
            secondaryMaterial = null;
            secondaryMaterialZielMenge = null;
        }

        await connection.ExecuteAsync(
            @"UPDATE dbo.SchichtplanEintrag
              SET MaterialStammID = @PrimaryMaterialId,
                  Material = @PrimaryMaterial,
                  MaterialZielMenge = @PrimaryMaterialZielMenge,
                  MaterialStammID2 = @SecondaryMaterialId,
                  Material2 = @SecondaryMaterial,
                  Material2ZielMenge = @SecondaryMaterialZielMenge,
                  ZuletztGeaendertAm = SYSDATETIME(),
                  ZuletztGeaendertVon = @ChangedBy
              WHERE ID = @EntryId;",
            new
            {
                EntryId = entryId.Value,
                PrimaryMaterialId = primaryMaterialId,
                PrimaryMaterial = primaryMaterial,
                PrimaryMaterialZielMenge = primaryMaterialZielMenge,
                SecondaryMaterialId = secondaryMaterialId,
                SecondaryMaterial = secondaryMaterial,
                SecondaryMaterialZielMenge = secondaryMaterialZielMenge,
                ChangedBy = NormalizeAuditName(changedBy)
            },
            transaction);

        await CleanupEntryIfEmptyAsync(connection, transaction, entryId.Value);

        var planId = await connection.ExecuteScalarAsync<int?>(
            "SELECT TOP (1) ID FROM dbo.SchichtplanPlan WHERE PlanDatum = @PlanDatum;",
            new { PlanDatum = planDate.Date },
            transaction);

        if (planId.HasValue)
        {
            await TouchPlanAsync(connection, transaction, planId.Value, planDate.Date, changedBy);
        }

        transaction.Commit();
    }

    public async Task UpdateEntryDetailsAsync(DateTime planDate, int workplaceId, string shift, string? faNr, string? bemerkung, string changedBy)
    {
        ValidateShift(shift);

        var normalizedFaNr = NormalizeNullable(faNr);
        var normalizedBemerkung = NormalizeNullable(bemerkung);

        using var connection = new SqlConnection(_fertigungConnectionString);
        await connection.OpenAsync();
        using var transaction = connection.BeginTransaction();

        var entryId = await GetEntryIdAsync(connection, transaction, planDate.Date, workplaceId, shift);
        if (!entryId.HasValue && normalizedFaNr is null && normalizedBemerkung is null)
        {
            transaction.Commit();
            return;
        }

        var planId = await EnsurePlanAsync(connection, transaction, planDate.Date, changedBy);
        var ensuredEntryId = entryId ?? await EnsureEntryAsync(connection, transaction, planId, workplaceId, shift, changedBy);

        await connection.ExecuteAsync(
            @"UPDATE dbo.SchichtplanEintrag
              SET FA_Nr = @FANr,
                  Bemerkung = @Bemerkung,
                  ZuletztGeaendertAm = SYSDATETIME(),
                  ZuletztGeaendertVon = @ChangedBy
              WHERE ID = @EntryId;",
            new
            {
                EntryId = ensuredEntryId,
                FANr = normalizedFaNr,
                Bemerkung = normalizedBemerkung,
                ChangedBy = NormalizeAuditName(changedBy)
            },
            transaction);

        await CleanupEntryIfEmptyAsync(connection, transaction, ensuredEntryId);
        await TouchPlanAsync(connection, transaction, planId, planDate.Date, changedBy);
        transaction.Commit();
    }

    private async Task<List<SchichtplanAvailableUserModel>> GetAvailableUsersAsync()
    {
        List<SchichtplanAvailableUserModel> loginUsers;
        using (var mainConnection = new SqlConnection(_mainConnectionString))
        {
            await mainConnection.OpenAsync();
            loginUsers = (await mainConnection.QueryAsync<SchichtplanAvailableUserModel>(
                @"SELECT DISTINCT
                        LTRIM(RTRIM(Benutzer)) AS DisplayName,
                        CAST(N'LoginDaten' AS NVARCHAR(20)) AS Source,
                        CAST(
                            CASE
                                WHEN NULLIF(LTRIM(RTRIM(Personalnummer)), N'') IS NOT NULL THEN LTRIM(RTRIM(Personalnummer))
                                ELSE N'LOGIN:' + LTRIM(RTRIM(Benutzer))
                            END AS NVARCHAR(150)
                        ) AS [Key],
                        NULLIF(LTRIM(RTRIM(Personalnummer)), N'') AS Personalnummer,
                        NULLIF(LTRIM(RTRIM(Rechte)), N'') AS Bereich,
                        CAST(0 AS bit) AS IsManual
                  FROM dbo.LoginDaten
                  WHERE ISNULL(LTRIM(RTRIM(Benutzer)), N'') <> N''
                    AND LTRIM(RTRIM(Benutzer)) <> N'Schichtplan Monitor'
                    AND
                    (
                        LTRIM(RTRIM(ISNULL(Rechte, N''))) LIKE N'%Thermoformung%'
                        OR LTRIM(RTRIM(ISNULL(Rechte, N''))) LIKE N'%Sauberraum%'
                    );"))
                .ToList();
        }

        List<SchichtplanAvailableUserModel> manualUsers;
        using (var fertigungConnection = new SqlConnection(_fertigungConnectionString))
        {
            await fertigungConnection.OpenAsync();
            manualUsers = (await fertigungConnection.QueryAsync<SchichtplanAvailableUserModel>(
                @"SELECT
                        Benutzer AS DisplayName,
                        CAST(N'Manuell' AS NVARCHAR(20)) AS Source,
                        CAST(ID AS NVARCHAR(150)) AS [Key],
                        CAST(NULL AS NVARCHAR(50)) AS Personalnummer,
                        CAST(NULL AS NVARCHAR(100)) AS Bereich,
                        CAST(1 AS bit) AS IsManual
                  FROM dbo.SchichtplanZusatzBenutzer
                  WHERE Aktiv = 1
                    AND LTRIM(RTRIM(Benutzer)) <> N'Schichtplan Monitor';"))
                .ToList();
        }

        return loginUsers
            .Concat(manualUsers)
            .Where(user =>
                !string.IsNullOrWhiteSpace(user.DisplayName) &&
                !string.Equals(user.DisplayName?.Trim(), "Schichtplan Monitor", StringComparison.OrdinalIgnoreCase))
            .GroupBy(user => $"{user.Source}|{user.Key}", StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(user => user.DisplayName ?? string.Empty, StringComparer.Create(System.Globalization.CultureInfo.GetCultureInfo("de-DE"), true))
            .ToList();
    }

    private async Task<List<SchichtplanMaterialModel>> GetMaterialsAsync()
    {
        using var connection = new SqlConnection(_fertigungConnectionString);
        await connection.OpenAsync();

        return (await connection.QueryAsync<SchichtplanMaterialModel>(
            @"SELECT ID, Material, TagesMenge, Sortierung, IstStandard
              FROM dbo.SchichtplanMaterialStamm
              WHERE Aktiv = 1
              ORDER BY Material;")).ToList();
    }

    public async Task<List<SchichtplanSauberraumProgressModel>> GetSauberraumProgressForUserAsync(string? personalnummer, string? displayName, DateTime? planDate = null)
    {
        await SchichtplanSchemaService.EnsureZielSnapshotColumnsAsync(_fertigungConnectionString);

        var normalizedPersonalnummer = NormalizeNullable(personalnummer);
        var normalizedDisplayName = NormalizeNullable(displayName);
        var normalizedPlanDate = (planDate ?? GetAutomaticPlanDate()).Date;

        using var fertigungConnection = new SqlConnection(_fertigungConnectionString);
        await fertigungConnection.OpenAsync();

        var assignedMaterials = (await fertigungConnection.QueryAsync<SauberraumAssignedMaterialRow>(
            @"
WITH AssignedMaterials AS
(
    SELECT
        e.MaterialStammID AS MaterialId,
        COALESCE(m1.Material, e.Material) AS Material,
        COALESCE(e.MaterialZielMenge, m1.TagesMenge) AS ZielMenge
    FROM dbo.SchichtplanPlan p
    INNER JOIN dbo.SchichtplanEintrag e
        ON e.SchichtplanPlanID = p.ID
    INNER JOIN dbo.SchichtplanEintragBenutzer ben
        ON ben.SchichtplanEintragID = e.ID
    INNER JOIN dbo.SchichtplanArbeitsplatz ap
        ON ap.ID = e.ArbeitsplatzID
    LEFT JOIN dbo.SchichtplanMaterialStamm m1
        ON m1.ID = e.MaterialStammID
    WHERE p.PlanDatum = @PlanDatum
      AND ap.Bereich = N'Sauberraum'
      AND ISNULL(LTRIM(RTRIM(COALESCE(m1.Material, e.Material))), N'') <> N''
      AND
      (
          (@Personalnummer IS NOT NULL AND LTRIM(RTRIM(ISNULL(ben.Personalnummer, N''))) = @Personalnummer)
          OR (@DisplayName IS NOT NULL AND LTRIM(RTRIM(ISNULL(ben.Benutzer, N''))) = @DisplayName)
      )

    UNION ALL

    SELECT
        e.MaterialStammID2 AS MaterialId,
        COALESCE(m2.Material, e.Material2) AS Material,
        COALESCE(e.Material2ZielMenge, m2.TagesMenge) AS ZielMenge
    FROM dbo.SchichtplanPlan p
    INNER JOIN dbo.SchichtplanEintrag e
        ON e.SchichtplanPlanID = p.ID
    INNER JOIN dbo.SchichtplanEintragBenutzer ben
        ON ben.SchichtplanEintragID = e.ID
    INNER JOIN dbo.SchichtplanArbeitsplatz ap
        ON ap.ID = e.ArbeitsplatzID
    LEFT JOIN dbo.SchichtplanMaterialStamm m2
        ON m2.ID = e.MaterialStammID2
    WHERE p.PlanDatum = @PlanDatum
      AND ap.Bereich = N'Sauberraum'
      AND ISNULL(LTRIM(RTRIM(COALESCE(m2.Material, e.Material2))), N'') <> N''
      AND
      (
          (@Personalnummer IS NOT NULL AND LTRIM(RTRIM(ISNULL(ben.Personalnummer, N''))) = @Personalnummer)
          OR (@DisplayName IS NOT NULL AND LTRIM(RTRIM(ISNULL(ben.Benutzer, N''))) = @DisplayName)
      )
)
SELECT
    MaterialId,
    Material,
    MAX(ZielMenge) AS ZielMenge
FROM AssignedMaterials
GROUP BY MaterialId, Material
ORDER BY Material;",
            new
            {
                PlanDatum = normalizedPlanDate,
                Personalnummer = normalizedPersonalnummer,
                DisplayName = normalizedDisplayName
            })).ToList();

        if (assignedMaterials.Count == 0)
        {
            return [];
        }

        List<ProducedMaterialRow> producedRows = [];
        var totalProducedToday = 0;

        if (normalizedPersonalnummer is not null)
        {
            using var mainConnection = new SqlConnection(_mainConnectionString);
            await mainConnection.OpenAsync();

            producedRows = (await mainConnection.QueryAsync<ProducedMaterialRow>(
                @"
SELECT
    LTRIM(RTRIM(Artikel)) AS Material,
    ISNULL(SUM(Gutteile), 0) AS GemachteMenge
FROM dbo.Table1
WHERE CAST(FSKdate AS date) = @PlanDatum
  AND LTRIM(RTRIM(ISNULL(Personalnummer, N''))) = @Personalnummer
  AND ISNULL(LTRIM(RTRIM(Artikel)), N'') <> N''
GROUP BY LTRIM(RTRIM(Artikel));",
                new
                {
                    PlanDatum = normalizedPlanDate,
                    Personalnummer = normalizedPersonalnummer
                }))
                .ToList();

            totalProducedToday = producedRows.Sum(row => row.GemachteMenge);
        }

        if (assignedMaterials.Count == 1)
        {
            var material = assignedMaterials[0];
            return
            [
                new SchichtplanSauberraumProgressModel
                {
                    MaterialId = material.MaterialId,
                    Material = material.Material,
                    ZielMenge = material.ZielMenge,
                    GemachteMenge = totalProducedToday
                }
            ];
        }

        var producedByAssignedMaterial = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var producedRow in producedRows)
        {
            var matchedMaterial = MatchAssignedMaterial(assignedMaterials, producedRow.Material);
            if (matchedMaterial is null)
            {
                continue;
            }

            producedByAssignedMaterial[matchedMaterial.Material] =
                producedByAssignedMaterial.GetValueOrDefault(matchedMaterial.Material) + producedRow.GemachteMenge;
        }

        return assignedMaterials
            .Select(material => new SchichtplanSauberraumProgressModel
            {
                MaterialId = material.MaterialId,
                Material = material.Material,
                ZielMenge = material.ZielMenge,
                GemachteMenge = producedByAssignedMaterial.TryGetValue(material.Material, out var gemachteMenge) ? gemachteMenge : 0
            })
            .OrderBy(material => material.Material, StringComparer.Create(System.Globalization.CultureInfo.GetCultureInfo("de-DE"), true))
            .ToList();
    }

    private static SauberraumAssignedMaterialRow? MatchAssignedMaterial(
        List<SauberraumAssignedMaterialRow> assignedMaterials,
        string producedMaterial)
    {
        SauberraumAssignedMaterialRow? bestMatch = null;
        var bestScore = 0;

        foreach (var assignedMaterial in assignedMaterials)
        {
            var score = ScoreMaterialMatch(assignedMaterial.Material, producedMaterial);
            if (score > bestScore)
            {
                bestScore = score;
                bestMatch = assignedMaterial;
            }
        }

        return bestScore >= 2 ? bestMatch : null;
    }

    private static int ScoreMaterialMatch(string assignedMaterial, string producedMaterial)
    {
        var assignedTokens = TokenizeMaterialName(assignedMaterial);
        var producedTokens = TokenizeMaterialName(producedMaterial);

        if (assignedTokens.Count == 0 || producedTokens.Count == 0)
        {
            return 0;
        }

        var commonTokens = assignedTokens.Intersect(producedTokens, StringComparer.OrdinalIgnoreCase).ToList();
        var assignedCompact = string.Concat(assignedTokens);
        var producedCompact = string.Concat(producedTokens);

        var score = commonTokens.Count;

        if (!string.IsNullOrWhiteSpace(assignedCompact) &&
            !string.IsNullOrWhiteSpace(producedCompact) &&
            (producedCompact.Contains(assignedCompact, StringComparison.OrdinalIgnoreCase) ||
             assignedCompact.Contains(producedCompact, StringComparison.OrdinalIgnoreCase)))
        {
            score += 2;
        }

        if (assignedTokens.Count <= 3 && commonTokens.Count == assignedTokens.Count)
        {
            score += 1;
        }

        return score;
    }

    private static List<string> TokenizeMaterialName(string? material)
    {
        if (string.IsNullOrWhiteSpace(material))
        {
            return [];
        }

        var cleaned = new string(
            material
                .ToUpperInvariant()
                .Select(ch => char.IsLetterOrDigit(ch) ? ch : ' ')
                .ToArray());

        return cleaned
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(token => token.Length > 1)
            .Where(token => token is not "BLENDE" and not "TEIL" and not "SATZ" and not "SET")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static SchichtplanBoardModel BuildBoard(
        DateTime planDate,
        PlanRow? plan,
        List<WorkplaceRow> workplaces,
        List<EntryRow> entries,
        List<AssignmentRow> assignments)
    {
        var assignmentLookup = assignments
            .GroupBy(item => item.SchichtplanEintragID)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderBy(item => item.Sortierung)
                    .Select(item => new SchichtplanAssignedUserModel
                    {
                        AssignmentId = item.ID,
                        DisplayName = item.Benutzer,
                        Source = item.BenutzerQuelle,
                        Key = item.BenutzerSchluessel,
                        Personalnummer = NormalizeNullable(item.Personalnummer),
                        Sortierung = item.Sortierung
                    })
                    .ToList());

        var entryLookup = entries.ToDictionary(
            entry => $"{entry.ArbeitsplatzID}|{entry.Schicht}",
            entry => entry);

        var sections = workplaces
            .Select(item => new
            {
                Workplace = item,
                DisplayArea = NormalizeAreaName(item.Bereich, item.ArbeitsplatzName),
                DisplaySort = NormalizeAreaSort(item.Bereich, item.ArbeitsplatzName, item.BereichSortierung)
            })
            .GroupBy(item => new { item.DisplayArea, item.DisplaySort })
            .OrderBy(group => group.Key.DisplaySort)
            .Select(group => new SchichtplanSectionModel
            {
                Name = group.Key.DisplayArea,
                ThemeClass = MapBoardThemeClass(group.Key.DisplayArea),
                Sortierung = group.Key.DisplaySort,
                Workplaces = group
                    .OrderBy(item => item.Workplace.ArbeitsplatzSortierung)
                    .ThenBy(item => item.Workplace.ArbeitsplatzName)
                    .Select(item => new SchichtplanWorkplaceModel
                    {
                        ArbeitsplatzId = item.Workplace.ID,
                        ArbeitsplatzNr = NormalizeWorkplaceNumber(item.Workplace.ArbeitsplatzNr, item.Workplace.ArbeitsplatzName),
                        ArbeitsplatzName = item.Workplace.ArbeitsplatzName,
                        Shifts = SchichtplanKonstanten.AlleSchichten
                            .Select(shift =>
                            {
                                entryLookup.TryGetValue($"{item.Workplace.ID}|{shift}", out var entry);

                                return new SchichtplanCellModel
                                {
                                    EntryId = entry?.ID,
                                    Shift = shift,
                                    MaterialStammId = entry?.MaterialStammID,
                                    Material = NormalizeNullable(entry?.Material),
                                    MaterialTagesMenge = entry?.MaterialTagesMenge,
                                    MaterialStammId2 = entry?.MaterialStammID2,
                                    Material2 = NormalizeNullable(entry?.Material2),
                                    Material2TagesMenge = entry?.Material2TagesMenge,
                                    FANr = NormalizeNullable(entry?.FA_Nr),
                                    Bemerkung = NormalizeNullable(entry?.Bemerkung),
                                    LastUpdatedAt = entry?.ZuletztGeaendertAm,
                                    AssignedUsers = entry is not null && assignmentLookup.TryGetValue(entry.ID, out var users)
                                        ? users
                                        : []
                                };
                            })
                            .ToList()
                    })
                    .ToList()
            })
            .ToList();

        return new SchichtplanBoardModel
        {
            PlanDatum = planDate,
            Kalenderwoche = plan?.Kalenderwoche ?? SchichtplanKonstanten.Kalenderwoche(planDate),
            LastUpdatedAt = plan?.ZuletztGeaendertAm,
            LastUpdatedBy = NormalizeNullable(plan?.ZuletztGeaendertVon),
            Sections = sections
        };
    }

    private static string MapThemeClass(string area) => area switch
    {
        "Thermoformung" => "theme-thermo",
        "Fräsen" => "theme-mill",
        "Stanzen" => "theme-stanzen",
        "UV" => "theme-uv",
        "Ohne Bereich" => "theme-flex",
        "Sauberraum" => "theme-cleanroom",
        "Sonstiges" => "theme-misc",
        _ => "theme-misc"
    };

    private static string MapBoardThemeClass(string area) => area switch
    {
        "Thermoformung" => "theme-thermo",
        "Fräsen" => "theme-mill",
        "Stanzen" => "theme-stanzen",
        "UV" => "theme-uv",
        "UV-Anlage" => "theme-uv",
        "Biegemaschine" => "theme-bending",
        "Ohne Bereich" => "theme-bending",
        "Sauberraum" => "theme-cleanroom",
        "Sonstiges" => "theme-misc",
        _ => "theme-misc"
    };

    private static string NormalizeAreaName(string? area, string? workplaceName)
    {
        if (string.Equals(workplaceName, "Biegemaschine", StringComparison.OrdinalIgnoreCase))
        {
            return "Biegemaschine";
        }

        if (string.Equals(area, "Ohne Bereich", StringComparison.OrdinalIgnoreCase))
        {
            return "Biegemaschine";
        }

        if (string.Equals(area, "UV", StringComparison.OrdinalIgnoreCase))
        {
            return "UV-Anlage";
        }

        return NormalizeNullable(area) ?? "Sonstiges";
    }

    private static int NormalizeAreaSort(string? area, string? workplaceName, int areaSort) =>
        string.Equals(workplaceName, "Biegemaschine", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(area, "Ohne Bereich", StringComparison.OrdinalIgnoreCase)
            ? 50
            : areaSort;

    private static string? NormalizeWorkplaceNumber(string? workplaceNumber, string? workplaceName)
    {
        var normalizedNumber = NormalizeNullable(workplaceNumber);
        if (normalizedNumber is not null)
        {
            return normalizedNumber;
        }

        return string.Equals(workplaceName, "Biegemaschine", StringComparison.OrdinalIgnoreCase)
            ? "005250"
            : null;
    }

    private static void ValidateShift(string shift)
    {
        if (!SchichtplanKonstanten.AlleSchichten.Contains(shift))
        {
            throw new InvalidOperationException("Unbekannte Schicht.");
        }
    }

    private static void ValidateMaterialSlot(int materialSlot)
    {
        if (materialSlot is < 1 or > 2)
        {
            throw new InvalidOperationException("Unbekannter Material-Slot.");
        }
    }

    private static string NormalizeAuditName(string? changedBy) =>
        NormalizeNullable(changedBy) ?? "Schichtplan";

    private static string? NormalizeNullable(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }

    private static DateTime GetAutomaticPlanDate()
    {
        var now = TimeZoneInfo.ConvertTime(DateTime.UtcNow, PlanTimeZone);
        return now.TimeOfDay >= PlanDateRolloverTime
            ? now.Date.AddDays(1)
            : now.Date;
    }

    private static TimeZoneInfo ResolvePlanTimeZone()
    {
        foreach (var timeZoneId in new[] { "Europe/Berlin", "W. Europe Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return TimeZoneInfo.Local;
    }

    private async Task<int> EnsurePlanAsync(SqlConnection connection, SqlTransaction transaction, DateTime planDate, string changedBy)
    {
        var existingId = await connection.ExecuteScalarAsync<int?>(
            @"SELECT TOP (1) ID
              FROM dbo.SchichtplanPlan
              WHERE PlanDatum = @PlanDatum;",
            new { PlanDatum = planDate.Date },
            transaction);

        if (existingId.HasValue)
        {
            return existingId.Value;
        }

        return await connection.ExecuteScalarAsync<int>(
            @"INSERT INTO dbo.SchichtplanPlan (PlanDatum, Kalenderwoche, CreatedBy, ZuletztGeaendertVon)
              OUTPUT INSERTED.ID
              VALUES (@PlanDatum, @Kalenderwoche, @ChangedBy, @ChangedBy);",
            new
            {
                PlanDatum = planDate.Date,
                Kalenderwoche = SchichtplanKonstanten.Kalenderwoche(planDate),
                ChangedBy = NormalizeAuditName(changedBy)
            },
            transaction);
    }

    private async Task<int> EnsureEntryAsync(SqlConnection connection, SqlTransaction transaction, int planId, int workplaceId, string shift, string changedBy)
    {
        var existingId = await connection.ExecuteScalarAsync<int?>(
            @"SELECT TOP (1) ID
              FROM dbo.SchichtplanEintrag
              WHERE SchichtplanPlanID = @PlanId
                AND ArbeitsplatzID = @WorkplaceId
                AND Schicht = @Shift;",
            new
            {
                PlanId = planId,
                WorkplaceId = workplaceId,
                Shift = shift
            },
            transaction);

        if (existingId.HasValue)
        {
            return existingId.Value;
        }

        return await connection.ExecuteScalarAsync<int>(
            @"INSERT INTO dbo.SchichtplanEintrag (SchichtplanPlanID, ArbeitsplatzID, Schicht, CreatedBy, ZuletztGeaendertVon)
              OUTPUT INSERTED.ID
              VALUES (@PlanId, @WorkplaceId, @Shift, @ChangedBy, @ChangedBy);",
            new
            {
                PlanId = planId,
                WorkplaceId = workplaceId,
                Shift = shift,
                ChangedBy = NormalizeAuditName(changedBy)
            },
            transaction);
    }

    private async Task<int?> GetEntryIdAsync(SqlConnection connection, SqlTransaction transaction, DateTime planDate, int workplaceId, string shift)
    {
        return await connection.ExecuteScalarAsync<int?>(
            @"SELECT TOP (1) entryRow.ID
              FROM dbo.SchichtplanEintrag entryRow
              INNER JOIN dbo.SchichtplanPlan sp ON sp.ID = entryRow.SchichtplanPlanID
              WHERE sp.PlanDatum = @PlanDatum
                AND entryRow.ArbeitsplatzID = @WorkplaceId
                AND entryRow.Schicht = @Shift;",
            new
            {
                PlanDatum = planDate.Date,
                WorkplaceId = workplaceId,
                Shift = shift
            },
            transaction);
    }

    private static async Task TouchEntryAsync(SqlConnection connection, SqlTransaction transaction, int entryId, string changedBy)
    {
        await connection.ExecuteAsync(
            @"UPDATE dbo.SchichtplanEintrag
              SET ZuletztGeaendertAm = SYSDATETIME(),
                  ZuletztGeaendertVon = @ChangedBy
              WHERE ID = @EntryId;",
            new
            {
                EntryId = entryId,
                ChangedBy = NormalizeAuditName(changedBy)
            },
            transaction);
    }

    private static async Task TouchPlanAsync(SqlConnection connection, SqlTransaction transaction, int planId, DateTime planDate, string changedBy)
    {
        await connection.ExecuteAsync(
            @"UPDATE dbo.SchichtplanPlan
              SET Kalenderwoche = @Kalenderwoche,
                  ZuletztGeaendertAm = SYSDATETIME(),
                  ZuletztGeaendertVon = @ChangedBy
              WHERE ID = @PlanId;",
            new
            {
                PlanId = planId,
                Kalenderwoche = SchichtplanKonstanten.Kalenderwoche(planDate),
                ChangedBy = NormalizeAuditName(changedBy)
            },
            transaction);
    }

    private static async Task CleanupEntryIfEmptyAsync(SqlConnection connection, SqlTransaction transaction, int entryId)
    {
        await connection.ExecuteAsync(
            @"
IF EXISTS
(
    SELECT 1
    FROM dbo.SchichtplanEintrag entryRow
    WHERE entryRow.ID = @EntryId
      AND ISNULL(LTRIM(RTRIM(entryRow.Material)), N'') = N''
      AND ISNULL(LTRIM(RTRIM(entryRow.Material2)), N'') = N''
      AND ISNULL(LTRIM(RTRIM(entryRow.FA_Nr)), N'') = N''
      AND ISNULL(LTRIM(RTRIM(entryRow.Bemerkung)), N'') = N''
      AND NOT EXISTS
      (
          SELECT 1
          FROM dbo.SchichtplanEintragBenutzer userRow
          WHERE userRow.SchichtplanEintragID = entryRow.ID
      )
)
BEGIN
    DELETE FROM dbo.SchichtplanEintrag
    WHERE ID = @EntryId;
END;",
            new { EntryId = entryId },
            transaction);
    }

    private sealed class PlanRow
    {
        public int ID { get; set; }
        public DateTime PlanDatum { get; set; }
        public int Kalenderwoche { get; set; }
        public DateTime? ZuletztGeaendertAm { get; set; }
        public string? ZuletztGeaendertVon { get; set; }
    }

    private sealed class PlanCopyRow
    {
        public int ID { get; set; }
        public string? Titel { get; set; }
        public string? Bemerkung { get; set; }
    }

    private sealed class WorkplaceRow
    {
        public int ID { get; set; }
        public string Bereich { get; set; } = string.Empty;
        public int BereichSortierung { get; set; }
        public string? ArbeitsplatzNr { get; set; }
        public string ArbeitsplatzName { get; set; } = string.Empty;
        public int ArbeitsplatzSortierung { get; set; }
    }

    private sealed class EntryRow
    {
        public int ID { get; set; }
        public int ArbeitsplatzID { get; set; }
        public string Schicht { get; set; } = string.Empty;
        public int? MaterialStammID { get; set; }
        public string? Material { get; set; }
        public int? MaterialTagesMenge { get; set; }
        public int? MaterialZielMenge { get; set; }
        public int? MaterialStammID2 { get; set; }
        public string? Material2 { get; set; }
        public int? Material2TagesMenge { get; set; }
        public int? Material2ZielMenge { get; set; }
        public string? FA_Nr { get; set; }
        public string? Bemerkung { get; set; }
        public DateTime? ZuletztGeaendertAm { get; set; }
    }

    private sealed class AssignmentRow
    {
        public int ID { get; set; }
        public int SchichtplanEintragID { get; set; }
        public string Benutzer { get; set; } = string.Empty;
        public string BenutzerQuelle { get; set; } = string.Empty;
        public string BenutzerSchluessel { get; set; } = string.Empty;
        public string? Personalnummer { get; set; }
        public byte Sortierung { get; set; }
    }

    private sealed class AssignmentRemoveRow
    {
        public int ID { get; set; }
        public string Benutzer { get; set; } = string.Empty;
        public int SchichtplanEintragID { get; set; }
        public int SchichtplanPlanID { get; set; }
        public DateTime PlanDatum { get; set; }
    }

    private sealed class MaterialRow
    {
        public int Id { get; set; }
        public string Material { get; set; } = string.Empty;
        public int? TagesMenge { get; set; }
    }

    private sealed class SauberraumAssignedMaterialRow
    {
        public int? MaterialId { get; set; }
        public string Material { get; set; } = string.Empty;
        public int? ZielMenge { get; set; }
    }

    private sealed class ProducedMaterialRow
    {
        public string Material { get; set; } = string.Empty;
        public int GemachteMenge { get; set; }
    }
}
