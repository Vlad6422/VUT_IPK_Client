
using System.Text;

namespace ipk25_chat.Messages
{
    /// <summary>
    /// Represents an error message sent to the server.
    /// </summary>
    public class ErrMessage
    {
        public byte MessageType { get; set; } = 0xFE;
        public ushort MessageID { get; set; }
        public string DisplayName { get; set; }
        public string MessageContents { get; set; }

        public ErrMessage(byte[] buff)
        {
            MessageType = buff[0];
            MessageID = BitConverter.ToUInt16(buff, 1);
            int displayNameLength = Array.IndexOf(buff, (byte)0, 3) - 3;
            DisplayName = Encoding.ASCII.GetString(buff, 3, displayNameLength);
            int messageContentsLength = Array.IndexOf(buff, (byte)0, 3 + displayNameLength + 1) - (3 + displayNameLength + 1);
            MessageContents = Encoding.ASCII.GetString(buff, 3 + displayNameLength + 1, messageContentsLength);
        }
        public ErrMessage(ushort messageID, string displayName, string messageContents)
        {
            MessageID = messageID;
            DisplayName = displayName;
            MessageContents = messageContents;
        }

        /// <summary>
        /// Converts the message to a byte array.
        /// </summary>
        /// <returns>Byte Array</returns>
        public byte[] ToByteArray()
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
