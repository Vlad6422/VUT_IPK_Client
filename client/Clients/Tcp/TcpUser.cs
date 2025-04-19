using ipk25_chat.Clients;
using System.Net.Sockets;
using System.Text;

namespace ipk24chat_client.Clients.Tcp
{
    public class TcpUser : User
    {
        private string _message = String.Empty; // Message to be sent to the server. It is set when user types a message in the console.
        private bool _isAuthorized; // Flag to check if the user is authorized. Used to prevent sending messages before authentication and reciving messages from the server.
        private NetworkStream _networkStream; // Network stream for sending and receiving messages. Passed from the main program to the client in constructor.
        private Thread _receiveThread; // Thread for receiving messages from the server.
        private bool _recieveThreadRunning = true; // Flag to control the thread loop, this is used to stop the thread when client gets BYE or ERR message to prevent it from processing messages after those packets.
        public TcpUser(NetworkStream networkStream)
        {
            _networkStream = networkStream;
            _receiveThread = new Thread(MessageRecieverThread);
            Console.CancelKeyPress += new ConsoleCancelEventHandler(Console_CancelKeyPress);
        }

        /// <summary>
        /// The main loop is responsible for user actions, console inputs, commands, etc.
        /// Just loop that uses switch case to handle commands(auth,join,rename...)
        /// if user input is not a command, it sends the message to the server. Before sending the message, it checks if the user is authorized,size,symbols in message.
        /// </summary>
        public void EnableChatTcp()
        {
            while (true)
            {
                string? userInput = Console.ReadLine(); // Read user input from the console.
                if (string.IsNullOrEmpty(userInput))
                {
                    if (userInput == null) // This is used to check if the user closed the console or pressed Ctrl+C. Ignores the empty input, it will just print it.
                    {
                        SendMessage("BYE FROM " + _displayName + "\r\n");
                        _recieveThreadRunning = false;
                        _networkStream.Close();
                        Environment.Exit(0);
                    }
                    WriteInternalError("Empty input. Please enter a command or message."); // This is used to prevent empty input from being sent to the server.
                    continue;
                }
                if (userInput.StartsWith("/")) // Check if the input command or not.
                {
                    // Handle commands
                    string[] commandParts = userInput.Substring(1).Split(' ');

                    if (commandParts.Length == 0)
                    {
                        WriteInternalError("Invalid command.Please provide a valid command.");
                        continue;
                    }

                    string commandName = commandParts[0].ToLower();

                    switch (commandName) // Switch case for commands
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
                            if ( // Initializing the username, secret and display name
                            ChangeUserName(commandParts[1]) &&
                            ChangeSecret(commandParts[2]) &&
                            ChangeDisplayName(commandParts[3]))
                            {
                                Authenticate(); // Authenticate the user with the server.
                                if (_isAuthorized)
                                {
                                    _receiveThread.Start(); // Start the thread for receiving messages from the server.
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
                            JoinChannel(commandParts[1]); // Join the channel with the given name.
                            break;
                        case "rename":
                            if (commandParts.Length != 2)
                            {
                                WriteInternalError("Invalid number of parameters for /rename command.");
                                continue;
                            }
                            ChangeDisplayName(commandParts[1]); // Change the display name of the user.
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
                else // Logic for non command input (Messages)
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
                        SendMessage("MSG FROM " + _displayName + " IS " + _message + "\r\n"); // Only one place in code where message is sent to the server.
                    }
                }
            }

        }

        /// <summary>
        /// This method is used to authenticate the user with the server.
        /// It handles all situations is AUTH state like ERR, BYE, REPLY, etc.
        /// It will run logic for UknownPacketType if gets any type of packet that is not expected. Server shouldn't send any other packet (like MSG) in AUTH state.
        /// It will change the _isAuthorized flag to true if the authentication is successful. And in method EnableChatTcp it will start the thread for receiving messages from the server.
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
                if (resultType == "OK") // Authentication success
                {
                    Console.WriteLine($"Action Success: {MessageContent}");
                    _isAuthorized = true;
                }
                else if (resultType == "NOK") // Not success
                {
                    Console.WriteLine($"Action Failure: {MessageContent}");
                }
            } // Additional states illustrated in FSM
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
        /// Checks if the channel name is valid (length and symbols).
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
        /// Runs after the user is authenticated.
        /// Split the message by \r\n and processes each message.
        /// It splits the message becouse TCP is stream based protocol and it can send multiple messages in one packet or one message in multiple packets.
        /// It works like this: if the message is not ended with \r\n, it will append the message to the buffer and wait for the next message. When it gets the next message, it will split the buffer by \r\n and process each message.
        /// So SrtingBuilder is not in while loop, it is outside and it will be cleared after processing the message.
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
        /// Just switch case for the first word of the message.
        /// Delete the last \r\n from the message.
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
        /// This is simle StreamWriter that writes the message to the network stream.
        /// This function is used to send all types of messages to the server.
        /// Example of message: "MSG FROM user IS Hello\r\n"
        /// </summary>
        /// <param name="message">Message to send in format desribed in IPK25-CHAT Protocol Documenattion</param>
        public void SendMessage(string message)
        {
            byte[] data = Encoding.ASCII.GetBytes(message);
            _networkStream.Write(data, 0, data.Length);
        }

        /// <summary>
        /// Receives a message from the server.
        /// Used in 2 places, in MessageRecieverThread and Authenticate method.
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


        // Bottom is logic for handling messages from the server.


        /// <summary>
        /// Handles BYE message from the server.
        /// Just closes the network stream and terminates the program with code 0.
        /// </summary>
        void HandleReceivedBYE()
        {
            _networkStream.Close();
            Environment.Exit(0);
        }

        /// <summary>
        /// Handles the received MSG message from the server.
        /// </summary>
        /// <param name="parts">Packet word by word ({MSG} {FROM} {NAME} {IS} ....)</param>
        void HandleReceivedMSG(string[] parts)
        {
            string displayName = parts[2];
            string messageContent = string.Join(" ", parts[4..]);
            Console.WriteLine($"{displayName}: {messageContent}");
        }

        /// <summary>
        /// Handles the received ERR message from the server.
        /// Displays the error message and terminates the program.
        /// It changed from last year project, last year it was sending BYE message to server and then terminating, now from FSM it seems that is not required.
        /// </summary>
        /// <param name="parts">Packet word by word ({ERR} {FROM} {SERVER} {IS} ....)</param>
        void HandleReceivedERR(string[] parts)
        {
            string errorDisplayName = parts[2];
            string errorContent = string.Join(" ", parts[4..]);
            Console.WriteLine($"ERROR FROM {errorDisplayName}: {errorContent}");
            // SendMessage("BYE FROM " + _displayName + "\r\n"); // This is 50/50 situation, i would send bye message, but it is not required how i see at FSM
            _networkStream.Close();
            Environment.Exit(0);
        }

        /// <summary>
        /// Handles the received REPLY message from the server.
        /// Writes {OK|NOK}.
        /// </summary>
        /// <param name="parts">Packet word by word ({REPLY} ....)</param>
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
        /// Writes Internal Error and sends a ERR packet to the server.
        /// Correct terminates with code 1.
        /// </summary>
        void HandeReceivedUnknown()
        {
            WriteInternalError("Unknown Packet Type");
            SendMessage($"ERR FROM {_displayName} IS Unknown Packet Type"+"\r\n");
            // SendMessage("BYE FROM " + _displayName + "\r\n"); // This is 50/50 situation, i would send bye message, but it is not required how i see at FSM
            _recieveThreadRunning = false;
            _networkStream.Close();
            Environment.Exit(1);
        }

        /// <summary>
        /// Event handler for console cancel key press.
        /// Called when the user presses Ctrl+C.
        /// It sends a BYE message to the server and closes the network stream with code 0.
        /// </summary>
        void Console_CancelKeyPress(object? sender, ConsoleCancelEventArgs e)
        {
            SendMessage("BYE FROM " + _displayName + "\r\n");
            _recieveThreadRunning = false;
            _networkStream.Close();
            Environment.Exit(0);
        }
    }
}
