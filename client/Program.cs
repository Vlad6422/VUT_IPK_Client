using ipk25_chat.CommandLineParser;

namespace ipk25_chat
{
    /// <summary>
    /// Author: Malashchuk Vladyslav (xmalas04)
    /// Program: Client for IPK25
    /// </summary>
    public partial class Program
    {
        // Start point of the program. It parses command line arguments and starts the client.
        static void Main(string[] args)
        {
            try
            {
                ServerSetings serverSetings = new ServerSetings(args); // Parse command line arguments
                switch (serverSetings.transportProtocol) // Choose the right client
                {
                    case "tcp": // Run TCP client
                        RunTcpClient(serverSetings);
                        break;
                    case "udp": // Run UDP client
                        RunUdpClient(serverSetings);
                        break;
                }
            }
            catch (Exception e) // Handle Errors
            {
                Console.WriteLine($"ERROR: {e.Message}");
                Environment.Exit(1);
            }
        }

    }
}