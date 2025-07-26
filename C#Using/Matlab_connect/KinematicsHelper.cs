using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matlab_connect
{
    class KinematicsHelper
    {
        public static int[] cal(double x, double y, double z)
        {
            double[] position = new double[3];

            if (x == 0 && z == 0)
            {
                position[0] = 0.0034 * y * y - 2 * y - 0.023;
                position[1] = 0.0034 * y * y + 2 * y - 0.023;
                position[2] = 0;
            }
            else if (y == 0 && z == 0)
            {
                position[0] = -0.00016 * x * x + 0.19 * x - 0.022;
                position[1] = position[0];
                position[2] = 0;
            }
            else if (x == 0 && y == 0)
            {
                position[0] = 0.0099 * z * z + 0.37 * z + 0.024;
                position[1] = 0.0099 * z * z - 0.37 * z + 0.024;
                position[2] = z;
            }

            int[] pulse = new int[3] { (int)(-position[0] * 65536), (int)(-position[1] * 65536), (int)(position[2] * 524288 / 360) };
            return pulse;
        }
        /// <summary>
        /// 位置逆解
        /// </summary>
        /// <param name="angle_x"></param>
        /// <param name="angle_y"></param>
        /// <param name="angle_z"></param>
        /// <returns></returns>
        /// 
        public static int[] InverseSolution(double angle_x, double angle_y, double angle_z)//输入绕x,y,z轴转过的角度
        {

            double[] b = { 105.0, 90.0, -34.68 };

            double[] A1 = { 189.92,90, -429.44 };

            double[] A2 = { -189.92, 90, -429.44 };

            double[] ROM = new double[9];                 //绕X轴的旋转矩阵
            double[] c_Rz_tmp = new double[9];            //绕Z轴的旋转矩阵
            double[] d_Rz_tmp = new double[9];
            double Rx_tmp;
            double Rz_tmp;
            double angle_x1;
            double angle_y1;
            double angle_z1;
            double b_Rx_tmp;
            double b_Rz_tmp;
            double d;
            int i;
            int k;

            // 杆1的初始长度，单位mm
            // 杆2的初始长度，单位mm

            // 转为弧度
            angle_x1 = angle_x / 180.0 * 3.1415926535897931;
            angle_y1 = angle_y / 180.0 * 3.1415926535897931;
            angle_z1 = angle_z / 180.0 * 3.1415926535897931;

            Rz_tmp = Math.Sin(angle_z1);
            b_Rz_tmp = Math.Cos(angle_z1);
            Rx_tmp = Math.Sin(angle_x1);
            b_Rx_tmp = Math.Cos(angle_x1);
            angle_x1 = Math.Sin(angle_y1);
            angle_z1 = Math.Cos(angle_y1);
            c_Rz_tmp[0] = b_Rz_tmp;
            c_Rz_tmp[3] = -Rz_tmp;
            c_Rz_tmp[6] = 0.0;
            c_Rz_tmp[1] = Rz_tmp;
            c_Rz_tmp[4] = b_Rz_tmp;
            c_Rz_tmp[7] = 0.0;
            ROM[0] = angle_z1;
            ROM[3] = 0.0;
            ROM[6] = angle_x1;
            c_Rz_tmp[2] = 0.0;
            ROM[1] = 0.0;
            c_Rz_tmp[5] = 0.0;
            ROM[4] = 1.0;
            c_Rz_tmp[8] = 1.0;
            ROM[7] = 0.0;
            ROM[2] = -angle_x1;
            ROM[5] = 0.0;
            ROM[8] = angle_z1;
            for (i = 0; i < 3; i++)
            {
                Rz_tmp = c_Rz_tmp[i];
                d = c_Rz_tmp[i + 3];
                k = (int)(c_Rz_tmp[i + 6]);
                for (int i1 = 0; i1 < 3; i1++)
                {
                    d_Rz_tmp[i + 3 * i1] = (Rz_tmp * ROM[3 * i1] + d * ROM[3 * i1 + 1]) +
                      (k) * ROM[3 * i1 + 2];
                }
            }

            c_Rz_tmp[0] = 1.0;
            c_Rz_tmp[3] = 0.0;
            c_Rz_tmp[6] = 0.0;
            c_Rz_tmp[1] = 0.0;
            c_Rz_tmp[4] = b_Rx_tmp;
            c_Rz_tmp[7] = -Rx_tmp;
            c_Rz_tmp[2] = 0.0;
            c_Rz_tmp[5] = Rx_tmp;
            c_Rz_tmp[8] = b_Rx_tmp;
            b_Rz_tmp = 0.0;

            // 逆解出来的杆长——全长
            b_Rx_tmp = 0.0;
            for (k = 0; k < 3; k++)
            {
                Rz_tmp = d_Rz_tmp[k];
                d = d_Rz_tmp[k + 3];
                angle_z1 = d_Rz_tmp[k + 6];
                angle_x1 = 0.0;
                for (i = 0; i < 3; i++)

                {
                    angle_y1 = (Rz_tmp * c_Rz_tmp[3 * i] + d * c_Rz_tmp[3 * i + 1]) + angle_z1
                      * c_Rz_tmp[3 * i + 2];
                    ROM[k + 3 * i] = angle_y1;
                    angle_x1 += angle_y1 * b[i];
                }

                angle_z1 = angle_x1 - (A1[k]);
                b_Rz_tmp += angle_z1 * angle_z1;
                Rz_tmp = ((ROM[k] * -105.0 + ROM[k + 3] * 90.0) + ROM[k + 6] * -34.68) -
                  (A2[k]);
                b_Rx_tmp += Rz_tmp * Rz_tmp;
            }

            double l1 = Math.Sqrt(b_Rz_tmp) - 407.24;

            // 杆的变化量
            double l2 = Math.Sqrt(b_Rx_tmp) - 407.24;

            //int[] pulse = new int[3] { (int)(-l1 * 6553.6*3), (int)(-l2 * 6553.6 * 3), (int)(angle_z * 18204.44) };
            int[] pulse = new int[3] { (int)(-l1 * 65535 / 3), (int)(-l2 * 65535 / 3), (int)(angle_z * 65535) };

            return pulse;
        }
    }
}
