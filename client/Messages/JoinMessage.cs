using System.Text;

namespace ipk25_chat.Messages
{
    /// <summary>
    /// Join message sent to the server. (JOIN)
    /// </summary>
    public class JoinMessage
    {
        public byte MessageType { get; set; } = 0x03;
        public ushort MessageID { get; set; }
        public string ChannelID { get; set; }
        public string DisplayName { get; set; }

        public JoinMessage(ushort messageID, string channelID, string displayName)
        {
            MessageID = messageID;
            ChannelID = channelID;
            DisplayName = displayName;
        }

        /// <summary>
        /// Converts the message to a byte array. (UDP)
        /// </summary>
        public byte[] GET()
        {
            List<byte> bytes = new List<byte>();
            bytes.Add(MessageType);
            bytes.AddRange(BitConverter.GetBytes(MessageID));
            bytes.AddRange(Encoding.ASCII.GetBytes(ChannelID));
            bytes.Add(0);
            bytes.AddRange(Encoding.ASCII.GetBytes(DisplayName));
            bytes.Add(0);
            return bytes.ToArray();
        }
    }
}
