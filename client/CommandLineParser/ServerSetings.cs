using System.Net;

namespace ipk25_chat.CommandLineParser
{
    public class ServerSetings
    {
        // Properties to hold command line arguments
        public string transportProtocol { get; } = "";
        public string serverAddress { get; } = "";
        public ushort serverPort { get; } = 4567;
        public ushort udpConfirmationTimeout { get; } = 250;
        public byte maxUdpRetransmissions { get; } = 3;

        /// <summary>
        /// Constructor to parse command line arguments and initialize properties.
        /// It takes an array of strings as input, which are the command line arguments.
        /// And you can use object properties to get the values.
        /// it handles Errors with Argument processing and prints help information.
        /// Eazy to use. Eazy to understand. Eazy to change.
        /// </summary>
        /// <param name="args">Arguments from console.</param>
        /// <exception cref="ArgumentException">Some Error with Argument processing(check message in it).</exception>
        public ServerSetings(string[] args)
        {
            // Parse command line arguments
            for (int i = 0; i < args.Length; i += 2)
            {
                switch (args[i]) // All arguments switch
                {
                    case "-t":
                        switch(args[i + 1].ToLower())
                        {
                            case "tcp":
                            case "udp":
                                transportProtocol = args[i + 1];
                                break;
                            default:
                                throw new System.ArgumentException("Invalid transport protocol");
                        }
                        break;
                    case "-s":
                        serverAddress = args[i + 1];
                        if (!IPAddress.TryParse(serverAddress, out IPAddress? ipAddress))
                        {
                            // It's not a valid IPv4 address, try resolving the domain name
                            try
                            {
                                IPAddress[] addresses = Dns.GetHostAddresses(serverAddress);

                                // Pick the first IPv4 address from the array
                                ipAddress = addresses.FirstOrDefault(addr => addr.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
                                if (ipAddress == null)
                                {
                                    throw new ArgumentException("Unable to resolve domain to IPv4 address");
                                }
                                serverAddress = ipAddress.ToString();
                            }
                            catch (System.Net.Sockets.SocketException)
                            {
                                throw new ArgumentException("Invalid server address");
                            }
                        }
                        break;
                    case "-p":
                        if (ushort.TryParse(args[i + 1], out ushort serverPort)) this.serverPort = serverPort;
                        break;
                    case "-d":
                        if (ushort.TryParse(args[i + 1], out ushort udpConfirmationTimeout)) this.udpConfirmationTimeout = udpConfirmationTimeout;
                        break;
                    case "-r":
                        if (byte.TryParse(args[i + 1], out byte maxUdpRetransmissions)) this.maxUdpRetransmissions = maxUdpRetransmissions;
                        break;
                    case "-h":
                        PrintHelp(); // Print help information
                        Environment.Exit(0);
                        break;
                    default:
                        throw new ArgumentException($"Unknown argument: {args[i]}");
                }
            }
            // Check for mandatory arguments
            if (string.IsNullOrEmpty(transportProtocol) || string.IsNullOrEmpty(serverAddress))
            {
                throw new ArgumentException("Mandatory arguments -t and -s must be specified.");
            }
        }
        // Print help information
        private static void PrintHelp()
        {
            Console.WriteLine("Program Help:");
            Console.WriteLine("-t <tcp/udp>          : Transport protocol used for connection");
            Console.WriteLine("-s <IP/hostname>      : Server IP or hostname");
            Console.WriteLine("-p <port>             : Server port");
            Console.WriteLine("-d <timeout>          : UDP confirmation timeout");
            Console.WriteLine("-r <retransmissions>  : Maximum number of UDP retransmissions");
            Console.WriteLine("-h                    : Prints program help output and exits");
        }
    }
}
