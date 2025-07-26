using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using CANNalyst;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Button;
using csmatio.types;
using csmatio.io;
using System.Windows.Forms.DataVisualization.Charting;




namespace Matlab_connect
{
    //声明一个委托，用于往后台传送需要修正的数据
    public delegate void setInsertValue();
    public delegate void setDoubleKMP();
    public partial class MachineControl : Form
    {
        //首先声明一些基础的类
        #region 一些声明（类、线程）
        KinematicsHelper PositionInverse = new KinematicsHelper();  //声明位置逆解类
        SerialportReceivingData serialportReceivingData;            //实例化串口接受数据线程
        SerialportReceivingDataForce serialportReceivingDataForce;
        Torquedata torquedata;
        Mcu_data mcu_Data = new Mcu_data();                         //解析编码器数据
        ManualResetEvent move = new ManualResetEvent(false);        //手动被动模式训练过程线程开启终止控制（false为等待信号开启
        Thread Monitoring_return_to_zero;                           //创建监测回零线程
        ManualResetEvent fa = new ManualResetEvent(false);          //手动被动模式归零过程线程开启终止控制（false为等待信号开启）
        Thread Zeroing_thread;                                      //声明手动模式回零线程
        public ManualResetEvent start = new ManualResetEvent(false);//手动被动模式编码器数值解析开启终止控制（false为等待信号开启）
        public ManualResetEvent start1 = new ManualResetEvent(false);
        VelocityJacobi velocityJacobi1 = new VelocityJacobi();
        VelocityJacobiUsing VJ = new VelocityJacobiUsing();

        public DataTrasfer dataTransfer = new DataTrasfer();               //数据变量，用于保存两个页面中需要传输的变量
        Thread MotorControl_thread;                                 //电机控制线程
        Thread WriteForce_thread;                                  //用于记录力学数据
        Thread CalculateForce_thread;                              //用于计算力数据与期望之间的数据

        Thread encoderRecord_thread;                               //用于记录编码器数据


        public event setInsertValue setFormInsertValue;             //声明一个委托类型的事件
        public event setDoubleKMP setFormDoubleKMP;             //声明一个委托类型的事件

        bool TorqeIsUpdate;
        byte[] M8128RevData;
        Tool tool = new Tool();
        #endregion

        #region 力位速上位机需要记录数据
        List<double[]> pos = new List<double[]>();//用于记录需要经过的路径
        List<double[]> vel = new List<double[]>();//用于记录需要经过的路径所需要的速度
        List<double[]> singleRoute = new List<double[]>();//用于保存需要记录的单条数据，用来计算速度
        List<double[]> forceList = new List<double[]>();//用于保存六维力传感器数据
        List<double[]> angleList = new List<double[]>();//用于保存编码器数据
        //用于记录实现保存好的力矩期望和方差数据
        DataTable x_torque_datatable = new DataTable();
        DataTable y_torque_datatable = new DataTable();
        DataTable z_torque_datatable = new DataTable();
        SqliteConnection sql = new SqliteConnection();

        int count = 0;//用于路径数组计数，在timer中
        #endregion

        //角度编码器解析变量 
        public double Act_position_1;                               //用于回零电机位置判断（1号电机，x轴）
        public double Act_position_2;                               //用于回零电机位置判断（2号电机，y轴）
        public double Act_position_3;                               //用于回零电机位置判断（3号电机，z轴）

        //力矩解析变量
        public double Act_torque_1;
        public double Act_torque_2;
        public double Act_torque_3;
        public double Act_torque_4;
        public double Act_torque_5;
        public double Act_torque_6;

        public float xAngle = -84.077f;
        public float yAngle = -207.709f;
        public float zAngle = -67.659f;
        //手动模式归零标志
        bool Manual_mode_zero = false;
        #region 使用CAN分析仪的初始声明
        const string Motor1 = "00000601";
        static CANalystHelper Cnh = new CANalystHelper();//声明关于Can的
        EposDriver Epos1 = new EposDriver("00000601", Cnh);//声明关于驱动器的        
        #endregion

        #region 一些判断变量
        public bool Ankle_chose = false;            //用于是否已经选择脚踝判断
        public bool Ankle_chose1 = false;           //用于手动被动是否已经选择脚踝判断（左）
        public bool Ankle_chose2 = false;           //用于手动被动是否已经选择脚踝判断（右）

        //手动被动选择模式标志
        public bool beishen = false;
        public bool zhiqu = false;
        public bool waifan = false;
        public bool neifan = false;
        public bool waizhan = false;
        public bool neishou = false;
        public int mode = 0;//模式代码
        public static int mode1 = 0;
        //手动被动选择各模式训练范围
        public double beishen_Range = 0;
        public double zhiqu_Range = 0;
        public double waifan_Range = 0;
        public double neifan_Range = 0;
        public double waizhan_Range = 0;
        public double neishou_Range = 0;
        //电机状态
        public static bool Motor_1 = false;                //一号电机状态（用于区分不同电机）
        public static bool Motor_2 = false;                //二号电机状态
        public static bool Motor_3 = false;                //三号电机状态
        //手动被动训练次数变量定义
        int training_Times = 0;
        int training_count = 0;
        bool initial_arriva_position = true;
        bool arriva_position = false;
        bool continuous_detection = true;
        int Motor_1_position = 0;//电机1位置
        int Motor_2_position = 0;//电机2位置
        int Motor_3_position = 0;//电机3位置
        #endregion

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

        private float X;//定义当前窗体的宽度
        private float Y;//定义当前窗体的高度
        //参数初始化
        private void MachineControl_Load(object sender, EventArgs e)
        {
            serialportReceivingData = new SerialportReceivingData(serialPort2);//角度采集
            torquedata = new Torquedata(serialPort1);//力矩采集

            serialPort1.DataReceived += new SerialDataReceivedEventHandler(serialPort1_DataReceived);//必须手动添加事件处理程序（力传感器事件）
            serialPort2.DataReceived += new SerialDataReceivedEventHandler(serialPort2_DataReceived);//必须手动添加事件处理程序（编码器采集）
            
            this.MotorControl_thread = new Thread(motorcontrol_event);//设置电机运行线程
            this.MotorControl_thread.IsBackground = true;

            this.WriteForce_thread = new Thread(writeforce_event);//记录力数据
            this.WriteForce_thread.IsBackground = true;

            //this.CalculateForce_thread = new Thread(calculateforce_event);//计算力数据与期望之间的差距
            //this.CalculateForce_thread.IsBackground = true;

            //this.encoderRecord_thread = new Thread(encoderrecord_event);
            //this.CalculateForce_thread.IsBackground = true;
            

            comboBox1.Text = "COM8";
            comboBox2.Text = "115200";
            comboBox3.Text = "COM7";//插左边的话是com10
            comboBox4.Text = "115200";
            //初始化图像格式
            InitChart();
            //初始化页面数据传输类
            dataTransfer.InitInsertPoint();
            
            //加载基础力期望与方差
            x_torque_datatable = readCSV("D:\\Project\\C#_work\\Matlab_connect\\Matlab_connect\\M_force\\torque_x.csv");
            y_torque_datatable = readCSV("D:\\Project\\C#_work\\Matlab_connect\\Matlab_connect\\M_force\\torque_y.csv");
            z_torque_datatable = readCSV("D:\\Project\\C#_work\\Matlab_connect\\Matlab_connect\\M_force\\torque_z.csv");
            drawChart1();

        }

        
        private void writeforce_event()
        {
            while(true)
            {
                if (isForce == true)
                {
                    double[] temp = { forceX, forceY, forceZ, torqueX, torqueY, torqueZ };
                    forceList.Add(temp);
                    //Console.WriteLine("数列中有{0}个数", forceList.Count());
                    double[] temp_angle = { Act_position_1 * (360.0 / 4095) + xAngle, Act_position_2 * (360.0 / 4095) + yAngle, Act_position_3 * (360.0 / 4095) + zAngle };
                    angleList.Add(temp_angle);
                }
                Thread.Sleep(20);
            }
        }





        #region 绘制力矩数据图
        private void drawChart1()
        {
            int count = x_torque_datatable.Rows.Count;
            for (int i = 0; i < count; i++)
            {
                double time = Convert.ToDouble(x_torque_datatable.Rows[i][0]);
                x_torqure_chart.Series["X力矩期望"].Points.AddXY(time, Convert.ToDouble(x_torque_datatable.Rows[i][1]));
                x_torqure_chart.Series["X力矩上方差"].Points.AddXY(time, Convert.ToDouble(x_torque_datatable.Rows[i][2]));
                x_torqure_chart.Series["X力矩下方差"].Points.AddXY(time, Convert.ToDouble(x_torque_datatable.Rows[i][3]));
                //刷新chart控件
                x_torqure_chart.Invalidate();
            }
            count = y_torque_datatable.Rows.Count;
            for (int i = 0; i < count; i++)
            {
                double time = Convert.ToDouble(y_torque_datatable.Rows[i][0]);
                y_torqure_chart.Series["Y力矩期望"].Points.AddXY(time, Convert.ToDouble(y_torque_datatable.Rows[i][1]));
                y_torqure_chart.Series["Y力矩上方差"].Points.AddXY(time, Convert.ToDouble(y_torque_datatable.Rows[i][2]));
                y_torqure_chart.Series["Y力矩下方差"].Points.AddXY(time, Convert.ToDouble(y_torque_datatable.Rows[i][3]));
                //刷新chart控件
                y_torqure_chart.Invalidate();
            }
            count = x_torque_datatable.Rows.Count;
            for (int i = 0; i < count; i++)
            {
                double time = Convert.ToDouble(z_torque_datatable.Rows[i][0]);
                z_torqure_chart.Series["Z力矩期望"].Points.AddXY(time, Convert.ToDouble(z_torque_datatable.Rows[i][1]));
                z_torqure_chart.Series["Z力矩上方差"].Points.AddXY(time, Convert.ToDouble(z_torque_datatable.Rows[i][2]));
                z_torqure_chart.Series["Z力矩下方差"].Points.AddXY(time, Convert.ToDouble(z_torque_datatable.Rows[i][3]));
                //刷新chart控件
                z_torqure_chart.Invalidate();
            }

        }
        #endregion


        //static bool on_off_button = true;
        public MachineControl()
        {
            InitializeComponent();      
            System.Windows.Forms.Control.CheckForIllegalCrossThreadCalls = false;
        }

        #region CAN口打开事件
        private void CAN_connect_Click(object sender, EventArgs e)
        {
            if (Cnh.Strat())
            {
                MessageBox.Show("连接成功！");
            }
            CAN_connect.Enabled = false;//连接按钮不可用
            CAN_disconnected.Enabled = true;//断开按钮可用
        }
        #endregion

        #region CAN口关闭事件
        private void CAN_disconnected_Click(object sender, EventArgs e)
        {
            if (Cnh.Close())
            {
                MessageBox.Show("断开成功！");
            }
            CAN_connect.Enabled = true;//连接按钮可用
            CAN_disconnected.Enabled = false;//断开按钮不可用
        }
        #endregion

        #region 力传感器扫描串口事件
        private void Scanning_port_btn_Click(object sender, EventArgs e)
        {
            SearchAndAddSerialToComboBox(serialPort1, comboBox1);
        }


        #endregion

        #region 力传感器打开串口事件
        private void Scanning_can_open_btn_Click(object sender, EventArgs e)
        {
            try
            {
                //串口配置
                serialPort1.PortName = comboBox1.Text;//获取串口名
                serialPort1.BaudRate = Convert.ToInt32(comboBox2.Text);//十进制数据转化
                serialPort1.Open();

                Scanning_can_open_btn.Enabled = false;//打开串口按钮不可用
                Scanning_can_close_btn.Enabled = true;//关闭串口按钮可用

            }
            catch
            {
                MessageBox.Show("端口错误，请检查串口", "错误");
            }
        }
        #endregion

        #region 力传感器关闭串口事件
        private void Scanning_can_close_btn_Click(object sender, EventArgs e)
        {
            try
            {
                serialPort1.Close();

                Scanning_can_open_btn.Enabled = true;//关闭串口按钮不可用
                Scanning_can_close_btn.Enabled = false;//打开串口按钮可用
            }
            catch
            {
                //一般情况下关闭串口不会失效，不用加程序
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

        #region 编码器串口扫描事件
        private void serial_scaning_btn_Click(object sender, EventArgs e)
        {
            SearchAndAddSerialToComboBox(serialPort2, comboBox3);
        }
        #endregion

        #region 编码器打开事件
        private void encoder_openning_Click(object sender, EventArgs e)
        {
            try
            {
                //串口配置
                serialPort2.PortName = comboBox3.Text;//获取串口名
                serialPort2.BaudRate = Convert.ToInt32(comboBox4.Text);//十进制数据转化
                serialPort2.Open();

                encoder_openning.Enabled = false;//打开串口按钮不可用
                encoder_closing.Enabled = true;//关闭串口按钮可用
            }
            catch
            {
                MessageBox.Show("端口错误，请检查串口", "错误");
            }
        }
        #endregion

        #region 编码器关闭事件
        private void encoder_closing_Click(object sender, EventArgs e)
        {
            try
            {
                serialPort2.Close();

                encoder_openning.Enabled = true;//关闭串口按钮不可用
                encoder_closing.Enabled = false;//打开串口按钮可用
            }
            catch
            {
                //一般情况下关闭串口不会失效，不用加程序
            }
        }
        #endregion



        #region 串口事件力传感器
        //串口1事件（力传感器)
        private float forceX;
        private float forceY;
        private double forceZ;
        private float torqueX;
        private float torqueY;
        private double torqueZ;
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
            forceX = BitConverter.ToSingle(M8128RevData, 4) + xForceOffset;
            torqueX = BitConverter.ToSingle(M8128RevData, 16) + xTorqueOffset;//实际力矩
            torquedata.data_floatX = forceX;
            torquedata.data_floatXM = torqueX;

            forceY = BitConverter.ToSingle(M8128RevData, 8) + yForceOffset;//实际力
            torqueY = BitConverter.ToSingle(M8128RevData, 20) + yTorqueOffset;//实际力矩
            torquedata.data_floatY = forceY;
            torquedata.data_floatYM = torqueY;

            forceZ = BitConverter.ToSingle(M8128RevData, 12) + zForceOffset;//实际力
            torqueZ = BitConverter.ToSingle(M8128RevData, 24) + zTorqueOffset;//实际力矩

        }
        //串口2事件（绝对值传感器)
        private void serialPort2_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {

        }

        #endregion



        //保存三个位置
        int[] position = new int[3];
        

        private void Location_access()//位置访问
        {
            Motor_1 = true;
            Epos1.location_Access();
            Motor_1_position = System.Math.Abs(Motor_position.weizhi1);
            Motor_1 = false;
            Thread.Sleep(2);

            Motor_2 = true;
            Epos1.location_Access();
            Motor_2_position = System.Math.Abs(Motor_position.weizhi2);
            Motor_2 = false;
            Thread.Sleep(2);

            Motor_3 = true;
            Epos1.location_Access();
            Motor_3_position = System.Math.Abs(Motor_position.weizhi3);
            Motor_3 = false;
            Thread.Sleep(2);
        }

        private void Position_check()//位置检查
        {
            while (arriva_position)
            {
                bool qq = true;
                bool qq1 = true;
                //Thread.Sleep(5);
                Location_access();
                //position_difference1 = System.Math.Abs(position[0]) - System.Math.Abs(Motor_1_position);
                //position_difference2 = System.Math.Abs(position[1]) - System.Math.Abs(Motor_2_position);
                //position_difference3 = System.Math.Abs(position[2]) - System.Math.Abs(Motor_3_position);
                //Location_access();
                //Motor_1_position = Cnh.fadc;
                switch (mode)
                {
                    case 1:
                    case 2:
                        if (Motor_1_position >= System.Math.Abs(position[0]) - 1000 && Motor_2_position >= System.Math.Abs(position[1]) - 1000)
                        {
                            Position_return_to_zero();
                            //电机开始运行
                            Motor_1 = true;
                            Motor_2 = true;
                            Motor_3 = true;
                            Epos1.StartPosition();
                            Motor_1 = false;
                            Motor_2 = false;
                            Motor_3 = false;
                            //arriva_position = false;
                            while (qq)
                            {
                                Location_access();
                                if (Motor_1_position <= 1000 && Motor_2_position <= 1000)
                                {

                                    arriva_position = true;
                                    qq = false;
                                }
                            }


                        }
                        else if (Motor_1_position <= 1000 && Motor_2_position <= 1000)
                        {

                            Motor_1 = true;
                            Epos1.SetPosition(position[0]);//推杆1的位置
                            Thread.Sleep(1);
                            Motor_1 = false;

                            Motor_2 = true;
                            Epos1.SetPosition(position[1]);//推杆2的位置
                            Thread.Sleep(1);
                            Motor_2 = false;

                            Motor_3 = true;
                            Epos1.SetPosition(position[2]);//主电机的位置
                            Thread.Sleep(1);
                            Motor_3 = false;

                            //电机开始运行
                            Motor_1 = true;
                            Motor_2 = true;
                            Motor_3 = true;
                            Epos1.StartPosition();
                            Motor_1 = false;
                            Motor_2 = false;
                            Motor_3 = false;
                            while (qq1)
                            {
                                Location_access();
                                if (Motor_1_position >= System.Math.Abs(position[0]) - 1000 && Motor_2_position >= System.Math.Abs(position[1]) - 1000)
                                {
                                    arriva_position = true;
                                    qq1 = false;
                                }
                            }
                        }
                        break;
                    case 3:
                    case 4:
                        if (Motor_1_position >= System.Math.Abs(position[0]) - 1000 && Motor_2_position >= System.Math.Abs(position[1]) - 1000)
                        {
                            Position_return_to_zero();
                            //电机开始运行
                            Motor_1 = true;
                            Motor_2 = true;
                            Motor_3 = true;
                            Epos1.StartPosition();
                            Motor_1 = false;
                            Motor_2 = false;
                            Motor_3 = false;
                            //arriva_position = false;
                            while (qq)
                            {
                                Location_access();
                                if (Motor_1_position <= 1000 && Motor_2_position <= 1000)
                                {

                                    arriva_position = true;
                                    qq = false;
                                }
                            }


                        }
                        else if (Motor_1_position <= 1000 && Motor_2_position <= 1000)
                        {

                            Motor_1 = true;
                            Epos1.SetPosition(position[0]);//推杆1的位置
                            Thread.Sleep(1);
                            Motor_1 = false;

                            Motor_2 = true;
                            Epos1.SetPosition(position[1]);//推杆2的位置
                            Thread.Sleep(1);
                            Motor_2 = false;

                            Motor_3 = true;
                            Epos1.SetPosition(position[2]);//主电机的位置
                            Thread.Sleep(1);
                            Motor_3 = false;

                            //电机开始运行
                            Motor_1 = true;
                            Motor_2 = true;
                            Motor_3 = true;
                            Epos1.StartPosition();
                            Motor_1 = false;
                            Motor_2 = false;
                            Motor_3 = false;
                            while (qq1)
                            {
                                Location_access();
                                if (Motor_1_position >= System.Math.Abs(position[0]) - 1000 && Motor_2_position >= System.Math.Abs(position[1]) - 1000)
                                {
                                    arriva_position = true;
                                    qq1 = false;
                                }
                            }
                        }
                        break;
                    case 5:
                    case 6:
                        //if (Motor_1_position >= System.Math.Abs(position[0]) - 60000 && Motor_2_position >= System.Math.Abs(position[1]) - 60000 && Motor_3_position >= System.Math.Abs(position[2]) - 60000)
                        if (Motor_3_position >= System.Math.Abs(position[2]) - 1000)
                        {
                            Position_return_to_zero();
                            //电机开始运行
                            Motor_1 = true;
                            Motor_2 = true;
                            Motor_3 = true;
                            Epos1.StartPosition();
                            Motor_1 = false;
                            Motor_2 = false;
                            Motor_3 = false;
                            //arriva_position = false;
                            while (qq)
                            {
                                Location_access();
                                //if (Motor_1_position <= 60000 && Motor_2_position <= 60000 && Motor_3_position <= 60000)
                                if (Motor_3_position <= 1000)
                                {

                                    arriva_position = true;
                                    qq = false;
                                }
                            }


                        }
                        //else if (Motor_1_position <= 60000 && Motor_2_position <= 60000 && Motor_3_position <= 60000)
                        if (Motor_3_position <= 1000)
                        {

                            Motor_1 = true;
                            Epos1.SetPosition(position[0]);//推杆1的位置
                            Thread.Sleep(1);
                            Motor_1 = false;

                            Motor_2 = true;
                            Epos1.SetPosition(position[1]);//推杆2的位置
                            Thread.Sleep(1);
                            Motor_2 = false;

                            Motor_3 = true;
                            Epos1.SetPosition(position[2]);//主电机的位置
                            Thread.Sleep(1);
                            Motor_3 = false;

                            //电机开始运行
                            Motor_1 = true;
                            Motor_2 = true;
                            Motor_3 = true;
                            Epos1.StartPosition();
                            Motor_1 = false;
                            Motor_2 = false;
                            Motor_3 = false;
                            while (qq1)
                            {
                                Location_access();
                                //if (Motor_1_position >= System.Math.Abs(position[0]) - 60000 && Motor_2_position >= System.Math.Abs(position[1]) - 60000 && Motor_3_position >= System.Math.Abs(position[2]) - 60000)
                                if (Motor_3_position >= System.Math.Abs(position[2]) - 1000)
                                {
                                    arriva_position = true;
                                    qq1 = false;
                                }
                            }
                        }
                        break;
                    default:
                        break;
                }
            }

        }

        private void Position_return_to_zero()//位置回零
        {
            Motor_1 = true;
            Motor_2 = true;
            Motor_3 = true;
            Epos1.SetPosition(0);
            Motor_1 = false;
            Motor_2 = false;
            Motor_3 = false;
        }


        #region 被动训练曲线绘制线程事件
        //解析编码器串口数据
        public static double x_angle;
        public static double y_angle;
        public static double z_angle;
        public double x_Angle;
        public double y_Angle;
        public double z_Angle;
        private void Monitoring_return_to_zero_event1()
        {
            //CAN_disconnected.BackColor = Color.Red;
            start.WaitOne();
            serialportReceivingData.StartTxRxThread();//启动通信对象的线程接收数据
            while (true)
            {
                if (serialportReceivingData.ser_receive_queue_.Count > 0)//回零监测显示
                {
                    while (serialportReceivingData.ser_receive_queue_.Count > 0)
                        mcu_Data = serialportReceivingData.GetBoardInfo();//获取了下位机信息
                    if (mcu_Data != null)
                    {

                        //start.WaitOne();
                        Act_position_1 = mcu_Data.motor[0].Act_position;
                        x_angle = Act_position_1 * (360.0 / 4095) + xOffset;
                        x_Angle = Act_position_1 * (360.0 / 4095) + xOffset;

                        Act_position_2 = mcu_Data.motor[1].Act_position;
                        y_angle = Act_position_2 * (360.0 / 4095) + yOffset;
                        y_Angle = Act_position_2 * (360.0 / 4095) + yOffset;

                        Act_position_3 = mcu_Data.motor[2].Act_position;
                        z_angle = Act_position_3 * (360.0 / 4095) + zOffset;
                        z_Angle = Act_position_3 * (360.0 / 4095) + zOffset;
                    }
                }
            }
        }
        #endregion



        private void leftButtom_CheckedChanged(object sender, EventArgs e)
        {
            Ankle_chose1 = true;
        }

        private void rightButtom_CheckedChanged(object sender, EventArgs e)
        {
            Ankle_chose2 = true;
        }
        int mode_Selection = 0;
        //手动被动选择分组事件

        DataTable CsvRoute = new DataTable();
        double[,] doubleObj;
        #region CSV数据输入按钮事件
        private void button1_Click(object sender, EventArgs e)
        {

            //string dirPath = @"D:\Project\C#_work\Matlab_connect\Matlab_connect\data";
            //System.IO.DirectoryInfo dirInfo = new System.IO.DirectoryInfo(dirPath);
            //int result = Form1.GetFilesCount(dirInfo);
            ////获取文件夹中最后生成的路径
            //string FilePath_3 = textBox1.Text;

            string dirPath = @"D:\Project\C#_work\Matlab_connect\Matlab_connect\data";
            System.IO.DirectoryInfo dirInfo = new System.IO.DirectoryInfo(dirPath);
            int result = Form1.GetFilesCount(dirInfo);
            //获取文件夹中最后生成的路径
            string FilePath_3 = "D:\\Project\\C#_work\\Matlab_connect\\Matlab_connect\\data\\route" + result.ToString() + ".csv";

            Console.WriteLine(FilePath_3);

            tool.readCSV(FilePath_3, out CsvRoute); // 调用函数

            //readCSV("D:\\Project\\C#_work\\Matlab_connect\\Matlab_connect\\data\\route6.csv", out CsvRoute); // 调用函数

            doubleObj = new double[CsvRoute.Rows.Count,4];//保存欧式空间角度
                                                          //清空图表

            //先清空chart中的数据点进行迭代
            chart2.Series["Standard_X_degree"].Points.Clear();
            chart2.Series["Standard_Y_degree"].Points.Clear();
            chart2.Series["Standard_Z_degree"].Points.Clear();
            // 遍历table 中的所有数据
            for (int i = 0; i < CsvRoute.Rows.Count; i++)
            {
                for (int j = 0; j < CsvRoute.Columns.Count; j++)
                {
                    object obj = CsvRoute.Rows[i][j]; //i行j列的值 
                    obj = obj.ToString();
                    doubleObj[i,j] = Convert.ToDouble(obj);  
                }
                //Console.WriteLine("mulRea11= {0}", doubleObj[i,1]);
                chart2.Series["Standard_X_degree"].Points.AddXY(doubleObj[i, 0], doubleObj[i, 1]);
                chart2.Series["Standard_Y_degree"].Points.AddXY(doubleObj[i, 0], doubleObj[i, 2]);
                chart2.Series["Standard_Z_degree"].Points.AddXY(doubleObj[i, 0], doubleObj[i, 3]);
                //刷新chart控件
                chart2.Invalidate();
            }
            Console.WriteLine("数据加载完成");
        }
        #endregion

        #region 内存数据输入按钮事件
        private void button2_Click(object sender, EventArgs e)
        {

            doubleObj = dataTransfer.getRouteData();//保存欧式空间角度
            drawChart2();//进行绘图
            Console.WriteLine("数据加载完成");
        }
        #endregion

        #region 绘制路径图
        private void drawChart2()
        {
            //先清空chart中的数据点进行迭代
            chart2.Series["Standard_X_degree"].Points.Clear();
            chart2.Series["Standard_Y_degree"].Points.Clear();
            chart2.Series["Standard_Z_degree"].Points.Clear();
            for (int i = 0; i < dataTransfer.getRouteData().GetLength(0); i++)
            {
                //doubleObj 第一列为时间，第二列-第四列分别为XYZ轴
                chart2.Series["Standard_X_degree"].Points.AddXY(doubleObj[i, 0], doubleObj[i, 1]);
                chart2.Series["Standard_Y_degree"].Points.AddXY(doubleObj[i, 0], doubleObj[i, 2]);
                chart2.Series["Standard_Z_degree"].Points.AddXY(doubleObj[i, 0], doubleObj[i, 3]);
                //刷新chart控件
                chart2.Invalidate();
            }
        }
        #endregion


        //用于判断现在是第几圈
        int routeCount;
        //测试按钮，监测实时路径
        private void testBtn_Click(object sender, EventArgs e)
        {
            //用于记录单一周期循环次数
            //设置为位置模式                   
            Epos1.PositInit();
            if (this.train_count_text.Text == string.Empty)
            {
                MessageBox.Show("请输入训练次数", "提示", MessageBoxButtons.OK);
            }
            else
            {
                Console.WriteLine("开始运行");
                //运动初始位置
                //posMove(pos[0], vel[0]);
                //计数器初始化
                count = 0;
                routeCount = 1;
                //开始记录数据
                isForce = false;
                //开始绘制编码器数据图片
                isPosition = false;
                //开启电机
                isMotor_Thread = true;
                //开始绘制力位图
                //timer2.Start();
                //开启电机
                this.MotorControl_thread.Start();
                //开启力数据记录线程
                this.WriteForce_thread.Start();
            }
        }
        #region 电机运行线程
        private void motorcontrol_event()
        {
            //用于记录现在是第几次经过路线
            int routeCount = 0;
            while (true)
            { 
                if (count == pos.Count - 1)
                {
                    //Console.WriteLine("count={0}", count);
                    //如果是最后一次运动
                    if (routeCount > System.Convert.ToDouble(this.train_count_text.Text.ToString()))
                    {
                        MessageBox.Show("康复过程完成");
                        //timer1.Stop();
                        //this.isDraw_Thread = false;
                        isForce = false;
                        isPosition = false;
                        MotorControl_thread.Suspend();
                        Epos1.StopPosition();
                        routeCount = 0;
                    }
                    else
                    {
                        routeCount++;
                        Thread.Sleep(100);
                        this.WriteForce_thread.Suspend();
                        //转到路径开头开始下一次运动
                        WriteForce();
                        WriteDegree();
                        //停止记录力数据
                        isForce = false;
                        //停止记录编码器数据
                        isPosition = false;
                        //Thread.Sleep(100);
                        MessageBox.Show("一次康复路径结束，力数据保存完成");
                        Console.WriteLine("force_count={0}", force_count);
                        //清空力图表，重新开始绘图
                        x_torqure_chart.Series["X力矩"].Points.Clear();
                        y_torqure_chart.Series["Y力矩"].Points.Clear();
                        z_torqure_chart.Series["Z力矩"].Points.Clear();
                        chart2.Series["X_degree"].Points.Clear();
                        chart2.Series["Y_degree"].Points.Clear();
                        chart2.Series["Z_degree"].Points.Clear();
                        force_count = 0;
                        //count = 0;
                        //this.WriteForce_thread.Resume();
                        this.MotorControl_thread.Suspend();
                        continue;
                    }
                }
                else if (count == 0)
                {
                    //double[] vel_init = { -50, -50, 0 };
                    double[] vel_init = { -50, 0, -50 };
                    pos[0][0] += 1;
                    pos[0][1] += 1;

                    //double[] vel_init = { -50, 0, 5 };
                    //pos[0][0] += 1;
                    //pos[0][2] += 1;
                    //pos[0][2] += 1;
                    //初始点
                    posMove(pos[0], vel_init);
                    Thread.Sleep(5000);
                    if (/*(Act_position_1 * (360.0 / 4095) + xAngle) > 12*/
                        (Act_position_3 * (360.0 / 4095) + zAngle) > 0)
                    {

                        //if ((Act_position_3 * (360.0 / 4095) + zAngle) >0)
                        //{
                        //重新开始记录力数据
                        isForce = true;
                        //重新开始记录编码器数据
                        isPosition = true;
                        count++;
                        //}
                    }
                }

                else
                {
                    posMove(pos[count + 1], vel[count]);
                    count++;
                    Thread.Sleep(10);
                } 
            }
        }
        #endregion

        

        double dt = 0.1;
        double force_count = 0;
        bool isForce = false;//用于判断是否开始保存六维力传感器数据
        bool isPosition = false;//用于判断是否开始显示编码器数据
        //计时器1：用于绘图
        private void timer1_Tick(object sender, EventArgs e)
        {
            #region 力传感器
            if (chart1.Series["X轴向力"].Points.Count > 100 | chart1.Series["Y轴向力"].Points.Count > 100 | chart1.Series["Z轴向力"].Points.Count > 100 | chart1.Series["X轴力矩"].Points.Count > 100 | chart1.Series["Y轴力矩"].Points.Count > 100 | chart1.Series["Z轴力矩"].Points.Count > 100)
            {

                chart1.Series["X轴向力"].Points.RemoveAt(0);
                chart1.Series["Y轴向力"].Points.RemoveAt(0);
                chart1.Series["Z轴向力"].Points.RemoveAt(0);
                chart1.Series["X轴力矩"].Points.RemoveAt(0);
                chart1.Series["Y轴力矩"].Points.RemoveAt(0);
                chart1.Series["Z轴力矩"].Points.RemoveAt(0);

            }
            chart1.Series["X轴向力"].Points.AddXY(DateTime.Now.ToString("HH:mm:ss"), forceX);
            chart1.Series["Y轴向力"].Points.AddXY(DateTime.Now.ToString("HH:mm:ss"), forceY);
            chart1.Series["Z轴向力"].Points.AddXY(DateTime.Now.ToString("HH:mm:ss"), forceZ);
            chart1.Series["X轴力矩"].Points.AddXY(DateTime.Now.ToString("HH:mm:ss"), torqueX);
            chart1.Series["Y轴力矩"].Points.AddXY(DateTime.Now.ToString("HH:mm:ss"), torqueY);
            chart1.Series["Z轴力矩"].Points.AddXY(DateTime.Now.ToString("HH:mm:ss"), torqueZ);
            //刷新chart控件
            chart1.Invalidate();

            #endregion

            #region 结果直观显示
            x_force_text.Text = Convert.ToString(forceX);
            x_torque_text.Text = Convert.ToString(torqueX);
            x_degree_text.Text = Convert.ToString(Act_position_1 * (360.0 / 4095) + xAngle);

            y_force_text.Text = Convert.ToString(forceY);
            y_torque_text.Text = Convert.ToString(torqueY);
            y_degree_text.Text = Convert.ToString(Act_position_2 * (360.0 / 4095) + yAngle);

            z_force_text.Text = Convert.ToString(forceZ);
            z_torque_text.Text = Convert.ToString(torqueZ);
            z_degree_text.Text = Convert.ToString(Act_position_3 * (360.0 / 4095) + zAngle);
            #endregion

            #region 绝对值编码器
            if(isPosition==true)
            {
                chart2.Series["X_degree"].Points.AddXY(force_count * 0.6, Act_position_1 * (360.0 / 4095) + xAngle);
                chart2.Series["Y_degree"].Points.AddXY(force_count * 0.6, Act_position_2 * (360.0 / 4095) + yAngle);
                chart2.Series["Z_degree"].Points.AddXY(force_count * 0.6, Act_position_3 * (360.0 / 4095) + zAngle);
                //刷新chart控件
                //chart2.Invalidate();
                x_torqure_chart.Series["X力矩"].Points.AddXY(force_count * 0.67, torqueX);
                y_torqure_chart.Series["Y力矩"].Points.AddXY(force_count * 0.67, torqueY);
                z_torqure_chart.Series["Z力矩"].Points.AddXY(force_count * 0.67, torqueZ);
                force_count++;
            }
            #endregion

        }

        #region 电机运行一次
        private void posMove(double[] position, double[] velocity)
        {
            MachineControl.Motor_1 = true;
            Epos1.SetPosition((int)position[0]);//推杆1的位置
            Thread.Sleep(1);
            MachineControl.Motor_1 = false;

            MachineControl.Motor_2 = true;
            Epos1.SetPosition((int)position[1]);//推杆2的位置
            Thread.Sleep(1);
            MachineControl.Motor_2 = false;

            MachineControl.Motor_3 = true;
            Epos1.SetPosition((int)position[2]);//主电机的位置
            Thread.Sleep(1);
            MachineControl.Motor_3 = false;

            //发送速度位置信息
            MachineControl.Motor_1 = true;
            Epos1.StartPositionVelocity1((int)velocity[0]);
            MachineControl.Motor_1 = false;

            MachineControl.Motor_2 = true;
            Epos1.StartPositionVelocity1((int)velocity[1]);
            MachineControl.Motor_2 = false;

            MachineControl.Motor_3 = true;
            Epos1.StartPositionVelocity1((int)velocity[2]);
            MachineControl.Motor_3 = false;

            //电机开始运行
            MachineControl.Motor_1 = true;
            MachineControl.Motor_2 = true;
            MachineControl.Motor_3 = true;
            Epos1.StartPosition();
            MachineControl.Motor_1 = false;
            MachineControl.Motor_2 = false;
            MachineControl.Motor_3 = false;
            //Console.WriteLine("route= {0}, {1}, {2},velocity={3}, {4}, {5}", (int)position[0], (int)position[1], (int)position[2], (int)velocity[0], (int)velocity[1],(int)velocity[2]);
        }
        #endregion

        


       private void updateRoute()
       {
            pos.Clear();
            vel.Clear();
            singleRoute.Clear();
            doubleObj = dataTransfer.getRouteData();//保存欧式空间角度
            for (int i = 0; i < doubleObj.GetLength(0); i+=2)
            {
                double[] testPosition = new double[3];
                //进行位置逆解
                testPosition[0] = KinematicsHelper.InverseSolution(doubleObj[i, 1] * 1.1 + 2, -doubleObj[i, 2] - 5, 0)[0];          //1号电机位置
                testPosition[1] = KinematicsHelper.InverseSolution(doubleObj[i, 1] * 1.1 + 2, -doubleObj[i, 2] - 5, 0)[1];          //2号电机位置
                testPosition[2] = KinematicsHelper.InverseSolution(doubleObj[i, 1] * 1.1 + 2, -doubleObj[i, 2] - 5, 0)[2];          //3号电机位置
                //加载路径数据，数据经过逆解得出
                pos.Add(testPosition);
                double[] tempRoute = new double[3] { doubleObj[i, 1] * 1.1 + 2, -doubleObj[i, 2] - 5, 0 };
                singleRoute.Add(tempRoute);
                Console.WriteLine("route={0},{1},{2}", testPosition[0], testPosition[1], testPosition[2]);
            }
            Console.WriteLine("路径数据更新完成，选取其中的{0}个数据", singleRoute.Count);
            //计算速度
            double Velocity_X = new double();
            double Velocity_Y = new double();
            double Velocity_Z = new double();
            double[] temp = new double[3];
            for (int i = 0; i < singleRoute.Count - 1; i++)
            {
                //velocityJacobi1.X_axis = true;
                Velocity_X = (singleRoute[i + 1][0] - singleRoute[i][0]) / 0.2;
                Velocity_Y = (singleRoute[i + 1][1] - singleRoute[i][1]) / 0.2;
                Velocity_Z = (singleRoute[i + 1][2] - singleRoute[i][2]) / 0.2;
                VJ.degree_x = (float)singleRoute[i + 1][0];//角度记录，关节运动角度的记录,目标点
                VJ.degree_y = (float)singleRoute[i + 1][1];
                VJ.degree_z = (float)singleRoute[i + 1][2];
                VJ.speed_x = Velocity_X;
                VJ.speed_y = Velocity_Y;
                VJ.speed_z = Velocity_Z;
                Console.WriteLine("route={0},{1},{2}", singleRoute[i + 1][0], singleRoute[i + 1][1], singleRoute[i + 1][2]);
                //进行雅可比矩阵计算
                VJ.analysis();
                //保存速度（比路径数据少一个）
                vel.Add(VJ.speed);
                Console.WriteLine("vel={0},{1},{2}", VJ.speed[0], VJ.speed[1], VJ.speed[2]);
            }
            Console.WriteLine("速度数据更新完成，选取其中的{0}个数据", vel.Count);
            drawChart2();//进行绘图
        }

        #region 更新路径按键事件
        private void updateRouteBtn_Click(object sender, EventArgs e)
        {
            pos.Clear();
            vel.Clear();
            singleRoute.Clear();
            //= new int[3,CsvRoute.Rows.Count];
            //每10个数取一次
            for (int i = 0; i < doubleObj.GetLength(0); i +=2)
            {
                double[] testPosition = new double[3];
                //进行位置逆解
                //testPosition[0] = KinematicsHelper.InverseSolution(doubleObj[i, 1], -doubleObj[i, 2], doubleObj[i, 3])[0];          //1号电机位置
                //testPosition[1] = KinematicsHelper.InverseSolution(doubleObj[i, 1], -doubleObj[i, 2], doubleObj[i, 3])[1];          //2号电机位置
                //testPosition[2] = KinematicsHelper.InverseSolution(doubleObj[i, 1], -doubleObj[i, 2], doubleObj[i, 3])[2];          //3号电机位置

                //testPosition[0] = KinematicsHelper.InverseSolution(doubleObj[i, 1] * 1.1 + 2, -doubleObj[i, 2], 0)[0];          //1号电机位置
                //testPosition[1] = KinematicsHelper.InverseSolution(doubleObj[i, 1] * 1.1 + 2, -doubleObj[i, 2], 0)[1];          //2号电机位置
                //testPosition[2] = KinematicsHelper.InverseSolution(doubleObj[i, 1] * 1.1 + 2, -doubleObj[i, 2], 0)[2];          //3号电机位置

                testPosition[0] = KinematicsHelper.InverseSolution(doubleObj[i, 1] , 0, doubleObj[i, 3])[0];          //1号电机位置
                testPosition[1] = KinematicsHelper.InverseSolution(doubleObj[i, 1] , 0, doubleObj[i, 3])[1];          //2号电机位置
                testPosition[2] = KinematicsHelper.InverseSolution(doubleObj[i, 1] , 0, doubleObj[i, 3])[2];

                //testPosition[0] = KinematicsHelper.InverseSolution(0, 0, 1.4 * doubleObj[i, 3])[0];          //1号电机位置
                //testPosition[1] = KinematicsHelper.InverseSolution(0, 0, 1.4 * doubleObj[i, 3])[1];          //2号电机位置
                //testPosition[2] = KinematicsHelper.InverseSolution(0, 0, 1.4 * doubleObj[i, 3])[2];

                //加载路径数据，数据经过逆解得出
                pos.Add(testPosition);
                double[] tempRoute = new double[3] { doubleObj[i, 1], 0, doubleObj[i, 3] };

                //double[] tempRoute = new double[3] {  doubleObj[i, 1] * 1.1 + 2, -doubleObj[i, 2] , 0 };

                //double[] tempRoute = new double[3] { 0, 0, doubleObj[i, 3] };
                singleRoute.Add(tempRoute);
                Console.WriteLine("route={0},{1},{2}", testPosition[0], testPosition[1], testPosition[2]);
            }
            Console.WriteLine("路径数据更新完成，选取其中的{0}个数据", singleRoute.Count);
            //计算速度
            double Velocity_X = new double();
            double Velocity_Y = new double();
            double Velocity_Z = new double();
            double[] temp = new double[3];
            for (int i = 0; i < singleRoute.Count - 1; i++)
            {
                Velocity_X = (singleRoute[i + 1][0] - singleRoute[i][0]) / 0.2;
                Velocity_Y = (singleRoute[i + 1][1] - singleRoute[i][1]) / 0.2;
                Velocity_Z = (singleRoute[i + 1][2] - singleRoute[i][2]) / 0.2;

                VJ.degree_x = (float)singleRoute[i + 1][0];//角度记录，关节运动角度的记录,目标点
                VJ.degree_y = (float)singleRoute[i + 1][1];
                VJ.degree_z = (float)singleRoute[i + 1][2];
                VJ.speed_x = Velocity_X;
                VJ.speed_y = Velocity_Y;
                VJ.speed_z = Velocity_Z;


                //Velocity_X = 0;
                //Velocity_Y = 0;
                //Velocity_Z = (singleRoute[i + 1][2] - singleRoute[i][2]) / 0.01; ;

                //VJ.degree_x = (float)singleRoute[0][0];//角度记录，关节运动角度的记录,目标点
                //VJ.degree_y = (float)singleRoute[0][1];
                //VJ.degree_z = (float)singleRoute[i + 1][2];
                //VJ.speed_x = Velocity_X;
                //VJ.speed_y = Velocity_Y;
                //VJ.speed_z = Velocity_Z;

                Console.WriteLine("route={0},{1},{2}", singleRoute[i + 1][0], singleRoute[i + 1][1], singleRoute[i + 1][2]);

                //进行雅可比矩阵计算
                VJ.analysis();
                //Console.WriteLine("route={0},{1},{2},vel={3},{4},{5}", pos[i][0], pos[i][1], pos[i][2],
                //VJ.speed[0], VJ.speed[1], VJ.speed[2]);
                //保存速度（比路径数据少一个）
                if (VJ.speed[2] > 0)
                {
                    VJ.speed[2] = 10;
                }
                else
                {
                    VJ.speed[2] = -10;
                }
                vel.Add(VJ.speed);
                Console.WriteLine("vel={0},{1},{2}", VJ.speed[0], VJ.speed[1], VJ.speed[2]);
            }
                Console.WriteLine("速度数据更新完成，选取其中的{0}个数据", vel.Count);

            

        }
        #endregion



        #region 回零按钮事件
        private void BackToZeroBtn_Click(object sender, EventArgs e)
        {
            MessageBox.Show("即将进行设备回零，请耐心等待");
            Epos1.VelocityInit();                                 //速度初始化
            Zeroing_thread = new Thread(Zeroing_thread_event);    //启用归零线程
            Zeroing_thread.IsBackground = true;
            fa.Set();
            Zeroing_thread.Start();
        }
        #endregion

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
                    Epos1.SetVelocity(-20);
                    while (true)
                    {
                        if (Act_position_2 < 2475.0)
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
                    Epos1.SetVelocity(20);
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
                        Act_position_1 = mcu_Data.motor[0].Act_position;

                        Act_position_2 = mcu_Data.motor[1].Act_position;

                        Act_position_3 = mcu_Data.motor[2].Act_position;

                        X_axis_zeroing_monitoring.Text = Act_position_1.ToString("0.00") + "mm";
                        Thread.Sleep(5);
                        Y_axis_zeroing_monitoring.Text = Act_position_2.ToString("0.00") + "mm";
                        Thread.Sleep(5);
                        Z_axis_zeroing_monitoring.Text = Act_position_3.ToString("0.00") + "mm";
                        Thread.Sleep(5);
                        //X = Act_position_1;
                        //Z = Act_position_2;
                        //X = Act_position_3;
                    }

                }
            }

        }
        #endregion


        #region 监测回零按钮事件
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
            }
            #endregion
        }
        #endregion

        #region 图表初始化事件
        private void InitChart()
        {

            #region chart1：六维力传感器
            //dataTable.Columns.Add("时间", typeof(String));
            //dataTable.Columns.Add("数据", typeof(float));

            this.chart1.ChartAreas.Clear();
            ChartArea chartArea1 = new ChartArea("C1");//实例化
            this.chart1.ChartAreas.Add(chartArea1);//向添加绘图区域
            //定义存储和显示点的容器
            this.chart1.Series.Clear();
            Series series1 = new Series("X轴向力");//实例化一条曲线
            Series series2 = new Series("Y轴向力");//实例化一条曲线
            Series series3 = new Series("Z轴向力");//实例化一条曲线
            Series series4 = new Series("X轴力矩");//实例化一条曲线
            Series series5 = new Series("Y轴力矩");//实例化一条曲线
            Series series6 = new Series("Z轴力矩");//实例化一条曲线            
            series1.ChartArea = "C1";//添加到
            series2.ChartArea = "C1";//添加到
            series3.ChartArea = "C1";//添加到
            series4.ChartArea = "C1";//添加到
            series5.ChartArea = "C1";//添加到
            series6.ChartArea = "C1";//添加到
            this.chart1.Series.Add(series1);
            this.chart1.Series.Add(series2);
            this.chart1.Series.Add(series3);
            this.chart1.Series.Add(series4);
            this.chart1.Series.Add(series5);
            this.chart1.Series.Add(series6);
            //设置图表显示样式
            this.chart1.ChartAreas[0].AxisY.Minimum = -100;
            this.chart1.ChartAreas[0].AxisY.Maximum = 100;
            this.chart1.ChartAreas[0].AxisX.Interval = 1;//设置轴间隔
            this.chart1.ChartAreas[0].AxisX.MajorGrid.LineColor = System.Drawing.Color.Silver;
            this.chart1.ChartAreas[0].AxisY.MajorGrid.LineColor = System.Drawing.Color.Silver;
            this.chart1.ChartAreas[0].AxisY.IsLabelAutoFit = true;
            this.chart1.ChartAreas[0].AxisY.IsStartedFromZero = true;
            this.chart1.Titles.Clear();
            this.chart1.Titles.Add("六维力传感器");
            //this.chart1.Titles[0].Text = "XXX显示";
            this.chart1.Titles[0].ForeColor = Color.RoyalBlue;
            this.chart1.Titles[0].Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
            //设置图表显示样式
            this.chart1.Series[0].Color = Color.Red;
            this.chart1.Series[1].Color = Color.Blue;
            this.chart1.Series[2].Color = Color.Green;
            this.chart1.Series[3].Color = Color.Black;
            this.chart1.Series[4].Color = Color.Transparent;
            this.chart1.Series[5].Color = Color.DimGray;
            //this.chart1.Titles[0].Text = string.Format("标题");
            this.chart1.Series[0].ChartType = SeriesChartType.Spline;//指定图标样式
            this.chart1.Series[1].ChartType = SeriesChartType.Spline;//指定图标样式
            this.chart1.Series[2].ChartType = SeriesChartType.Spline;//指定图标样式
            this.chart1.Series[3].ChartType = SeriesChartType.Spline;//指定图标样式
            this.chart1.Series[4].ChartType = SeriesChartType.Spline;//指定图标样式
            this.chart1.Series[5].ChartType = SeriesChartType.Spline;//指定图标样式

            this.chart1.Series[0].Points.Clear();
            this.chart1.Series[1].Points.Clear();
            this.chart1.Series[2].Points.Clear();
            this.chart1.Series[3].Points.Clear();
            this.chart1.Series[4].Points.Clear();
            this.chart1.Series[5].Points.Clear();
            //invokeChartData[0] =  chart1;
            //invokeChartData[1] = dataTable;
            #endregion

            #region cahrt2:编码器传感器
            this.chart2.ChartAreas.Clear();
            ChartArea chartArea2 = new ChartArea("C2");//实例化
            this.chart2.ChartAreas.Add(chartArea2);//向添加绘图区域
            //定义存储和显示点的容器
            this.chart2.Series.Clear();
            Series series7 = new Series("Standard_X_degree");//实例化一条曲线,作为基础路线
            Series series8 = new Series("Standard_Y_degree");//实例化一条曲线,作为基础路线
            Series series9 = new Series("Standard_Z_degree");//实例化一条曲线,作为基础路线

            Series series10 = new Series("X_degree");//实例化一条曲线
            Series series11 = new Series("Y_degree");//实例化一条曲线
            Series series12 = new Series("Z_degree");//实例化一条曲线

            series7.ChartArea = "C2";//添加到
            series8.ChartArea = "C2";//添加到
            series9.ChartArea = "C2";//添加到
            series10.ChartArea = "C2";//添加到
            series11.ChartArea = "C2";//添加到
            series12.ChartArea = "C2";//添加到   
            this.chart2.Series.Add(series7);
            this.chart2.Series.Add(series8);
            this.chart2.Series.Add(series9);
            this.chart2.Series.Add(series10);
            this.chart2.Series.Add(series11);
            this.chart2.Series.Add(series12);
            //设置图表显示样式
            this.chart2.ChartAreas[0].AxisY.Minimum = -20;
            this.chart2.ChartAreas[0].AxisY.Maximum = 25;
            this.chart2.ChartAreas[0].AxisX.Interval = 10;//设置轴间隔
            this.chart2.ChartAreas[0].AxisX.MajorGrid.LineColor = System.Drawing.Color.Silver;
            this.chart2.ChartAreas[0].AxisY.IsLabelAutoFit = true;
            this.chart2.ChartAreas[0].AxisY.IsStartedFromZero = false;
            this.chart1.ChartAreas[0].AxisY.MajorGrid.LineColor = System.Drawing.Color.Silver;
            //设置标题
            this.chart2.Titles.Clear();
            this.chart2.Titles.Add("degree");
            //this.chart1.Titles[0].Text = "XXX显示";
            this.chart2.Titles[0].ForeColor = Color.RoyalBlue;
            this.chart2.Titles[0].Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
            //设置图表显示样式           
            this.chart2.Series[0].Color = Color.Red;
            this.chart2.Series[1].Color = Color.Blue;
            this.chart2.Series[2].Color = Color.Green;
            this.chart2.Series[3].Color = Color.DarkGreen;
            this.chart2.Series[4].Color = Color.GreenYellow;
            this.chart2.Series[5].Color = Color.DimGray;

            this.chart2.Series[0].ChartType = SeriesChartType.Spline;//指定图标样式
            this.chart2.Series[1].ChartType = SeriesChartType.Spline;//指定图标样式
            this.chart2.Series[2].ChartType = SeriesChartType.Spline;//指定图标样式
            this.chart2.Series[3].ChartType = SeriesChartType.Spline;//指定图标样式
            this.chart2.Series[4].ChartType = SeriesChartType.Spline;//指定图标样式
            this.chart2.Series[5].ChartType = SeriesChartType.Spline;//指定图标样式

            this.chart2.Series[0].Points.Clear();
            this.chart2.Series[1].Points.Clear();
            this.chart2.Series[2].Points.Clear();
            this.chart2.Series[3].Points.Clear();
            this.chart2.Series[4].Points.Clear();
            this.chart2.Series[5].Points.Clear();
            #endregion
             
            #region torquex-z:单独观察x-z轴力矩
            this.x_torqure_chart.ChartAreas.Clear();
            this.y_torqure_chart.ChartAreas.Clear();
            this.z_torqure_chart.ChartAreas.Clear();
            ChartArea x_torqure_Area = new ChartArea("X1");//实例化
            ChartArea y_torqure_Area = new ChartArea("Y1");//实例化
            ChartArea z_torqure_Area = new ChartArea("Z1");//实例化

            this.x_torqure_chart.ChartAreas.Add(x_torqure_Area);//向添加绘图区域
            this.y_torqure_chart.ChartAreas.Add(y_torqure_Area);//向添加绘图区域
            this.z_torqure_chart.ChartAreas.Add(z_torqure_Area);//向添加绘图区域

            //定义存储和显示点的容器
            this.x_torqure_chart.Series.Clear();
            this.y_torqure_chart.Series.Clear();
            this.z_torqure_chart.Series.Clear();
            //实例化一条曲线
            Series x_standard_series = new Series("X力矩期望");
            Series x_sigmaup_series = new Series("X力矩上方差");
            Series x_sigmadown_series = new Series("X力矩下方差");
            Series x_real_serise = new Series("X力矩");
            Series y_standard_series = new Series("Y力矩期望");
            Series y_sigmaup_series = new Series("Y力矩上方差");
            Series y_sigmadown_series = new Series("Y力矩下方差");
            Series y_real_serise = new Series("Y力矩");
            Series z_standard_series = new Series("Z力矩期望");
            Series z_sigmaup_series = new Series("Z力矩上方差");
            Series z_sigmadown_series = new Series("Z力矩下方差");
            Series z_real_serise = new Series("Z力矩");


            //添加到
            this.x_torqure_chart.Series.Add(x_sigmaup_series);
            this.x_torqure_chart.Series.Add(x_standard_series);
            this.x_torqure_chart.Series.Add(x_sigmadown_series);
            this.x_torqure_chart.Series.Add(x_real_serise);
            this.y_torqure_chart.Series.Add(y_sigmaup_series);
            this.y_torqure_chart.Series.Add(y_standard_series);
            this.y_torqure_chart.Series.Add(y_sigmadown_series);
            this.y_torqure_chart.Series.Add(y_real_serise);
            this.z_torqure_chart.Series.Add(z_sigmaup_series);
            this.z_torqure_chart.Series.Add(z_standard_series);
            this.z_torqure_chart.Series.Add(z_sigmadown_series);
            this.z_torqure_chart.Series.Add(z_real_serise);
            //指定图标样式
            this.x_torqure_chart.Series[0].ChartType = SeriesChartType.Spline;
            this.x_torqure_chart.Series[1].ChartType = SeriesChartType.Spline;
            this.x_torqure_chart.Series[2].ChartType = SeriesChartType.Spline;
            this.x_torqure_chart.Series[3].ChartType = SeriesChartType.Spline;
            this.y_torqure_chart.Series[0].ChartType = SeriesChartType.Spline;
            this.y_torqure_chart.Series[1].ChartType = SeriesChartType.Spline;
            this.y_torqure_chart.Series[2].ChartType = SeriesChartType.Spline;
            this.y_torqure_chart.Series[3].ChartType = SeriesChartType.Spline;
            this.z_torqure_chart.Series[0].ChartType = SeriesChartType.Spline;
            this.z_torqure_chart.Series[1].ChartType = SeriesChartType.Spline;
            this.z_torqure_chart.Series[2].ChartType = SeriesChartType.Spline;
            this.z_torqure_chart.Series[3].ChartType = SeriesChartType.Spline;
            //X方向力矩
            //this.x_torqure_chart.ChartAreas[0].AxisY.Minimum = -2;
            //this.x_torqure_chart.ChartAreas[0].AxisY.Maximum = 3.5;
            this.x_torqure_chart.ChartAreas[0].AxisX.Interval = 100;//设置轴间隔
            this.x_torqure_chart.ChartAreas[0].AxisY.IsLabelAutoFit = true;
            this.x_torqure_chart.ChartAreas[0].AxisY.IsStartedFromZero = true;
            this.x_torqure_chart.Titles.Clear();
            this.x_torqure_chart.Titles.Add("xtorque");
            //Y方向力矩
            //this.y_torqure_chart.ChartAreas[0].AxisY.Minimum = 0;
            //this.y_torqure_chart.ChartAreas[0].AxisY.Maximum = 4.5;
            this.y_torqure_chart.ChartAreas[0].AxisX.Interval = 100;//设置轴间隔
            this.y_torqure_chart.ChartAreas[0].AxisY.IsLabelAutoFit = true;
            this.y_torqure_chart.ChartAreas[0].AxisY.IsStartedFromZero = true;
            this.y_torqure_chart.Titles.Clear();
            this.y_torqure_chart.Titles.Add("ytorque");
            //Z方向力矩
            //this.z_torqure_chart.ChartAreas[0].AxisY.Minimum = -1;
            //this.z_torqure_chart.ChartAreas[0].AxisY.Maximum = 1;
            this.z_torqure_chart.ChartAreas[0].AxisX.Interval = 100;//设置轴间隔
            this.z_torqure_chart.ChartAreas[0].AxisY.IsLabelAutoFit = true;
            this.z_torqure_chart.ChartAreas[0].AxisY.IsStartedFromZero = true;
            this.z_torqure_chart.Titles.Clear();
            this.z_torqure_chart.Titles.Add("ztorque");
            #endregion
        }

        #endregion




        //操作电机
        private void timer2_Tick(object sender, EventArgs e)
        {
            #region 绝对值编码器
            if (isPosition == true)
            {
                chart2.Series["X_degree"].Points.AddXY(force_count * 0.1, Act_position_1 * (360.0 / 4095) + xAngle);
                chart2.Series["Y_degree"].Points.AddXY(force_count * 0.1, Act_position_2 * (360.0 / 4095) + yAngle);
                chart2.Series["Z_degree"].Points.AddXY(force_count * 0.1, Act_position_3 * (360.0 / 4095) + zAngle);
                //刷新chart控件
                //chart2.Invalidate();
                x_torqure_chart.Series["X力矩"].Points.AddXY(force_count * 0.1, torqueX);
                y_torqure_chart.Series["Y力矩"].Points.AddXY(force_count * 0.1, torqueY);
                z_torqure_chart.Series["Z力矩"].Points.AddXY(force_count * 0.1, torqueZ);
                force_count++;
            }
            #endregion
        }

        #region 保存CSV z_score数据
        private void WriteZscore()
        {

            string dirPath = @"D:\Project\C#_work\Matlab_connect\Matlab_connect\zscore_data";
            System.IO.DirectoryInfo dirInfo = new System.IO.DirectoryInfo(dirPath);
            int result = GetFilesCount(dirInfo);
            string path = "D:\\Project\\C#_work\\Matlab_connect\\Matlab_connect\\zscore_data\\route" + (result + 1).ToString() + ".csv";
            Console.WriteLine(path);


            if (!File.Exists(path))
                File.Create(path).Close();

            StreamWriter sw = new StreamWriter(path, true, Encoding.UTF8);

            //写入title
            sw.Write("No." + "," + "z_score_x" + "," + "z_score_y" + ','+ "\t\n");

            for (int row = 0; row < z_score_x.GetLength(0); row++)
            {
                string str =row.ToString()+","+ z_score_x[row].ToString() + "," + z_score_y[row].ToString() + ',' + "\t\n";
                sw.Write(str);
            }

            sw.Flush();
            sw.Close();

        }
        #endregion


        #region 保存CSV编码器数据
        private void WriteDegree()
        {

            string dirPath = @"D:\Project\C#_work\Matlab_connect\Matlab_connect\degree_data";
            System.IO.DirectoryInfo dirInfo = new System.IO.DirectoryInfo(dirPath);
            int result = GetFilesCount(dirInfo);
            string path = "D:\\Project\\C#_work\\Matlab_connect\\Matlab_connect\\degree_data\\route" + (result + 1).ToString() + ".csv";
            Console.WriteLine(path);


            if (!File.Exists(path))
                File.Create(path).Close();

            StreamWriter sw = new StreamWriter(path, true, Encoding.UTF8);
            sql.OpenList();
            //写入title
            sw.Write("degreeX" + ","+ "degreeY" + ","+ "degreeZ" +  "\t\n");

            for (int row = 0; row < angleList.Count; row++)
            {
                string str = angleList[row][0].ToString() + "," + angleList[row][1].ToString() + ","+ angleList[row][2].ToString() + "\t\n";
                sql.InsertRouteData("encoder_route_data", angleList[row][0], angleList[row][0], angleList[row][0], routeCount);
                sw.Write(str);
            }
            
            
            sql.CloseList();
            sw.Flush();
            sw.Close();

        }
        #endregion


        #region 保存CSV力数据
        private void WriteForce()
        {

            string dirPath = @"D:\Project\C#_work\Matlab_connect\Matlab_connect\force_data";
            System.IO.DirectoryInfo dirInfo = new System.IO.DirectoryInfo(dirPath);
            int result = GetFilesCount(dirInfo);
            string path = "D:\\Project\\C#_work\\Matlab_connect\\Matlab_connect\\force_data\\route" + (result + 1).ToString() + ".csv";
            Console.WriteLine(path);


            if (!File.Exists(path))
                File.Create(path).Close();

            StreamWriter sw = new StreamWriter(path, true, Encoding.UTF8);

            //写入title
            sw.Write("forceX" + "," + "forceY" + "," + "forceZ" + "," + "torqueX" + "," + "torqueY" + "," + "torqueZ" + "\t\n");

            Console.WriteLine("forceList.Count={0}", forceList.Count);

            for (int row = 0; row < forceList.Count; row++)
            {
                string str = forceList[row][0].ToString() + "," + forceList[row][1].ToString() + "," + forceList[row][2].ToString() + ","
                    + forceList[row][3].ToString() + "," + forceList[row][4].ToString() + "," + forceList[row][5].ToString() + "," + "\t\n";
                sw.Write(str);
            }

            sw.Flush();
            sw.Close();

        }
        #endregion

        //获取文件夹中文件数量
        public static int GetFilesCount(DirectoryInfo dirInfo)
        {

            int totalFile = 0;
            //totalFile += dirInfo.GetFiles().Length;//获取全部文件
            totalFile += dirInfo.GetFiles("*.csv").Length;//获取某种格式
            foreach (System.IO.DirectoryInfo subdir in dirInfo.GetDirectories())
            {
                totalFile += GetFilesCount(subdir);
            }
            return totalFile;
        }

        bool isDraw_Thread = false;//用于判断绘图线程是否开启
        bool isMotor_Thread = false;//用于判断电机线程是否开启
        #region 设备初始化按钮事件
        private void init_btn_Click(object sender, EventArgs e)
        {

                //serialPort1.ReceivedBytesThreshold = 31;
                //serialPort1.DtrEnable = true;
                //serialPort1.RtsEnable = true;
                //serialPort1.ReadTimeout = 1000;
                //serialPort1.Open();

            serialPort1.Write("AT+SGDM=(A01,A02,A03,A04,A05,A06);E;1(WMA:1)\r\n");
            System.Threading.Thread.Sleep(10);
            serialPort1.Write("AT+GSD\r\n");
            isDraw_Thread = true;
            //this.DrawChart_thread = new Thread(drawchart_event);
            //this.DrawChart_thread.IsBackground = true;
            //this.DrawChart_thread.Start();
            timer1.Start();
            //MessageBox.Show("设备初始化完成", "提示");
        }
        #endregion

        #region 绘图与数据保存线程
        private void drawchart_event()
        {
            while (true)
            {
                if(isDraw_Thread == true)
                {
                    #region 力传感器
                    if (chart1.Series["X轴向力"].Points.Count > 10 | chart1.Series["Y轴向力"].Points.Count > 10 | chart1.Series["Z轴向力"].Points.Count > 10 | chart1.Series["X轴力矩"].Points.Count > 10 | chart1.Series["Y轴力矩"].Points.Count > 10 | chart1.Series["Z轴力矩"].Points.Count > 10)
                    {

                        chart1.Series["X轴向力"].Points.RemoveAt(0);
                        chart1.Series["Y轴向力"].Points.RemoveAt(0);
                        chart1.Series["Z轴向力"].Points.RemoveAt(0);
                        chart1.Series["X轴力矩"].Points.RemoveAt(0);
                        chart1.Series["Y轴力矩"].Points.RemoveAt(0);
                        chart1.Series["Z轴力矩"].Points.RemoveAt(0);

                    }
                    chart1.Series["X轴向力"].Points.AddXY(DateTime.Now.ToString("HH:mm:ss"), forceX);
                    chart1.Series["Y轴向力"].Points.AddXY(DateTime.Now.ToString("HH:mm:ss"), forceY);
                    chart1.Series["Z轴向力"].Points.AddXY(DateTime.Now.ToString("HH:mm:ss"), forceZ);
                    chart1.Series["X轴力矩"].Points.AddXY(DateTime.Now.ToString("HH:mm:ss"), torqueX);
                    chart1.Series["Y轴力矩"].Points.AddXY(DateTime.Now.ToString("HH:mm:ss"), torqueY);
                    chart1.Series["Z轴力矩"].Points.AddXY(DateTime.Now.ToString("HH:mm:ss"), torqueZ);
                    //刷新chart控件
                    chart1.Invalidate();
                    if (isForce == true)
                    {
                        double[] temp = { forceX, forceY, forceZ, torqueX, torqueY, torqueZ };
                        forceList.Add(temp);
                        //Console.WriteLine("数列中有{0}个数", forceList.Count());
                    }
                    #endregion

                    #region 结果直观显示
                    x_force_text.Text = Convert.ToString(forceX);
                    x_torque_text.Text = Convert.ToString(torqueX);
                    x_degree_text.Text = Convert.ToString(Act_position_1 * (360.0 / 4095) + xAngle);

                    y_force_text.Text = Convert.ToString(forceX);
                    y_torque_text.Text = Convert.ToString(torqueY);
                    y_degree_text.Text = Convert.ToString(Act_position_2 * (360.0 / 4095) + yAngle);

                    z_force_text.Text = Convert.ToString(forceX);
                    z_torque_text.Text = Convert.ToString(torqueZ);
                    z_degree_text.Text = Convert.ToString(Act_position_3 * (360.0 / 4095) + zAngle);
                    #endregion
                    //延时40ms
                    System.Threading.Thread.Sleep(40);
                }
                else
                {
                    return;
                }

            }
        }
        #endregion

        double[] z_score_x = new double[1000];
        double[] z_score_y = new double[1000];
        double[] z_score_z = new double[1000];

        private void route_change_btn_Click(object sender, EventArgs e)
        {
            //计算z_score
            //for (int i = 0; i < 1000; i++)
            //{
            //    double mean_x = Convert.ToDouble(x_torque_datatable.Rows[i][1]);
            //    double up_sigma_x = Convert.ToDouble(x_torque_datatable.Rows[i][2]);
            //    double down_sigma_x = Convert.ToDouble(x_torque_datatable.Rows[i][3]);
            //    z_score_x[i] = 1.5 * (mean_x - forceList[(int)i * forceList.Count / 1000][3]) / (up_sigma_x - down_sigma_x);
            //    if (z_score_x[i] > 1)
            //    {
            //        int flag = 1;
            //        Console.WriteLine("x轴需要向下修正的点为：{0}", i);
            //        if (i > 100 && i < 900)
            //        {
            //            int num = (int)i / 10;
            //            dataTransfer.setInsertPoint(num * 10, 1, 0, flag, 0);
            //        }

            //    }
            //    else if (z_score_x[i] < -1)
            //    {
            //        int flag = 0;
            //        Console.WriteLine("x轴需要向上修正的点为：{0}", i);
            //        if (i > 100 && i < 900)
            //        {
            //            int num = (int)i / 10;
            //            dataTransfer.setInsertPoint(num * 10, 1, 0, flag, 0);
            //        }
            //    }
            //    z_score_y[i] = 0;
                //double mean_y = Convert.ToDouble(y_torque_datatable.Rows[i][1]);
                //double up_sigma_y = Convert.ToDouble(y_torque_datatable.Rows[i][2]);
                //double down_sigma_y = Convert.ToDouble(y_torque_datatable.Rows[i][3]);
                //z_score_y[i] = 2 * (mean_y - forceList[(int)i * forceList.Count / 1000][4]) / (up_sigma_y - down_sigma_y);
                //if (z_score_y[i] > 1)
                //{
                //    int flag = 1;
                //    Console.WriteLine("y轴需要向下修正的点为：{0}", i);
                //    //for (int j = -2; j < 3; j++)
                //    //{
                //    //    dataTransfer.setInsertPoint(i + j, 1, 0, flag, 1);
                //    //}
                //    dataTransfer.setInsertPoint(i, 1, 0, flag, 0);
                //}
                //else if (z_score_y[i] < -1)
                //{
                //    int flag = 0;
                //    Console.WriteLine("y轴需要向上修正的点为：{0}", i);
                //    //for (int j = -2; j < 3; j++)
                //    //{
                //    //    dataTransfer.setInsertPoint(i + j, 1, 0, flag, 1);
                //    //}
                //    dataTransfer.setInsertPoint(i, 1, 0, flag, 1);
                //}
            //}

            if (is_route_change.Checked==true)
            {
                //WriteZscore();
                //触发事件
                setFormInsertValue();
                doubleObj = dataTransfer.getRouteData();//保存欧式空间角度
                InsertPoint_x.Clear();
                InsertPoint_y.Clear();
                InsertPoint_z.Clear();
                //计算更新的数据
                updateRoute();
                count = 0;
                //重新开启电机和力写入线程
                this.MotorControl_thread.Resume();
                this.WriteForce_thread.Resume();
                dataTransfer.CleanInsertPoint();
                drawChart2();
                forceList.Clear();
                angleList.Clear();
            }
            else
            {
                //WriteZscore();
                count = 0;
                forceList.Clear();
                angleList.Clear();
                dataTransfer.CleanInsertPoint();
                this.MotorControl_thread.Resume();
                this.WriteForce_thread.Resume();
            }
        }

        private void route_change()
        {
            dataTransfer.dtInsertTimeSort();
            //Console.WriteLine("flag={0},{1},{2} ", dataTransfer.getFlag(0), dataTransfer.getFlag(1), dataTransfer.getFlag(2));
            //Console.WriteLine("count={0} ", InsertPoint_x.Count);
            if (InsertPoint_x.Count == 0 && InsertPoint_y.Count == 0 && InsertPoint_y.Count == 0)
            {
                MessageBox.Show("未输入插入点", "提示");
            }
            else
            {
                //触发事件
                setFormInsertValue();
                Thread.Sleep(100);
                doubleObj = dataTransfer.getRouteData();//保存欧式空间角度
                drawChart2();
                InsertPoint_x.Clear();
                InsertPoint_y.Clear();
                InsertPoint_z.Clear();
            }
        }


        int ChangePoint;//进行参数初始化
        List<double> InsertPoint_x = new List<double>();
        List<double> InsertPoint_y = new List<double>();
        List<double> InsertPoint_z = new List<double>();


        #region 停止设备运动按钮事件
        private void stopBtn_Click(object sender, EventArgs e)
        {
            //计时器暂停
            //timer1.Stop();
            //timer2.Stop();
            if (isMotor_Thread == true)
            {
                MotorControl_thread.Suspend();
                //停止电机
                Epos1.StopPosition();
                //停止电机驱动
                isMotor_Thread = false;
                //停止力数据记录
                this.isForce = false;
                this.stopBtn.Enabled = false;
                this.continueBtn.Enabled = true;
            }
            else
            {
                MessageBox.Show("电机已经是停止状态了");
            }
        }
        #endregion

        #region 继续设备运行按钮
        private void continueBtn_Click(object sender, EventArgs e)
        {
            if(isMotor_Thread==false)
            {

                MotorControl_thread.Resume();
                Epos1.StartPosition();
                isMotor_Thread = true;
                this.continueBtn.Enabled = false;
                this.stopBtn.Enabled = true;
            }
            else
            {
                MessageBox.Show("电机已经是启动状态");
            }
            
        }
        #endregion

        private DataTable readCSV(String filename)
        {
            DataTable dt = new DataTable();

            //文件流读取
            System.IO.FileStream fs = new System.IO.FileStream(filename, System.IO.FileMode.Open);
            System.IO.StreamReader sr = new System.IO.StreamReader(fs, Encoding.GetEncoding("gb2312"));

            string tempText = "";
            bool isFirst = true;
            while ((tempText = sr.ReadLine()) != null)
            {
                string[] arr = tempText.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                //一般第一行为标题，所以取出来作为标头
                if (isFirst)
                {
                    foreach (string str in arr)
                    {
                        dt.Columns.Add(str);
                    }
                    isFirst = false;
                }
                else
                {
                    //从第二行开始添加到datatable数据行
                    DataRow dr = dt.NewRow();
                    for (int i = 0; i < dt.Columns.Count; i++)
                    {
                        dr[i] = i < arr.Length ? arr[i] : "";
                    }
                    dt.Rows.Add(dr);
                }
            }
            return dt;

        }

        private void positiveFlagBox_CheckedChanged(object sender, EventArgs e)
        {
            NegativeFlagBox.Checked = false;
        }

        private void NegativeFlagBox_CheckedChanged(object sender, EventArgs e)
        {
            positiveFlagBox.Checked = false;
        }

        private void insert_data_btn_Click(object sender, EventArgs e)
        {
            double insert_data = System.Convert.ToDouble(this.route_change_box.Text.ToString());
            int flag = 0;
            if (this.positiveFlagBox.Checked == true && this.NegativeFlagBox.Checked == false)
            {
                flag = 1;
            }
            else if (this.NegativeFlagBox.Checked == true && this.positiveFlagBox.Checked == false)
            {
                flag = 0;
            }
            else
            {
                MessageBox.Show("请选择方向");
                return;
            }
            for(int i=-2;i<3;i++)
            {
                #region 确定好修正轴
                if (x_check.Checked == true)
                {

                    InsertPoint_x.Add(insert_data);
                    dataTransfer.setInsertPoint(insert_data+i, 1, 0, flag, 0);

                }
                if (y_check.Checked == true)
                {

                    InsertPoint_y.Add(insert_data);
                    dataTransfer.setInsertPoint(insert_data + i, 1, 0, flag, 1);
                }
                if (z_check.Checked == true)
                {
                    InsertPoint_z.Add(insert_data);
                    dataTransfer.setInsertPoint(insert_data + i, 1, 0, flag, 2);
                }
            }


            this.route_change_box.Clear();
            #endregion
        }

        private void textBox1_DragDrop(object sender, DragEventArgs e)
        {
            //GetValue(0) 为第1个文件全路径
            //DataFormats 数据的格式，下有多个静态属性都为string型，除FileDrop格式外还有Bitmap,Text,WaveAudio等格式
            string path = ((System.Array)e.Data.GetData(DataFormats.FileDrop)).GetValue(0).ToString();
            textBox1.Text = path;
            this.textBox1.Cursor = System.Windows.Forms.Cursors.IBeam; //还原鼠标形状
        }

        private void textBox1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Link;
                this.textBox1.Cursor = System.Windows.Forms.Cursors.Arrow;  //指定鼠标形状（更好看）  
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }

        }



        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void doubleKMP_Click(object sender, EventArgs e)
        {
            if (is_route_change.Checked == true)
            {
                //WriteZscore();
                //触发事件
                setFormDoubleKMP();
                doubleObj = dataTransfer.getRouteData();//保存欧式空间角度
                InsertPoint_x.Clear();
                InsertPoint_y.Clear();
                InsertPoint_z.Clear();
                //计算更新的数据
                updateRoute();
                count = 0;
                //重新开启电机和力写入线程
                this.MotorControl_thread.Resume();
                this.WriteForce_thread.Resume();
                dataTransfer.CleanInsertPoint();
                drawChart2();
                forceList.Clear();
                angleList.Clear();
            }
            else
            {
                //WriteZscore();
                count = 0;
                forceList.Clear();
                angleList.Clear();
                dataTransfer.CleanInsertPoint();
                this.MotorControl_thread.Resume();
                this.WriteForce_thread.Resume();
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            
        }

        private void Y_axis_zeroing_monitoring_TextChanged(object sender, EventArgs e)
        {

        }

        private void X_axis_zeroing_monitoring_TextChanged(object sender, EventArgs e)
        {

        }

        private void Z_axis_zeroing_monitoring_TextChanged(object sender, EventArgs e)
        {

        }

        private void label14_Click(object sender, EventArgs e)
        {

        }

        private void label13_Click(object sender, EventArgs e)
        {

        }

        private void label12_Click(object sender, EventArgs e)
        {

        }

        private void button4_Click(object sender, EventArgs e)
        {
            string dirPath = @"D:\Project\C#_work\Matlab_connect\Matlab_connect\data";
            System.IO.DirectoryInfo dirInfo = new System.IO.DirectoryInfo(dirPath);
            int result = Form1.GetFilesCount(dirInfo);
            //获取文件夹中最后生成的路径
            string FilePath_3 = "D:\\Project\\C#_work\\Matlab_connect\\Matlab_connect\\csv_datasets\\route" + result.ToString() + ".csv";

            Console.WriteLine(FilePath_3);

            tool.readCSV(FilePath_3, out CsvRoute); // 调用函数

            //readCSV("D:\\Project\\C#_work\\Matlab_connect\\Matlab_connect\\data\\route6.csv", out CsvRoute); // 调用函数

            doubleObj = new double[CsvRoute.Rows.Count, 4];//保存欧式空间角度
                                                           //清空图表

            //先清空chart中的数据点进行迭代
            chart2.Series["Standard_X_degree"].Points.Clear();
            chart2.Series["Standard_Y_degree"].Points.Clear();
            chart2.Series["Standard_Z_degree"].Points.Clear();
            // 遍历table 中的所有数据
            for (int i = 0; i < CsvRoute.Rows.Count; i++)
            {
                for (int j = 0; j < CsvRoute.Columns.Count; j++)
                {
                    object obj = CsvRoute.Rows[i][j]; //i行j列的值 
                    obj = obj.ToString();
                    doubleObj[i, j] = Convert.ToDouble(obj);
                }
                //Console.WriteLine("mulRea11= {0}", doubleObj[i,1]);
                chart2.Series["Standard_X_degree"].Points.AddXY(doubleObj[i, 0], doubleObj[i, 1]);
                chart2.Series["Standard_Y_degree"].Points.AddXY(doubleObj[i, 0], doubleObj[i, 2]);
                chart2.Series["Standard_Z_degree"].Points.AddXY(doubleObj[i, 0], doubleObj[i, 3]);
                //刷新chart控件
                chart2.Invalidate();
            }
            Console.WriteLine("数据加载完成");
        }
    }
}

