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
                ServerSetings serverSetings = new ServerSetings(args);
                switch (serverSetings.transportProtocol)
                {
                    case "tcp":
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
                    case "udp":
                        UdpUser udpUser = new UdpUser(serverSetings);
                        udpUser.Start();
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