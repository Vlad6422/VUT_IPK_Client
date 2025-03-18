namespace ipk25_chat.Messages
{
    public class ConfirmMessage
    {
        public byte MessageType { get; set; } = 0x00;
        public ushort RefMessageID { get; set; }

        // Constructor
        public ConfirmMessage(ushort RefMessageID)
        {
            this.RefMessageID = RefMessageID;
        }

        // Method to get the bytes of the message
        public byte[] GET()
        {
            List<byte> bytes = new List<byte>();
            bytes.Add(MessageType);
            bytes.AddRange(BitConverter.GetBytes(RefMessageID));
            return bytes.ToArray();
        }
    }
}
