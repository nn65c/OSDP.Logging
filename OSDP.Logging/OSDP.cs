using System.IO.Ports;

namespace OSDP.Logging;

class OSDP
{
    public byte Address { get; }
    public ushort Length { get; }
    public byte Ctrl { get; }
    public byte CmdReply { get; }
    public byte[] Data { get; }
    public ushort Checksum { get; }
    public byte[] Message { get; }
    public bool ValidMessage { get; } = false;
    public bool PollAck { get; }
    public bool CRC { get; set; }

    public OSDP(SerialPort sp)
    {
        byte[] header = new byte[5];
        sp.Read(header, 0, 5);

        Length = (ushort)(header[1] + (header[2] << 8));

        if (CheckHeader())
        {
            Message = new byte[Length];
            Message[0] = 0x53;

            Data = new byte[Length - 7];
            sp.Read(Data, 0, Length - 7);

            if (Length > 7)
            {
                Array.Copy(Data, 0, Message, 6, Length - 7);
            }

            Array.Copy(header, 0, Message, 1, 5);
            Message[Length - 1] = (byte)sp.ReadByte();
            Address = Message[1];
            Ctrl = Message[4];
            CmdReply = Message[5];

            if ((Ctrl & (1 << 2)) != 0)
            {
                Checksum = (ushort)(Message[Length - 2] + (Message[Length - 1] << 8));
                CRC = true;
            }
            else
            {
                Checksum = Message[Length - 1];
                CRC = false;
            }

            ValidMessage = CheckMessage();
            PollAck = CmdReply == 0x40 || CmdReply == 0x60;
        }

    }

    private bool CheckHeader()
    {
        return Length >= 7 && Length <= 1440;
    }

    private bool CheckMessage()
    {
        if (CRC)
        {
            byte[] messageCRC = new byte[Message.Length - 2];
            Array.Copy(Message, messageCRC, Message.Length - 2);

            var calculateCRC = CalculateCRC(messageCRC);

            return Checksum == calculateCRC;
        }
        else
        {
            byte[] messageCKSUM = new byte[Message.Length - 1];
            Array.Copy(Message, messageCKSUM, Message.Length - 1);

            var calculateCKSUM = CalculateCKSUM(messageCKSUM);

            return Checksum == calculateCKSUM;
        }
    }

    // Use 16 bit CRC (CRC16-CCITT) if bit 3 in CTRL is SET.
    private static ushort CalculateCRC(byte[] data)
    {
        const ushort Polynomial = 0x1021;
        ushort crcValue = 0xFFFF;
        
        byte[] augBytes = new byte[] { 0, 0 };  // Augmented zeros
        byte[] fullData = new byte[augBytes.Length + data.Length];

        // Combine augmented bytes and data
        augBytes.CopyTo(fullData, 0);
        data.CopyTo(fullData, augBytes.Length);

        foreach (byte by in fullData)
        {
            crcValue ^= (ushort)(by << 8);
            for (ushort i = 0; i < 8; ++i)
            {
                if ((crcValue & 0x8000) > 0)
                    crcValue = (ushort)((crcValue << 1) ^ Polynomial);
                else
                    crcValue <<= 1;
            }
        }
        return crcValue;
    }

    // Use 8-bit CKSUM if bit 3 in CTRL is UNSET.
    // The CKSUM value is the 8 least significant bits of the 2’s complement value of the sum of all previous characters of the message.
    private static byte CalculateCKSUM(byte[] bytes)
    {
        int calcSum = 0;
        int bytesLength = bytes.Length;

        for (int i = 0; i < bytesLength; i++)
        {
            calcSum += bytes[i];
        }

        calcSum = ~calcSum;
        calcSum++;

        return (byte)calcSum;
    }

    public void ConsoleLog()
    {
        string consoleMessage = Timestamp() + ";" + SerialTool.BytesToHexString(Message);

        if (!ValidMessage)
        {
            Console.WriteLine(consoleMessage);
            Console.Write("- Message not valid. Checksum ERROR");
        }
        else if (PollAck)
        {
            SerialTool.CursorSpin(5);
        }
        else
        {
            Console.WriteLine(consoleMessage);
        }
    }

    public void FileLog(string filename)
    {
        string fileMessage = Timestamp() + ";" + SerialTool.BytesToHexString(Message);

        if (!ValidMessage)
        {
            fileMessage += ";NotValid";
        }

        if (!PollAck)
        {
            try
            {
                FileStream fileStream = File.Open(filename, FileMode.Append, FileAccess.Write);
                StreamWriter fileWriter = new StreamWriter(fileStream);

                fileWriter.Write(fileMessage);
                fileWriter.WriteLine();
                fileWriter.Flush();
                fileWriter.Close();
            }

            catch (IOException ioe)
            {
                Console.WriteLine(ioe);
            }
        }
    }

    #region Helpers
    private static string Timestamp()
    {
        return DateTime.Now.ToString("dd.MM.yy HH:mm:ss");
    }
    #endregion
}
