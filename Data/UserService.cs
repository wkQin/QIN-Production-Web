namespace QIN_Production_Web.Data
{
    public class UserService
    {
        public UserSession? CurrentUser { get; private set; }

        public void SetUser(UserSession user)
        {
            CurrentUser = user;
        }

        public void ClearUser()
        {
            CurrentUser = null;
        }

        public string Personalnummer => CurrentUser?.Personalnummer ?? "100";
        public string Name => CurrentUser?.Name ?? "Guest";
        public bool IsLoggedIn => CurrentUser != null;
    }
}
