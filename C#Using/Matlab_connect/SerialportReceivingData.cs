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
    #region 通信类 Communication
    class SerialportReceivingData
    {
        /// <summary>
        /// 此类用于实现上下位机之间的通讯
        /// </summary>
        private SerialPort Ser;//串口  
        public byte[] RX_buffer = new byte[100];//定义一个字节的接收缓存数组     
        private bool run_flag = false;//运行标志
        private Thread RxTx_thread;//收发线程 
        public Queue ser_receive_queue_ = new Queue();  //串口接收队列即数据

        /// <summary>
        /// 构造函数 初始化串口 加载串口接收(相当于串口事件)
        /// </summary>
        /// <param name="Serial">串口</param>
        public SerialportReceivingData(SerialPort Serial)
        {
            Ser = Serial;
        }

        /// <summary>
        /// 获取队列中的一个数据 队列的元素是一个Mcu_data类型
        /// </summary>
        /// <returns></returns>
        public Mcu_data GetBoardInfo()
        {
            Mcu_data Info = (Mcu_data)ser_receive_queue_.Dequeue();//出队
            return Info;
        }

        /// <summary>
        /// 启动收发线程
        /// </summary>
        public void StartTxRxThread()
        {
            run_flag = true;
            RxTx_thread = new Thread(run);
            RxTx_thread.IsBackground = true;
            RxTx_thread.Start();
        }

        /// <summary>
        /// 停止收发线程
        /// </summary>
        public void StopTxRxThread()
        {
            run_flag = false;
        }

        /// <summary>
        /// 收发线程需要运行的函数  接收数据&发送指令
        /// </summary>
        public void run()
        {
            int state = 0;//读取的状态标识
            int rx_num = 0;
            for (; run_flag == true;)
            {
                //接收数据..
                if (Ser.IsOpen && Ser.BytesToRead == 0)

                    continue;
                byte Rx_data;//接收缓存            
                
                
                Rx_data = Convert.ToByte(Ser.ReadByte());
                switch (state)
                {
                    case 0:
                        {
                            if (Rx_data == 0x12)//判断是不是有效数据的第一个数
                            {
                                state = 1;
                            }
                            break;
                        }
                    case 1:
                        {
                            if (Rx_data == 0xf1)//判断是不是第二个数
                            {
                                state = 2;
                                rx_num = 0;
                            }
                            else
                            {
                                state = 0;
                            }
                            break;

                        }
                    case 2:
                        {
                            RX_buffer[rx_num] = Rx_data;
                            rx_num++;
                            if (rx_num == 6)
                            {
                                Mcu_data mcus = new Mcu_data();//实例化下位机信息对象
                                mcus.Get_McuData(RX_buffer);//获取下位机信息
                                ser_receive_queue_.Enqueue(mcus);//将信息入队
                                state = 0;
                            }
                            break;
                        }
                }
            }
        }
    }
    #endregion



    #region 电机信息类
    /// <summary>
    /// 电机类
    /// </summary>
    public class Motor
    {

        public double Act_position = 0;//实际位置
        /// <summary>
        /// 根据接收的数据获取电机的信息
        /// </summary>
        /// <param name="rx_buffer"></param>
        public void Get_MotorInfo(byte[] rx_buffer, byte baseoffset)
        {
            //Act_position =(double) ((rx_buffer[0 + 2 * baseoffset] << 8) | (rx_buffer[1 + 2 * baseoffset] ));
            Act_position = (((Int32)rx_buffer[1 + 2 * baseoffset] << 8) | ((Int32)rx_buffer[0 + 2 * baseoffset]));
        }
    }
    #endregion

    #region 下位机信息类 Mcu_data
    /// <summary>
    /// 此类用于解析下位机的所有数据
    /// </summary>
    public class Mcu_data
    {
        public Motor[] motor = new Motor[3];//三个编码器位置
        /// <summary>
        /// 从下位机传输的数组获取信息
        /// </summary>
        /// <param name="rx_buffer"></param>
        public void Get_McuData(byte[] rx_buffer)
        {
            //电机实际速度和位置
            //for(byte i=0;i<6;i++)
            //motor[i].Get_MotorInfo(rx_buffer, i);
            Motor motor0 = new Motor();
            motor0.Get_MotorInfo(rx_buffer, 0);//0是指baseoffset
            motor[0] = motor0;

            Motor motor1 = new Motor();
            motor1.Get_MotorInfo(rx_buffer, 1);
            motor[1] = motor1;

            Motor motor2 = new Motor();
            motor2.Get_MotorInfo(rx_buffer, 2);
            motor[2] = motor2;
        }
    }
    #endregion
}



