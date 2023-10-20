using System.IO.Ports;

namespace OSDP.Logging;

class Program
{
    static void Main(string[] args)
    {
        string comPort = SerialTool.CheckComPort(args);
        if (comPort == "")
        {
            return;
        }

        bool start = false;
        string filename = "osdp_" + DateTime.Now.ToString("yyyMMdd_HHmmss_fff") + ".txt";
        Console.CursorVisible = false;

        using SerialPort sp = new SerialPort(comPort);
        sp.Open();

        while (true)
        {
            int buf = sp.ReadByte();

            if (buf == 0x53)
            {
                OSDP osdp = new OSDP(sp);

                if (osdp.ValidMessage)
                {
                    start = true;
                    osdp.ConsoleLog();
                    osdp.FileLog(filename);
                }
                else
                {
                    Console.WriteLine("OSDP not valid. CKSUM or CRC not correct.");
                }
            }
            else if (buf != 0xFF && !start)
            {
                Console.Write($"{buf:X2},");
            }
        }
    }
}
