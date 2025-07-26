using CANNalyst;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Button;
using MathWorks.MATLAB.NET.Arrays;
using MathWorks.MATLAB.NET.Utility;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.IO;
using System.Net.Sockets;
using System.Net;
using RAS;
using System.Collections;
using System.Runtime.InteropServices.ComTypes;

namespace Matlab_connect
{
    public partial class KMPTunnel : Form
    {
        private Thread thStartServer;
        string str_ip = "127.0.0.1";
        private string str_port = "8080";
        bool isSendData = false;
        private TcpListener tlistener;
        private TcpClient remoteClient;
        private BinaryReader br;
        private BinaryWriter bw;
        public float xAngle = -84.077f;
        public float yAngle = -197.709f;
        public float zAngle = -82.659f;

        static CANalystHelper Cnh = new CANalystHelper();//声明关于Can的
        HDTDriver HDTX = new HDTDriver("00000601", Cnh);
        HDTDriver HDTY = new HDTDriver("00000602", Cnh);
        HDTDriver HDTZ = new HDTDriver("00000603", Cnh);

        EposDriver Epos1 = new EposDriver("00000601", Cnh);//声明关于驱动器的        
        ManualResetEvent fa = new ManualResetEvent(false);//手动被动模式归零过程线程开启终止控制（false为等待信号开启）
        Thread Monitoring_return_to_zero;                           //创建监测回零线程
        SerialportReceivingData serialportReceivingData;            //实例化串口接受数据线程
        Torquedata torquedata;
        Mcu_data mcu_Data = new Mcu_data();                         //解析编码器数据
        TrainModel trainModel = new TrainModel();
        public static Object M8128Lock = new Object();//采集卡线程间安全锁
        public static Object STMLock = new Object();//MCU线程安全锁

        static byte[] M8128RevData=new byte[29];
        static byte[] STMRevData = new byte[14];//定义MCU接收的一帧的字节
        public static bool  isSingle = false;
        Tool tool= new Tool();
        static bool TorqeIsUpdate = false;//力矩更新标识符
        static bool AngleIsUpdate = false;//角度更新标识符


        //角度编码器解析变量 
        public double Act_position_1;                               //用于回零电机位置判断（1号电机，x轴）
        public double Act_position_2;                               //用于回零电机位置判断（2号电机，y轴）
        public double Act_position_3;                               //用于回零电机位置判断（3号电机，z轴）

        #region 动态图表相关变量
        static int NumOfPoint = 100;//曲线绘制点个数

        private Queue<double> XDegreedata = new Queue<double>(NumOfPoint);
        private Queue<double> YDegreedata = new Queue<double>(NumOfPoint);
        private Queue<double> ZDegreedata = new Queue<double>(NumOfPoint);

        static private Queue<double> XFdata = new Queue<double>(NumOfPoint);
        static private Queue<double> YFdata = new Queue<double>(NumOfPoint);
        static private Queue<double> ZFdata = new Queue<double>(NumOfPoint);

        static private Queue<double> XTdata = new Queue<double>(NumOfPoint);
        static private Queue<double> YTdata = new Queue<double>(NumOfPoint);
        static private Queue<double> ZTdata = new Queue<double>(NumOfPoint);

        public static Object ChartLock = new Object();//采集卡线程间安全锁

        bool IsPainting = false;

        #region 定义机器人传感器信息
        public static M8128 FTSensor = new M8128();//定义采集卡的力和力矩的数据结构
        public static DeDetail DegreeSensor = new DeDetail();//定义编码器三个角度的数据结构
        public static DeDetail IdealDegreeSensor = new DeDetail();//定义下发到机器人的编码器角度
        #endregion

        #region 传感器标定相关参数
        static public float Offset_Fx = 55.701f;
        static public float Offset_Fy = -12.738f;
        static public float Offset_Fz = -52.249f;
        static public float Offset_Tx = -1.227f;
        static public float Offset_Ty = -0.677f;
        static public float Offset_Tz = 0.615f;


        static public float Offset_Dx = -3.954f;
        static public float Offset_Dy = 5.491f;
        static public float Offset_Dz = 1.456f;
        #endregion

        /********消息队列相关集合*************************/
        #region 消息队列相关集合
        static private Queue DegreeXQueue = new Queue();
        static private Queue DegreeYQueue = new Queue();
        static private Queue DegreeZQueue = new Queue();
        static private Queue TorqueXQueue = new Queue();
        static private Queue TorqueYQueue = new Queue();
        static private Queue TorqueZQueue = new Queue();
        #endregion
        /************************************************/


        #endregion


        //电机状态
        public static bool Motor_1 = false;                //一号电机状态（用于区分不同电机）
        public static bool Motor_2 = false;                //二号电机状态
        public static bool Motor_3 = false;                //三号电机状态

        #region 六维力传感器修正量
        float xOffset = -85.534f;
        float yOffset = -209.846f;
        float zOffset = -68.1319f;
        float xForceOffset = -85.534f;
        float yForceOffset = -209.846f;
        float zForceOffset = -68.1319f;
        float xTorqueOffset = -0.055f;
        float yTorqueOffset = -0.088f;
        float zTorqueOffset = 0.027f;
        #endregion

        /********配置实时日志相关类*************************/
        #region 配置实时日志相关类
        LogListUI LogUI = new LogListUI();//定义实时日志类
        #endregion
        /************************************************/

        #region 线程声明
        Thread Zeroing_thread;                                      //声明手动模式回零线程
        #endregion
        //手动模式归零标志
        bool Manual_mode_zero = false;
        public KMPTunnel()
        {
            InitializeComponent();
            TrainModel_DriverInit();
            LogInit();
            LEDInit();
            System.Windows.Forms.Control.CheckForIllegalCrossThreadCalls = false;
            LogUI.Log(Thread.CurrentThread.ManagedThreadId, "初始化", "结束..........");
        }

        void LEDInit()
        {
            LogUI.Log(Thread.CurrentThread.ManagedThreadId, "初始化", "LED控件", "初始化成功");
        }

        /// <summary>
        /// Log UI控件初始化
        /// </summary>
        void LogInit()
        {
            LogUI.SetListBox(LogListBox);
        }
        void TrainModel_DriverInit()
        {
            trainModel.SetHDTDriver(HDTX, HDTY, HDTZ);
        }


        #region 定义机器人硬件信息结构体
        /// <summary>
        /// 六维力矩信息
        /// </summary>
        public struct M8128
        {
            public float forceX;//实际力
            public float torqueX;//实际力矩

            public float forceY;//实际力
            public float torqueY;//实际力矩

            public float forceZ;//实际力
            public float torqueZ;//实际力矩
        }
        /// <summary>
        /// 末端位姿信息
        /// </summary>
        public struct DeDetail
        {
            public float degreeX;//X方向实际角度
            public float degreeY;//Y方向实际角度
            public float degreeZ;//Z方向实际角度
        }
        #endregion


        #region 串口事件力传感器

        private void serialPort1_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            TorqeIsUpdate = false;
            M8128RevData = new byte[29];
            SerialPort sp = (SerialPort)sender;
            if (sp.BytesToRead <= 0)
            {
                return;
            }
            _ = M8128RevData;
            bool FirstIsOk = false;
            bool HeadIsOK = false;

            int RevNum = 0;

            for (; RevNum < 29;)
            {
                byte RevData = 0x00;
                if (!sp.IsOpen) { return; }
                try
                {
                    RevData = Convert.ToByte(sp.ReadByte());
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
                if (HeadIsOK)
                {
                    M8128RevData[RevNum] = RevData;
                    RevNum++;
                }
                if (!FirstIsOk && RevData == 0xAA)
                {
                    FirstIsOk = true;
                }
                if (FirstIsOk && RevData == 0x55 && !HeadIsOK)
                {
                    HeadIsOK = true;
                }
            }
            if (M8128RevData == null)
            {
                //Console.WriteLine(ByteArrayToHexString(M8128RevData));
                return;
            }

            try
            {
                if (M8128RevData[0] == 0x00 && M8128RevData[1] == 0x1b) { } else { return; }

                //if (!StringHelper.IsSUM(M8128RevData))
                //{
                //    Console.WriteLine(DateTime.Now.ToLocalTime().ToString() + "\t校验失败。");
                //    return;
                //}

                if ((BitConverter.ToSingle(M8128RevData, 4) + Offset_Fx) > 5000.0f || (BitConverter.ToSingle(M8128RevData, 4) + Offset_Fx) < -5000)
                {
                    Console.WriteLine("Fx:{0}", BitConverter.ToSingle(M8128RevData, 4) + Offset_Fx);
                }
                else
                { FTSensor.forceX = BitConverter.ToSingle(M8128RevData, 4) + Offset_Fx; }
                if ((BitConverter.ToSingle(M8128RevData, 16) + Offset_Tx) > 5000.0f || (BitConverter.ToSingle(M8128RevData, 16) + Offset_Tx) < -5000)
                {
                    Console.WriteLine("Tx:{0}", BitConverter.ToSingle(M8128RevData, 16) + Offset_Tx);
                }
                else
                { FTSensor.torqueX = BitConverter.ToSingle(M8128RevData, 16) + Offset_Tx; }


                if ((BitConverter.ToSingle(M8128RevData, 8) + Offset_Fy) > 5000.0f || (BitConverter.ToSingle(M8128RevData, 8) + Offset_Fy) < -5000)
                {
                    Console.WriteLine("Fy:{0}", BitConverter.ToSingle(M8128RevData, 8) + Offset_Fy);
                }
                else
                { FTSensor.forceY = BitConverter.ToSingle(M8128RevData, 8) + Offset_Fy; }
                if ((BitConverter.ToSingle(M8128RevData, 20) + Offset_Ty) > 5000.0f || (BitConverter.ToSingle(M8128RevData, 20) + Offset_Ty) < -5000)
                {
                    Console.WriteLine("Ty:{0}", BitConverter.ToSingle(M8128RevData, 20) + Offset_Ty);
                }
                else
                { FTSensor.torqueY = BitConverter.ToSingle(M8128RevData, 20) + Offset_Ty-15; }


                if ((BitConverter.ToSingle(M8128RevData, 12) + Offset_Fz) > 5000.0f || (BitConverter.ToSingle(M8128RevData, 12) + Offset_Fz) < -5000)
                {
                    Console.WriteLine("Fz:{0}", BitConverter.ToSingle(M8128RevData, 12) + Offset_Fz);
                }
                else
                { FTSensor.forceZ = BitConverter.ToSingle(M8128RevData, 12) + Offset_Fz; }
                if ((BitConverter.ToSingle(M8128RevData, 24) + Offset_Tz) > 5000.0f || (BitConverter.ToSingle(M8128RevData, 24) + Offset_Tz) < -5000)
                {
                    Console.WriteLine("Tz:{0}", BitConverter.ToSingle(M8128RevData, 24) + Offset_Tz);
                }
                else
                { FTSensor.torqueZ = BitConverter.ToSingle(M8128RevData, 24) + Offset_Tz-220; }

                lock (M8128Lock)
                {
                    TorqueXQueue.Enqueue(FTSensor.torqueX);
                    TorqueYQueue.Enqueue(FTSensor.torqueY);
                    TorqueZQueue.Enqueue(FTSensor.torqueZ);
                    TorqeIsUpdate = true;
#if DEBUG && qDEBUG_TQ
                    Console.WriteLine("Fx=" + FTSensor.forceX + "\tTx=" + FTSensor.torqueX
                        + "\tFy=" + FTSensor.forceY + "\tTy=" + FTSensor.torqueY
                        + "\tFz=" + FTSensor.forceZ + "\tTz=" + FTSensor.torqueZ
                        );
#endif
                }
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.Message);
            }

            //FTSensor.forceX= BitConverter.ToSingle(M8128RevData, 4) + xForceOffset;
            //FTSensor.torqueX = BitConverter.ToSingle(M8128RevData, 16) + xTorqueOffset;//实际力矩
            //torquedata.data_floatX = FTSensor.forceX;
            //torquedata.data_floatXM = FTSensor.torqueX;

            //FTSensor.forceY = BitConverter.ToSingle(M8128RevData, 8) + yForceOffset;//实际力
            //FTSensor.torqueY = BitConverter.ToSingle(M8128RevData, 20) + yTorqueOffset;//实际力矩
            //torquedata.data_floatY = FTSensor.forceY;
            //torquedata.data_floatYM = FTSensor.torqueY;

            //FTSensor.forceZ = BitConverter.ToSingle(M8128RevData, 12) + zForceOffset;//实际力
            //FTSensor.torqueZ = BitConverter.ToSingle(M8128RevData, 24) + zTorqueOffset;//实际力矩

        }
        //串口2事件（绝对值传感器)
        private void serialPort2_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            
        }

        #endregion
        //serial1力矩采集
        //serial2编码器采集
        private void KMPTunnel_Load(object sender, EventArgs e)
        {
            ChartInit();
            ChartTimer.Enabled = false;

            
            torquedata = new Torquedata(serialPort1);//力矩采集
            serialportReceivingData = new SerialportReceivingData(serialPort2);//角度采集

            serialPort1.DataReceived += new SerialDataReceivedEventHandler(serialPort1_DataReceived);//必须手动添加事件处理程序（力传感器事件）
            serialPort2.DataReceived += new SerialDataReceivedEventHandler(serialPort2_DataReceived);//必须手动添加事件处理程序（编码器采集）

            comboBox2.Text = "COM7";//编码器,
            comboBox3.Text = "COM8";//力传感器
        }


        #region 监测回零线程事件
        private void Monitoring_return_to_zero_event()      //监测回零线程事件
        {
            //CAN_disconnected.BackColor = Color.Red;

            serialportReceivingData.StartTxRxThread();//启动通信对象的线程接收数据
            while (Manual_mode_zero)
            {
                if (serialportReceivingData.ser_receive_queue_.Count > 0)//回零监测显示
                {
                    while (serialportReceivingData.ser_receive_queue_.Count > 0)
                        mcu_Data = serialportReceivingData.GetBoardInfo();//获取了下位机信息
                    if (mcu_Data != null)
                    {

                        fa.WaitOne();
                        DegreeSensor.degreeX = (float)(mcu_Data.motor[0].Act_position * (360.0 / 4095)) + xAngle;

                        DegreeSensor.degreeY = (float)(mcu_Data.motor[1].Act_position * (360.0 / 4095)) + yAngle;

                        DegreeSensor.degreeZ = (float)(mcu_Data.motor[2].Act_position * (360.0 / 4095)) + zAngle;

                        Act_position_1 = mcu_Data.motor[0].Act_position;

                        Act_position_2 = mcu_Data.motor[1].Act_position;

                        Act_position_3 = mcu_Data.motor[2].Act_position;

                        //DegreeSensor.degreeX = BitConverter.ToSingle(STMRevData, 1) + Offset_Dx;
                        //DegreeXQueue.Enqueue(DegreeSensor.degreeX);
                        //DegreeSensor.degreeY = BitConverter.ToSingle(STMRevData, 5) + Offset_Dy;
                        //DegreeYQueue.Enqueue(-DegreeSensor.degreeY);
                        //DegreeSensor.degreeZ = BitConverter.ToSingle(STMRevData, 9) + Offset_Dz;
                        //DegreeZQueue.Enqueue(-DegreeSensor.degreeZ);

                        X_axis_monitoring.Text = DegreeSensor.degreeX.ToString();
                        Thread.Sleep(5);
                        Y_axis_monitoring.Text = DegreeSensor.degreeY.ToString();
                        Thread.Sleep(5);
                        Z_axis_monitoring.Text = DegreeSensor.degreeZ.ToString();
                        Thread.Sleep(5);

                        //Console.WriteLine(mcu_Data.motor[0].Act_position);
                    }
                }
            }

        }
        #endregion

        #region 图表绘制
        /// <summary>
        /// 图表初始化函数
        /// </summary>
        private void ChartInit()
        {
            //AngleChart.Titles.Add("Real Time Angle Data");

            AngleChart.Series.Clear();

            Series seriesX = new Series("X");
            seriesX.ChartArea = "ChartArea1";
            seriesX.Color = System.Drawing.Color.Red;
            AngleChart.Series.Add(seriesX);

            Series seriesY = new Series("Y");
            seriesY.ChartArea = "ChartArea1";
            seriesY.Color = System.Drawing.Color.Purple;
            AngleChart.Series.Add(seriesY);

            Series seriesZ = new Series("Z");
            seriesZ.ChartArea = "ChartArea1";
            seriesZ.Color = System.Drawing.Color.Navy;
            AngleChart.Series.Add(seriesZ);

            for (int i = 0; i < 3; i++)
            {
                AngleChart.Series[i].ChartType = SeriesChartType.Spline;
                AngleChart.Series[i].BorderWidth = 2; //线条粗细
            }

            //添加的两组Test数据
            List<int> txData2 = new List<int>() { 1, 2, 3, 4, 5, 6 };
            List<int> tyData2 = new List<int>() { 9, 6, 7, 4, 5, 4 };
            List<int> txData3 = new List<int>() { 1, 2, 3, 4, 5, 6 };
            List<int> tyData3 = new List<int>() { 3, 8, 2, 5, 4, 9 };
            AngleChart.Series[0].Points.DataBindXY(txData2, tyData2); //添加数据
            AngleChart.Series[1].Points.DataBindXY(txData3, tyData3); //添加数据

            //M8128Chart.Titles.Add("Real Time Torque Data");

            M8128Chart.Series.Clear();

            Series seriesFX = new Series("Fx");
            seriesFX.ChartArea = "ChartArea1";
            seriesFX.Color = System.Drawing.Color.Red;
            M8128Chart.Series.Add(seriesFX);

            Series seriesFY = new Series("Fy");
            seriesFY.ChartArea = "ChartArea1";
            seriesFY.Color = System.Drawing.Color.Purple;
            M8128Chart.Series.Add(seriesFY);

            Series seriesFZ = new Series("Fz");
            seriesFZ.ChartArea = "ChartArea1";
            seriesFZ.Color = System.Drawing.Color.Navy;
            M8128Chart.Series.Add(seriesFZ);

            Series seriesTX = new Series("Tx");
            seriesTX.ChartArea = "ChartArea1";
            seriesTX.Color = System.Drawing.Color.Green;
            M8128Chart.Series.Add(seriesTX);

            Series seriesTY = new Series("Ty");
            seriesTY.ChartArea = "ChartArea1";
            seriesTY.Color = System.Drawing.Color.Fuchsia;
            M8128Chart.Series.Add(seriesTY);

            Series seriesTZ = new Series("Tz");
            seriesTZ.ChartArea = "ChartArea1";
            seriesTZ.Color = System.Drawing.Color.Orange;
            M8128Chart.Series.Add(seriesTZ);


            for (int i = 0; i < 6; i++)
            {
                M8128Chart.Series[i].ChartType = SeriesChartType.Spline;
                M8128Chart.Series[i].BorderWidth = 2; //线条粗细
            }

            M8128Chart.Series[4].Points.DataBindXY(txData2, tyData2); //添加数据
            M8128Chart.Series[5].Points.DataBindXY(txData3, tyData3); //添加数据

        }
        #endregion


        double[,] doubleObj;
        #region 轨迹载入
        private void button1_Click(object sender, EventArgs e)
        {

            DataTable CsvRoute = new DataTable();
            
            string FilePath_3 = "D:\\Project\\matlab_work\\Channel_Process\\Channel.csv";//获取CSV文件的路径
            //string FilePath_3 = "D:\\Project\\matlab_work\\Channel_Process\\route22.csv";//获取CSV文件的路径
            tool.readCSV(FilePath_3, out CsvRoute); // 调用函数

            doubleObj = new double[CsvRoute.Rows.Count, 3];//保存欧式空间角度
                                                           //清空图表
            for (int i = 0; i < CsvRoute.Rows.Count; i++)
            {
                for (int j = 0; j < CsvRoute.Columns.Count; j++)
                {
                    object obj = CsvRoute.Rows[i][j]; //i行j列的值 
                    obj = obj.ToString();
                    doubleObj[i, j] = Convert.ToDouble(obj);
                }
            }

            MessageBox.Show("数据读取完成");
        }
        #endregion

        private void button2_Click(object sender, EventArgs e)
        {

        }

        #region 回零线程事件
        private void Zeroing_thread_event()            //回零线程事件
        {
            //电机反转是推杆伸长，正转缩短
            //x代表推杆1，y代表推杆2，z代表主电机
            fa.WaitOne();
            //z
            while (Manual_mode_zero)
            {
                if (Act_position_3 == 750.0)                          //如果此时z轴的在零点位置
                {
                    MessageBox.Show("z轴已经修正");
                }
                else if (Act_position_3 > 750.0)                      //假设角度大于零点位置
                {
                    Motor_3 = true;
                    MessageBox.Show("即将修正z轴，请稍后");
                    //使用速度模式进行修正（使主电机（3号电机）逆时针转）              
                    Epos1.SetVelocity(-20);
                    while (true)
                    {
                        if (Act_position_3 < 750.0)
                        {
                            Epos1.SetVelocity(0);                   //停止电机
                            Thread.Sleep(5);
                            Epos1.Reseat();                         //失能电机
                            Motor_3 = false;
                            break;
                        }
                    }
                }
                else if (Act_position_3 < 750.0)                      //假设角度小于零点位置
                {
                    Motor_3 = true;
                    MessageBox.Show("即将修正z轴，请稍后");
                    Epos1.SetVelocity(20);
                    while (true)
                    {
                        if (Act_position_3 > 750.0)
                        {
                            Epos1.SetVelocity(0);                   //停止电机
                            Thread.Sleep(5);
                            Epos1.Reseat();                         //失能电机
                            Motor_3 = false;
                            break;
                        }
                    }
                }
                Console.WriteLine("z= ", Act_position_3);

                //y
                if (Act_position_2 == 2275.0)//如果此时y轴的在零点位置
                {
                    MessageBox.Show("y轴已经修正");
                }
                else if (Act_position_2 > 2275.0)//假设角度大于零点位置
                {
                    Motor_2 = true;
                    MessageBox.Show("即将修正y轴，请稍后");
                    //使用速度模式进行修正
                    Epos1.SetVelocity(20);
                    while (true)
                    {
                        if (Act_position_2 < 2275.0)
                        {
                            Epos1.SetVelocity(0);   //停止电机
                            Thread.Sleep(5);
                            Epos1.Reseat();         //失能电机
                            Motor_2 = false;
                            break;
                        }
                    }
                }
                else if (Act_position_2 < 2275.0)//假设角度小于零点位置
                {
                    Motor_2 = true;
                    MessageBox.Show("即将修正y轴，请稍后");
                    Epos1.SetVelocity(-20);
                    while (true)
                    {
                        if (Act_position_2 > 2275.0)
                        {
                            Epos1.SetVelocity(0);   //停止电机
                            Thread.Sleep(5);
                            Epos1.Reseat();         //失能电机
                            Motor_2 = false;
                            break;
                        }
                    }
                }
                //x
                if (Act_position_1 == 970.0)//如果此时x轴的在零点位置
                {
                    MessageBox.Show("x轴已经修正");
                }
                else if (Act_position_1 > 970.0)//假设角度大于零点位置
                {
                    Motor_1 = true;
                    Motor_2 = true;
                    MessageBox.Show("即将修正x轴，请稍后");
                    //使用速度模式进行修正 (两个推杆同时动,缩短以修正)              
                    Epos1.SetVelocity(-20);
                    while (true)
                    {
                        if (Act_position_1 < 970.0)
                        {
                            Epos1.SetVelocity(0);   //停止电机
                            Thread.Sleep(5);
                            Epos1.Reseat();         //失能电机
                            Motor_1 = false;
                            Motor_2 = false;
                            break;
                        }
                    }
                }
                else if (Act_position_1 < 970.0)//假设角度小于零点位置
                {
                    Motor_1 = true;
                    Motor_2 = true;
                    MessageBox.Show("即将修正x轴，请稍后");
                    //使用速度模式进行修正 (两个推杆同时动,伸长以修正)
                    Epos1.SetVelocity(20);
                    while (true)
                    {
                        if (Act_position_1 > 970.0)
                        {
                            Epos1.SetVelocity(0);   //停止电机
                            Thread.Sleep(5);
                            Epos1.Reseat();         //失能电机
                            Motor_1 = false;
                            Motor_2 = false;
                            break;
                        }
                    }
                }
                Manual_mode_zero = false;
            }
            if (Manual_mode_zero == false)
            {
                Thread.Sleep(100);
                Thread.CurrentThread.Interrupt();
                MessageBox.Show("回零过程已经完成");
                //serialPort2.Close();
                fa.Reset();                         //关闭整个归零过程所用到的所有线程
                //serialPort2.Close();
                //encoder_openning.Enabled = true;        //绝对值编码器的串口打开按钮可用
                return;
            }
        }
        #endregion

        #region 扫描串口方法
        private void SearchAndAddSerialToComboBox(SerialPort MyPort, System.Windows.Forms.ComboBox MyBox)//扫描串口
        {

            string Buffer;                     //缓存
            MyBox.Items.Clear();               //清空ComboBox中的内容
            for (int i = 1; i < 20; i++)       //循环
            {
                try
                {
                    Buffer = "COM" + i.ToString();
                    MyPort.PortName = Buffer;
                    MyPort.Open();                //如果串口打开成功，才可以执行下一句
                    MyBox.Items.Add(Buffer);      //将串口名（缓存）添加到下拉列表中
                    MyPort.Close();
                }
                catch
                { }
            }
        }
        #endregion

        private void button3_Click(object sender, EventArgs e)
        {
            if (Cnh.Strat())
            {
                MessageBox.Show("连接成功！");
                button3.Enabled = false;//连接按钮不可用
                button4.Enabled = true;//断开按钮可用
            }
            else
            {
                MessageBox.Show("连接失败", "ERROR");
            }
            
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (Cnh.Close())
            {
                MessageBox.Show("断开成功！");
            }
            button3.Enabled = true;//连接按钮可用
            button4.Enabled = false;//断开按钮不可用
        }

        private void button7_Click(object sender, EventArgs e)
        {
            SearchAndAddSerialToComboBox(serialPort1, comboBox3);//串口1：编码器
            SearchAndAddSerialToComboBox(serialPort2, comboBox2);//串口2：六维力传感器
        }

        private void button5_Click(object sender, EventArgs e)
        {
            try
            {
                //串口1配置,力矩编码器
                serialPort1.PortName = comboBox3.Text;//获取串口名
                serialPort1.BaudRate = Convert.ToInt32(textBox7.Text);//十进制数据转化
                serialPort1.Open();
                if(serialPort1.IsOpen)
                {
                    serialPort1.Write("AT+SGDM=(A01,A02,A03,A04,A05,A06);E;1(WMA:1)\r\n");
                    System.Threading.Thread.Sleep(10);
                    serialPort1.Write("AT+GSD\r\n");
                }

                //串口2配置，编码器
                serialPort2.PortName = comboBox2.Text;//获取串口名
                serialPort2.BaudRate = Convert.ToInt32(textBox7.Text);//十进制数据转化
                serialPort2.Open();

                //button5.Enabled = false;//打开串口按钮不可用
                //button6.Enabled = true;//关闭串口按钮可用
                MessageBox.Show("端口连接成功", "OK");
                
            }
            catch
            {
                MessageBox.Show("端口错误，请检查串口", "错误");
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// 角度曲线更新函数
        /// </summary>
        private void UpdateAngleQueue()
        {

            int realpoint = 0;
            if ( XDegreedata.Count > realpoint) realpoint = XDegreedata.Count;
            if ( YDegreedata.Count > realpoint) realpoint = YDegreedata.Count;
            if ( ZDegreedata.Count > realpoint) realpoint = ZDegreedata.Count;

            lock (ChartLock)
            {
                if (realpoint > NumOfPoint)
                {
                    XDegreedata.Dequeue();
                    YDegreedata.Dequeue();
                    ZDegreedata.Dequeue();
                }


                lock (STMLock)
                {
                    XDegreedata.Enqueue(DegreeSensor.degreeX);
                    YDegreedata.Enqueue(DegreeSensor.degreeY);
                    //YDegreedata.Enqueue(RealDegree);
                    ZDegreedata.Enqueue(DegreeSensor.degreeZ);
                }

                for (int i = 0; i < 3; i++)
                {
                    AngleChart.Series[i].Points.Clear();
                }

                for (int i = 0; i < XDegreedata.Count; i++)
                {
                    AngleChart.Series[0].Points.AddXY((i + 1), XDegreedata.ElementAt(i));
                    AngleChart.Series[1].Points.AddXY((i + 1), YDegreedata.ElementAt(i));
                    AngleChart.Series[2].Points.AddXY((i + 1), ZDegreedata.ElementAt(i));
                }
            }
        }

        private void UpdateTorqeQueue()
        {
            int realpoint = 0;
            if ( XFdata.Count > realpoint) realpoint = XFdata.Count;
            if ( YFdata.Count > realpoint) realpoint = YFdata.Count;
            if ( ZFdata.Count > realpoint) realpoint = ZFdata.Count;
            if ( XTdata.Count > realpoint) realpoint = XTdata.Count;
            if ( YTdata.Count > realpoint) realpoint = YTdata.Count;
            if ( ZTdata.Count > realpoint) realpoint = ZTdata.Count;

            if (realpoint > NumOfPoint)
            {
                XFdata.Dequeue();
                YFdata.Dequeue();
                ZFdata.Dequeue();
                XTdata.Dequeue();
                YTdata.Dequeue();
                ZTdata.Dequeue();
            }
            lock (M8128Lock)
            {
                XFdata.Enqueue(FTSensor.forceX);
                XTdata.Enqueue(FTSensor.torqueX);
                YFdata.Enqueue(FTSensor.forceY);
                YTdata.Enqueue(FTSensor.torqueY);
                ZFdata.Enqueue(FTSensor.forceZ);
                ZTdata.Enqueue(FTSensor.torqueZ);
            }
            PaintTorqe();
        }

        private void PaintTorqe()
        {
            TorqeIsUpdate = false;

            for (int i = 0; i < 6; i++)
            {
                M8128Chart.Series[i].Points.Clear();
            }
            for (int i = 0; i < XTdata.Count; i++)
            {
                //M8128Chart.Series[0].Points.AddXY((i + 1), XFdata.ElementAt(i));

                //M8128Chart.Series[1].Points.AddXY((i + 1), YFdata.ElementAt(i));

                //M8128Chart.Series[2].Points.AddXY((i + 1), ZFdata.ElementAt(i));

                M8128Chart.Series[3].Points.AddXY((i + 1), XTdata.ElementAt(i));

                M8128Chart.Series[4].Points.AddXY((i + 1), YTdata.ElementAt(i));

                M8128Chart.Series[5].Points.AddXY((i + 1), ZTdata.ElementAt(i));
            }
        }

        private void ChartTimer_Tick(object sender, EventArgs e)
        {
            UpdateAngleQueue();
            UpdateTorqeQueue();
        }

        /// <summary>
        /// 机器人回到零点
        /// </summary>
        public void GoToZero()
        {
            HDTX.PositInit();
            HDTY.PositInit();
            HDTZ.PositInit();

            Thread.Sleep(100);


            HDTX.SetPosition(0);
            HDTY.SetPosition(0);
            HDTZ.SetPosition(0);
        }

        private void BackToZeroBtn_Click(object sender, EventArgs e)
        {
            MessageBox.Show("即将进行设备回零，请耐心等待");
            Epos1.VelocityInit();                                 //速度初始化
            Zeroing_thread = new Thread(Zeroing_thread_event);    //启用归零线程
            Zeroing_thread.IsBackground = true;
            fa.Set();
            Zeroing_thread.Start();
            //GoToZero();
        }

        private void CheckBackZeroBtn_Click(object sender, EventArgs e)
        {
            #region 监视过程
            if (serialPort2.IsOpen)
            {
                Manual_mode_zero = true;
                Monitoring_return_to_zero = new Thread(new ThreadStart(Monitoring_return_to_zero_event));
                Monitoring_return_to_zero.IsBackground = true;
                fa.Set();//发送信号，让fa.WaitOne()接收到信号执行fa.WaitOne()后面的线程程序,
                Monitoring_return_to_zero.Start();
                //time = new Thread(tim);
                //time.IsBackground = true;
                fa.Set();
                //time.Start();
                ChartTimer.Enabled = true;
            }
            else
            {
                MessageBox.Show("串口未打开", "错误");

            }
            #endregion
        }

        private void button8_Click(object sender, EventArgs e)
        {
            isSingle = false;
            int axix = 0x01;
            int Low = 10;
            int High = 10;
            int Speed = 60;
            int Thd = Convert.ToInt32(textBox25.Text);
            //导纳参数读取
            float M = Convert.ToInt32(textBox1.Text);
            float B = Convert.ToInt32(textBox2.Text);
            float K = Convert.ToInt32(textBox3.Text);


            axix = Train_Axis.BeishenZhiqu;
            High = Convert.ToInt32(textBox16.Text);
            Low = Convert.ToInt32(textBox17.Text);


            //trainModel.Set_Train_UI_Parameter(DengSu_UI_panel, Start_Dengsu_Button, Stop_Dengsu_Button);
            trainModel.Set_ZhuDong_Parameter(axix, High, Low, Speed, Thd);
            trainModel.setMBK(M, B, K);
            trainModel.ZhuDongTrain_Start();
        }

        private void button9_Click(object sender, EventArgs e)
        {

        }
        

        private void button10_Click(object sender, EventArgs e)
        {
            isSingle = true;
            int axix = 0x01;
            int Low = 30;
            int High = 30;
            int Speed = 60;
            int Thd = Convert.ToInt32(textBox25.Text);

            //导纳参数读取
            float M = Convert.ToInt32(textBox1.Text);
            float B = Convert.ToInt32(textBox2.Text);
            float K = Convert.ToInt32(textBox3.Text);

            if (radioButton6.Checked)
            {
                axix = Train_Axis.BeishenZhiqu;
                High = Convert.ToInt32(textBox16.Text);
                Low = Convert.ToInt32(textBox17.Text);
            }
            else if (radioButton5.Checked)
            {
                axix = Train_Axis.NeishouWaizhan;
                Low = Convert.ToInt32(textBox20.Text);
                High = Convert.ToInt32(textBox21.Text);
            }
            else if (radioButton4.Checked)
            {
                axix = Train_Axis.NeifanWaifan;
                Low = Convert.ToInt32(textBox18.Text);
                High = Convert.ToInt32(textBox19.Text);
            }

            //trainModel.Set_Train_UI_Parameter(DengSu_UI_panel, Start_Dengsu_Button, Stop_Dengsu_Button);
            trainModel.Set_ZhuDong_Parameter(axix, High, Low, Speed, Thd);
            trainModel.setMBK(M, B, K);
            trainModel.ZhuDongTrain_Start();
        }

        private void button11_Click(object sender, EventArgs e)
        {
            //double[] A = { doubleObj[0, 0], doubleObj[0, 1] , doubleObj[0, 2] };
            //double[] B = { doubleObj[1, 0], doubleObj[1, 1] , doubleObj[1, 2] };
            //double[] P = {5,2,-1 };
            //(double res, double[] D) =tool.DistanceParallel(A, B, P);
            //textBox5.Text=res.ToString();

            textBox5.Text = doubleObj[0, 0].ToString();
        }

        private void button12_Click(object sender, EventArgs e)
        {
            thStartServer = new Thread(StartServer);
            thStartServer.Start();//启动该线程
        }

        private void StartServer()
        {
            const int bufferSize = 8792;//缓存大小,8192字节

            //IPAddress ip = IPAddress.Parse("192.168.0.13");

            //TcpListener tlistener = new TcpListener(ip, 10001);

            tlistener = new TcpListener(IPAddress.Parse(str_ip), int.Parse(str_port));

            tlistener.Start();

            Console.WriteLine("Socket服务器监听启动......");
            remoteClient = tlistener.AcceptTcpClient();//接收已连接的客户端,阻塞方法

            Console.WriteLine("客户端已连接！local:" + remoteClient.Client.LocalEndPoint + "<---Client:" + remoteClient.Client.RemoteEndPoint);

            NetworkStream streamToClient = remoteClient.GetStream();//获得来自客户端的流

            do
            {
                try //直接关掉客户端，服务器端会抛出异常
                {
                    //接收客户端发送的数据部分
                    byte[] buffer = new byte[bufferSize];//定义一个缓存buffer数组

                    int byteRead = streamToClient.Read(buffer, 0, bufferSize);//将数据搞入缓存中（有朋友说read()是阻塞方法，测试中未发现程序阻塞）
                    if (byteRead == 0)//连接断开，或者在TCPClient上调用了Close()方法，或者在流上调用了Dispose()方法。
                    {
                        Console.WriteLine("客户端连接断开......");
                        break;
                    }

                    string msg = Encoding.Unicode.GetString(buffer, 0, byteRead);//从二进制转换为字符串对应的客户端会有从字符串转换为二进制的方法
                    Console.WriteLine("服务端 接收数据：" + msg + ".数据长度:[" + byteRead + "byte]");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("客户端异常：" + ex.Message);
                    break;
                }
            }
            while (true);
        }

        private void button13_Click(object sender, EventArgs e)
        {
            //利用TcpClient对象GetStream方法得到网络流
            NetworkStream clientStream = remoteClient.GetStream();
            bw = new BinaryWriter(clientStream);
            //写入
            String SocketData = "1.55,3.44,7.70";
            //写入
            bw.Write(SocketData);
        }

        private void button14_Click(object sender, EventArgs e)
        {
            Thread childThread = new Thread(OffSet);
            childThread.Start();
        }
        /// <summary>
        /// 实现力矩平衡函数
        /// </summary>
        private void OffSet()
        {
            button14.Enabled = false;
            Offset_Fx = 0;
            Offset_Fy = 0;
            Offset_Fz = 0;
            Offset_Tx = 0;
            Offset_Ty = 0;
            Offset_Tz = 0;
#if ENCODER_OFFSET
            Offset_Dx = 0;
            Offset_Dy = 0;
            Offset_Dz=0;
#endif
            float result1 = 0, result2 = 0, result3 = 0, result4 = 0, result5 = 0, result6 = 0;
#if ENCODER_OFFSET
            float a1 = 0;float a2 = 0;float a3 = 0;
#endif
            for (int i = 0; i < 100; i++)
            {
                result1 += FTSensor.forceX;
                result2 += FTSensor.torqueX;
                result3 += FTSensor.forceY;
                result4 += FTSensor.torqueY;
                result5 += FTSensor.forceZ;
                result6 += FTSensor.torqueZ;
#if ENCODER_OFFSET
                a1 += DegreeSensor.degreeX;
                a2 += DegreeSensor.degreeY;
                a3 += DegreeSensor.degreeZ;
#endif
                Thread.Sleep(40);
            }
            Offset_Fx = -result1 / 100.0f;
            Offset_Fy = -result3 / 100.0f;
            Offset_Fz = -result5 / 100.0f;
            Offset_Tx = -result2 / 100.0f;
            Offset_Ty = -result4 / 100.0f;
            Offset_Tz = -result6 / 100.0f;
#if ENCODER_OFFSET
            Offset_Dx = -a1 / 100.0f;
            Offset_Dy = -a2 / 100.0f;
            Offset_Dz = -a3 / 100.0f;
#endif
            Thread.Sleep(800);

            LogUI.Log(Thread.CurrentThread.ManagedThreadId, "", "", "Fx：" + Offset_Fx.ToString("0.000") + "；Fy：" + Offset_Fy.ToString("0.000") + "；Fz：" + Offset_Fz.ToString("0.000"));
            LogUI.Log(Thread.CurrentThread.ManagedThreadId, "", "", "Tx：" + Offset_Tx.ToString("0.000") + "；Ty：" + Offset_Ty.ToString("0.000") + "；Tz：" + Offset_Tz.ToString("0.000"));

            //弹窗显示力矩偏执
            // MessageBox.Show("校准完成！Fx偏置为：" + Offset_Fx.ToString("0.000") + "；Tx的偏置为：" + Offset_Tx.ToString("0.000") + "；Fy的偏置为：" + Offset_Fy.ToString("0.000")+ "；Ty的偏置为：" + Offset_Ty.ToString("0.000") + "；Fz的偏置为：" + Offset_Fz.ToString("0.000") + "；Tz的偏置为：" + Offset_Tz.ToString("0.000") + ".", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
#if ENCODER_OFFSET
            MessageBox.Show("校准完成！Ax偏置为：" + Offset_Dx.ToString("0.000") + "；Ay的偏置为：" + Offset_Dy.ToString("0.000") + "；Az的偏置为：" + Offset_Dz.ToString("0.000")+".", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
#endif

            button14.Enabled = true;

        }

    }
}
