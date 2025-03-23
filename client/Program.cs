using ipk24chat_client.Clients.Tcp;
using ipk24chat_client.Clients.Udp;
using ipk25_chat.CommandLineParser;
using System.Net.Sockets;

namespace ipk25_chat
{
    public class Program
    {
        static void Main(string[] args)
        {
            try
            {
                ServerSetings serverSetings = new ServerSetings(args); // Parse command line arguments
                switch (serverSetings.transportProtocol) // Choose the right client
                {
                    case "tcp": // Run TCP client
                        using (TcpClient tcpClient = new TcpClient(AddressFamily.InterNetwork))
                        {
                            tcpClient.Connect(serverSetings.serverAddress, serverSetings.serverPort);
                            using (NetworkStream networkStream = tcpClient.GetStream())
                            {
                                TcpUser tcpUser = new TcpUser(networkStream);
                                tcpUser.EnableChatTcp();
                            }
                            tcpClient.Close();
                        }
                        break;
                    case "udp": // Run UDP client
                        UdpUser udpUser = new UdpUser(serverSetings);
                        udpUser.EnableChatUDP();
                        break;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"ERROR: {e.Message}");
                Environment.Exit(1);
            }
        }
    }
}