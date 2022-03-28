using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace Tracerout
{
    public class TracertClass
    {
        public static void Main() {
            //String s;
            Console.WriteLine("write it");
            Tracert(Console.ReadLine());
        }

        class ICMP
        {
            public byte Type;
            public byte Code;
            public UInt16 Checksum;
            public int dataSize;
            public byte[] data = new byte[1024];

            public ICMP()
            {
                this.Type = 0x08;
                this.Code = 0x00;
                this.Checksum = 0;
                Buffer.BlockCopy(BitConverter.GetBytes(0), 0, this.data, 0, 4);
                byte[] datagram = new byte[1024];
                datagram = Encoding.UTF8.GetBytes("packet");
                Buffer.BlockCopy(datagram, 0, this.data, 4, datagram.Length);
                this.dataSize = datagram.Length + 4;
                this.calcChecksum();
            }

            public ICMP(int size, byte[] datagram)
            {
                Type = datagram[20];
                Code = datagram[21];
                Checksum = BitConverter.ToUInt16(datagram, 22);
                dataSize = size - 24;
                Buffer.BlockCopy(datagram, 24, data, 0, dataSize);

            }

            public byte[] makeBytes()
            {
                byte[] datagram = new byte[dataSize + 9];////////////////////////////////////////
                Buffer.BlockCopy(BitConverter.GetBytes(Type), 0, datagram, 0, 1);
                Buffer.BlockCopy(BitConverter.GetBytes(Code), 0, datagram, 1, 1);
                Buffer.BlockCopy(BitConverter.GetBytes(Checksum), 0, datagram, 2, 2);
                Buffer.BlockCopy(data, 0, datagram, 4, dataSize);
                return datagram;
            }

            public void calcChecksum()
            {
                UInt32 sum = 0;
                byte[] data = makeBytes();
                int packetsize = dataSize + 8;
                int index = 0;

                while (index < packetsize)
                {
                    sum += Convert.ToUInt32(BitConverter.ToUInt16(data, index));
                    index += 2;
                }
                sum = (sum >> 16) + (sum & 0xffff);
                sum += (sum >> 16);
                this.Checksum = (UInt16)(~sum);
            }
        }

        static void Tracert(String remoteHost)
        {
            IPHostEntry hostIP;
            try
            {
                hostIP = Dns.GetHostEntry(remoteHost);
            }
            catch (Exception)
            {
                Console.WriteLine("Не удаётся разрешить системное имя узла " + remoteHost);
                Console.ReadLine();
                return;
            }

            Console.WriteLine("Трассировка маршрута к " + hostIP.HostName);
            IPAddress ipAddr = hostIP.AddressList[0];
            IPEndPoint iep = new IPEndPoint(ipAddr, 0);
            EndPoint ep = iep;

            Socket mainSocket = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.Icmp);

            ICMP icmpPacket = new ICMP();

            mainSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 6000);

            int ReqLimit = 0;
            const int reqsetLimit = 4;
            int recPacket = 0;
            byte[] datagram;
            int packetsize = icmpPacket.dataSize + 4;
            for (int i = 1; i < 31; i++)
            {
                mainSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.IpTimeToLive, i);

                DateTime startTime = DateTime.Now;
                mainSocket.SendTo(icmpPacket.makeBytes(), packetsize, SocketFlags.None, iep);
                ep = iep;

                try
                {
                    datagram = new byte[1024];
                    recPacket = mainSocket.ReceiveFrom(datagram, ref ep);
                    TimeSpan timeInterval = DateTime.Now - startTime;
                    ICMP response = new ICMP(recPacket, datagram);

                    if (response.Type == 11)
                    {
                        Console.WriteLine( i + "\t" + (timeInterval.Milliseconds.ToString()) + " ms " + "\t" + ep.ToString());
                    }

                    if (response.Type == 0)
                    {
                        Console.WriteLine(i + "\t" + (timeInterval.Milliseconds.ToString()) + " ms " + "\t" + ep.ToString()); //!!!!!!!!!!!!!!!!!!!!
                        Console.WriteLine( "Трассировка завершена.\n");
                        break;
                    }

                    ReqLimit = 0;
                }
                catch (SocketException)
                {
                    Console.WriteLine(i + ": Превышен интервал ожидания для запроса " + ep.ToString() + "\n");
                    ReqLimit++;

                    if (ReqLimit == reqsetLimit)
                    {
                        Console.WriteLine("Нет соединения\n");
                        break;
                    }
                }
            }
            mainSocket.Shutdown(SocketShutdown.Both);
            mainSocket.Close();
            Console.ReadLine();
        }
    }
}
