using ipk25_chat.Clients;
using System.Net.Sockets;
using System.Text;

namespace ipk24chat_client.Clients.Tcp
{
    public class TcpUser : User
    {
        private string _message = String.Empty;
        private bool _isAuthorized;
        private NetworkStream _networkStream;
        private Thread _receiveThread;
        private bool _recieveThreadRunning = true;
        public TcpUser(NetworkStream networkStream)
        {
            _networkStream = networkStream;
            _receiveThread = new Thread(MessageRecieverThread);
            Console.CancelKeyPress += new ConsoleCancelEventHandler(Console_CancelKeyPress);
        }

        /// <summary>
        /// This method handles the main communication loop for the TCP client.
        /// It reads user input from the console, processes commands, and sends messages to the server.
        /// It also handles authentication, joining channels, renaming display names, and other commands.
        /// </summary>
        public void EnableChatTcp()
        {
            while (true)
            {
                string? userInput = Console.ReadLine();
                if (string.IsNullOrEmpty(userInput))
                {
                    if (userInput == null)
                    {
                        SendMessage("BYE FROM " + _displayName + "\r\n");
                        _recieveThreadRunning = false;
                        _networkStream.Close();
                        Environment.Exit(0);
                    }
                    WriteInternalError("Empty input. Please enter a command or message.");
                    continue;
                }
                if (userInput.StartsWith("/"))
                {
                    // Handle commands
                    string[] commandParts = userInput.Substring(1).Split(' ');

                    if (commandParts.Length == 0)
                    {
                        WriteInternalError("Invalid command.Please provide a valid command.");
                        continue;
                    }

                    string commandName = commandParts[0].ToLower();

                    switch (commandName)
                    {
                        case "auth":
                            if (_isAuthorized)
                            {
                                WriteInternalError("You are already Authorized.");
                                continue;
                            }
                            if (commandParts.Length != 4)
                            {
                                WriteInternalError("Invalid number of parameters for /auth command.");
                                continue;
                            }
                            if (
                            ChangeUserName(commandParts[1]) &&
                            ChangeSecret(commandParts[2]) &&
                            ChangeDisplayName(commandParts[3]))
                            {
                                Authenticate();
                                if (_isAuthorized)
                                {
                                    _receiveThread.Start();
                                }
                            }
                            break;
                        case "join":
                            if (commandParts.Length != 2)
                            {
                                WriteInternalError("Invalid number of parameters for /join command.");
                                continue;
                            }
                            if (!_isAuthorized)
                            {
                                WriteInternalError("You are not Authorized");
                                continue;
                            }
                            JoinChannel(commandParts[1]);
                            break;

                        case "rename":
                            if (commandParts.Length != 2)
                            {
                                WriteInternalError("Invalid number of parameters for /rename command.");
                                continue;
                            }
                            ChangeDisplayName(commandParts[1]);
                            break;

                        case "help":
                            // Print out supported local commands with their parameters and a description
                            Console.WriteLine("/auth {Username} {Secret} {DisplayName} - Sends AUTH message to the server");
                            Console.WriteLine("/join {ChannelID} - Sends JOIN message to the server");
                            Console.WriteLine("/rename {DisplayName} - Locally changes the display name of the user");
                            Console.WriteLine("/help - Prints out supported local commands with their parameters and a description");
                            break;

                        default:
                            WriteInternalError($"Unknown command '{commandName}'. Type '/help' for a list of supported commands.");
                            break;
                    }
                }
                else
                {
                    if (!_isAuthorized)
                    {
                        WriteInternalError("You are not Authorized");
                        continue;
                    }
                    _message = userInput;
                    if (_message.Length > 60000)
                    {
                        WriteInternalError("Input exceeds maximum length of 60000 characters.");
                        continue;
                    }

                    // Validate input characters
                    foreach (char c in _message)
                    {
                        if (c < 0x20 || c > 0x7E)
                        {
                            WriteInternalError("Invalid character detected. Only printable ASCII characters (0x20-7E) are allowed.");
                            continue;
                        }
                    }
                    if (_message != null && _message.Length > 0)
                    {
                        SendMessage("MSG FROM " + _displayName + " IS " + _message + "\r\n");
                    }
                }
            }

        }

        /// <summary>
        /// This method is used to authenticate the user with the server.
        /// </summary>
        public void Authenticate()
        {
            SendMessage("AUTH " + _username + " AS " + _displayName + " USING " + _secret + "\r\n");
            string response = RecieveMessage();
            response = response.TrimEnd('\r', '\n');
            string[] parts = response.Split();
            string msgType = parts[0];
            if (msgType == "REPLY")
            {
                string resultType = parts[1];
                string MessageContent = string.Join(" ", parts[3..]);
                if (resultType == "OK")
                {
                    Console.WriteLine($"Action Success: {MessageContent}");
                    _isAuthorized = true;
                }
                else if (resultType == "NOK")
                {
                    Console.WriteLine($"Action Failure: {MessageContent}");
                }
            }
            else if (msgType == "BYE")
            {
                HandleReceivedBYE();
            }
            else if (msgType == "ERR")
            {
                HandleReceivedERR(parts);
            }
            else if (response.Length > 1)
            {
                HandeReceivedUnknown();
            }

        }

        /// <summary>
        /// This method is used to join a channel.
        /// </summary>
        /// <param name="channelName">Channel Name</param>
        public void JoinChannel(string channelName)
        {
            if (channelName.Length > 20 || !System.Text.RegularExpressions.Regex.IsMatch(channelName, @"^[A-Za-z0-9\-]+$"))
            {
                WriteInternalError("Too Big ChannelName OR Incorrect");
            }
            else
            {
                SendMessage("JOIN " + channelName + " AS " + _displayName + "\r\n");
            }

        }

        /// <summary>
        /// This method runs in a separate thread and continuously listens for incoming messages from the server.
        /// </summary>
        public void MessageRecieverThread()
        {
            try
            {
                StringBuilder buffer = new StringBuilder();
                while (_recieveThreadRunning)
                {
                    string response = RecieveMessage();
                    if (response == "ERROR")
                        break;

                    buffer.Append(response);

                    string[] messages = buffer.ToString().Split("\r\n", StringSplitOptions.RemoveEmptyEntries);

                    buffer.Clear();
                    if (!response.EndsWith("\r\n"))
                    {
                        buffer.Append(messages[^1]);
                        messages = messages[..^1];
                    }

                    foreach (string message in messages)
                    {
                        ProcessMessage(message);
                    }
                }
            }
            catch 
            {
                Environment.Exit(0);
            }
        }

        /// <summary>
        /// Processes the received message from the server.
        /// </summary>
        /// <param name="response">Message to process</param>
        private void ProcessMessage(string response)
        {
            response = response.TrimEnd('\r', '\n');
            string[] parts = response.Split();
            if (parts.Length == 0) return;

            string msgType = parts[0];
            switch (msgType)
            {
                case "MSG":
                    HandleReceivedMSG(parts);
                    break;
                case "ERR":
                    HandleReceivedERR(parts);
                    break;
                case "REPLY":
                    HandleReceivedREPLY(parts);
                    break;
                case "BYE":
                    HandleReceivedBYE();
                    break;
                default:
                    HandeReceivedUnknown();
                    break;
            }
        }

        /// <summary>
        /// Sends a message to the server.
        /// </summary>
        /// <param name="message">Message to send</param>
        public void SendMessage(string message)
        {
            byte[] data = Encoding.ASCII.GetBytes(message);
            _networkStream.Write(data, 0, data.Length);
        }

        /// <summary>
        /// Receives a message from the server.
        /// </summary>
        /// <returns>Message</returns>
        public string RecieveMessage()
        {
            try
            {
                byte[] buffer = new byte[65535];
                int bytesRead = _networkStream.Read(buffer, 0, buffer.Length);
                string response = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                return response;
            }
            catch (IOException)
            {
                return "ERROR";
            }

        }

        /// <summary>
        /// Handles the case BYE message from the server.
        /// </summary>
        void HandleReceivedBYE()
        {
            _networkStream.Close();
            Environment.Exit(0);
        }

        /// <summary>
        /// Handles the received MSG message from the server.
        /// </summary>
        /// <param name="parts">Packet word by word</param>
        void HandleReceivedMSG(string[] parts)
        {
            string displayName = parts[2];
            string messageContent = string.Join(" ", parts[4..]);
            Console.WriteLine($"{displayName}: {messageContent}");
        }

        /// <summary>
        /// Handles the received ERR message from the server.
        /// </summary>
        /// <param name="parts">Packet word by word</param>
        void HandleReceivedERR(string[] parts)
        {
            string errorDisplayName = parts[2];
            string errorContent = string.Join(" ", parts[4..]);
            Console.WriteLine($"ERROR FROM {errorDisplayName}: {errorContent}");
            // SendMessage("BYE FROM " + _displayName + "\r\n"); // This is 50/50 situation, i would send bye message, but it is not required how i see at FSM
            Environment.Exit(0);
        }

        /// <summary>
        /// Handles the received REPLY message from the server.
        /// </summary>
        /// <param name="parts">Packet word by word</param>
        void HandleReceivedREPLY(string[] parts)
        {
            string resultType = parts[1];
            string MessageContent = string.Join(" ", parts[3..]);
            if (resultType == "OK")
            {
                Console.WriteLine($"Action Success: {MessageContent}");
            }
            else if (resultType == "NOK")
            {
                Console.WriteLine($"Action Failure: {MessageContent}");
            }
        }

        /// <summary>
        /// Handles the case when an unknown packet type is received.
        /// </summary>
        void HandeReceivedUnknown()
        {
            WriteInternalError("Unknown Packet Type");
            SendMessage($"ERR FROM {_displayName} IS Unknown Packet Type"+"\r\n");
            // SendMessage("BYE FROM " + _displayName + "\r\n"); // This is 50/50 situation, i would send bye message, but it is not required how i see at FSM
            _recieveThreadRunning = false;
            Environment.Exit(1);
        }

        // Event handler for console cancel key press
        void Console_CancelKeyPress(object? sender, ConsoleCancelEventArgs e)
        {
            SendMessage("BYE FROM " + _displayName + "\r\n");
            _recieveThreadRunning = false;
            _networkStream.Close();
            Environment.Exit(0);
        }
    }
}
