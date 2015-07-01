using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VAdapter;
using System.Net;
using System.Net.Sockets;
using System.Security;
using System.Security.Cryptography;
using System.Net.Security;
using System.IO;
namespace NetRouter
{
    class Program
    {
        static void Main(string[] args)
        {


            List<IPEndPoint> clients = new List<IPEndPoint>();
            UdpClient mclient = new UdpClient(new IPEndPoint(IPAddress.Any,8099));
            Console.WriteLine("Enter the IP address to send packets to (or blank for server)");
            try
            {
                clients.Add(new IPEndPoint(IPAddress.Parse(Console.ReadLine()), 8099));

            }
            catch (Exception er)
            {

            }

            InterfaceManager manager = new InterfaceManager();
            var networkInterface = manager.GetDevices().First(); //Access virtual network interface
            
            //Plug the cable in to the Ethernet adapter!
            using (var devStr = manager.OpenDevice(networkInterface))
            {
                System.Threading.Thread mthread = new System.Threading.Thread(delegate() {
                    while (true)
                    {
                        IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);
                        byte[] packet = mclient.Receive(ref ep);
                        lock (clients)
                        {
                            if (!clients.Contains(ep))
                            {
                                clients.Add(ep);
                            }
                        }
                        devStr.Write(packet, 0, packet.Length);
                        devStr.Flush();
                    }
                });
                mthread.Start();
                //Read packets from virtual interface
                while (true)
                {
                    //Receive
                    byte[] recvBuffer = new byte[10000];
                    
                    int count = devStr.Read(recvBuffer, 0, recvBuffer.Length);
                    
                    lock (clients)
                    {
                        foreach (var et in clients)
                        {
                            mclient.Send(recvBuffer, count, et);
                        }
                    }
                }
            }
            
        }
    }
}
