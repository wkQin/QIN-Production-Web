using Microsoft.Data.SqlClient;
using System.Data;

namespace QIN_Production_Web.Data
{
    public sealed class AdminUserRecord
    {
        public string OriginalBenutzer { get; set; } = "";
        public string Benutzer { get; set; } = "";
        public string Anmeldename { get; set; } = "";
        public string Personalnummer { get; set; } = "";
        public string ChipHex { get; set; } = "";
        public string Rechte { get; set; } = "";
        public bool Admin { get; set; }
        public bool ZeiterfassungVerwalten { get; set; }
        public int WochenMinuten { get; set; } = 2250;
        public int ArbeitstageProWoche { get; set; } = 5;
        public int TaglicheArbeitszeit { get; set; } = 450;
        public string NichtArbeitstage { get; set; } = "";
        public DateTime? LastSeen { get; set; }
    }

    public sealed class AdminManagementService
    {
        public async Task EnsureSchemaAsync()
        {
            await using var con = new SqlConnection(SqlManager.connectionString);
            await con.OpenAsync();

            const string hasColumnSql = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'LoginDaten' AND COLUMN_NAME = 'Admin';";
            await using (var hasColumnCmd = new SqlCommand(hasColumnSql, con))
            {
                var columnCount = Convert.ToInt32(await hasColumnCmd.ExecuteScalarAsync());
                if (columnCount == 0)
                {
                    const string addColumnSql = "ALTER TABLE dbo.LoginDaten ADD Admin bit NULL;";
                    const string fillColumnSql = "UPDATE dbo.LoginDaten SET Admin = 0 WHERE Admin IS NULL;";
                    const string setNotNullSql = "ALTER TABLE dbo.LoginDaten ALTER COLUMN Admin bit NOT NULL;";

                    await using var addColumnCmd = new SqlCommand(addColumnSql, con);
                    await using var fillColumnCmd = new SqlCommand(fillColumnSql, con);
                    await using var setNotNullCmd = new SqlCommand(setNotNullSql, con);

                    await addColumnCmd.ExecuteNonQueryAsync();
                    await fillColumnCmd.ExecuteNonQueryAsync();
                    await setNotNullCmd.ExecuteNonQueryAsync();
                }
            }

            const string seedSql = @"
UPDATE dbo.LoginDaten
SET Admin = 1
WHERE Benutzer IN (N'Selim Köse', N'Werner Klein', N'Patrick Kolbus');";

            await using var seedCmd = new SqlCommand(seedSql, con);
            await seedCmd.ExecuteNonQueryAsync();
        }

        public async Task<List<AdminUserRecord>> LoadUsersAsync()
        {
            const string sql = @"
SELECT
    Benutzer,
    Anmeldename,
    Personalnummer,
    ChipHex,
    Rechte,
    ISNULL(Admin, 0) AS Admin,
    ISNULL(Zeiterfassung_Verwalten, 0) AS Zeiterfassung_Verwalten,
    ISNULL(WochenMinuten, 2250) AS WochenMinuten,
    ISNULL(ArbeitstageProWoche, 5) AS ArbeitstageProWoche,
    ISNULL(Tagliche_Arbeitszeit, 450) AS Tagliche_Arbeitszeit,
    ISNULL(Nicht_Arbeitstage, N'') AS Nicht_Arbeitstage,
    LastSeen
FROM dbo.LoginDaten
ORDER BY Benutzer;";

            var users = new List<AdminUserRecord>();

            await using var con = new SqlConnection(SqlManager.connectionString);
            await using var cmd = new SqlCommand(sql, con);
            await con.OpenAsync();
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                users.Add(new AdminUserRecord
                {
                    OriginalBenutzer = reader["Benutzer"]?.ToString()?.Trim() ?? string.Empty,
                    Benutzer = reader["Benutzer"]?.ToString()?.Trim() ?? string.Empty,
                    Anmeldename = reader["Anmeldename"]?.ToString()?.Trim() ?? string.Empty,
                    Personalnummer = reader["Personalnummer"]?.ToString()?.Trim() ?? string.Empty,
                    ChipHex = reader["ChipHex"]?.ToString()?.Trim() ?? string.Empty,
                    Rechte = reader["Rechte"]?.ToString()?.Trim() ?? string.Empty,
                    Admin = ReadBool(reader["Admin"]),
                    ZeiterfassungVerwalten = ReadBool(reader["Zeiterfassung_Verwalten"]),
                    WochenMinuten = ReadInt(reader["WochenMinuten"], 2250),
                    ArbeitstageProWoche = ReadInt(reader["ArbeitstageProWoche"], 5),
                    TaglicheArbeitszeit = ReadInt(reader["Tagliche_Arbeitszeit"], 450),
                    NichtArbeitstage = reader["Nicht_Arbeitstage"]?.ToString()?.Trim() ?? string.Empty,
                    LastSeen = reader["LastSeen"] == DBNull.Value ? null : Convert.ToDateTime(reader["LastSeen"])
                });
            }

            return users;
        }

        public async Task<bool> UpdateUserAsync(AdminUserRecord user)
        {
            const string sql = @"
UPDATE dbo.LoginDaten
SET
    Benutzer = @Benutzer,
    Anmeldename = @Anmeldename,
    Personalnummer = @Personalnummer,
    ChipHex = @ChipHex,
    Rechte = @Rechte,
    Admin = @Admin,
    Zeiterfassung_Verwalten = @ZeiterfassungVerwalten,
    WochenMinuten = @WochenMinuten,
    ArbeitstageProWoche = @ArbeitstageProWoche,
    Tagliche_Arbeitszeit = @TaglicheArbeitszeit,
    Nicht_Arbeitstage = @NichtArbeitstage
WHERE Benutzer = @OriginalBenutzer;";

            await using var con = new SqlConnection(SqlManager.connectionString);
            await using var cmd = new SqlCommand(sql, con);

            cmd.Parameters.Add("@Benutzer", SqlDbType.NVarChar).Value = ToDbString(user.Benutzer);
            cmd.Parameters.Add("@Anmeldename", SqlDbType.NVarChar).Value = ToDbString(user.Anmeldename);
            cmd.Parameters.Add("@Personalnummer", SqlDbType.NVarChar).Value = ToDbString(user.Personalnummer);
            cmd.Parameters.Add("@ChipHex", SqlDbType.NVarChar).Value = ToDbString(user.ChipHex);
            cmd.Parameters.Add("@Rechte", SqlDbType.NVarChar).Value = ToDbString(user.Rechte);
            cmd.Parameters.Add("@Admin", SqlDbType.Bit).Value = user.Admin;
            cmd.Parameters.Add("@ZeiterfassungVerwalten", SqlDbType.Bit).Value = user.ZeiterfassungVerwalten;
            cmd.Parameters.Add("@WochenMinuten", SqlDbType.Int).Value = user.WochenMinuten;
            cmd.Parameters.Add("@ArbeitstageProWoche", SqlDbType.Int).Value = user.ArbeitstageProWoche;
            cmd.Parameters.Add("@TaglicheArbeitszeit", SqlDbType.Int).Value = user.TaglicheArbeitszeit;
            cmd.Parameters.Add("@NichtArbeitstage", SqlDbType.NVarChar).Value = ToDbString(user.NichtArbeitstage);
            cmd.Parameters.Add("@OriginalBenutzer", SqlDbType.NVarChar).Value = ToDbString(string.IsNullOrWhiteSpace(user.OriginalBenutzer) ? user.Benutzer : user.OriginalBenutzer);

            await con.OpenAsync();
            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public async Task<(bool Success, string Message)> CreateUserAsync(AdminUserRecord user, string? password = null)
        {
            if (string.IsNullOrWhiteSpace(user.Benutzer))
            {
                return (false, "Bitte einen Benutzernamen angeben.");
            }

            if (string.IsNullOrWhiteSpace(user.Anmeldename))
            {
                return (false, "Bitte einen Anmeldenamen angeben.");
            }

            await using var con = new SqlConnection(SqlManager.connectionString);
            await con.OpenAsync();

            const string existsSql = @"
SELECT COUNT(*)
FROM dbo.LoginDaten
WHERE Benutzer = @Benutzer
   OR Anmeldename = @Anmeldename
   OR (@Personalnummer IS NOT NULL AND Personalnummer = @Personalnummer)
   OR (@ChipHex IS NOT NULL AND ChipHex = @ChipHex);";

            await using (var existsCmd = new SqlCommand(existsSql, con))
            {
                existsCmd.Parameters.Add("@Benutzer", SqlDbType.NVarChar).Value = ToDbString(user.Benutzer);
                existsCmd.Parameters.Add("@Anmeldename", SqlDbType.NVarChar).Value = ToDbString(user.Anmeldename);
                existsCmd.Parameters.Add("@Personalnummer", SqlDbType.NVarChar).Value = ToDbString(user.Personalnummer);
                existsCmd.Parameters.Add("@ChipHex", SqlDbType.NVarChar).Value = ToDbString(user.ChipHex);

                var existingCount = Convert.ToInt32(await existsCmd.ExecuteScalarAsync());
                if (existingCount > 0)
                {
                    return (false, "Benutzer, Anmeldename oder Personalnummer existiert bereits.");
                }
            }

            const string insertSql = @"
INSERT INTO dbo.LoginDaten
(
    Benutzer,
    Anmeldename,
    Password,
    Rechte,
    Personalnummer,
    ChipHex,
    FreigabeRecht,
    Zeiterfassung_Verwalten,
    WochenMinuten,
    ArbeitstageProWoche,
    Tagliche_Arbeitszeit,
    Nicht_Arbeitstage,
    Admin
)
VALUES
(
    @Benutzer,
    @Anmeldename,
    @Password,
    @Rechte,
    @Personalnummer,
    @ChipHex,
    0,
    @ZeiterfassungVerwalten,
    @WochenMinuten,
    @ArbeitstageProWoche,
    @TaglicheArbeitszeit,
    @NichtArbeitstage,
    @Admin
);";

            await using var insertCmd = new SqlCommand(insertSql, con);
            insertCmd.Parameters.Add("@Benutzer", SqlDbType.NVarChar).Value = ToDbString(user.Benutzer);
            insertCmd.Parameters.Add("@Anmeldename", SqlDbType.NVarChar).Value = ToDbString(user.Anmeldename);
            insertCmd.Parameters.Add("@Password", SqlDbType.NVarChar).Value = ToDbString(password);
            insertCmd.Parameters.Add("@Rechte", SqlDbType.NVarChar).Value = ToDbString(string.IsNullOrWhiteSpace(user.Rechte) ? "Benutzer" : user.Rechte);
            insertCmd.Parameters.Add("@Personalnummer", SqlDbType.NVarChar).Value = ToDbString(user.Personalnummer);
            insertCmd.Parameters.Add("@ChipHex", SqlDbType.NVarChar).Value = ToDbString(user.ChipHex);
            insertCmd.Parameters.Add("@ZeiterfassungVerwalten", SqlDbType.Bit).Value = user.ZeiterfassungVerwalten;
            insertCmd.Parameters.Add("@WochenMinuten", SqlDbType.Int).Value = user.WochenMinuten <= 0 ? 2250 : user.WochenMinuten;
            insertCmd.Parameters.Add("@ArbeitstageProWoche", SqlDbType.Int).Value = user.ArbeitstageProWoche <= 0 ? 5 : user.ArbeitstageProWoche;
            insertCmd.Parameters.Add("@TaglicheArbeitszeit", SqlDbType.Int).Value = user.TaglicheArbeitszeit <= 0 ? 450 : user.TaglicheArbeitszeit;
            insertCmd.Parameters.Add("@NichtArbeitstage", SqlDbType.NVarChar).Value = ToDbString(user.NichtArbeitstage);
            insertCmd.Parameters.Add("@Admin", SqlDbType.Bit).Value = user.Admin;

            await insertCmd.ExecuteNonQueryAsync();
            return (true, "Benutzer wurde angelegt.");
        }

        public async Task<(bool Success, string Message)> DeleteUserAsync(string originalBenutzer)
        {
            if (string.IsNullOrWhiteSpace(originalBenutzer))
            {
                return (false, "Kein Benutzer zum Löschen ausgewählt.");
            }

            const string sql = @"DELETE FROM dbo.LoginDaten WHERE Benutzer = @OriginalBenutzer;";

            await using var con = new SqlConnection(SqlManager.connectionString);
            await using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.Add("@OriginalBenutzer", SqlDbType.NVarChar).Value = ToDbString(originalBenutzer);

            await con.OpenAsync();
            var affectedRows = await cmd.ExecuteNonQueryAsync();
            return affectedRows > 0
                ? (true, "Benutzer wurde gelöscht.")
                : (false, "Benutzer konnte nicht gelöscht werden.");
        }

        public async Task<bool> UpdatePasswordAsync(string originalBenutzer, string newPassword)
        {
            const string sql = @"
UPDATE dbo.LoginDaten
SET Password = @Password
WHERE Benutzer = @OriginalBenutzer;";

            await using var con = new SqlConnection(SqlManager.connectionString);
            await using var cmd = new SqlCommand(sql, con);

            cmd.Parameters.Add("@Password", SqlDbType.NVarChar).Value = ToDbString(newPassword);
            cmd.Parameters.Add("@OriginalBenutzer", SqlDbType.NVarChar).Value = ToDbString(originalBenutzer);

            await con.OpenAsync();
            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public async Task<(bool Success, string Password)> ResetPasswordToPersonalnummerAsync(AdminUserRecord user)
        {
            var resetPassword = !string.IsNullOrWhiteSpace(user.Personalnummer)
                ? user.Personalnummer.Trim()
                : !string.IsNullOrWhiteSpace(user.Anmeldename)
                    ? user.Anmeldename.Trim()
                    : user.Benutzer.Trim();

            if (string.IsNullOrWhiteSpace(resetPassword))
            {
                return (false, string.Empty);
            }

            var success = await UpdatePasswordAsync(
                string.IsNullOrWhiteSpace(user.OriginalBenutzer) ? user.Benutzer : user.OriginalBenutzer,
                resetPassword);

            return (success, resetPassword);
        }

        private static object ToDbString(string? value) =>
            string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();

        private static bool ReadBool(object value)
        {
            if (value == DBNull.Value)
            {
                return false;
            }

            return value is bool direct ? direct : Convert.ToInt32(value) != 0;
        }

        private static int ReadInt(object value, int fallback)
        {
            if (value == DBNull.Value)
            {
                return fallback;
            }

            return Convert.ToInt32(value);
        }
    }
}
