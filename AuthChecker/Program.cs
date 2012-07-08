/*  ----------------------------------------------------------------------------
 *  CODEMPLOSION.com
 *  ----------------------------------------------------------------------------
 *  File:       Program.cs
 *  Author:     starz and team
 *  License:    Creative Commons Attribution-NonCommercial-ShareAlike (http://creativecommons.org/licenses/by-nc-sa/3.0/)
 *  ----------------------------------------------------------------------------
 */

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
            Console.WriteLine("Honorbuddy auth server state checker");
            Console.WriteLine("by starz");
            Console.WriteLine("");
            //Internet Check
            if (HasConnection())
            {
                Console.WriteLine("Internet: Internet is available");
            }
            else
            {
                Console.WriteLine("Internet: Internet isn't available");
                Console.ReadKey();
                return;
            }
            //Ping Check
            var ping = new Ping();
            PingReply pingreply = ping.Send("94.174.242.69");
            if (pingreply != null && pingreply.Status.ToString() != "Success")
            {
                Console.WriteLine(pingreply.Status);
                Console.WriteLine("Ping: Can't ping server");
                Console.WriteLine("Ping: Server may be offline");
            }
            else
            {
                if (pingreply != null)
                {
                    Console.WriteLine("Ping: " + pingreply.Status);
                    Console.WriteLine("Ping: Pings look good");
                }
                else
                {
                    Console.WriteLine("Ping: Null error");
                }
            }

            //Socket connection test
            //Usually we can detect network troubles with that
            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                socket.ReceiveTimeout = 5000;
                try
                {
                    socket.Connect("94.174.242.69", 5125);
                    socket.Close();
                    Console.WriteLine("Socket: Socket connection good");
                }
                catch (SocketException ex)
                {
                    Console.WriteLine("Socket: " + ex.SocketErrorCode);
                    Console.WriteLine("Socket: Socket connection failed");
                    socket.Close();
                }
            }
            Console.ReadKey();
        }

        #region

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

        #endregion

        #region Nested type: ConnectionStatusEnum

        [Flags]
        private enum ConnectionStatusEnum
        {
            InternetConnectionOffline = 0x20,
        }

        #endregion
    }
}