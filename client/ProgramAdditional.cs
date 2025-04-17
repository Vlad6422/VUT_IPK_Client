using ipk24chat_client.Clients.Tcp;
using ipk24chat_client.Clients.Udp;
using ipk25_chat.CommandLineParser;
using System.Net.Sockets;

namespace ipk25_chat
{
    // Additional methods for the client startup. Just for better readability.
    public partial class Program
    {
        // Creates TcpClient and connects to the server. Then my TcpUser class is created with stream and the chat is started.
        static void RunTcpClient(ServerSetings serverSetings)
        {
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
        }
        // Creates UdpUser,dont need connection,so gets server setting to send packets there.
        static void RunUdpClient(ServerSetings serverSetings)
        {
            UdpUser udpUser = new UdpUser(serverSetings);
            udpUser.EnableChatUDP();
        }
    }
}
