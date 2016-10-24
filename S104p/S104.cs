/*
 * Auther:haha
 * CreateTime:2015/10/25
 * Describe:for 104
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net;
using System.Net.Sockets;
using libez;

namespace S104p
{
    class S104
    {
        System.Threading.Thread RCV_THREAD;     //receive thread
        private Socket SOCKET;                  //receive socket
        private int RCVD_NUM;                       //Acceptance number
        private int SENT_NUM;                       //Sent number
        private DateTime lastTime = DateTime.Now;

        public void Begin()
        {
        connect:
            SOCKET = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            SOCKET.Connect(IPAddress.Parse("127.0.0.1"), 2404);
            Console.WriteLine("S104 establish a link successfully, try to receive...");

            RCV_THREAD = new System.Threading.Thread(BeginReceive);
            RCV_THREAD.Start(SOCKET);

            while (true)
            {
                System.Threading.Thread.Sleep(4000);
                this.Send_UFram(Uflag.testfr_active);
                if (RCVD_NUM >= 20000)
                {
                    SOCKET.Shutdown(SocketShutdown.Receive);
                    RCV_THREAD.Abort();
                    System.Threading.Thread.Sleep(4000);
                    SOCKET.Shutdown(SocketShutdown.Send);
                    SOCKET.Dispose();

                    RCVD_NUM = 0;
                    goto connect;
                }
                if (DateTime.Now - lastTime > new TimeSpan(0, 0, 4))
                {
                    this.Send_SFram(RCVD_NUM);
                    Console.WriteLine("overtime send S fram...");
                }
            }
        }
        private void BeginReceive(Object obj)
        {
            Socket sock_ptr = (Socket)obj;     //receive socket in BeginReceive
            byte[] buffer = new byte[4069];   //Date cache

            byte[] start_active = iec104.create_Uframe(Uflag.startdt_active);   //start_active
            if (sock_ptr.Send(start_active) == start_active.Length)
            {
                Console.WriteLine("start_active");
            }

            while (true)
            {
                if (sock_ptr.Available > 0)
                {
                    int read = sock_ptr.Receive(buffer, 0, sock_ptr.Available, SocketFlags.None);
                    if (read > 0)
                    {
                        ParseFram(buffer, read);
                        lastTime = DateTime.Now;
                    }
                }
            }

        }
        private void ParseFram(byte[] _frm, int _size)
        {
            if (iec104.isUframe(_frm))
            {
                Uflag uflag = iec104.parse_Uframe(_frm);
                switch (uflag)
                {
                    case Uflag.startdt_active:
                        {
                            this.Send_UFram(Uflag.startdt_confirm);
                            break;
                        }
                    case Uflag.testfr_active:
                        {
                            this.Send_UFram(Uflag.testfr_confirm);
                            break;
                        }
                    case Uflag.stopdt_active:
                        {
                            this.Send_UFram(Uflag.stopdt_confirm);
                            break;
                        }
                    default:
                        {
                            Console.WriteLine("received U frame:{0}", uflag.ToString());
                            break;
                        }
                }
            }
            else if (iec104.parseIframe_id(_frm) != null)
            {
                int messageType = _frm[6];
                int verb = _frm[7];
                int reason = (_frm[9] << 8) | _frm[8];
                int machineAddress = (_frm[11] << 8) | _frm[10];
                Func<int, bool> isSuccessing = VERB => (VERB >> 7 == 1);

                #region 测量值，遥测，标度化值，浮点型
                if (messageType == 0x0d)
                {
                    int pointCount = verb & 0x7f;
                    if (isSuccessing(verb))
                    {
                        int pointStartAddress =
                            _frm[14] << 16 |
                            _frm[13] << 8 |
                            _frm[12];
                        for (int i = 0; i < pointCount; i++)
                        {
                            int pointAddress = pointStartAddress + i;
                            float value = BitConverter.ToSingle(_frm, 15 + i * 5);
                            int quantity = _frm[15 + i * 5 + 2];

                            Console.WriteLine("pointAddress:{0},value:{1},quantity;{2}", pointAddress, value, quantity);
                        }
                    }
                    else
                    {
                        for (int i = 0; i < pointCount; i++)
                        {
                            int pointAddress =
                                _frm[14 + i * 8] << 16 |
                                _frm[13 + i * 8] << 8 |
                                _frm[12 + i * 8];
                            float value = BitConverter.ToSingle(_frm, 12 + i * 8 + 3);
                            int quantity = _frm[12 + i * 8 + 7];

                            Console.WriteLine("pointAddress:{0},value:{1},quantity;{2}", pointAddress, value, quantity);
                        }
                    }
                }
                #endregion
                #region 测量值，遥测，归一化值，短整型
                else if (messageType == 0x09)
                {
                    int pointCount = verb & 0x7f;
                    if (isSuccessing(verb))
                    {
                        int pointStartAddress =
                            _frm[14] << 16 |
                            _frm[13] << 8 |
                            _frm[12];
                        for (int i = 0; i < pointCount; i++)
                        {
                            int pointAddress = pointStartAddress + i;
                            int value = BitConverter.ToInt16(_frm, 15 + i * 3);
                            int quantity = _frm[15 + i * 3 + 2];

                            Console.WriteLine("pointAddress:{0},value:{1},quantity;{2}", pointAddress, value, quantity);

                        }
                    }
                    else
                    {
                        for (int i = 0; i < pointCount; i++)
                        {
                            int pointAddress =
                                _frm[14 + i * 6] << 16 |
                                _frm[13 + i * 6] << 8 |
                                _frm[12 + i * 6];
                            int value = BitConverter.ToInt16(_frm, 12 + i * 6 + 3);
                            int quantity = _frm[12 + i * 6 + 5];

                            Console.WriteLine("pointAddress:{0},value:{1},quantity;{2}", pointAddress, value, quantity);
                        }
                    }
                }
                #endregion

                this.RCVD_NUM++;
                this.Send_SFram(this.RCVD_NUM);
                Console.WriteLine("{0}: Received Success! Count:{1}", DateTime.Now, RCVD_NUM);
            }
        }

        private void Send_UFram(Uflag _uflag)
        {
            byte[] frm = iec104.create_Uframe(_uflag);
            if (SOCKET.Send(frm) == frm.Length)
                Console.WriteLine("sent U fram...");
        }
        private void Send_SFram(int _rcv)
        {
            byte[] frm = iec104.create_Sframe(_rcv);
            if (SOCKET.Send(frm) == frm.Length)
                Console.WriteLine("sent S fram...");
        }
    }
}
