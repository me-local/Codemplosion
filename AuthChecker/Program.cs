using System;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;

//This is just all-in-one version of auth checkers available

namespace CODEMPLOSION
{
    internal class Program
    {
        private static void Main()
        {
            //Internet Check
            if (HasConnection())
            {
                Console.WriteLine("Internet is available");
            }
            else
            {
                Console.WriteLine("Internet isn't available");
                Console.ReadKey();
                return;
            }
            //Ping Check
            var ping = new Ping();
            PingReply pingreply = ping.Send("94.174.242.69");
            if (pingreply != null && pingreply.Status.ToString() != "Success")
            {
                Console.WriteLine(pingreply.Status);
                Console.WriteLine("Can't ping server");
                Console.WriteLine("Server may be offline");
            }
            else
            {
                if (pingreply != null)
                {
                    Console.WriteLine(pingreply.Status);
                    Console.WriteLine("Pings look good");
                }
                else
                {
                    Console.WriteLine("Null error");
                }
            }

            //Socket connection test
            //Usually we can detect network troubles with that
            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                try
                {
                    socket.Connect("94.174.242.69", 5125);
                    socket.Close();
                    Console.WriteLine("Socket connection good");
                }
                catch (SocketException ex)
                {
                    if (ex.SocketErrorCode == SocketError.ConnectionRefused ||
                        ex.SocketErrorCode == SocketError.TimedOut)
                    {
                        Console.WriteLine(ex.SocketErrorCode);
                        Console.WriteLine("Socket connection failed");
                        socket.Close();
                    }
                }
            }
            Console.ReadKey();
        }


        [DllImport("wininet", CharSet = CharSet.Auto)]
        private static extern bool InternetGetConnectedState(ref ConnectionStatusEnum flags, int dw);

        private static bool HasConnection()
        {
            //instance of our ConnectionStatusEnum          
            ConnectionStatusEnum state = 0;

            //call the API                  
            InternetGetConnectedState(ref state, 0);

            //check the status              
            if (((int) ConnectionStatusEnum.InternetConnectionOffline & (int) state) != 0)
            {
                return false;
            }
            return true;
        }

        #region Nested type: ConnectionStatusEnum

        [Flags]
        private enum ConnectionStatusEnum
        {
            InternetConnectionOffline = 0x20,
        }

        #endregion
    }
}