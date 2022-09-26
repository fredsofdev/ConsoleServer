
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;

namespace ConsoleServer
{
    class Program
    {

        public static void Main()
        {

            //Console.WriteLine(int.Parse("35"));
            // ShowNetworkInterfaces();
            Task.Run(() => StartServer());
            connectServer();
            //Console.Read();
        }

        public static void ShowNetworkInterfaces()
        {
            System.Net.NetworkInformation.IPGlobalProperties computerProperties = IPGlobalProperties.GetIPGlobalProperties();
            NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
            Console.WriteLine("Interface information for {0}.{1}     ",
                    computerProperties.HostName, computerProperties.DomainName);
            if (nics == null || nics.Length < 1)
            {
                Console.WriteLine("  No network interfaces found.");
                return;
            }

            Console.WriteLine("  Number of interfaces .................... : {0}", nics.Length);
            foreach (NetworkInterface adapter in nics)
            {
                IPInterfaceProperties properties = adapter.GetIPProperties();
                
                Console.WriteLine();
                Console.WriteLine(adapter.Description);
                Console.WriteLine(String.Empty.PadLeft(adapter.Description.Length, '='));
                Console.WriteLine("  Interface type .......................... : {0}", adapter.NetworkInterfaceType);
                Console.WriteLine("  Physical Address ........................ : {0}",
                           adapter.GetPhysicalAddress().ToString());
                Console.WriteLine("  Operational status ...................... : {0}",
                    adapter.OperationalStatus);
                
                string versions = "";

                // Create a display string for the supported IP versions.
                if (adapter.Supports(NetworkInterfaceComponent.IPv4))
                {
                    versions = "IPv4";
                }
                if (adapter.Supports(NetworkInterfaceComponent.IPv6))
                {
                    if (versions.Length > 0)
                    {
                        versions += " ";
                    }
                    versions += "IPv6";
                }
                Console.WriteLine("  IP version .............................. : {0}", versions);
                // ShowIPAddresses(properties);

                // The following information is not useful for loopback adapters.
                if (adapter.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                {
                    continue;
                }
                Console.WriteLine("  DNS suffix .............................. : {0}",
                    properties.DnsSuffix);

                string label;
                if (adapter.Supports(NetworkInterfaceComponent.IPv4))
                {
                    IPv4InterfaceProperties ipv4 = properties.GetIPv4Properties();
                    Console.WriteLine("  MTU...................................... : {0}", ipv4.Mtu);
                    if (ipv4.UsesWins)
                    {

                        IPAddressCollection winsServers = properties.WinsServersAddresses;
                        if (winsServers.Count > 0)
                        {
                            label = "  WINS Servers ............................ :";
                            // ShowIPAddresses(label, winsServers);
                        }
                    }
                }

                Console.WriteLine("  DNS enabled ............................. : {0}",
                    properties.IsDnsEnabled);
                Console.WriteLine("  Dynamically configured DNS .............. : {0}",
                    properties.IsDynamicDnsEnabled);
                Console.WriteLine("  Receive Only ............................ : {0}",
                    adapter.IsReceiveOnly);
                Console.WriteLine("  Multicast ............................... : {0}",
                    adapter.SupportsMulticast);
                // ShowInterfaceStatistics(adapter);

                Console.WriteLine();
            }
        }

        public static void StartServer()
        {
            TcpListener server = null;
            try
            {
                // Set the TcpListener on port 13000.
                Int32 port = 5010;
                IPAddress localAddr = IPAddress.Parse("192.168.50.13");

                // TcpListener server = new TcpListener(port);
                server = new TcpListener(localAddr, port);

                // Start listening for client requests.
                server.Start();

                // Buffer for reading data
                
                

                // Enter the listening loop.
                while (true)
                {
                    Console.WriteLine("Waiting for a connection... ");

                    // Perform a blocking call to accept requests.
                    // You could also use server.AcceptSocket() here.
                    TcpClient client = server.AcceptTcpClient();
                    
                    Console.WriteLine("Connected! {0}", client.Client.RemoteEndPoint);

                    Byte[] bytes = new Byte[18];

                    // Get a stream object for reading and writing
                    NetworkStream stream = client.GetStream();

                    int i;

                    // Loop to receive all the data sent by the client.
                    while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                    {
                        // Translate data bytes to a ASCII string.
                        try
                        {
                            var header = new Header()
                            {
                                Command = Encoding.ASCII.GetString(bytes, 0, 4),
                                CompanyCode = Encoding.ASCII.GetString(bytes,4,7),
                                ChimneyCode = Encoding.ASCII.GetString(bytes,11,3),
                                TotalLength = int.Parse(Encoding.ASCII.GetString(bytes,14,4))
                            };


                            byte[] body = new byte[header.TotalLength - 18];

                            var count = stream.Read(body, 0, body.Length);

                            var errorCode = BitConverter.ToString(body, count - 2, 2);

                            var headerData = Encoding.ASCII.GetString(bytes, 0, i);
                            var data = Encoding.ASCII.GetString(body, 0, count);
                            Console.WriteLine("Received: {0}{1}",headerData, data);
                            Console.WriteLine($"ErrorCode: {errorCode.Replace("-", "")}");

                            // Process the data sent by the client.

                            // string answer = "06";
                            byte[] msg = StringToByteArray("06");
                            // Send back a response.
                            Console.WriteLine("Sent: {0}", BitConverter.ToString(msg));
                            stream.Write(msg, 0, msg.Length);
                            
                        }
                        catch
                        {}
                        
                    }

                    // Shutdown and end connection
                    client.Close();
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
            }
            finally
            {
                // Stop listening for new clients.
                server.Stop();
            }

            Console.WriteLine("\nHit enter to continue...");
            Console.Read();
        }

        private static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }

        public static void connectServer()
        {
            Dictionary<String, Func<byte[]>> protoList = new();

            protoList.Add("PDUM", generateDataPDUM);
            protoList.Add("PSEP", generateDataPSEP);
            protoList.Add("PSET", generateDataPSET);
            protoList.Add("PFCC", generateDataPFCC);
            protoList.Add("PUPG", generateDataPUPG);
            protoList.Add("PVER", generateDataPVER);
            protoList.Add("PFST", generateDataPFST);
            protoList.Add("PFRS", generateDataPFRS);
            protoList.Add("PRSI", generateDataPRSI);
            protoList.Add("PCNG", generateDataPCNG);
            protoList.Add("PFCR", generateDataPFCR);
            protoList.Add("PTIM", generateDataPTIM);


            while (true)
            {
                Console.WriteLine("Type proto:");
                var proto = Console.ReadLine();
                if (proto == null || !protoList.ContainsKey(proto)) continue; 
                TcpClient client = new TcpClient();
                NetworkStream stream = null;
                try
                {
                    client.Connect(new IPEndPoint(IPAddress.Parse("192.168.50.249"), 9090));
                    stream = client.GetStream();
                    Console.WriteLine("Connected to GW");
                    byte[] requestByte = protoList[proto].Invoke();
                    stream.Write(requestByte, 0, requestByte.Length);

                    Byte[] bytes = new Byte[18];
                    int i;

                    // Loop to receive all the data sent by the client.
                    while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                    {
                        // Translate data bytes to a ASCII string.

                        try
                        {

                            if (i == 1)
                            {
                                var response = Helpers.GetResponse(
                                BitConverter.ToString(bytes, 0, i).Replace("-", ""));
                                Console.WriteLine(response);
                                if (response == AkcNakEot.EOT) break;
                            }

                            var header = new Header()
                            {
                                Command = Encoding.ASCII.GetString(bytes, 0, 4),
                                CompanyCode = Encoding.ASCII.GetString(bytes, 4, 7),
                                ChimneyCode = Encoding.ASCII.GetString(bytes, 11, 3),
                                TotalLength = int.Parse(Encoding.ASCII.GetString(bytes, 14, 4))
                            };


                            byte[] body = new byte[header.TotalLength - 18];

                            var count = stream.Read(body, 0, body.Length);

                            var errorCode = BitConverter.ToString(body, count - 2, 2);

                            var headerData = Encoding.ASCII.GetString(bytes, 0, i);
                            var data = Encoding.ASCII.GetString(body, 0, count);
                            Console.WriteLine("Received: {0}{1}", headerData, data);
                            Console.WriteLine();
                            //Console.WriteLine($"ErrorCode: {errorCode.Replace("-", "")}");

                            // Process the data sent by the client.

                            // string answer = "06";
                            //Console.WriteLine();
                            byte[] msg = StringToByteArray("06");
                            //byte[] msg = Encoding.ASCII.GetBytes("A");
                            // Send back a response.
                            Console.WriteLine("Sent: {0}", BitConverter.ToString(msg));
                            stream.Write(msg, 0, msg.Length);

                        }
                        catch
                        { }
                    }

                    client.Close();
                    Console.WriteLine("Disconnected");
                }
                catch (Exception ex)
                { Console.WriteLine($"Disconnected {ex.Message}"); }
            }

            
        }

        public static byte[] converDataToBytes(byte[] fullMessageByte)
        {
            //var fullMessage = String.Join("", header.ToArray());

            Crc16Ccitt errorCheck = new Crc16Ccitt(InitialCrcValue.NonZero1);
            //byte[] fullMessageByte = Encoding.ASCII.GetBytes(fullMessage);
            ushort errorCode = errorCheck.ComputeChecksum(fullMessageByte);
            byte[] errorCodeByte = BitConverter.GetBytes(errorCode);
            Array.Reverse(errorCodeByte);
            //Console.WriteLine(BitConverter.ToString(errorCodeByte, 0, errorCodeByte.Length).Replace("-", ""));
            Console.WriteLine($"Sending: ASC - {Encoding.ASCII.GetString(fullMessageByte)}");
            Console.WriteLine($"Hex - {Convert.ToHexString(fullMessageByte)}{Convert.ToHexString(errorCodeByte)}");
            return Helpers.Combine(fullMessageByte, errorCodeByte);
        }

        private static byte[] generateDataPDUM()
        {
            var current = DateTime.Now.AddHours(-5);
            var startTime = current.AddMinutes(-10);

            List<string> header = new List<string>()
                {
                    String.Format("{0,4}", "PDUM"),
                    String.Format("{0,7}", "1111111"),
                    String.Format("{0,3}", "001"),
                    String.Format("{0,4}", 40),
                    String.Format("{0,10}", startTime.ToString("yyMMddHHmm")),
                    String.Format("{0,10}", current.ToString("yyMMddHHmm"))
                };

            byte[] data = Encoding.ASCII.GetBytes(String.Join("", header.ToArray()));
            return converDataToBytes(data);
        }
        private static byte[] generateDataPSEP()
        {

            List<string> header = new List<string>()
                {
                    String.Format("{0,4}", "PSEP"),
                    String.Format("{0,7}", "1111111"),
                    String.Format("{0,3}", "001"),
                    String.Format("{0,4}", 36),
                    String.Format("{0,16}", "22222222"),
                    
                };

            byte[] data = Encoding.ASCII.GetBytes(String.Join("", header.ToArray()));
            return converDataToBytes(data);
        }
        private static byte[] generateDataPSET()
        {

            List<string> header = new List<string>()
                {
                    String.Format("{0,4}", "PSET"),
                    String.Format("{0,7}", "1111111"),
                    String.Format("{0,3}", "001"),
                    String.Format("{0,4}", 32),
                    String.Format("{0,12}", $"{DateTime.Now:yyMMddHHmmss}"),

                };

            byte[] data = Encoding.ASCII.GetBytes(String.Join("", header.ToArray()));
            return converDataToBytes(data);
        }
        private static byte[] generateDataPFCC()
        {

            List<string> header = new List<string>()
                {
                    String.Format("{0,4}", "PFCC"),
                    String.Format("{0,7}", "1111111"),
                    String.Format("{0,3}", "001"),
                    String.Format("{0,4}", 30),
                    String.Format("{0,5}", "F0014"),
                    String.Format("{0,5}", "F0000"),
                };
            byte[] data = Encoding.ASCII.GetBytes(String.Join("", header.ToArray()));
            return converDataToBytes(data);
        }
        private static byte[] generateDataPUPG()
        {
            List<string> body = new()
            {
                String.Format("{0,1}", "1"),
                String.Format("{0,40}", "https://localhost"),
                String.Format("{0,5}", "8080"),
                String.Format("{0,50}", "/product/version"),
                String.Format("{0,10}", "1111111"),
                String.Format("{0,10}", "1111111"),
                String.Format("{0,15}", "192.168.50.13"),
            };

            byte[] bodyByte = Encoding.ASCII.GetBytes(String.Join("", body.ToArray()));
            string HexBody = BitConverter.ToString(bodyByte).Replace("-","");
            byte[] cypherBody = Helpers.SEED(HexBody, 1);

            List<string> header = new List<string>()
                {
                    String.Format("{0,4}", "PUPG"),
                    String.Format("{0,7}", "1111111"),
                    String.Format("{0,3}", "001"),
                    String.Format("{0,4}", 20 + cypherBody.Count()),
                    
                };

            byte[] headerByte = Encoding.ASCII.GetBytes(String.Join("", header.ToArray()));

            


            return converDataToBytes(Helpers.Combine(headerByte, cypherBody));
        }
        private static byte[] generateDataPVER()
        {

            List<string> header = new List<string>()
                {
                    String.Format("{0,4}", "PVER"),
                    String.Format("{0,7}", "1111111"),
                    String.Format("{0,3}", "001"),
                    String.Format("{0,4}", 20),
                };

            byte[] data = Encoding.ASCII.GetBytes(String.Join("", header.ToArray()));
            return converDataToBytes(data);
        }
        private static byte[] generateDataPFST()
        {
            List<string> header = new List<string>()
                {
                    String.Format("{0,4}", "PFST"),
                    String.Format("{0,7}", "1111111"),
                    String.Format("{0,3}", "001"),
                    String.Format("{0,4}", 24),
                    String.Format("{0,4}", "0930"),

                };

            byte[] data = Encoding.ASCII.GetBytes(String.Join("", header.ToArray()));
            return converDataToBytes(data);
        }
        private static byte[] generateDataPFRS()
        {
            List<string> header = new List<string>()
            {
                String.Format("{0,4}", "PFRS"),
                String.Format("{0,7}", "1111111"),
                String.Format("{0,3}", "001"),
                String.Format("{0,4}", 32),
                String.Format("{0,2}", "1"),
                String.Format("{0,5}", "E0100"),
                String.Format("{0,5}", "F0000"),
            };

            byte[] data = Encoding.ASCII.GetBytes(String.Join("", header.ToArray()));
            return converDataToBytes(data);
        }
        private static byte[] generateDataPRSI()
        {
            List<string> header = new List<string>()
                {
                    String.Format("{0,4}", "PRSI"),
                    String.Format("{0,7}", "1111111"),
                    String.Format("{0,3}", "001"),
                    String.Format("{0,4}", 35),
                    String.Format("{0,15}", "192.168.50.13"),
                };

            byte[] data = Encoding.ASCII.GetBytes(String.Join("", header.ToArray()));
            return converDataToBytes(data);
        }
        private static byte[] generateDataPCNG()
        {
            List<string> header = new List<string>()
                {
                    String.Format("{0,4}", "PCNG"),
                    String.Format("{0,7}", "1111111"),
                    String.Format("{0,3}", "001"),
                    String.Format("{0,4}", 20),

                };

            byte[] data = Encoding.ASCII.GetBytes(String.Join("", header.ToArray()));
            return converDataToBytes(data);
        }
        private static byte[] generateDataPFCR()
        {
            List<string> header = new List<string>()
                {
                    String.Format("{0,4}", "PFCR"),
                    String.Format("{0,7}", "1111111"),
                    String.Format("{0,3}", "001"),
                    String.Format("{0,4}", 20),

                };

            byte[] data = Encoding.ASCII.GetBytes(String.Join("", header.ToArray()));
            return converDataToBytes(data);
        }
        private static byte[] generateDataPTIM()
        {

            List<string> header = new List<string>()
                {
                    String.Format("{0,4}", "PTIM"),
                    String.Format("{0,7}", "1111111"),
                    String.Format("{0,3}", "001"),
                    String.Format("{0,4}", 32),
                    String.Format("{0,12}", $"{DateTime.Now:yyMMddHHmmss}"),

                };

            byte[] data = Encoding.ASCII.GetBytes(String.Join("", header.ToArray()));
            return converDataToBytes(data);
        }

    }

    public class Header
    {
        public string Command { get; set; }
        public string CompanyCode { get; set; }
        public string ChimneyCode { get; set; }
        public int TotalLength { get; set; }

    }
}
