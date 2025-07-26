using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.IO.Ports;
using System.Runtime.InteropServices;
using System.Threading;
using CANNalyst;

using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Button;
using csmatio.types;
using csmatio.io;
using System.Windows.Forms.DataVisualization.Charting;




namespace Matlab_connect
{
    public partial class DanjiTIaoshi : Form
    {
        CANalystHelper CANalyst;
        static CANalystHelper Cnh = new CANalystHelper();//声明关于Can的
        EposDriver Epos1 = new EposDriver("00000601", Cnh);//声明关于驱动器的    
        public DanjiTIaoshi()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Matlab_connect.MachineControl.Motor_1 = true;
            Matlab_connect.MachineControl.Motor_2 = false;
            Matlab_connect.MachineControl.Motor_3 = false;
            Epos1.Single_Motor_VelocityInit("00000601");
        }

        private void button4_Click(object sender, EventArgs e)
        {
            Matlab_connect.MachineControl.Motor_1 = false;
            Matlab_connect.MachineControl.Motor_2 = true;
            Matlab_connect.MachineControl.Motor_3 = false;
            Epos1.Single_Motor_VelocityInit("00000602");
        }

        private void button5_Click(object sender, EventArgs e)
        {
            Matlab_connect.MachineControl.Motor_1 = false;
            Matlab_connect.MachineControl.Motor_2 = false;
            Matlab_connect.MachineControl.Motor_3 = true;
            Epos1.Single_Motor_VelocityInit("00000603");
        }

        private void DanjiTIaoshi_Load(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            Epos1.SetVelocity(20);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Epos1.SetVelocity(-20);
        }

        private void button10_Click(object sender, EventArgs e)
        {
            Epos1.SetVelocity(0);                   //停止电机
            Thread.Sleep(5);
            Epos1.Reseat();                         //失能电机
        }

        private void button6_Click(object sender, EventArgs e)
        {
            Epos1.SetVelocity(20);
        }

        private void button7_Click(object sender, EventArgs e)
        {
            Epos1.SetVelocity(20);
        }
    }
}
