using System;

namespace QIN_Production_Web.Data.Zeiterfassung
{
    internal static class ZeiterfassungUserRules
    {
        private const string SchichtplanMonitorUser = "Schichtplan Monitor";

        public static bool IsExcludedSystemUser(string? user)
        {
            if (string.IsNullOrWhiteSpace(user)) return false;

            var normalized = string.Join(
                " ",
                user.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));

            return string.Equals(
                normalized,
                SchichtplanMonitorUser,
                StringComparison.OrdinalIgnoreCase);
        }
    }
}
