using System.Net;
using System.Net.Sockets;
using ipk25_chat.Clients;
using ipk25_chat.CommandLineParser;
using ipk25_chat.Messages;

namespace ipk24chat_client.Clients.Udp
{
    public class UdpUser : User
    {
        // if field dont have comment, find it in TcpUser class
        private string _message { get; set; } = string.Empty; 
        private ushort _messageId { get; set; } = 0;
        private ushort udpConfirmationTimeout { get; } // Timeout for UDP confirmation in milliseconds
        private byte maxUdpRetransmissions { get; } // Maximum number of retransmissions for UDP messages
        private bool _isAuthorized { get; set; }
        private UdpClient _client = new UdpClient(0); // UDP client for sending and receiving messages on the local port (random).
        private IPEndPoint _serverEndPoint; // Server endpoint for sending messages.
        List<ushort> confirmedMessages = new List<ushort>(); // List of confirmed messages. Need to check if message was confirmed by server and resend it if not.
        private Thread thread; // Thread for receiving UDP packets.
        private Dictionary<ushort, bool> _alreadyRecievedMessages = new Dictionary<ushort, bool>(); // This dictionary is used to track messages that have already been received, preventing duplicate processing.
        public UdpUser(ServerSetings server)
        {
            udpConfirmationTimeout = server.udpConfirmationTimeout;
            maxUdpRetransmissions = server.maxUdpRetransmissions;
            _serverEndPoint = new IPEndPoint(IPAddress.Parse(server.serverAddress), server.serverPort);
            thread= new Thread(RecieveUdpPacket);
        }
        // Not used, was used for testing purposes before creating ServerSetings class. But can be used in future so it is public.
        public UdpUser(string IpAdress, ushort port, ushort udpConfirmationTimeout, byte maxUdpRetransmissions)
        {
            this.udpConfirmationTimeout = udpConfirmationTimeout;
            this.maxUdpRetransmissions = maxUdpRetransmissions;
            _serverEndPoint = new IPEndPoint(IPAddress.Parse(IpAdress), port);
            thread = new Thread(RecieveUdpPacket);
        }

        /// <summary>
        /// This method handles the main communication loop for the UDP client.
        /// Working +- same as in TcpUser class.
        /// But have some differences like resending messages if they are not confirmed by server.
        /// </summary>
        public void EnableChatUDP()
        {
            thread.Start();
            Console.CancelKeyPress += new ConsoleCancelEventHandler(Console_CancelKeyPress);
            while (true)
            {
                string? userInput = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(userInput))
                {
                    if (userInput == null)
                    {
                        ByeMessage byeMessage = new ByeMessage(_messageId,_displayName);
                        _client.Send(byeMessage.GET(), byeMessage.GET().Length, _serverEndPoint);
                        for (int i = 0; i < maxUdpRetransmissions; i++)
                        {
                            Thread.Sleep(udpConfirmationTimeout);
                            if (!confirmedMessages.Contains(_messageId))
                            {

                                _client.Send(byeMessage.GET(), byeMessage.GET().Length, _serverEndPoint);
                            }
                            else
                            {

                                break;
                            }
                        }
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
                            ChangeDisplayName(commandParts[3]) &&
                            ChangeSecret(commandParts[2]))
                            {
                                Authenticate();
                            }

                            break;

                        case "join":
                            if (!_isAuthorized)
                            {
                                WriteInternalError("You are not Authorized.");
                                continue;
                            }
                            if (commandParts.Length != 2)
                            {
                                WriteInternalError("Invalid number of parameters for /join command.");
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
                    _message = userInput;

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
                    foreach (char c in _message)
                    {
                        if (c < 0x20 || c > 0x7E)
                        {
                            WriteInternalError("Invalid character detected. Only printable ASCII characters (0x20-7E) are allowed.");
                            continue;
                        }
                    }
                    SendMessage(_message);
                }
            }
        }

        /// <summary>
        /// Authenticates the user by sending an AUTH message to the server.
        /// Resends the message if it is not confirmed by the server.
        /// Uses confirmedMessages list to check if the message was confirmed. Remove it from the list if it was confirmed.
        /// </summary>
        void Authenticate()
        {
            AuthMessage authMessage = new AuthMessage(_messageId, _username, _displayName, _secret);
            
            _client.Send(authMessage.GET(), authMessage.GET().Length, _serverEndPoint);
            
            for (int i = 0; i < maxUdpRetransmissions; i++)
            {
                Thread.Sleep(udpConfirmationTimeout);
                if (!confirmedMessages.Contains(_messageId))
                {
                    _client.Send(authMessage.GET(), authMessage.GET().Length, _serverEndPoint);
                }
                else
                {
                    confirmedMessages.Remove(_messageId);
                    break;
                }
            }
            _messageId++;
        }

        /// <summary>
        /// This method sends JOIN packet to the server.
        /// </summary>
        public void JoinChannel(string channelName)
        {
            if (channelName.Length > 20 || !System.Text.RegularExpressions.Regex.IsMatch(channelName, @"^[A-Za-z0-9\-]+$"))
            {
                WriteInternalError("Too Big ChannelName OR Incorrect");
            }
            else
            {
                JoinMessage joinMessage = new JoinMessage(_messageId, channelName, _displayName);
                _client.Send(joinMessage.GET(), joinMessage.GET().Length, _serverEndPoint);
                _messageId++;
            }
        }

        /// <summary>
        /// This method sends a message to the server.
        /// Supports resending the message if it is not confirmed by the server.
        /// +1 to messageId for each message sent.
        /// </summary>
        /// <param name="message">Message in string, will be formated to correct form in bytes</param>
        public void SendMessage(string message)
        {
            MsgMessage msgMessage = new MsgMessage(_messageId, _displayName, message);
            _client.Send(msgMessage.GET(), msgMessage.GET().Length, _serverEndPoint);

            for (int i = 0; i < maxUdpRetransmissions; i++)
            {
                Thread.Sleep(udpConfirmationTimeout);
                if (!confirmedMessages.Contains(_messageId))
                {
                    _client.Send(msgMessage.GET(), msgMessage.GET().Length, _serverEndPoint);
                }
                else
                {
                    confirmedMessages.Remove(_messageId);
                    break;
                }
            }
            _messageId++;

        }

        /// <summary>
        /// This is main loop for receiving UDP packets.
        /// It handles all incoming messages from the server.
        /// I didnt want to write method for each message type, so I used if. Becouse it is easier to read and every message type is handled by 2-3 lines.
        /// Can be rewritten to use switch case, but I think it is not needed. In practice if statement is easier to read and understand. Like if this is true, do this. If not, do that.
        /// Working easy - checks type of message and calls the correct case.
        /// If it is confirmation message, it adds it to the confirmedMessages list.
        /// If it is not confirmation message, it checks if the message is already received. And if it is not, it adds it to the _alreadyRecievedMessages dictionary.
        /// Processes the message based on its type.
        /// Handles errors and unknown message types.
        /// </summary>
        void RecieveUdpPacket()
        {
            while (true)
            {
                try
                {
                    byte[] buff = _client.Receive(ref _serverEndPoint);
                    ushort result = BitConverter.ToUInt16(buff, 1);
                    
                    ConfirmMessage confirmMessage = new ConfirmMessage(result);
                    // Check if the message is a confirmation message
                    if (buff[0] != 0x00)
                    {
                        _client.Send(confirmMessage.GET(), confirmMessage.GET().Length, _serverEndPoint);
                        // Check if the message has already been received
                        if (_alreadyRecievedMessages.ContainsKey(result))
                        {
                            continue;
                        }
                        else
                        {
                            _alreadyRecievedMessages.Add(result, true);
                        }
                        // Process the message based on its type
                        if (buff[0] == 0x01)
                        {
                            ReplyMessage replyMessage = new ReplyMessage(buff);
                            if (replyMessage.Result == 0x01)
                            {
                                Console.WriteLine("Action Success: " + replyMessage.MessageContents);
                                _isAuthorized = true;
                            }
                            else if (replyMessage.Result == 0x00)
                            {
                                Console.WriteLine("Action Failure: " + replyMessage.MessageContents);
                            }
                        }

                        if (buff[0] == 0x04)
                        {
                            MsgMessage msgMessage = new MsgMessage(buff);
                            Console.WriteLine(msgMessage.DisplayName + ": " + msgMessage.MessageContents);
                        }
                        if (buff[0] == 0xFE)
                        {
                            ErrMessage errMessage = new ErrMessage(buff);
                            Console.WriteLine("ERROR FROM " + errMessage.DisplayName + ": " + errMessage.MessageContents);
                            Environment.Exit(1);
                        }
                        if (buff[0] == 0xFF)
                        {
                            Environment.Exit(0);
                        }
                        if (buff[0]!= 0x00 && buff[0] != 0x01 && buff[0] != 0x04 && buff[0] != 0xFE && buff[0] != 0xFF && buff[0] != 0xFD)
                        {
                            ErrMessage errMessage = new ErrMessage(_messageId,_displayName,"Incorrect packet Type");
                            _client.Send(errMessage.ToByteArray(), errMessage.ToByteArray().Length, _serverEndPoint);
                            WriteInternalError("Unknown message type" + buff[0]);
                            Environment.Exit(1);
                        }
                    }
                    else if (buff[0] == 0x00)
                    {
                        confirmedMessages.Add(buff[1]);
                    }
                }
                
                catch (Exception ex)
                {
                    Console.WriteLine($"ERROR: {ex.Message}");
                    Environment.Exit(1);
                }
            }
        }

        /// <summary>
        /// Event handler for console cancel key press.
        /// Called when the user presses Ctrl+C.
        /// It sends a BYE message to the server and closes the network stream with code 0.
        /// </summary>
        void Console_CancelKeyPress(object? sender, ConsoleCancelEventArgs e)
        {
            ByeMessage byeMessage = new ByeMessage(_messageId,_displayName);
            _client.Send(byeMessage.GET(), byeMessage.GET().Length, _serverEndPoint);
            for (int i = 0; i < maxUdpRetransmissions; i++)
            {
                Thread.Sleep(udpConfirmationTimeout);
                if (!confirmedMessages.Contains(_messageId))
                {
                    _client.Send(byeMessage.GET(), byeMessage.GET().Length, _serverEndPoint);
                }
                else
                {
                    break;
                }
            }
            Environment.Exit(0);
        }
    }
}