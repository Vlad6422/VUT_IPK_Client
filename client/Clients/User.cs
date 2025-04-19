using System;

namespace ipk25_chat.Clients
{
    /// <summary>
    /// Represents a user. Username, secret (password), and display name.
    /// Provides methods for changing these properties with validation checks.
    /// Same for Tcp and Udp.
    /// All properties are protected, so they can be used in TcpUser and UdpUser classes.
    /// Eazy to change in the future.
    /// </summary>
    public class User
    {
        protected string _username { get; set; } = String.Empty;
        protected string _secret { get; set; } = String.Empty;
        protected string _displayName { get; set; } = String.Empty;
        protected bool _isAuthorized { get; set; } = false;  // Flag to check if the user is authorized. Used to prevent sending messages before authentication and reciving messages from the server.
        protected string _message { get; set; } = string.Empty;
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
        protected void PrintHelpCommands()
        {
            // Print out supported local commands with their parameters and a description
            Console.WriteLine("/auth {Username} {Secret} {DisplayName} - Sends AUTH message to the server");
            Console.WriteLine("/join {ChannelID} - Sends JOIN message to the server");
            Console.WriteLine("/rename {DisplayName} - Locally changes the display name of the user");
            Console.WriteLine("/help - Prints out supported local commands with their parameters and a description");
        }
        protected bool IsMessageValid(string message,bool isAuthorized)
        {
            if (!isAuthorized)
            {
                WriteInternalError("You are not Authorized");
                return false;
            }
            if (_message.Length > 60000)
            {
                WriteInternalError("Input exceeds maximum length of 60000 characters.");
                return false;
            }
            foreach (char c in _message)
            {
                if (c < 0x20 || c > 0x7E)
                {
                    WriteInternalError("Invalid character detected. Only printable ASCII characters (0x20-7E) are allowed.");
                    return false;
                }
            }
            return true;
        }
    }
}
