/**
* @ClassName：TrainModel
* @Author: Joey (zy19970@hotmail.com)
* @Date: 2024.04.03
* @Para: BeiDong_Train_Axis、SetHDTDriver()、Set_BeiDong_Parameter()
* @Rely: 私有类CANalystHelper、HDTDriver
* @Description: 实现三个方向的被动康复训练。
*/

using CANNalyst;
using ankle1._0;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Button;



namespace Matlab_connect
{
    /// <summary>
    /// 定义训练方向
    /// </summary>
    public static class Train_Axis
    {
        public static int BeishenZhiqu = 0x01;
        public static int NeishouWaizhan = 0x02;
        public static int NeifanWaifan = 0x03;
    }
    internal class TrainModel
    {

        public double RealDegree;

        public double M2Admittance = 0.1;
        public double B2Admittance = 0.5;
        public double K2Admittance = 0.8;

        public double HighLimit = 30;
        public double LowLimit = 30;

        public UInt32 BeidongIndex = 0;
        public double BeidongCycleTime = 10;//s
        public int BeidongCycleMs = 25;//ms

        public bool IsTrainCycle = false; //训练状态标志位

        public double[] Poistion;//位置队列
        public double[] Response;//响应队列


        public static CANalystHelper Cnh = new CANalystHelper();
        public HDTDriver HDTX;
        public HDTDriver HDTY;
        public HDTDriver HDTZ;

        Panel UIpanel;
        Button StratBtn;
        Button StopBtn;

        int CurrentTrainingDirection = 0x01;//被动训练运动方向

        admittance XAxisIsotonic = new admittance();     //创建三个轴的导纳控制类(x轴)
        admittance YAxisIsotonic = new admittance();
        admittance ZAxisIsotonic = new admittance();
        #region 配置传感器
        public void SetTrainParameter()
        {

        }
        /// <summary>
        /// 配置机器人参与训练的驱动器
        /// </summary>
        /// <param name="X"></param>
        /// <param name="Y"></param>
        /// <param name="Z"></param>
        public void SetHDTDriver(HDTDriver X, HDTDriver Y, HDTDriver Z)
        {
            HDTX = X;
            HDTY = Y;
            HDTZ = Z;
        }
        /// <summary>
        /// 发送插补位置
        /// </summary>
        /// <param name="pluse"></param>
        private void SendPos(int[] pluse)
        {
            HDTX.SetIPosition(pluse[0]);
            HDTY.SetIPosition(pluse[1]);
            HDTZ.SetIPosition(pluse[2]);
        }
        /// <summary>
        /// 发送位置模式
        /// </summary>
        /// <param name="pluse"></param>
        private void SendPos1(int[] pluse)
        {
            HDTX.SetIPosition(pluse[0]);
            HDTY.SetIPosition(pluse[1]);
            HDTZ.SetIPosition(pluse[2]);
        }


        /// <summary>
        /// 配置训练交互UI
        /// </summary>
        /// <param name="p">模式变更控件</param>
        /// <param name="on">开始训练按钮</param>
        /// <param name="off">结束训练按钮</param>
        public void Set_Train_UI_Parameter(Panel p, Button on, Button off)
        {
            UIpanel = p; StratBtn = on; StopBtn = off;
        }
        #endregion


        #region 经典主动等速训练
        private float Isotonic_T = 3.0f;//力矩阈值
        private float ZhuDong_increment = 0.0f;//位置增量
        private float ZhuDong_Degree = 0.0f;//位置增量
        private float ZhuDong_Degree_X = 0.0f;//位置增量
        private float ZhuDong_Degree_Y = 0.0f;//位置增量
        private float ZhuDong_Degree_Z = 0.0f;//位置增量
        private float Isotonic_V = 10.0f;//运动速度
        float Tanh(float T)
        {
            return (float)((float)Sgn(T) * (Arctan(Sgn(T) * 6 * T / Isotonic_T - 4) + 1) / 2);
        }
        float Sgn(float x)
        {
            if (x > 0)
            {
                return 1.0f;
            }
            else if (x < 0)
            {
                return -1.0f;
            }
            else
            {
                return 0.0f;
            }
        }
        float Arctan(float T)
        {
            float a = (float)Math.Exp(T);
            float b = (float)Math.Exp(-T);
            return (a - b) / (a + b);
        }

        /// <summary>
        /// 配置主动等速参数
        /// </summary>
        /// <param name="axis">训练方向，使用BeiDong_Train_Axis类</param>
        /// <param name="hLimit">正限位</param>
        /// <param name="lLimit">负限位(正数)</param>
        /// <param name="speed">运动速度</param>
        /// <param name="Thd">力矩阈值</param>
        public void Set_ZhuDong_Parameter(int axis, double hLimit, double lLimit, int speed = 15, int Thd = 3)
        {
            HighLimit = hLimit;
            LowLimit = lLimit;
            CurrentTrainingDirection = axis;
            Isotonic_V = speed;
            Isotonic_T = Thd;
        }
        /// <summary>
        /// 开始主动训练
        /// </summary>
        public void ZhuDongTrain_Start()
        {
            //StopBtn.Enabled = true;
            //StratBtn.Enabled = false;
            //UIpanel.Enabled = false;

            HDTX.IPInit();
            HDTY.IPInit();
            HDTZ.IPInit();

            IsTrainCycle = true;
            Thread.Sleep(20);

            Thread mythread = new Thread(ZhuDongTrain_SingleAxis_ThreadEntry);
            mythread.Start();


        }
        /// <summary>
        /// 设置导纳参数
        /// </summary>
        /// <param name="M"></param>
        /// <param name="B"></param>
        /// <param name="K"></param>
        public void setMBK(float M, float B, float K)
        {
            XAxisIsotonic.M = M;
            YAxisIsotonic.M = M;
            ZAxisIsotonic.M = M;

            XAxisIsotonic.D = B;
            YAxisIsotonic.D = B;
            ZAxisIsotonic.D = B;

            XAxisIsotonic.K = K;
            YAxisIsotonic.K = K;
            ZAxisIsotonic.K = K;

        }

        #region 传入力矩线程事件
        public void incomingTorque()
        {
            #region 导纳力矩传入  
            while (true)//判断是否传入力矩
            {
                if(KMPTunnel.isSingle)//如果是单轴主动模式
                {
                    //选择背伸\跖屈
                    if (CurrentTrainingDirection == Train_Axis.BeishenZhiqu)
                    {
                        XAxisIsotonic.nowTorque = KMPTunnel.FTSensor.torqueX;
                        //MessageBox.Show(XAxisIsotonic.nowTorque.ToString());
                        YAxisIsotonic.nowTorque = 0;
                        ZAxisIsotonic.nowTorque = 0;
                    }
                    //选择外翻、内翻
                    else if (CurrentTrainingDirection == Train_Axis.NeishouWaizhan)
                    {
                        XAxisIsotonic.nowTorque = 0;
                        YAxisIsotonic.nowTorque = 0;
                        ZAxisIsotonic.nowTorque = KMPTunnel.FTSensor.torqueZ;
                        //MessageBox.Show(YAxisIsotonic.nowTorque.ToString());
                    }
                    //选择外展、内收
                    else if (CurrentTrainingDirection == Train_Axis.NeifanWaifan)
                    {
                        XAxisIsotonic.nowTorque = 0;
                        YAxisIsotonic.nowTorque = KMPTunnel.FTSensor.torqueY;
                        ZAxisIsotonic.nowTorque = 0;
                        //MessageBox.Show(ZAxisIsotonic.nowTorque.ToString());
                    }
                    //Console.WriteLine(XAxisIsotonic.nowTorque);
                }
                else
                {
                                    
                    XAxisIsotonic.nowTorque = KMPTunnel.FTSensor.torqueX;
                    YAxisIsotonic.nowTorque = KMPTunnel.FTSensor.torqueY;
                    ZAxisIsotonic.nowTorque = KMPTunnel.FTSensor.torqueZ;
                }
                
            }
            #endregion
        }
        #endregion

        // <summary>
        // 主动训练线程入口
        // </summary>
        private void ZhuDongTrain_SingleAxis_ThreadEntry()
        {

            while (IsTrainCycle)
            {
                float Tq = 0.0f;
                if (CurrentTrainingDirection == Train_Axis.BeishenZhiqu)
                {
                    Tq = KMPTunnel.FTSensor.torqueX;

                }
                else if (CurrentTrainingDirection == Train_Axis.NeishouWaizhan)
                {
                    Tq = KMPTunnel.FTSensor.torqueZ;
                }
                else if (CurrentTrainingDirection == Train_Axis.NeifanWaifan)
                {
                    Tq = KMPTunnel.FTSensor.torqueY;
                }
                ZhuDong_increment = 0;
                if (Math.Abs(Tq) <= Isotonic_T)
                {
                    ZhuDong_increment = Tanh(Tq) * Isotonic_V * 25 / 1000;
                }
                else
                {
                    ZhuDong_increment = Sgn(Tq) * Isotonic_V * 25 / 1000;
                }
                ZhuDong_Degree = ZhuDong_Degree + ZhuDong_increment;
                if (ZhuDong_Degree>= HighLimit)
                {
                    ZhuDong_Degree = (float)HighLimit;
                }
                else if (ZhuDong_Degree <= -LowLimit)
                {
                    ZhuDong_Degree = -(float)LowLimit;
                }

                if (CurrentTrainingDirection == Train_Axis.BeishenZhiqu)
                {
                    KMPTunnel.IdealDegreeSensor.degreeX = (float)ZhuDong_Degree; 
                    KMPTunnel.IdealDegreeSensor.degreeY = 0; 
                    KMPTunnel.IdealDegreeSensor.degreeZ = 0; 
                }
                else if (CurrentTrainingDirection == Train_Axis.NeishouWaizhan) 
                {
                    KMPTunnel.IdealDegreeSensor.degreeX = 0; 
                    KMPTunnel.IdealDegreeSensor.degreeY = 0; 
                    KMPTunnel.IdealDegreeSensor.degreeZ = (float)ZhuDong_Degree; 
                }
                else if (CurrentTrainingDirection == Train_Axis.NeifanWaifan) 
                {
                    KMPTunnel.IdealDegreeSensor.degreeX = 0; 
                    KMPTunnel.IdealDegreeSensor.degreeY = (float)ZhuDong_Degree; 
                    KMPTunnel.IdealDegreeSensor.degreeZ = 0; 
                }
                SendPos(KinematicsHelper.InverseSolution(
                    KMPTunnel.IdealDegreeSensor.degreeX, 
                    KMPTunnel.IdealDegreeSensor.degreeY,
                    KMPTunnel.IdealDegreeSensor.degreeZ));

                
                Thread.Sleep(24);
            }
            if (!IsTrainCycle)
            {
                Thread.Sleep(100);
                Thread.CurrentThread.Interrupt();
                return;
            }
        }




        private void ZhuDongTrain_MultiAxis_ThreadEntry()
        {

            while (IsTrainCycle)
            {
                //固定移动距离
                //(XAxisIsotonic.Xo/System.Math.Abs(XAxisIsotonic.Xo))用于判断方向
                ZhuDong_Degree_X = ZhuDong_Degree_X + (XAxisIsotonic.Xo / System.Math.Abs(XAxisIsotonic.Xo)) * XAxisIsotonic.Deviation;
                if (ZhuDong_Degree_X >= HighLimit)
                {
                    ZhuDong_Degree_X = (float)HighLimit;
                }
                else if (ZhuDong_Degree_X <= -LowLimit)
                {
                    ZhuDong_Degree_X = -(float)LowLimit;
                }
                ZhuDong_Degree_Z = ZhuDong_Degree_Z + (ZAxisIsotonic.Xo / System.Math.Abs(ZAxisIsotonic.Xo)) * ZAxisIsotonic.Deviation;
                if (ZhuDong_Degree_Z >= HighLimit)
                {
                    ZhuDong_Degree_Z = (float)HighLimit;
                }
                else if (ZhuDong_Degree_Z <= -LowLimit)
                {
                    ZhuDong_Degree_Z = -(float)LowLimit;
                }
                ZhuDong_Degree_Y = ZhuDong_Degree_Y + (YAxisIsotonic.Xo / System.Math.Abs(YAxisIsotonic.Xo)) * YAxisIsotonic.Deviation;
                if (ZhuDong_Degree_Y >= HighLimit)
                {
                    ZhuDong_Degree_Y = (float)HighLimit;
                }
                else if (ZhuDong_Degree_Y <= -LowLimit)
                {
                    ZhuDong_Degree_Y = -(float)LowLimit;
                }


                KMPTunnel.IdealDegreeSensor.degreeX = (float)ZhuDong_Degree_X;
                KMPTunnel.IdealDegreeSensor.degreeY = (float)ZhuDong_Degree_Y;
                KMPTunnel.IdealDegreeSensor.degreeZ = (float)ZhuDong_Degree_Z;

                SendPos(KinematicsHelper.InverseSolution(KMPTunnel.IdealDegreeSensor.degreeX,
                    KMPTunnel.IdealDegreeSensor.degreeY,
                    KMPTunnel.IdealDegreeSensor.degreeZ));
                Thread.Sleep(24);
            }
            if (!IsTrainCycle)
            {
                Thread.Sleep(100);
                Thread.CurrentThread.Interrupt();
                return;
            }
        }
        /// <summary>
        /// 停止主动训练
        /// </summary>
        public void ZhuDongTrain_Stop()
        {
            IsTrainCycle = false;
            HDTX.IPStop();
            HDTY.IPStop();
            HDTZ.IPStop();

            StopBtn.Enabled = false;
            StratBtn.Enabled = true;
            UIpanel.Enabled = true;
        }
        #endregion


    }



}
