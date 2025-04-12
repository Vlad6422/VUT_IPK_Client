using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using ipk25_chat.Clients;
using ipk25_chat.CommandLineParser;
using ipk25_chat.Messages;

namespace ipk24chat_client.Clients.Udp
{
    public class UdpUser : User
    {
        private string _message { get; set; } = string.Empty;
        private ushort _messageId { get; set; } = 0;
        private ushort udpConfirmationTimeout { get; }
        private byte maxUdpRetransmissions { get; }
        private bool _isAuthorized { get; set; }
        private UdpClient _client = new UdpClient(0);
        private IPEndPoint _serverEndPoint;
        List<ushort> confirmedMessages = new List<ushort>();
        Thread thread;
        public UdpUser(ServerSetings server)
        {
            udpConfirmationTimeout = server.udpConfirmationTimeout;
            maxUdpRetransmissions = server.maxUdpRetransmissions;
            _serverEndPoint = new IPEndPoint(IPAddress.Parse(server.serverAddress), server.serverPort);
            thread= new Thread(RecieveUdpPacket);
        }
        public UdpUser(string IpAdress, ushort port, ushort udpConfirmationTimeout, byte maxUdpRetransmissions)
        {
            this.udpConfirmationTimeout = udpConfirmationTimeout;
            this.maxUdpRetransmissions = maxUdpRetransmissions;
            _serverEndPoint = new IPEndPoint(IPAddress.Parse(IpAdress), port);
            thread = new Thread(RecieveUdpPacket);
        }
        /// <summary>
        /// This method handles the main communication loop for the UDP client.
        /// It reads user input from the console, processes commands, and sends messages to the server.
        /// It also handles authentication, joining channels, renaming display names, and other commands.
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
        void RecieveUdpPacket()
        {
            while (true)
            {
                try
                {
                    byte[] buff = _client.Receive(ref _serverEndPoint);
                    ushort result = BitConverter.ToUInt16(buff, 1);
                    ConfirmMessage confirmMessage = new ConfirmMessage(result);
                    if (buff[0] != 0x00)
                    {
                        _client.Send(confirmMessage.GET(), confirmMessage.GET().Length, _serverEndPoint);
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
                            ByeMessage byeMessage = new ByeMessage(_messageId, _displayName);
                            _client.Send(byeMessage.GET(), byeMessage.GET().Length, _serverEndPoint);
                            Environment.Exit(1);
                        }
                        if (buff[0] == 0xFF)
                        {
                            _client.Close();
                            Environment.Exit(0);
                        }
                        if (buff[0]!= 0x00 && buff[0] != 0x01 && buff[0] != 0x04 && buff[0] != 0xFE && buff[0] != 0xFF && buff[0] != 0xFD)
                        {
                            ErrMessage errMessage = new ErrMessage(_messageId,_displayName,"Incorrect packet Type");
                            _client.Send(errMessage.ToByteArray(), errMessage.ToByteArray().Length, _serverEndPoint);
                            ByeMessage byeMessage = new ByeMessage(_messageId, _displayName);
                            _client.Send(byeMessage.GET(), byeMessage.GET().Length, _serverEndPoint);
                            WriteInternalError("Unknown message type" + buff[0]);
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
            _client.Close();
            Environment.Exit(0);
        }
    }
}