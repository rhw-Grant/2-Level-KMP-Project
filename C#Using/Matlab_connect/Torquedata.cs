using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Matlab_connect
{

    /// <summary>
    /// 采集的力矩信息解析过程
    /// </summary>
    class Torquedata
    {
        private Thread Torque_read;                         //力矩解析线程

        private SerialPort Ser;                             //串口  

        float xTorqueOffset = -0.055f;                      //力矩修正量
        float yTorqueOffset = -0.088f;
        float zTorqueOffset = 0.027f;
        public Queue ser_receive_torque = new Queue();  //串口接收队列即数据
        /// <summary>
        /// 构造函数 初始化串口 加载串口接收(相当于串口事件)
        /// </summary>
        /// <param name="Serial">串口</param>
        public Torquedata(SerialPort Serial)
        {
            Ser = Serial;
        }
        public Mcu_Data GetBoardInfo()
        {
            Mcu_Data Info1 = (Mcu_Data)ser_receive_torque.Dequeue();//出队
            return Info1;
        }
        public void Startread()
        {
            Torque_read = new Thread(torque_read);
            Torque_read.IsBackground = true;
            Torque_read.Start();
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
        public void torque_read()
        {
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
                //byte[] Data1 = new byte[4];
                //byte[] Data2 = new byte[4];
                //byte[] Data3 = new byte[4];
                //byte[] Data4 = new byte[4];
                //byte[] Data5 = new byte[4];
                //byte[] Data6 = new byte[4];

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
                                Mcu_Data mcus = new Mcu_Data();//实例化下位机信息对象                           
                                isfourth = false;
                                index = 0;
                                #region 无用
                                /* Data1[0] = buf[0];
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
                                 floatZM = data_floatZM;*/
                                #endregion

                                Mcu_Data mcus1 = new Mcu_Data();//实例化下位机信息
                                mcus1.Get_mcuData(buf);//获取下位机信息
                                ser_receive_torque.Enqueue(mcus1);
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



    /// <summary>
    /// 此类用于解析下位机的所有数据
    /// </summary>
    public class Mcu_Data
    {
        public Torque[] motor = new Torque[6];//三个编码器位置
        /// <summary>
        /// 从下位机传输的数组获取信息
        /// </summary>
        /// <param name="rx_buffer"></param>
        public void Get_mcuData(byte[] rx_buffer)
        {

            Torque motor0 = new Torque();
            motor0.Get_MotorInfo(rx_buffer, 0);//0是指baseoffset
            motor[0] = motor0;

            Torque motor1 = new Torque();
            motor1.Get_MotorInfo(rx_buffer, 4);
            motor[1] = motor1;

            Torque motor2 = new Torque();
            motor2.Get_MotorInfo(rx_buffer, 8);
            motor[2] = motor2;

            Torque motor3 = new Torque();
            motor3.Get_MotorInfo(rx_buffer, 12);
            motor[3] = motor3;

            Torque motor4 = new Torque();
            motor4.Get_MotorInfo(rx_buffer, 16);
            motor[4] = motor4;

            Torque motor5 = new Torque();
            motor5.Get_MotorInfo(rx_buffer, 20);
            motor[5] = motor5;
        }
    }
    public class Torque
    {

        public double Act_torque = 0;//实际位置
        /// <summary>
        /// 根据接收的数据获取电机的信息
        /// </summary>
        /// <param name="rx_buffer"></param>
        public void Get_MotorInfo(byte[] rx_buffer, byte baseoffset)
        {
            Act_torque = BitConverter.ToSingle(rx_buffer, baseoffset);
        }
    }
}