using System.Text;

namespace ipk25_chat.Messages
{
    /// <summary>
    /// Represents an reply message sent to the server.
    /// </summary>
    public class ReplyMessage
    {
        public byte MessageType { get; set; } = 0x01;
        public ushort MessageID { get; set; }
        public byte Result { get; set; }
        public ushort RefMessageID { get; set; }
        public string MessageContents { get; set; }
        public ReplyMessage(byte[] buff)
        {
            MessageType = buff[0];
            MessageID = BitConverter.ToUInt16(buff, 1);
            Result = buff[3];
            RefMessageID = BitConverter.ToUInt16(buff, 4);
            int messageContentsLength = Array.IndexOf(buff, (byte)0, 6) - 6;
            MessageContents = Encoding.ASCII.GetString(buff, 6, messageContentsLength);
        }
    }
}
