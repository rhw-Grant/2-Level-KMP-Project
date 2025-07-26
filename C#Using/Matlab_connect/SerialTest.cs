using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Matlab_connect
{
    public partial class SerialTest : Form
    {

        SerialportReceivingData serialportReceivingData;            //实例化串口接受数据线程
        SerialportReceivingDataForce serialportReceivingDataForce;
        Torquedata torquedata;
        Mcu_Data mcu_data = new Mcu_Data();                         //力矩传感器解析所用
        Thread Analysis_torque;                                   //声明手动被动模式编码器数值解析线程
        Thread DrawChart_thread;                                    //绘图线程
        bool TorqeIsUpdate;
        byte[] M8128RevData ;
        ForceOrDegree fd = new ForceOrDegree();
        Thread SendMsg_thread;                                      //声明测试线程
        SqliteConnection sql = new SqliteConnection();

        //力矩解析变量
        public double Act_torque_1;
        public double Act_torque_2;
        public double Act_torque_3;
        public double Act_torque_4;
        public double Act_torque_5;
        public double Act_torque_6;

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
        public SerialTest()
        {
            InitializeComponent();
            System.Windows.Forms.Control.CheckForIllegalCrossThreadCalls = false;
            InitChart();
            
        }

        #region 绘制力矩数据图
        private void drawChart1()
        {
            DataTable x_torque_datatable = new DataTable();
            x_torque_datatable= readCSV("D:\\Project\\C#_work\\Matlab_connect\\Matlab_connect\\M_force\\torque_x.csv");
            int count = x_torque_datatable.Rows.Count;
            for (int i = 0; i < count; i++)
            {
                chart2.Series["测试1"].Points.AddXY((float)Convert.ToSingle(x_torque_datatable.Rows[i][0]), (float)Convert.ToSingle(x_torque_datatable.Rows[i][1]));
                //刷新chart控件
                chart2.Invalidate();
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

        private void openSerial_Click(object sender, EventArgs e)
        {

            try
            {
                //串口配置
                serialPort1.PortName = "COM6";//获取串口名
                serialPort1.BaudRate = Convert.ToInt32("115200");//十进制数据转化
                serialPort1.Open();
                MessageBox.Show("端口打开", "OK");
            }
            catch
            {
                MessageBox.Show("端口错误，请检查串口", "错误");
            }
        }

        private void ChangeData_Click(object sender, EventArgs e)
        {
            serialPort1.Write("AT+SGDM=(A01,A02,A03,A04,A05,A06);E;1(WMA:1)\r\n");
            System.Threading.Thread.Sleep(10);
            serialPort1.Write("AT+GSD\r\n");

            //开启力传感器
            //Analysis_torque = new Thread(Monitoring_return_to_zero_event2);
            //Analysis_torque.IsBackground = true;
            //Analysis_torque.Start();

            //开启画图线程
            //this.DrawChart_thread = new Thread(drawchart_event);
            //this.DrawChart_thread.IsBackground = true;
            //this.DrawChart_thread.Start();
            this.timer1.Enabled = true;

            Console.WriteLine("forceX={0}", forceX);
        }

        float forceX;
        float forceY;
        double forceZ;
        float torqueX;
        float torqueY;
        double torqueZ;

        private void Monitoring_return_to_zero_event2()
        {
            
        }

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
            this.chart1.Series.Add(series1);//
            this.chart1.Series.Add(series2);//
            this.chart1.Series.Add(series3);//
            this.chart1.Series.Add(series4);//
            this.chart1.Series.Add(series5);//
            this.chart1.Series.Add(series6);//
            //设置图表显示样式
            this.chart1.ChartAreas[0].AxisY.Minimum = -200;
            this.chart1.ChartAreas[0].AxisY.Maximum = 200;
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
            this.chart1.Series[0].ChartType = SeriesChartType.Spline;
            this.chart1.Series[1].ChartType = SeriesChartType.Spline;
            this.chart1.Series[2].ChartType = SeriesChartType.Spline;
            this.chart1.Series[3].ChartType = SeriesChartType.Spline;
            this.chart1.Series[4].ChartType = SeriesChartType.Spline;
            this.chart1.Series[5].ChartType = SeriesChartType.Spline;

            this.chart1.Series[0].Points.Clear();
            this.chart1.Series[1].Points.Clear();
            this.chart1.Series[2].Points.Clear();
            this.chart1.Series[3].Points.Clear();
            this.chart1.Series[4].Points.Clear();
            this.chart1.Series[5].Points.Clear();
            //invokeChartData[0] =  chart1;
            //invokeChartData[1] = dataTable;
            #endregion

            this.chart2.ChartAreas.Clear();
            ChartArea chartArea2 = new ChartArea("C2");//实例化
            this.chart2.ChartAreas.Add(chartArea2);//向添加绘图区域
            //定义存储和显示点的容器
            this.chart2.Series.Clear();
            Series seriestest1 = new Series("测试1");
            Series seriestest2 = new Series("测试2");
            seriestest1.ChartArea = "C2";//添加到
            seriestest2.ChartArea = "C2";//添加到
            this.chart2.Series.Add(seriestest1);
            this.chart2.Series.Add(seriestest2);
            this.chart2.Series[0].ChartType = SeriesChartType.Spline;
            this.chart2.Series[1].ChartType = SeriesChartType.Spline;

        }

        private void SerialTest_Load(object sender, EventArgs e)
        {
            torquedata = new Torquedata(serialPort1);//力矩采集

            serialPort1.DataReceived += new SerialDataReceivedEventHandler(serialPort1_DataReceived);//必须手动添加事件处理程序（力传感器事件）
            InitChart();

        }
        //串口1事件（力传感器)
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

        private void timer1_Tick(object sender, EventArgs e)
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
            Console.WriteLine(forceZ);
            #endregion
        }

        List<string> timeList = new List<string>();
        bool isThread = false;
        int timer_count = 0;//用于记录计时器计数
        int thread_count = 0;
        private void button1_Click(object sender, EventArgs e)
        {
            this.timer2.Start();
            isThread = true;
            this.SendMsg_thread = new Thread(showMessage_event);
            this.SendMsg_thread.IsBackground = true;
            this.SendMsg_thread.Start();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //Write();
            //this.timer2.Stop();
            if(isThread)
            {
                Console.WriteLine("线程暂停");
                isThread = false;
                SendMsg_thread.Suspend();
            }
            else
            {
                Console.WriteLine("线程继续");
                isThread = true;
                SendMsg_thread.Resume();
            }
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            timer_count++;
            Console.WriteLine("计时器生成数据为{0}", DateTime.Now.ToString("HH:mm:ss"));
            //timeList.Add(DateTime.Now.ToString());
            //if (timer_count == 5)
            //{
            //    timer2.Stop();
            //    isThread = false;
            //    Console.WriteLine("计时器运行{0}次,线程运行{1}次", timer_count, thread_count);

            //}
        }
        private void Write()
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
            sw.Write("No." + "," + "time" + "\t\n");


            for (int row = 0; row <timeList.Count; row++)
            {
                string str = row .ToString() + "," + timeList[row].ToString()  + "\t\n";
                sw.Write(str);
            }

            sw.Flush();
            sw.Close();

        }

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

        private void OpenThreadBtn_Click(object sender, EventArgs e)
        {
            SendMsg_thread.Resume();
        }
        private void stopThread_btn_Click(object sender, EventArgs e)
        {
            SendMsg_thread.Suspend();
        }
        private void showMessage_event()
        {  
            while(true)
            {
                thread_count++;
                Console.WriteLine("线程2显示数据");
                Thread.Sleep(100);
                if(thread_count==20)
                {
                    Console.WriteLine("线程2自动停止");
                    SendMsg_thread.Suspend();
                }
            }
        }

        private void CSV_read_Click(object sender, EventArgs e)
        {
            DataTable dt = new DataTable();

            //文件流读取
            System.IO.FileStream fs = new System.IO.FileStream("D:\\Project\\matlab_work\\kmp_route_test\\route_process\\tempscript\\force_x.csv", System.IO.FileMode.Open);
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
            //展示到页面
            dataGridView1.DataSource = dt;
            //关闭流
            sr.Close(); fs.Close();

        }
        int forcecount = 0;
        private void button3_Click(object sender, EventArgs e)
        {
            drawChart1();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            DataTable x_torque_datatable = new DataTable();
            x_torque_datatable = readCSV("D:\\Project\\C#_work\\Matlab_connect\\Matlab_connect\\M_force\\torque_x.csv");
            

            chart2.Series["测试2"].Points.AddXY((float)Convert.ToSingle(x_torque_datatable.Rows[forcecount*5][0]), (float)Convert.ToSingle(x_torque_datatable.Rows[forcecount * 5][2]));
            Console.WriteLine("Point=({0},{1})", x_torque_datatable.Rows[forcecount * 5][0], x_torque_datatable.Rows[forcecount * 5][2]);
            forcecount++;
            chart2.Invalidate();
            if(forcecount==15)
            {
                Console.WriteLine("重新开始");
                chart2.Series["测试2"].Points.Clear();
                //chart2.Series["测试2"].Points.RemoveAt(0);
            }
        }

        private void sql_insert_Click(object sender, EventArgs e)
        {
            sql.OpenList();
            sql.InsertRouteData("encoder_route_data", Convert.ToDouble(textBox1.Text), Convert.ToDouble(textBox2.Text.ToString()), Convert.ToDouble(textBox3.Text.ToString()),1);
            sql.CloseList();
            
        }
    }
}
