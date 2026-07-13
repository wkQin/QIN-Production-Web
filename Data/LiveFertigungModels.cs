namespace QIN_Production_Web.Data;

public sealed class LiveFertigungEndkontrolleTableModel
{
    public int TableNumber { get; set; }
    public bool IsOccupied { get; set; }
    public string User { get; set; } = "-";
    public string? Personalnummer { get; set; }
    public IReadOnlyList<string> Materials { get; set; } = [];
    public int DoneQuantity { get; set; }
    public int BadQuantity { get; set; }
    public int TargetQuantity { get; set; }
    public IReadOnlyList<LiveFertigungWeekEntryModel> WeekEntries { get; set; } = [];
    public IReadOnlyList<LiveFertigungHistoryEntryModel> HistoryEntries { get; set; } = [];
}

public sealed class LiveFertigungWeekEntryModel
{
    public string Date { get; set; } = string.Empty;
    public string Material { get; set; } = string.Empty;
    public string Charge { get; set; } = string.Empty;
    public int Good { get; set; }
    public int Bad { get; set; }
    public string Note { get; set; } = string.Empty;
}

public sealed class LiveFertigungHistoryEntryModel
{
    public string Date { get; set; } = string.Empty;
    public string Material { get; set; } = string.Empty;
    public int Done { get; set; }
    public int? Target { get; set; }
}
