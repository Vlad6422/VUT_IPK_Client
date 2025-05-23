﻿using System.Text;

namespace ipk25_chat.Messages
{
    /// <summary>
    /// Bye message sent to the server. (BYE)
    /// </summary>
    public class ByeMessage
    {
        public byte MessageType { get; set; } = 0xFF;
        public ushort MessageID { get; set; }
        public string DisplayName { get; set; }
        public ByeMessage(ushort MessageId,string displayName)
        {
            MessageID = MessageId;
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
            bytes.AddRange(Encoding.ASCII.GetBytes(DisplayName));
            bytes.Add(0);
            return bytes.ToArray();
        }
    }
}
