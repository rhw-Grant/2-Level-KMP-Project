using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections;
using System.Windows.Forms.DataVisualization.Charting;

using MathWorks.MATLAB.NET.Arrays;
using MathWorks.MATLAB.NET.Utility;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;



namespace Matlab_connect
{
    public partial class Form1 : Form
    {

        public Form1()
        {
            InitializeComponent();
            //设置初始变量
            this.textBox1.Text = "D:\\Project\\matlab_work\\kmp_route_test\\route_process\\route_data_x.csv";
            this.textBox2.Text = "D:\\Project\\matlab_work\\kmp_route_test\\route_process\\route_data_y.csv";
            this.textBox3.Text = "D:\\Project\\matlab_work\\kmp_route_test\\route_process\\route_data_z.csv";
            this.timeBox.Text = "100";
            this.dtBox.Text = "0.1";
            this.routeNumberBox.Text = "5";


        }
            
        private void button1_Click(object sender, EventArgs e)
        {
            // 定义两个2*3的二维数组
            double[,] x1 = new double[,] { { 1.25, 2.13, 3 }, { 4, 5.5764, 6 } };
            double[,] x2 = new double[,] { { 1.25, 2, 3 }, { 4, 5, 6 } };

            // 将C#的数据格式转换为MATLAB能够识别的数据格式
            MWNumericArray arr1 = x1;
            MWNumericArray arr2 = x2;

            // 实例化对象
            add.Class1 mySum = new add.Class1();

            // 调用dll，并接收返回结果(out是关键字，不要用out来接收结果)
            MWArray rst = mySum.add(arr1, arr2);

            // 将MATLAB的数据转化为中间过渡格式
            MWNumericArray result = (MWNumericArray)rst;

            // 将中间过渡格式转化为C#的数据格式
            double[,] csArray = (double[,])result.ToArray(MWArrayComponent.Real); // 接收数据
        }
       
        int N =5;//5条路径
        double tau = 100;//总时间长度为100s
        double dt = 0.1;//单步时间间隔为0.1s
        double[,] Data;//用于生成CSV文件的数据路径(四元数)
        double[,] newdata;//用于生成CSV文件的数据路径(欧拉角)
        int insertNum=0;//用于记录需要插入几个数据点           
        MWArray oldr;//函数输出为模仿的最终路径
        ArrayList insertlist = new ArrayList();
        MWArray demo;//CSV文件的matlab使用格式
        MWNumericArray temp;
        MWStructArray quatRef;//用于保存GMM-GMR生成的路径
        MWArray trajAda;//用于保存KMP计算后的路径
        int ChangePoint;//进行参数初始化
        

        
        private void button2_Click(object sender, EventArgs e)
        {
            N = int.Parse(routeNumberBox.Text);//5条路径
            tau = double.Parse(timeBox.Text);//总时间长度为100s
            dt = double.Parse(dtBox.Text);//单步时间间隔为0.1s
            //showimage.Class1 a = new showimage.Class1();
            //a.showimage();
            MessageBox.Show("参数初始化完成", "提示", MessageBoxButtons.OK);
        }
        //基本参数
        //加载csv数据

        //设置初始数据链表，【0】——X方向，【1】——Y方向，【2】——Z方向
        List<double[,]> DataList = new List<double[,]>();
        List<double[,]> newdataList = new List<double[,]>();
        private void button3_Click(object sender, EventArgs e)
        {
            
            load_route.Class1 a = new load_route.Class1();
            MWArray[] agrsout = null;//设置一个两个元素的ml数组
            MWArray[] agrsin= new MWArray[4];
            MWNumericArray N_mat = N;
            MWNumericArray tau_mat = tau;
            MWNumericArray dt_mat = dt;
            MWStringArray route_mat_x = textBox1.Text.ToString();
            MWStringArray route_mat_y = textBox2.Text.ToString();
            MWStringArray route_mat_z = textBox3.Text.ToString();
            //X方向数据
            agrsout =a.load_route(2,route_mat_x, tau_mat, dt_mat, N_mat);
            // 将MATLAB的数据转化为中间过渡格式
            MWNumericArray result1 = (MWNumericArray)agrsout[0];
            MWNumericArray result2 = (MWNumericArray)agrsout[1];
            // 将中间过渡格式转化为C#的数据格式
            Data = (double[,])result1.ToArray(MWArrayComponent.Real); // 接收拼接数据
            newdata = (double[,])result2.ToArray(MWArrayComponent.Real);//接收路径数据
            DataList.Add(Data);
            newdataList.Add(newdata);
            //Y方向数据
            agrsout = a.load_route(2, route_mat_y, tau_mat, dt_mat, N_mat);
            // 将MATLAB的数据转化为中间过渡格式
            result1 = (MWNumericArray)agrsout[0];
            result2 = (MWNumericArray)agrsout[1];
            // 将中间过渡格式转化为C#的数据格式
            Data = (double[,])result1.ToArray(MWArrayComponent.Real); // 接收拼接数据
            newdata = (double[,])result2.ToArray(MWArrayComponent.Real);//接收路径数据
            DataList.Add(Data);
            newdataList.Add(newdata);
            //Z方向数据
            agrsout = a.load_route(2, route_mat_z, tau_mat, dt_mat, N_mat);
            // 将MATLAB的数据转化为中间过渡格式
            result1 = (MWNumericArray)agrsout[0];
            result2 = (MWNumericArray)agrsout[1];
            // 将中间过渡格式转化为C#的数据格式
            Data = (double[,])result1.ToArray(MWArrayComponent.Real); // 接收拼接数据
            newdata = (double[,])result2.ToArray(MWArrayComponent.Real);//接收路径数据
            DataList.Add(Data);
            newdataList.Add(newdata);
            MessageBox.Show("路径加载完成", "提示", MessageBoxButtons.OK);
        }

        //加载gmm数据
        private void button7_Click(object sender, EventArgs e)
        {
            load_gmm_route.Class1 a = new load_gmm_route.Class1();
            for(int i=1;i<=3;i++)
            {
                quatRef = (MWStructArray)a.load_gmm_route("D:/Project/matlab_work/kmp_route_test/route_process/vicon_route_process/gmm_route5.mat",i);
                quatRefList.Add(quatRef);
            }
            MessageBox.Show("高斯拟合数据加载完成", "提示", MessageBoxButtons.OK);

            #region 打开下位机控制器
            machineControl = new MachineControl();

            machineControl.setFormInsertValue += new setInsertValue(_setXYZdirection);
            machineControl.setFormDoubleKMP += new setDoubleKMP(_setXYZdoubleKMP);
            machineControl.Show();
            #endregion
        }

        private void ChartDraw()
        {
            //this.chart1.Series.Add(series);
        }
        //进行高斯混合分布
        List<MWStructArray> quatRefList=new List<MWStructArray>();//用于保存GMM-GMR生成的路径(三个方向)

        MachineControl machineControl;
        //调用控制器页面
        private void button4_Click(object sender, EventArgs e)
        {

            generate_gmm_route.Class1 a = new generate_gmm_route.Class1();
            for(int i=0;i<3;i++)
            {
                MWNumericArray ma = DataList[i];
                demo = ma;
                quatRef = (MWStructArray)a.generate_gmm_route(demo, tau, dt);
                quatRefList.Add(quatRef);
            }
            MessageBox.Show("高斯拟合完成", "提示", MessageBoxButtons.OK);
            #region 打开下位机控制器
            machineControl = new MachineControl();

            machineControl.setFormInsertValue += new setInsertValue(_setXYZdirection);
            machineControl.setFormDoubleKMP += new setDoubleKMP(_setXYZdoubleKMP);

            machineControl.Show();
            #endregion
        }

        List<MWArray> trajAdaList=new List<MWArray>();//用于保存KMP计算后的路径(三个方向)
        //进行kmp运算
        private void button5_Click(object sender, EventArgs e)
        {

            //进行kmp生成运算
            generate_kmp_route.Class1 a = new generate_kmp_route.Class1();
            for(int i=0;i<3;i++)
            {
                quatRef = quatRefList[i];
                trajAda = a.generate_kmp_route(quatRef, tau, dt);
                trajAdaList.Add(trajAda);
            }

            double[] insertarray = {0.1};
            MWNumericArray ma1 = insertarray.ToArray();
            MWArray insertpoint = ma1;
            show_pics.Class1 b = new show_pics.Class1();
            b.show_pics(trajAdaList[0], trajAdaList[1], trajAdaList[2], insertpoint);

            #region 绘制路线图
            List<double[,]> result = new List<double[,]>();
            // 将MATLAB的数据转化为中间过渡格式
            MWNumericArray ma = (MWNumericArray)trajAdaList[0];
            result.Add((double[,])ma.ToArray(MWArrayComponent.Real));
            ma = (MWNumericArray)trajAdaList[1];
            result.Add((double[,])ma.ToArray(MWArrayComponent.Real));
            ma = (MWNumericArray)trajAdaList[2];
            result.Add((double[,])ma.ToArray(MWArrayComponent.Real));
            #endregion
            //设置时间
            
            double[,] routeData=new double[ Convert.ToInt32(tau / dt),4];
            for (int j=0;j<tau/dt;j++)
            {
                routeData[j, 0] = j*dt;
            }
            //输入路径
            for(int i=1;i<4;i++)
            {
                for(int j=0;j<tau/dt;j++)
                {
                    routeData[j, i] = result[i-1][1, j];
                }
            }
            machineControl.dataTransfer.setRouteData(routeData);

        }

        //查询路径下有多少图片
        public static int GetFilesCount(DirectoryInfo dirInfo,string filetype)
        {

            int totalFile = 0;
            //totalFile += dirInfo.GetFiles().Length;//获取全部文件
            totalFile += dirInfo.GetFiles(filetype).Length;//获取某种格式
            foreach (System.IO.DirectoryInfo subdir in dirInfo.GetDirectories())
            {
                totalFile += GetFilesCount(subdir,filetype);
            }
            return totalFile;
        }


        private void Write(double[,] route)
        {
            
            string dirPath = @"D:\Project\C#_work\Matlab_connect\Matlab_connect\data";
            System.IO.DirectoryInfo dirInfo = new System.IO.DirectoryInfo(dirPath);
            int result = GetFilesCount(dirInfo);
            string path = "D:\\Project\\C#_work\\Matlab_connect\\Matlab_connect\\data\\route"+(result+1).ToString()+".csv";
            Console.WriteLine(path);


            if (!File.Exists(path))
                File.Create(path).Close();

            StreamWriter sw = new StreamWriter(path, true, Encoding.UTF8);
            //for (int column = 0; column < route.GetLength(1); column++)
            //{
            //    sw.Write(column*dt + ",");
            //    sw.Write("\r\n");
            //}

            int m = route.GetLength(0);
            int n = route.GetLength(1);
            Console.WriteLine("m= {0},n={1}", m,n);
            //设置源路径的转置，写入csv文件
            double[,] route1= new double[n,m];
            for (int row = 0; row < route.GetLength(0); row++)
            {
                for (int column = 0; column < route.GetLength(1); column++)
                {
                    route1[column, row] = route[row, column];
                }
            }
            Console.WriteLine("m= {0},n={1}", route1.GetLength(0), route1.GetLength(1));
            //写入title
            sw.Write("time" + "," + "x_degree" + "," + "y_degree" + "," + "z_degree" + "\t\n");
            for (int row = 0; row < route1.GetLength(0); row++)
            {
                string str = (row * dt).ToString() + "," + route1[row, 1].ToString() + "," + 0 + "," + 0 + "\t\n";
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


        private void textBox1_DragDrop(object sender, DragEventArgs e)
        {
            //textBox1.Text = ((Array)e.Data.GetData(DataFormats.FileDrop)).GetValue(0).ToString();
            //textBox1.Text.ToString().Replace('\', '/');
        }

        private void textBox1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Link;
            else e.Effect = DragDropEffects.None;
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
        


        //用于记录插入点数据
        List<MWArray> insertpointList = new List<MWArray>();
        public void  _setXYZdirection()
        {
            int u = 0;
            //3个方向的kmpinsert计算
            while(u<3)
            {
                //正负两个方向分别进行判断
                for(int j=0;j<2;j++)
                {
                    //matlab函数trajAda=kmp_insert_point(oldr,quatRef,insertpoint,demo,tau,dt)
                    //输入与生成kmp相同
                    //进行kmp插入点运算
                    if (machineControl.dataTransfer.getInsertPoint(u,j).Count>0)
                    {
                        List<routeInfo> temp = machineControl.dataTransfer.getInsertPoint(u, j);
                        double[] insertarray = new double[temp.Count];
                        for (int i = 0; i < temp.Count; i++)
                        {
                            insertarray[i] = temp[i].route_time/10;
                        }
                        //double[] insertarray = { 10, 12.5 };
                        MWNumericArray ma1 = insertarray.ToArray();
                        MWArray insertpoint = ma1;
                        insertpointList.Add(ma1);
                        kmp_insert_point.Class1 a = new kmp_insert_point.Class1();
                        //输入老路径
                        oldr = trajAdaList[u];
                        MWArray[] agrsout = null;//设置一个有三个元素的ml数组
                        Console.WriteLine("insertpoint={0}", insertarray[0]);
                        agrsout = a.kmp_insert_point(3, oldr, quatRefList[u], insertpoint, tau, dt, j);
                        //输出新路径
                        trajAdaList[u] = agrsout[1];
                        quatRefList[u] = (MWStructArray)agrsout[2];
                    }
                    
                }
                u++;
            }
            double[,] routeData = new double[Convert.ToInt32(tau / dt), 4];
            for (int j = 0; j < tau / dt; j++)
            {
                routeData[j, 0] = j * dt;
            }
            //输入路径
                for (int i = 1; i < 4; i++)
            {
                double[] s = new double[1];
                MWNumericArray ma = (MWNumericArray)trajAdaList[i - 1];
                double[] result = (double[])ma.ToVector(MWArrayComponent.Real);
                for (int j = 0; j < tau / dt; j++)
                {
                    s = result;
                    routeData[j, i] = s[3 * j + 1];
                    //Console.WriteLine("s={0}", s[3 * j + 1]);
                }
            }
            //将新生成的数据保存到新类中
            machineControl.dataTransfer.setRouteData(routeData);

            //double[] insertarray1 = { 10, 12.5 };
            //MWNumericArray ma11 = insertarray1.ToArray();
            //MWArray insertpoint1 = ma11;
            //绘制三维图
            show_pics.Class1 b = new show_pics.Class1();
            for(int i=0;i< insertpointList.Count; i++)
            {
                b.show_pics(trajAdaList[0], trajAdaList[1], trajAdaList[2], insertpointList[i]);
            }
            //清空设置轴数组
            machineControl.dataTransfer.setFlag(0, false);
            machineControl.dataTransfer.setFlag(1, false);
            machineControl.dataTransfer.setFlag(2, false);
            insertpointList.Clear();
        }

        public void _setXYZdoubleKMP()
        {
            int u = 0;
            //3个方向的kmpinsert计算
            while (u < 3)
            {
                //正负两个方向分别进行判断
                for (int j = 0; j < 2; j++)
                {
                    //matlab函数trajAda=kmp_insert_point(oldr,quatRef,insertpoint,demo,tau,dt)
                    //输入与生成kmp相同
                    //进行kmp插入点运算
                    if (machineControl.dataTransfer.getInsertPoint(u, j).Count > 0)
                    {
                        List<routeInfo> temp = machineControl.dataTransfer.getInsertPoint(u, j);
                        double[] insertarray = new double[temp.Count];
                        for (int i = 0; i < temp.Count; i++)
                        {
                            insertarray[i] = temp[i].route_time / 10;
                        }
                        //double[] insertarray = { 10, 12.5 };
                        MWNumericArray ma1 = insertarray.ToArray();
                        MWArray insertpoint = ma1;
                        insertpointList.Add(ma1);
                        double_KMP.Class1 a = new double_KMP.Class1();
                        //输入老路径
                        oldr = trajAdaList[u];
                        MWArray[] agrsout = null;//设置一个有三个元素的ml数组
                        Console.WriteLine("insertpoint={0}", insertarray[0]);
                        agrsout = a.double_KMP(3, oldr, quatRefList[u], insertpoint, tau, dt, j,1,0.1);
                        //输出新路径
                        trajAdaList[u] = agrsout[1];
                        quatRefList[u] = (MWStructArray)agrsout[2];
                    }

                }
                u++;
            }
            double[,] routeData = new double[Convert.ToInt32(tau / dt), 4];
            for (int j = 0; j < tau / dt; j++)
            {
                routeData[j, 0] = j * dt;
            }
            //输入路径
            for (int i = 1; i < 4; i++)
            {
                double[] s = new double[1];
                MWNumericArray ma = (MWNumericArray)trajAdaList[i - 1];
                double[] result = (double[])ma.ToVector(MWArrayComponent.Real);
                for (int j = 0; j < tau / dt; j++)
                {
                    s = result;
                    routeData[j, i] = s[3 * j + 1];
                    //Console.WriteLine("s={0}", s[3 * j + 1]);
                }
            }
            //将新生成的数据保存到新类中
            machineControl.dataTransfer.setRouteData(routeData);

            //double[] insertarray1 = { 10, 12.5 };
            //MWNumericArray ma11 = insertarray1.ToArray();
            //MWArray insertpoint1 = ma11;
            //绘制三维图
            show_pics.Class1 b = new show_pics.Class1();
            for (int i = 0; i < insertpointList.Count; i++)
            {
                b.show_pics(trajAdaList[0], trajAdaList[1], trajAdaList[2], insertpointList[i]);
            }
            //清空设置轴数组
            machineControl.dataTransfer.setFlag(0, false);
            machineControl.dataTransfer.setFlag(1, false);
            machineControl.dataTransfer.setFlag(2, false);
            insertpointList.Clear();
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            SerialTest serialTest = new SerialTest();
            serialTest.Show();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            #region 打开下位机控制器
            machineControl = new MachineControl();

            machineControl.setFormInsertValue += new setInsertValue(_setXYZdirection);
            machineControl.setFormDoubleKMP += new setDoubleKMP(_setXYZdoubleKMP);

            machineControl.Show();
            #endregion
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }
        #region 打开手动调试控制器
        private void button_8_Click(object sender, EventArgs e)
        {
            DanjiTIaoshi dianJiTiaoShi = new DanjiTIaoshi();
            dianJiTiaoShi.Show();
        }
        #endregion

        private void button8_Click(object sender, EventArgs e)
        {
            KMPTunnel kMPTunnel = new KMPTunnel();
            kMPTunnel.Show();
        }
    }
}
