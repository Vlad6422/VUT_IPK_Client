namespace ipk25_chat.Clients
{
    public class User
    {
        protected string _username { get; set; } = String.Empty;
        protected string _secret { get; set; } = String.Empty;
        protected string _displayName { get; set; } = String.Empty;
        protected bool ChangeUserName(string username)
        {
            if (username.Length > 20 || !System.Text.RegularExpressions.Regex.IsMatch(username, @"^[A-Za-z0-9\-]+$"))
            {
                WriteInternalError("Too Big UserName or Incorect");
                return false;
            }
            else
            {
                _username = username;
            }
            return true;
        }
        protected bool ChangeSecret(string secret)
        {
            if (secret.Length > 128 || !System.Text.RegularExpressions.Regex.IsMatch(secret, @"^[A-Za-z0-9\-]+$"))
            {
                WriteInternalError("Too Big Secret or Incorect");
                return false;
            }
            else
            {
                _secret = secret;
            }
            return true;

        }
        protected bool ChangeDisplayName(string newName)
        {
            if (!newName.All(c => c >= 0x21 && c <= 0x7E))
            {
                WriteInternalError("DisplayName contains invalid characters");
                return false;
            }
            if (newName.Length > 20)
            {
                WriteInternalError("Too Big DisplayName or Incorect");
                return false;
            }
            else
            {
                _displayName = newName;
            }
            return true;
        }
        protected void WriteInternalError(string error)
        {
            Console.WriteLine($"ERROR: {error}");
        }
    }
}
