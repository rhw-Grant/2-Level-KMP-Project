using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Matlab_connect
{
    class SerialportReceivingDataForce
    {
        private SerialPort Ser;//串口  
        bool run_flag = false;
        Thread RxTx_thread;

        #region 六维力传感器修正量
        float xOffset = -85.534f;
        float yOffset = -209.846f;
        float zOffset = -68.1319f;
        float xForceOffset = -85.534f;
        float yForceOffset = -209.846f;
        float zForceOffset = -68.1319f;
        float xTorqueOffset = 0.8664f;
        float yTorqueOffset = -0.2763f;
        float zTorqueOffset = 0.1150f;
        #endregion

        /// <summary>
        /// 构造函数 初始化串口 加载串口接收(相当于串口事件)
        /// </summary>
        /// <param name="Serial">串口</param>
        public SerialportReceivingDataForce(SerialPort Serial)
        {
            Ser = Serial;
        }

        public void StartTxRxThread()
        {
            run_flag = true;
            RxTx_thread = new Thread(run);
            RxTx_thread.IsBackground = true;
            RxTx_thread.Start();
        }


        static byte index;
        static bool isfirst = false, issecond = false, isthird = false, isfourth = false;
        public float data_floatX;
        public float data_floatY;
        public float data_floatZ;
        public float data_floatXM;
        public float data_floatYM;
        public float data_floatZM;
        public static double floatXM;
        public static double floatYM;
        public static double floatZM;
        public void run()
        {
            //SerialPort DataReceived = (SerialPort)sender;
            if (Ser.IsOpen && Ser.BytesToRead <= 0)
            {
                return;
            }
            if (!Ser.IsOpen)
            {
                return;
            }
            while (Ser.IsOpen && Ser.BytesToRead != 0)
            {
                byte[] Data1 = new byte[4];
                byte[] Data2 = new byte[4];
                byte[] Data3 = new byte[4];
                byte[] Data4 = new byte[4];
                byte[] Data5 = new byte[4];
                byte[] Data6 = new byte[4];

                byte[] longbuf = new byte[Ser.BytesToRead];    //定义缓冲数组
                byte length = (byte)longbuf.Length;
                byte[] buf = new byte[24];
                Ser.Read(longbuf, 0, longbuf.Length);
                if (length > 124) return;
                if (true)
                {
                    for (byte i = 0; i < length; i++)  //循环处理一条长报文所包含的字节
                    {
                        if (isfourth)
                        {
                            isthird = false;
                            buf[index] = longbuf[i + 2];
                            index++;
                            if (index >= 24)
                            {
                                isfourth = false;
                                index = 0;

                                Data1[0] = buf[0];
                                Data1[1] = buf[1];
                                Data1[2] = buf[2];
                                Data1[3] = buf[3];
                                data_floatX = BitConverter.ToSingle(Data1, 0);

                                Data2[0] = buf[4];
                                Data2[1] = buf[5];
                                Data2[2] = buf[6];
                                Data2[3] = buf[7];
                                data_floatY = BitConverter.ToSingle(Data2, 0);

                                Data3[0] = buf[8];
                                Data3[1] = buf[9];
                                Data3[2] = buf[10];
                                Data3[3] = buf[11];
                                data_floatZ = BitConverter.ToSingle(Data3, 0);

                                Data4[0] = buf[12];
                                Data4[1] = buf[13];
                                Data4[2] = buf[14];
                                Data4[3] = buf[15];
                                data_floatXM = BitConverter.ToSingle(Data4, 0) + xTorqueOffset;
                                floatXM = data_floatXM;
                                //XAxisIsotonic.nowTorque = data_floatXM;
                                //Class1.public_torque = XAxisIsotonic.nowTorque;


                                Data5[0] = buf[16];
                                Data5[1] = buf[17];
                                Data5[2] = buf[18];
                                Data5[3] = buf[19];
                                data_floatYM = BitConverter.ToSingle(Data5, 0) + yTorqueOffset;
                                floatYM = data_floatYM;

                                Data6[0] = buf[20];
                                Data6[1] = buf[21];
                                Data6[2] = buf[22];
                                Data6[3] = buf[23];
                                data_floatZM = BitConverter.ToSingle(Data6, 0) + zTorqueOffset;
                                floatZM = data_floatZM;
                                //Console.WriteLine("buf=0x" + ByteArrayToHexString(buf));
                                //this.BeginInvoke(new DelChangText(intoText), data_floatXM);

                            }
                        }
                        if (isthird)
                        {
                            if (longbuf[i] == 0x1B)
                            {
                                isfourth = true; isfirst = false; issecond = false;
                            }
                            else
                            {
                                isfirst = false; issecond = false; isthird = false;
                            }
                        }
                        if (issecond)
                        {
                            if (longbuf[i] == 0x00)
                            {
                                isthird = true; isfirst = false;
                            }
                            else
                            {
                                isfirst = false; issecond = false;
                            }
                        }
                        if (isfirst)
                        {
                            if (longbuf[i] == 0x55)
                            {
                                issecond = true;
                            }
                            else
                            {
                                isfirst = false;
                            }
                        }
                        if (longbuf[i] == 0xAA && (i + 31 <= length))
                        {
                            isfirst = true;
                        }
                    }
                }
            }
        }
    }
}
