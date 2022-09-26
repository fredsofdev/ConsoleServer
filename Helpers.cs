using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleServer
{
    public enum AkcNakEot { ACK, NAK, EOT }
    public static class Helpers
    {

        public static AkcNakEot GetResponse(string hex)
        {
            
            switch (hex)
            {
                case "06":
                    return AkcNakEot.ACK;
                case "15":
                    return AkcNakEot.NAK;
                case "04":
                    return AkcNakEot.EOT;
                default:
                    return AkcNakEot.NAK;
            }
        }

        public static byte[] Combine(byte[] first, byte[] second)
        {
            byte[] ret = new byte[first.Length + second.Length];
            Buffer.BlockCopy(first, 0, ret, 0, first.Length);
            Buffer.BlockCopy(second, 0, ret, first.Length, second.Length);
            return ret;
        }

        public static byte[] HexStringToByteArray(string hex)
        {
            hex = hex.Replace(",", "").Replace("<", "").Replace(">", "").Trim();
            Console.WriteLine(hex);
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0).ToList()
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }

        public static byte[] SEED(string Input, int isEncrypt)
        {
            try
            {
                using (Process myProcess = new Process())
                {
                    myProcess.StartInfo.UseShellExecute = false;
                    myProcess.StartInfo.FileName = AppDomain.CurrentDomain.BaseDirectory + "\\CEED\\Project1.exe";
                    myProcess.StartInfo.CreateNoWindow = true;
                    myProcess.StartInfo.RedirectStandardInput = true;
                    myProcess.StartInfo.RedirectStandardOutput = true;
                    myProcess.Start();

                    StreamWriter myStreamWriter = myProcess.StandardInput;
                    StreamReader reader = myProcess.StandardOutput;

                    myStreamWriter.WriteLine(Input);
                    myStreamWriter.WriteLine(isEncrypt);
                    string output = reader.ReadToEnd();
                    Console.WriteLine(output);
                    myStreamWriter.Close();
                    myProcess.WaitForExit();
                    return HexStringToByteArray(output.Split('\n').Last());
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return new byte[0];
            }
        }
    }
}
