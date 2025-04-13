using System.Text;

namespace ipk25_chat.Messages
{
    /// <summary>
    /// Represents an authentication message sent to the server.
    /// </summary>
    public class AuthMessage
    {
        public byte MessageType { get; set; } = 0x02;
        public ushort MessageID { get; set; }
        public string Username { get; set; }
        public string DisplayName { get; set; }
        public string Secret { get; set; }

        // Constructor
        public AuthMessage(ushort MessageID, string Username, string DisplayName, string Secret)
        {
            this.MessageID = MessageID;
            this.Username = Username;
            this.DisplayName = DisplayName;
            this.Secret = Secret;
        }

        /// <summary>
        /// Converts the message to a byte array.
        /// </summary>
        /// <returns>Byte Array</returns>
        public byte[] GET()
        {
            List<byte> bytes = new List<byte>();
            bytes.Add(MessageType);
            bytes.AddRange(BitConverter.GetBytes(MessageID));
            bytes.AddRange(Encoding.ASCII.GetBytes(Username));
            bytes.Add(0);
            bytes.AddRange(Encoding.ASCII.GetBytes(DisplayName));
            bytes.Add(0);
            bytes.AddRange(Encoding.ASCII.GetBytes(Secret));
            bytes.Add(0);
            return bytes.ToArray();
        }
    }
}
