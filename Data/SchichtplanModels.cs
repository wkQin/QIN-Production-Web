using System.Globalization;

namespace QIN_Production_Web.Data;

public static class SchichtplanKonstanten
{
    public const string Nachtschicht = "Nachtschicht";
    public const string Frühschicht = "Frühschicht";
    public const string Spätschicht = "Spätschicht";

    public static readonly string[] AlleSchichten =
    [
        Nachtschicht,
        Frühschicht,
        Spätschicht
    ];

    public static int Kalenderwoche(DateTime planDatum) =>
        ISOWeek.GetWeekOfYear(planDatum.Date);
}

public sealed class SchichtplanBoardModel
{
    public DateTime PlanDatum { get; set; }
    public int Kalenderwoche { get; set; }
    public DateTime? LastUpdatedAt { get; set; }
    public string? LastUpdatedBy { get; set; }
    public List<SchichtplanSectionModel> Sections { get; set; } = [];
}

public sealed class SchichtplanSectionModel
{
    public string Name { get; set; } = string.Empty;
    public string ThemeClass { get; set; } = string.Empty;
    public int Sortierung { get; set; }
    public List<SchichtplanWorkplaceModel> Workplaces { get; set; } = [];
}

public sealed class SchichtplanWorkplaceModel
{
    public int ArbeitsplatzId { get; set; }
    public string? ArbeitsplatzNr { get; set; }
    public string ArbeitsplatzName { get; set; } = string.Empty;
    public List<SchichtplanCellModel> Shifts { get; set; } = [];
}

public sealed class SchichtplanCellModel
{
    public int? EntryId { get; set; }
    public string Shift { get; set; } = string.Empty;
    public int? MaterialStammId { get; set; }
    public string? Material { get; set; }
    public string? FANr { get; set; }
    public string? Bemerkung { get; set; }
    public DateTime? LastUpdatedAt { get; set; }
    public List<SchichtplanAssignedUserModel> AssignedUsers { get; set; } = [];

    public bool HasContent =>
        !string.IsNullOrWhiteSpace(Material) ||
        !string.IsNullOrWhiteSpace(FANr) ||
        !string.IsNullOrWhiteSpace(Bemerkung) ||
        AssignedUsers.Count > 0;
}

public sealed class SchichtplanAssignedUserModel
{
    public int AssignmentId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string? Personalnummer { get; set; }
    public byte Sortierung { get; set; }
}

public sealed class SchichtplanAvailableUserModel
{
    public string DisplayName { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string? Personalnummer { get; set; }
    public string? Bereich { get; set; }
    public bool IsManual { get; set; }
}

public sealed class SchichtplanMaterialModel
{
    public int Id { get; set; }
    public string Material { get; set; } = string.Empty;
    public int Sortierung { get; set; }
    public bool IstStandard { get; set; }
}

public sealed class SchichtplanManagementModel
{
    public SchichtplanBoardModel Board { get; set; } = new();
    public List<SchichtplanAvailableUserModel> AvailableUsers { get; set; } = [];
    public List<SchichtplanMaterialModel> Materials { get; set; } = [];
}
