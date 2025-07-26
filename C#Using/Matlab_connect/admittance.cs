using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using System.Windows.Forms;
using System.Threading;
using System.Timers;

namespace ankle1._0
{
    class admittance
    {
        public float Xo = 0;//角度偏差
        public float Deviation=1.0f;//固定偏差
        private float X = 0;//一级中间变量
        public float[] data = new float[250];
        private float[] time = new float[250];
        public Thread myThread;//创建线程
        private bool run_flag;
        public bool compute_flag;
        public float nowTorque = 0;
        private float torqueIn = 0;
        public float cycleTime = 0;
        //导纳参数
        public float M=1;
        public float D=0.8f;
        public float K=1;

        public float Ts = 9.9899f;
        public float wn = 1;
        public float Zeta = (float)0.4;
        public float Ki = 1;//比例系数
        private float timespan = 0.1f;
        private int times = 0;
        private Int16 number=250;
        private float[][] Data = new float[250][]; //使用二维数组替代下面数据
        private float _conversionRatio = 57.3f/5.73f;
        public float _positionAngleLimit = 22f;                     //正限位
        public float _nagetiveAngleLimit = -22f;                    //负限位

        /// <summary>
        /// 启动线程
        /// </summary>
        public void myThreadStart()
        {
            compute_flag=true;
            run_flag = true;
            myThread = new Thread(threadOut);
            myThread.IsBackground = true;          
            myThread.Start();
            Application.DoEvents();
        }
        /// <summary>
        /// 终止线程
        /// </summary>
        public void myThreadStop()
        {
           myThread.Suspend();
        }
        /// <summary>
        /// 初始化
        /// </summary>
        private void common_zero()
        {
            for (byte i = 0; i < number; i++)
            {
                Data[i] = new float[number];
                data[i] = 0;
            }
            for (byte k = 0; k < number; k++)
            {
                for (byte p = 0; p < number; p++)
                    Data[k][p] = 0;
            }
        }
        /// <summary>
        /// 导纳函数
        /// </summary>
        /// <param name="Wn"></param>无阻尼自振角频率
        /// <param name="zeta"></param>阻尼不能等于1
        /// <param name="Ti"></param>力矩
        /// <param name="t"></param>时间
        /// <returns></returns>
        public float impedanceControl(float Wn, float zeta, float Ti, float t)
        {
            float delta = (float)Math.Sqrt(1 - Math.Pow(zeta, 2));
            float Wd = Wn * delta;
            X = (float)(Ki * Ti * (1 - Math.Exp(-zeta * Wn * t) * Math.Sin(Wd * t //阶跃响应
                + Math.Atan(delta / zeta)) / delta));
            return X* _conversionRatio;
        }
        private void getTime(float Wn, float zeta)
        {
            cycleTime = (float)((-Math.Log(0.02) - Math.Log(Math.Sqrt(1 - Math.Pow(zeta, 2)))) / (Wn * zeta));
            //误差进入0.02的调整时间
        }
        private void load_data(float Wn, float zeta, float Ti)
        {
            float[] A = new float[number];
            float[] B = new float[number];
            B[0] = 0;
            for (int i = 0; i < (number - 1); i++)
            {
                time[i] = timespan * (i + 1);
                A[i] = impedanceControl(Wn, zeta, Ti, time[i]);
                B[i + 1] = impedanceControl(Wn, zeta, Ti, time[i]);
            }
            A[number - 1] = impedanceControl(Wn, zeta, Ti, timespan * number);
            for (int i = 0; i < number; i++)
                data[i] = A[i] - B[i];
        }
        private void processTorque()
        {
                if (nowTorque < 100 && nowTorque > -100)
                    if (nowTorque < 0.3 & nowTorque > -0.3)
                        nowTorque = 0;//消去抖动偏移误差 
                torqueIn = nowTorque;
        }
        /// <summary>
        /// 软件限位
        /// </summary>
        private void LimitAngle()
        {
            if (Xo > _positionAngleLimit || Xo == _positionAngleLimit)
            {
                Xo = _positionAngleLimit;
            }
            else if (Xo < _nagetiveAngleLimit || Xo == _nagetiveAngleLimit)
            {
                Xo = _nagetiveAngleLimit;
            }
        }
        /// <summary>
        /// 转入数据函数
        /// </summary>
        /// <param name="data1"></param>
        /// <param name="data2"></param>
        /// <param name="k"></param>
        private void reset(float[] data1, float[] data2, int k)
        {
            for (int i = 0; i <(number - k); i++)
            {
                data1[i + k] = data2[i];

            }
            for (int i = 0; i < k; i++)
            {
                data1[i] = data2[number - k + i];
            }
        }
        public void array(int t)
        {
            float sum = 0;
            reset(Data[t], data, t);
            for (int i = 0; i < number; i++)
                sum += Data[i][t];
            Xo = sum;
        }
        /// <summary>
        /// 重置导纳
        /// </summary>
        public void resetAdmittance()
        {
            common_zero();
            torqueIn = 0;
            Xo = 0;
            times = 0;
        }
        /// <summary>
        /// 线程入口函数
        /// </summary>
        public void threadOut()
        {
            common_zero();//初始化
            while (true)
            {
                for (; run_flag;)
                {
                    if (compute_flag)
                    {
                        times++;
                        processTorque();
                        load_data(wn, Zeta, torqueIn);
                        array(times);
                        if (times == number-1)
                            times = -1;
                        //compute_flag = false;
                    }
                    //Application.DoEvents();
                    Thread.Sleep(10);
                }
            }
        }
    }
}
