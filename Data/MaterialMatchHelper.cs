using System.Text.RegularExpressions;

namespace QIN_Production_Web.Data;

internal static class MaterialMatchHelper
{
    internal const int StrongMatchThreshold = 100;

    internal sealed record SearchInput(string Label, string Text, int Bonus);

    internal static List<SearchInput> BuildSearchInputs(string? artikel, string? projekt, string? dekor)
    {
        var inputs = new List<SearchInput>();

        void AddInput(string label, string? text, int bonus)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            string trimmed = text.Trim();
            if (!inputs.Any(existing => existing.Text.Equals(trimmed, StringComparison.OrdinalIgnoreCase)))
            {
                inputs.Add(new SearchInput(label, trimmed, bonus));
            }
        }

        AddInput("Artikel", artikel, 0);

        foreach (var artikelVariante in BuildArtikelVarianten(artikel))
        {
            AddInput("Artikel-Variante", artikelVariante, 5);
            if (!string.IsNullOrWhiteSpace(projekt))
            {
                AddInput("Artikel + Projekt", $"{artikelVariante} {projekt}", 30);
            }

            if (!string.IsNullOrWhiteSpace(dekor))
            {
                AddInput("Artikel + Dekor", $"{artikelVariante} {dekor}", 25);
            }
        }

        AddInput("Projekt", projekt, 8);
        AddInput("Dekor", dekor, 12);

        if (!string.IsNullOrWhiteSpace(projekt) && !string.IsNullOrWhiteSpace(dekor))
        {
            AddInput("Projekt + Dekor", $"{projekt} {dekor}", 35);
            AddInput("Artikel + Projekt + Dekor", $"{artikel} {projekt} {dekor}", 40);
        }

        return inputs;
    }

    internal static IEnumerable<string> BuildArtikelVarianten(string? artikel)
    {
        if (string.IsNullOrWhiteSpace(artikel))
        {
            return Enumerable.Empty<string>();
        }

        var varianten = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            artikel.Trim()
        };

        string basis = artikel.Trim();
        varianten.Add(basis.Replace(" Links", " LL", StringComparison.OrdinalIgnoreCase));
        varianten.Add(basis.Replace(" Rechts", " RL", StringComparison.OrdinalIgnoreCase));
        varianten.Add(basis.Replace(" Links", " LH", StringComparison.OrdinalIgnoreCase));
        varianten.Add(basis.Replace(" Rechts", " RH", StringComparison.OrdinalIgnoreCase));
        varianten.Add(basis.Replace(" LL", " Links", StringComparison.OrdinalIgnoreCase));
        varianten.Add(basis.Replace(" RL", " Rechts", StringComparison.OrdinalIgnoreCase));
        varianten.Add(basis.Replace(" LH", " Links", StringComparison.OrdinalIgnoreCase));
        varianten.Add(basis.Replace(" RH", " Rechts", StringComparison.OrdinalIgnoreCase));

        return varianten
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim());
    }

    internal static int ScoreSearchInput(SearchInput input, string? candidate, bool isCombinedCandidate = false)
    {
        int score = ScoreMaterialMatch(input.Text, candidate);
        if (score <= 0)
        {
            return 0;
        }

        score += input.Bonus;
        if (isCombinedCandidate)
        {
            score += 10;
        }

        return score;
    }

    internal static int ScoreMaterialMatch(string input, string? candidate)
    {
        if (string.IsNullOrWhiteSpace(input) || string.IsNullOrWhiteSpace(candidate))
        {
            return 0;
        }

        string normalizedInput = NormalizeMaterialText(input);
        string normalizedCandidate = NormalizeMaterialText(candidate);
        if (string.IsNullOrWhiteSpace(normalizedInput) || string.IsNullOrWhiteSpace(normalizedCandidate))
        {
            return 0;
        }

        if (normalizedInput.Equals(normalizedCandidate, StringComparison.OrdinalIgnoreCase))
        {
            return 1000 + normalizedCandidate.Length;
        }

        var inputTokens = GetMaterialTokens(input);
        var candidateTokens = GetMaterialTokens(candidate);
        int commonTokenCount = inputTokens.Intersect(candidateTokens, StringComparer.OrdinalIgnoreCase).Count();
        bool allInputTokensMatch = inputTokens.Count > 0 && commonTokenCount == inputTokens.Count;

        if (allInputTokensMatch && inputTokens.Count >= 2)
        {
            return 850 + commonTokenCount;
        }

        if (normalizedCandidate.Contains(normalizedInput, StringComparison.OrdinalIgnoreCase))
        {
            return 700 + normalizedInput.Length + (commonTokenCount * 10);
        }

        if (normalizedInput.Contains(normalizedCandidate, StringComparison.OrdinalIgnoreCase))
        {
            return 650 + normalizedCandidate.Length + (commonTokenCount * 10);
        }

        return commonTokenCount >= 2 ? (commonTokenCount * 60) : 0;
    }

    internal static bool IsStrongAssignedMaterialMatch(string? artikel, string? projekt, string? dekor, string? assignedMaterial)
    {
        if (string.IsNullOrWhiteSpace(assignedMaterial))
        {
            return false;
        }

        var assignedTokens = GetMaterialTokens(assignedMaterial);
        if (assignedTokens.Count == 0)
        {
            return false;
        }

        string normalizedAssigned = NormalizeMaterialText(assignedMaterial);
        foreach (var input in BuildSearchInputs(artikel, projekt, dekor))
        {
            var inputTokens = GetMaterialTokens(input.Text);
            if (assignedTokens.IsSubsetOf(inputTokens))
            {
                return true;
            }

            string normalizedInput = NormalizeMaterialText(input.Text);
            if (!string.IsNullOrWhiteSpace(normalizedInput) &&
                normalizedInput.Contains(normalizedAssigned, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    internal static int GetAssignedMaterialFallbackScore(string? artikel, string? projekt, string? dekor, string? assignedMaterial)
    {
        if (string.IsNullOrWhiteSpace(assignedMaterial))
        {
            return 0;
        }

        var assignedTokens = GetMaterialTokens(assignedMaterial);
        if (assignedTokens.Count == 0)
        {
            return 0;
        }

        int bestScore = 0;
        foreach (var input in BuildSearchInputs(artikel, projekt, dekor))
        {
            var inputTokens = GetMaterialTokens(input.Text);
            int commonTokenCount = inputTokens.Intersect(assignedTokens, StringComparer.OrdinalIgnoreCase).Count();
            if (commonTokenCount <= 0)
            {
                continue;
            }

            int score = (commonTokenCount * 100) + input.Bonus;
            string normalizedInput = NormalizeMaterialText(input.Text);
            string normalizedAssigned = NormalizeMaterialText(assignedMaterial);
            if (!string.IsNullOrWhiteSpace(normalizedInput) &&
                !string.IsNullOrWhiteSpace(normalizedAssigned) &&
                (normalizedInput.Contains(normalizedAssigned, StringComparison.OrdinalIgnoreCase) ||
                 normalizedAssigned.Contains(normalizedInput, StringComparison.OrdinalIgnoreCase)))
            {
                score += 25;
            }

            if (score > bestScore)
            {
                bestScore = score;
            }
        }

        return bestScore;
    }

    internal static string NormalizeMaterialText(string value)
    {
        return string.Concat(Regex.Matches(value.ToUpperInvariant().Replace("Ã‚Âµ", "U").Replace("Âµ", "U"), @"[\p{L}\p{N}]+")
            .Select(match => match.Value)
            .Where(token => token.Length > 1));
    }

    internal static HashSet<string> GetMaterialTokens(string value)
    {
        return Regex.Matches(value.ToUpperInvariant().Replace("Ã‚Âµ", "U").Replace("Âµ", "U"), @"[\p{L}\p{N}]+")
            .Select(match => match.Value)
            .Where(token => token.Length > 1)
            .Where(token => token is not "BLENDE" and not "TEIL" and not "SATZ" and not "SET")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
}
