using Microsoft.Data.SqlClient;

namespace QIN_Production_Web.Data
{
    public class UserSession
    {
        public string? Name { get; set; }
        public string? Rechte { get; set; }
        public string? Personalnummer { get; set; }
    }

    public class LoginService
    {
        public async Task<UserSession?> LoginAsync(string username, string password)
        {
            try
            {
                // We map to UserSession which is stored in browser
                using (var connection = new SqlConnection(SqlManager.connectionString))
                {
                    await connection.OpenAsync();
                    string query = @"
                        SELECT Anmeldename, Password, Rechte, Personalnummer, Benutzer
                        FROM LoginDaten
                        WHERE (Anmeldename = @Username OR Personalnummer = @Username)
                          AND Password = @Password";

                    UserSession? user = null;

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Username", username);
                        command.Parameters.AddWithValue("@Password", password);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                user = new UserSession
                                {
                                    Name = reader["Benutzer"] != DBNull.Value ? reader["Benutzer"].ToString() : string.Empty,
                                    Rechte = reader["Rechte"] != DBNull.Value ? reader["Rechte"].ToString() : string.Empty,
                                    Personalnummer = reader["Personalnummer"] != DBNull.Value ? reader["Personalnummer"].ToString() : string.Empty
                                };
                            }
                        }
                    }

                    if (user != null)
                    {
                        // Update LastSeen
                        using (var updateCmd = new SqlCommand("UPDATE LoginDaten SET LastSeen = SYSDATETIME() WHERE Benutzer = @user", connection))
                        {
                            updateCmd.Parameters.AddWithValue("@user", user.Name);
                            await updateCmd.ExecuteNonQueryAsync();
                        }
                    }

                    return user;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Login Service Error: " + ex.Message);
                return null;
            }
        }

        public async Task<UserSession?> ADLoginAsync(string username)
        {
            try
            {
                using (var connection = new SqlConnection(SqlManager.connectionString))
                {
                    await connection.OpenAsync();
                    string query = @"
                        SELECT Anmeldename, Password, Rechte, Personalnummer, Benutzer
                        FROM LoginDaten
                        WHERE Anmeldename = @Username";

                    UserSession? user = null;

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Username", username);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                user = new UserSession
                                {
                                    Name = reader["Benutzer"] != DBNull.Value ? reader["Benutzer"].ToString() : string.Empty,
                                    Rechte = reader["Rechte"] != DBNull.Value ? reader["Rechte"].ToString() : string.Empty,
                                    Personalnummer = reader["Personalnummer"] != DBNull.Value ? reader["Personalnummer"].ToString() : string.Empty
                                };
                            }
                        }
                    }

                    if (user != null)
                    {
                        using (var updateCmd = new SqlCommand("UPDATE LoginDaten SET LastSeen = SYSDATETIME() WHERE Benutzer = @user", connection))
                        {
                            updateCmd.Parameters.AddWithValue("@user", user.Name);
                            await updateCmd.ExecuteNonQueryAsync();
                        }
                    }

                    return user;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("AD Login Service Error: " + ex.Message);
                return null;
            }
        }

        public async Task<UserSession?> FastLoginAsync(string personalnummer)
        {
            try
            {
                using (var connection = new SqlConnection(SqlManager.connectionString))
                {
                    await connection.OpenAsync();
                    // FAST LOGINS ARE RESTRICTED TO VERWALTUNG AND ADMIN!
                    string query = @"
                        SELECT Anmeldename, Rechte, Personalnummer, Benutzer
                        FROM LoginDaten
                        WHERE Personalnummer = @Personalnummer 
                          AND Rechte IN ('Admin', 'Verwaltung')";

                    UserSession? user = null;

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Personalnummer", personalnummer);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                user = new UserSession
                                {
                                    Name = reader["Benutzer"] != DBNull.Value ? reader["Benutzer"].ToString() : string.Empty,
                                    Rechte = reader["Rechte"] != DBNull.Value ? reader["Rechte"].ToString() : string.Empty,
                                    Personalnummer = reader["Personalnummer"] != DBNull.Value ? reader["Personalnummer"].ToString() : string.Empty
                                };
                            }
                        }
                    }

                    if (user != null)
                    {
                        using (var updateCmd = new SqlCommand("UPDATE LoginDaten SET LastSeen = SYSDATETIME() WHERE Benutzer = @user", connection))
                        {
                            updateCmd.Parameters.AddWithValue("@user", user.Name);
                            await updateCmd.ExecuteNonQueryAsync();
                        }
                    }

                    return user;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("FastLogin Service Error: " + ex.Message);
                return null;
            }
        }

        public async Task<List<UserSession>> GetOnlineUsersAsync()
        {
            var users = new List<UserSession>();
            try
            {
                using (var connection = new SqlConnection(SqlManager.connectionString))
                {
                    await connection.OpenAsync();
                    // Letzte 15 Minuten als "Online" werten
                    string query = @"
                        SELECT Benutzer, Rechte, Personalnummer 
                        FROM LoginDaten 
                        WHERE LastSeen >= DATEADD(minute, -15, SYSDATETIME())
                        ORDER BY LastSeen DESC";

                    using (var command = new SqlCommand(query, connection))
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            users.Add(new UserSession
                            {
                                Name = reader["Benutzer"] != DBNull.Value ? reader["Benutzer"].ToString() : string.Empty,
                                Rechte = reader["Rechte"] != DBNull.Value ? reader["Rechte"].ToString() : string.Empty,
                                Personalnummer = reader["Personalnummer"] != DBNull.Value ? reader["Personalnummer"].ToString() : string.Empty
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetOnlineUsers Error: " + ex.Message);
            }
            return users;
        }
    }
}
