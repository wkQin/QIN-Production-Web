using Dapper;
using Microsoft.Data.SqlClient;

namespace QIN_Production_Web.Data;

public static class SchichtplanSchemaService
{
    public static async Task EnsureZielSnapshotColumnsAsync(string connectionString)
    {
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        await connection.ExecuteAsync(
            @"
BEGIN TRY
    IF COL_LENGTH(N'dbo.SchichtplanEintrag', N'MaterialZielMenge') IS NULL
    BEGIN
        ALTER TABLE dbo.SchichtplanEintrag
            ADD MaterialZielMenge INT NULL;
    END;
END TRY
BEGIN CATCH
    IF ERROR_NUMBER() <> 2705
        THROW;
END CATCH;

BEGIN TRY
    IF COL_LENGTH(N'dbo.SchichtplanEintrag', N'Material2ZielMenge') IS NULL
    BEGIN
        ALTER TABLE dbo.SchichtplanEintrag
            ADD Material2ZielMenge INT NULL;
    END;
END TRY
BEGIN CATCH
    IF ERROR_NUMBER() <> 2705
        THROW;
END CATCH;");
    }
}
