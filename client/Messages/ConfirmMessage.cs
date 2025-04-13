namespace ipk25_chat.Messages
{
    /// <summary>
    /// Represents an confirm message sent to the server.
    /// </summary>
    public class ConfirmMessage
    {
        public byte MessageType { get; set; } = 0x00;
        public ushort RefMessageID { get; set; }

        // Constructor
        public ConfirmMessage(ushort RefMessageID)
        {
            this.RefMessageID = RefMessageID;
        }

        /// <summary>
        /// Converts the message to a byte array.
        /// </summary>
        /// <returns>Byte Array</returns>
        public byte[] GET()
        {
            List<byte> bytes = new List<byte>();
            bytes.Add(MessageType);
            bytes.AddRange(BitConverter.GetBytes(RefMessageID));
            return bytes.ToArray();
        }
    }
}
