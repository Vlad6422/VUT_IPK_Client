using System.Text;

namespace ipk25_chat.Messages
{
    /// <summary>
    /// Represents a message sent to the server.
    /// </summary>
    public class MsgMessage
    {
        public byte MessageType { get; set; } = 0x04;
        public ushort MessageID { get; set; }
        public string DisplayName { get; set; }
        public string MessageContents { get; set; }

        // Constructor
        public MsgMessage(byte[] buff)
        {
            MessageID = BitConverter.ToUInt16(buff, 1);
            int displayNameLength = Array.IndexOf(buff, (byte)0, 3) - 3;
            DisplayName = Encoding.ASCII.GetString(buff, 3, displayNameLength);
            int messageContentsLength = Array.IndexOf(buff, (byte)0, 3 + displayNameLength + 1) - (3 + displayNameLength + 1);
            MessageContents = Encoding.ASCII.GetString(buff, 3 + displayNameLength + 1, messageContentsLength);
        }

        // Constructor overload
        public MsgMessage(ushort MessageId, string DisplayName, string MessageContent)
        {
            MessageID = MessageId;
            this.DisplayName = DisplayName;
            MessageContents = MessageContent;
        }

        // Method to get the bytes of the message
        public byte[] GET()
        {
            List<byte> bytes = new List<byte>();
            bytes.Add(MessageType);
            bytes.AddRange(BitConverter.GetBytes(MessageID));
            bytes.AddRange(Encoding.ASCII.GetBytes(DisplayName));
            bytes.Add(0);
            bytes.AddRange(Encoding.ASCII.GetBytes(MessageContents));
            bytes.Add(0);
            return bytes.ToArray();
        }
    }
}
