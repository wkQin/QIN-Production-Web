using Dapper;
using Microsoft.Data.SqlClient;

namespace QIN_Production_Web.Data;

public sealed class LiveFertigungService
{
    private static readonly int[] DefaultTableOrder = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12];
    private static readonly TimeSpan PlanDateRolloverTime = new(21, 45, 0);
    private static readonly TimeZoneInfo PlanTimeZone = ResolvePlanTimeZone();

    private readonly string _fertigungConnectionString = SqlManager.FertigungConnectionString;
    private readonly string _mainConnectionString = SqlManager.connectionString;

    public async Task<IReadOnlyDictionary<int, LiveFertigungEndkontrolleTableModel>> GetEndkontrolleTablesAsync(DateTime? planDate = null)
    {
        await SchichtplanSchemaService.EnsureZielSnapshotColumnsAsync(_fertigungConnectionString);

        var normalizedDate = (planDate ?? GetAutomaticPlanDate()).Date;
        var assignments = await GetSauberraumAssignmentsAsync(normalizedDate);
        var productionRows = await GetProductionRowsAsync(normalizedDate.AddDays(-6), normalizedDate);
        var historicalTargets = await GetHistoricalTargetsAsync(normalizedDate.AddDays(-6), normalizedDate.AddDays(-1));

        var tables = DefaultTableOrder
            .ToDictionary(
                tableNumber => tableNumber,
                tableNumber => CreateEmptyTable(tableNumber));

        var groupedProductionRows = productionRows
            .GroupBy(row => NormalizeNullable(row.Personalnummer) ?? row.Benutzer, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);

        foreach (var tableGroup in assignments.Where(assignment => assignment.TableNumber is >= 1 and <= 12).GroupBy(assignment => assignment.TableNumber!.Value))
        {
            tables[tableGroup.Key] = BuildOccupiedTable(tableGroup.Key, tableGroup.ToList(), groupedProductionRows, historicalTargets, normalizedDate);
        }

        return tables;
    }

    private async Task<List<LiveAssignmentRow>> GetSauberraumAssignmentsAsync(DateTime planDate)
    {
        using var connection = new SqlConnection(_fertigungConnectionString);
        await connection.OpenAsync();

        var rows = (await connection.QueryAsync<LiveAssignmentMaterialRow>(
            @"
SELECT
    ben.ID AS AssignmentId,
    ben.Benutzer,
    ben.Personalnummer,
    ben.Sortierung,
    ap.ArbeitsplatzName,
    ap.ArbeitsplatzNr,
    ap.ArbeitsplatzSortierung,
    e.ID AS EntryId,
    1 AS MaterialSlot,
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

UNION ALL

SELECT
    ben.ID AS AssignmentId,
    ben.Benutzer,
    ben.Personalnummer,
    ben.Sortierung,
    ap.ArbeitsplatzName,
    ap.ArbeitsplatzNr,
    ap.ArbeitsplatzSortierung,
    e.ID AS EntryId,
    2 AS MaterialSlot,
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
ORDER BY ArbeitsplatzSortierung, EntryId, Sortierung, AssignmentId, MaterialSlot;",
            new { PlanDatum = planDate }))
            .ToList();

        return rows
            .GroupBy(row => row.AssignmentId)
            .Select(group =>
            {
                var first = group.First();
                var materialRows = group
                    .OrderBy(row => row.MaterialSlot)
                    .Select(row => new
                    {
                        Material = NormalizeNullable(row.Material),
                        ZielMenge = row.ZielMenge ?? 0
                    })
                    .ToList();
                var hasMaterial = materialRows.Any(row => row.Material is not null);

                return new LiveAssignmentRow
                {
                    AssignmentId = first.AssignmentId,
                    Benutzer = NormalizeNullable(first.Benutzer) ?? "Unbekannt",
                    Personalnummer = NormalizeNullable(first.Personalnummer),
                    TableNumber = ResolveTableNumber(first.ArbeitsplatzName, first.ArbeitsplatzSortierung),
                    ArbeitsplatzSortierung = first.ArbeitsplatzSortierung,
                    Sortierung = first.Sortierung,
                    Materials = materialRows
                        .Where(row => hasMaterial ? row.Material is not null : true)
                        .Select(row => new LiveAssignmentMaterial
                        {
                            Material = row.Material ?? "Ohne Material",
                            ZielMenge = row.ZielMenge
                        })
                        .ToList()
                };
            })
            .OrderBy(row => row.ArbeitsplatzSortierung)
            .ThenBy(row => row.Sortierung)
            .ThenBy(row => row.Benutzer, StringComparer.Create(System.Globalization.CultureInfo.GetCultureInfo("de-DE"), true))
            .ToList();
    }

    private async Task<List<LiveProductionRow>> GetProductionRowsAsync(DateTime fromDate, DateTime toDate)
    {
        using var connection = new SqlConnection(_mainConnectionString);
        await connection.OpenAsync();

        return (await connection.QueryAsync<LiveProductionRow>(
            @"
SELECT
    t.FSKdate,
    LTRIM(RTRIM(ISNULL(t.Artikel, N''))) AS Material,
    LTRIM(RTRIM(ISNULL(t.Projekt, N''))) AS Projekt,
    LTRIM(RTRIM(ISNULL(t.Dekor, N''))) AS Dekor,
    LTRIM(RTRIM(ISNULL(t.Charge, N''))) AS Charge,
    ISNULL(t.Gutteile, 0) AS Good,
    ISNULL(t.Fusseln, 0)
        + ISNULL(t.Nadelstiche, 0)
        + ISNULL(t.Pickel, 0)
        + ISNULL(t.Dekorfehler, 0)
        + ISNULL(t.Color, 0)
        + ISNULL(t.Flecken, 0)
        + ISNULL(t.Nebel, 0)
        + ISNULL(t.Vertiefung, 0)
        + ISNULL(t.Oelflecken, 0)
        + ISNULL(t.Tiefziehfehler, 0)
        + ISNULL(t.Fraesfehler, 0)
        + ISNULL(t.Knicke, 0)
        + ISNULL(t.Kratzer, 0) AS Bad,
    LTRIM(RTRIM(ISNULL(t.Bemerkungen, N''))) AS Note,
    LTRIM(RTRIM(ISNULL(t.Personalnummer, N''))) AS Personalnummer,
    COALESCE(NULLIF(LTRIM(RTRIM(l.Benutzer)), N''), LTRIM(RTRIM(ISNULL(t.Personalnummer, N'')))) AS Benutzer
FROM dbo.Table1 t
LEFT JOIN dbo.LoginDaten l
    ON ISNULL(CAST(t.Personalnummer AS NVARCHAR(100)), N'') = ISNULL(CAST(l.Personalnummer AS NVARCHAR(100)), N'')
WHERE t.FSKdate >= @FromDate
  AND t.FSKdate < DATEADD(day, 1, @ToDate)
ORDER BY t.FSKdate DESC;",
            new
            {
                FromDate = fromDate.Date,
                ToDate = toDate.Date
            }))
            .ToList();
    }

    private async Task<List<LiveHistoricalTargetRow>> GetHistoricalTargetsAsync(DateTime fromDate, DateTime toDate)
    {
        if (toDate < fromDate)
        {
            return [];
        }

        using var connection = new SqlConnection(_fertigungConnectionString);
        await connection.OpenAsync();

        return (await connection.QueryAsync<LiveHistoricalTargetRow>(
            @"
WITH TargetRows AS
(
    SELECT
        p.PlanDatum,
        ap.ArbeitsplatzName,
        COALESCE(m1.Material, e.Material) AS Material,
        CASE
            WHEN e.MaterialZielMenge IS NOT NULL THEN e.MaterialZielMenge
            WHEN m1.ID IS NULL THEN NULL
            WHEN CAST(m1.CreatedAt AS date) > p.PlanDatum THEN NULL
            WHEN m1.UpdatedAt IS NOT NULL AND CAST(m1.UpdatedAt AS date) > p.PlanDatum THEN NULL
            ELSE m1.TagesMenge
        END AS ZielMenge
    FROM dbo.SchichtplanPlan p
    INNER JOIN dbo.SchichtplanEintrag e
        ON e.SchichtplanPlanID = p.ID
    INNER JOIN dbo.SchichtplanArbeitsplatz ap
        ON ap.ID = e.ArbeitsplatzID
    LEFT JOIN dbo.SchichtplanMaterialStamm m1
        ON m1.ID = e.MaterialStammID
    WHERE p.PlanDatum >= @FromDate
      AND p.PlanDatum <= @ToDate
      AND ap.Bereich = N'Sauberraum'
      AND ISNULL(LTRIM(RTRIM(COALESCE(m1.Material, e.Material))), N'') <> N''

    UNION ALL

    SELECT
        p.PlanDatum,
        ap.ArbeitsplatzName,
        COALESCE(m2.Material, e.Material2) AS Material,
        CASE
            WHEN e.Material2ZielMenge IS NOT NULL THEN e.Material2ZielMenge
            WHEN m2.ID IS NULL THEN NULL
            WHEN CAST(m2.CreatedAt AS date) > p.PlanDatum THEN NULL
            WHEN m2.UpdatedAt IS NOT NULL AND CAST(m2.UpdatedAt AS date) > p.PlanDatum THEN NULL
            ELSE m2.TagesMenge
        END AS ZielMenge
    FROM dbo.SchichtplanPlan p
    INNER JOIN dbo.SchichtplanEintrag e
        ON e.SchichtplanPlanID = p.ID
    INNER JOIN dbo.SchichtplanArbeitsplatz ap
        ON ap.ID = e.ArbeitsplatzID
    LEFT JOIN dbo.SchichtplanMaterialStamm m2
        ON m2.ID = e.MaterialStammID2
    WHERE p.PlanDatum >= @FromDate
      AND p.PlanDatum <= @ToDate
      AND ap.Bereich = N'Sauberraum'
      AND ISNULL(LTRIM(RTRIM(COALESCE(m2.Material, e.Material2))), N'') <> N''
)
SELECT
    PlanDatum,
    ArbeitsplatzName,
    Material,
    MAX(ZielMenge) AS ZielMenge
FROM TargetRows
GROUP BY PlanDatum, ArbeitsplatzName, Material;",
            new
            {
                FromDate = fromDate.Date,
                ToDate = toDate.Date
            }))
            .Select(row =>
            {
                row.TableNumber = ResolveTableNumber(row.ArbeitsplatzName, 0);
                return row;
            })
            .ToList();
    }

    private static List<LiveProductionRow> GetRowsForAssignment(
        IReadOnlyDictionary<string, List<LiveProductionRow>> groupedRows,
        LiveAssignmentRow assignment)
    {
        if (!string.IsNullOrWhiteSpace(assignment.Personalnummer) &&
            groupedRows.TryGetValue(assignment.Personalnummer, out var rowsByPersonalnummer))
        {
            return rowsByPersonalnummer;
        }

        return groupedRows.TryGetValue(assignment.Benutzer, out var rowsByName)
            ? rowsByName
            : [];
    }

    private static List<LiveProductionRow> GetFilteredRowsForAssignment(
        IReadOnlyDictionary<string, List<LiveProductionRow>> groupedRows,
        LiveAssignmentRow assignment)
    {
        var rows = GetRowsForAssignment(groupedRows, assignment);
        if (rows.Count == 0)
        {
            return [];
        }

        var assignedMaterials = assignment.Materials
            .Select(material => NormalizeNullable(material.Material))
            .Where(material => !string.IsNullOrWhiteSpace(material))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (assignedMaterials.Count == 0)
        {
            return rows;
        }

        var matchedRows = new List<LiveProductionRow>();

        foreach (var assignedMaterial in assignedMaterials)
        {
            var strongMatches = rows
                .Where(row => MaterialMatchHelper.IsStrongAssignedMaterialMatch(row.Material, row.Projekt, row.Dekor, assignedMaterial))
                .ToList();

            if (strongMatches.Count > 0)
            {
                matchedRows.AddRange(strongMatches);
                continue;
            }

            var fallbackScores = rows
                .Select(row => new
                {
                    Row = row,
                    Score = MaterialMatchHelper.GetAssignedMaterialFallbackScore(row.Material, row.Projekt, row.Dekor, assignedMaterial)
                })
                .Where(entry => entry.Score > 0)
                .ToList();

            if (fallbackScores.Count == 0)
            {
                continue;
            }

            int bestFallbackScore = fallbackScores.Max(entry => entry.Score);
            matchedRows.AddRange(fallbackScores
                .Where(entry => entry.Score == bestFallbackScore)
                .Select(entry => entry.Row));
        }

        return matchedRows
            .Distinct()
            .ToList();
    }

    private static LiveFertigungEndkontrolleTableModel BuildOccupiedTable(
        int tableNumber,
        IReadOnlyList<LiveAssignmentRow> assignments,
        IReadOnlyDictionary<string, List<LiveProductionRow>> groupedProductionRows,
        IReadOnlyList<LiveHistoricalTargetRow> historicalTargets,
        DateTime planDate)
    {
        var allProductionRows = assignments
            .SelectMany(assignment => GetRowsForAssignment(groupedProductionRows, assignment))
            .GroupBy(row => new
            {
                row.FSKdate,
                row.Material,
                row.Projekt,
                row.Dekor,
                row.Charge,
                row.Good,
                row.Bad,
                row.Note,
                row.Personalnummer,
                row.Benutzer
            })
            .Select(group => group.First())
            .ToList();

        var matchedProductionRows = assignments
            .SelectMany(assignment => GetFilteredRowsForAssignment(groupedProductionRows, assignment))
            .GroupBy(row => new
            {
                row.FSKdate,
                row.Material,
                row.Projekt,
                row.Dekor,
                row.Charge,
                row.Good,
                row.Bad,
                row.Note,
                row.Personalnummer,
                row.Benutzer
            })
            .Select(group => group.First())
            .ToList();

        var matchedRowKeys = matchedProductionRows
            .Select(CreateProductionRowKey)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var todayRows = matchedProductionRows
            .Where(row => row.FSKdate.Date == planDate)
            .OrderByDescending(row => row.FSKdate)
            .ToList();

        var materials = assignments
            .SelectMany(assignment => assignment.Materials)
            .Select(material => material.Material)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var targetQuantity = assignments.SelectMany(assignment => assignment.Materials).Sum(material => material.ZielMenge);

        return new LiveFertigungEndkontrolleTableModel
        {
            TableNumber = tableNumber,
            IsOccupied = true,
            User = string.Join(", ", assignments.Select(assignment => assignment.Benutzer).Distinct(StringComparer.OrdinalIgnoreCase)),
            Personalnummer = string.Join(", ", assignments.Select(assignment => assignment.Personalnummer).Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.OrdinalIgnoreCase)),
            Materials = materials,
            DoneQuantity = todayRows.Sum(row => row.Good),
            BadQuantity = todayRows.Sum(row => row.Bad),
            TargetQuantity = targetQuantity,
            WeekEntries = allProductionRows
                .Where(row => row.FSKdate.Date >= planDate.AddDays(-6) && row.FSKdate.Date <= planDate)
                .OrderByDescending(row => row.FSKdate)
                .Take(14)
                .Select(row => new LiveFertigungWeekEntryModel
                {
                    Date = row.FSKdate.ToString("dd.MM."),
                    Material = string.IsNullOrWhiteSpace(row.Material) ? "Ohne Artikel" : row.Material,
                    Charge = string.IsNullOrWhiteSpace(row.Charge) ? "Ohne Charge" : row.Charge,
                    Good = row.Good,
                    Bad = row.Bad,
                    Note = row.Note,
                    MatchesAssignedMaterial = matchedRowKeys.Contains(CreateProductionRowKey(row))
                })
                .ToList(),
            HistoryEntries = allProductionRows
                .Where(row => row.FSKdate.Date < planDate)
                .GroupBy(row => new { Date = row.FSKdate.Date, Material = string.IsNullOrWhiteSpace(row.Material) ? "Ohne Artikel" : row.Material })
                .OrderByDescending(group => group.Key.Date)
                .ThenBy(group => group.Key.Material)
                .Take(8)
                .Select(group => new LiveFertigungHistoryEntryModel
                {
                    Date = group.Key.Date.ToString("dd.MM."),
                    Material = group.Key.Material,
                    Done = group.Sum(row => row.Good),
                    Target = ResolveHistoricalTarget(historicalTargets, tableNumber, group.Key.Date, group.Key.Material),
                    MatchesAssignedMaterial = group.All(row => matchedRowKeys.Contains(CreateProductionRowKey(row)))
                })
                .ToList()
        };
    }

    private static string CreateProductionRowKey(LiveProductionRow row)
    {
        return string.Join("|",
            row.FSKdate.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
            row.Material,
            row.Projekt,
            row.Dekor,
            row.Charge,
            row.Good.ToString(System.Globalization.CultureInfo.InvariantCulture),
            row.Bad.ToString(System.Globalization.CultureInfo.InvariantCulture),
            row.Note,
            row.Personalnummer,
            row.Benutzer);
    }

    private static int? ResolveHistoricalTarget(
        IReadOnlyList<LiveHistoricalTargetRow> historicalTargets,
        int tableNumber,
        DateTime productionDate,
        string producedMaterial)
    {
        var match = historicalTargets
            .Where(row => row.TableNumber == tableNumber && row.PlanDatum.Date == productionDate.Date && row.ZielMenge.HasValue)
            .OrderByDescending(row => ScoreMaterialMatch(row.Material, producedMaterial))
            .FirstOrDefault(row => ScoreMaterialMatch(row.Material, producedMaterial) >= 2);

        return match?.ZielMenge;
    }

    private static LiveFertigungEndkontrolleTableModel CreateEmptyTable(int tableNumber) =>
        new()
        {
            TableNumber = tableNumber,
            IsOccupied = false,
            User = "-",
            Materials = [],
            DoneQuantity = 0,
            BadQuantity = 0,
            TargetQuantity = 0,
            WeekEntries = [],
            HistoryEntries = []
        };

    private static int? ResolveTableNumber(string? workplaceName, int workplaceSort)
    {
        var normalizedName = NormalizeNullable(workplaceName);
        if (normalizedName is not null && normalizedName.Contains("Tisch", StringComparison.OrdinalIgnoreCase))
        {
            var digits = new string(normalizedName.Where(char.IsDigit).ToArray());
            if (int.TryParse(digits, out var numberFromName) && numberFromName is >= 1 and <= 12)
            {
                return numberFromName;
            }
        }

        if (workplaceSort is >= 10 and <= 120 && workplaceSort % 10 == 0)
        {
            return workplaceSort / 10;
        }

        return null;
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

    private static string? NormalizeNullable(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

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

    private sealed class LiveAssignmentMaterialRow
    {
        public int AssignmentId { get; set; }
        public string? Benutzer { get; set; }
        public string? Personalnummer { get; set; }
        public byte Sortierung { get; set; }
        public string? ArbeitsplatzName { get; set; }
        public string? ArbeitsplatzNr { get; set; }
        public int ArbeitsplatzSortierung { get; set; }
        public int MaterialSlot { get; set; }
        public string? Material { get; set; }
        public int? ZielMenge { get; set; }
    }

    private sealed class LiveAssignmentRow
    {
        public int AssignmentId { get; set; }
        public string Benutzer { get; set; } = string.Empty;
        public string? Personalnummer { get; set; }
        public int? TableNumber { get; set; }
        public byte Sortierung { get; set; }
        public int ArbeitsplatzSortierung { get; set; }
        public IReadOnlyList<LiveAssignmentMaterial> Materials { get; set; } = [];
    }

    private sealed class LiveAssignmentMaterial
    {
        public string Material { get; set; } = string.Empty;
        public int ZielMenge { get; set; }
    }

    private sealed class LiveHistoricalTargetRow
    {
        public DateTime PlanDatum { get; set; }
        public string ArbeitsplatzName { get; set; } = string.Empty;
        public int? TableNumber { get; set; }
        public string Material { get; set; } = string.Empty;
        public int? ZielMenge { get; set; }
    }

    private sealed class LiveProductionRow
    {
        public DateTime FSKdate { get; set; }
        public string Material { get; set; } = string.Empty;
        public string Projekt { get; set; } = string.Empty;
        public string Dekor { get; set; } = string.Empty;
        public string Charge { get; set; } = string.Empty;
        public int Good { get; set; }
        public int Bad { get; set; }
        public string Note { get; set; } = string.Empty;
        public string Personalnummer { get; set; } = string.Empty;
        public string Benutzer { get; set; } = string.Empty;
    }
}
