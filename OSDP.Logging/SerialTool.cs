using System.IO.Ports;

namespace OSDP.Logging;

class SerialTool
{
    static private int counter = 0;
    static private int skip = 0;

    public static void CursorSpin(int delay)
    {
        string[] sequence = new string[] { ".    ", "..   ", "...  ", ".... ", "....." };
        skip++;

        if (delay == skip)
        {
            counter++;
            skip = 0;
        }

        if (counter >= sequence.Length)
        {
            counter = 0;
        }

        Console.Write(sequence[counter]);
        Console.SetCursorPosition(Console.CursorLeft - sequence[counter].Length, Console.CursorTop);
    }

    public static string BytesToHexString(byte[] bytes, string separator = ";")
    {
        return string.Join(separator, bytes.Select(i => i.ToString("X2")));
    }

    public static string CheckComPort(string[] args)
    {
        string[] portNames = SerialPort.GetPortNames();

        if (args.Length == 0 || !portNames.Contains(args[0]))
        {
            Console.WriteLine("Please specify a valid COM-port.");

            Console.WriteLine();
            Console.WriteLine("Available COM-ports:");
            foreach (string portName in portNames)
            {
                Console.WriteLine(portName);
            }

            return "";
        }

        return args[0];
    }
}
