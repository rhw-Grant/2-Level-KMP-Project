using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Matlab_connect
{
    internal class VelocityJacobiUsing
    {
        double[,] E = { { 0 }, { 0 }, { 1 } };
        double[,] E1 = { { 0 }, { 1 }, { 0 } };
        double[,] E2 = { { 1 }, { 0 }, { 0 } };

        //旋转矩阵所用定义
        #region
        public float degree_x = 0;//输入角度
        public float degree_y = 0;
        public float degree_z = 0;
        private float angle_x1;//弧度制的角度
        private float angle_y1;
        private float angle_z1;
        double[,] Rz0;
        double[,] Rx0;
        double[,] Ry0;
        public Matrix ROM = new Matrix(3, 3);
        Matrix Rz = new Matrix(3, 3);
        Matrix Rx = new Matrix(3, 3);
        Matrix Ry = new Matrix(3, 3);
        Matrix RAi = new Matrix(3, 1);//求Rop*Ai矩阵乘积
        #endregion

        //求k1\k2\k3所用变量
        #region
        double[,] A1 = { { 105.0 }, { 90.0 }, { -34.68 } };//球铰链1动系坐标
        double[,] A2 = { { -105.0 }, { 90.0 }, { -34.68 } };//球铰链2动系坐标
        double[,] B1 = { { 189.92 }, { 90 }, { -429.44 } };//底部铰链1定系坐标
        double[,] B2 = { { -189.92 }, { 90 }, { -429.44 } };//底部铰链2定系坐标
        Matrix ai = new Matrix(3, 1);
        Matrix bi = new Matrix(3, 1);
        Matrix a1 = new Matrix(3, 1);//接收以上坐标用于矩阵运算
        Matrix a2 = new Matrix(3, 1);
        Matrix b1 = new Matrix(3, 1);
        Matrix b2 = new Matrix(3, 1);
        public double[,] R01_1;
        Matrix R01 = new Matrix(3, 3);
        Matrix inverse_of_R01 = new Matrix(3, 3);//R01的逆矩阵
        Matrix Rop_Ai_Bi = new Matrix(3, 1);
        double Rop_Ai_Bi_determinant; //Rop* Ai-Bi矩阵的行列式
        Matrix Ki = new Matrix(3, 1);
        Matrix Ez = new Matrix(3, 1);
        Matrix Ez1 = new Matrix(3, 1);
        Matrix Ez2 = new Matrix(3, 1);
        double ki1;
        double ki2;
        double ki3;
        #endregion

        //求矩阵R02、Zi3
        #region 
        private double angle_i1;
        private double angle_i2;
        double[,] Ri_12;
        Matrix R12_i = new Matrix(3, 3);
        Matrix R02_i = new Matrix(3, 3);
        Matrix Zi3 = new Matrix(3, 1);
        #endregion

        //求Q
        #region
        Matrix Et = new Matrix(1, 3);//E的转置
        Matrix Et1 = new Matrix(1, 3);//E1的转置
        Matrix Et2 = new Matrix(1, 3);//E2的转置        
        double Qi_12;
        double Qi_13;
        double Qi_21;
        double Qi_23;
        double Qi_31;
        double Qi_32;
        double[,] Qi;
        #endregion

        //求雅可比矩阵J、最终结果
        #region
        Matrix Zi3_T = new Matrix(1, 3);//Zi3矩阵的转置
        Matrix Qi_1 = new Matrix(3, 3);
        Matrix Zi3_T_Qi = new Matrix(1, 3);//Zi3矩阵的转置*Qi
        Matrix Z13_T_Q1 = new Matrix(1, 3);//接收Zi3矩阵的转置*Qi
        Matrix Z23_T_Q2 = new Matrix(1, 3);
        double[,] J;
        Matrix J_1 = new Matrix(3, 3);
        public double w;
        public double speed_x;
        public double speed_y;
        public double speed_z;
        double[,] speed_w;
        double[,] wx;
        double[,] wy;
        double[,] wz;
        Matrix W = new Matrix(3, 1);
        Matrix Speed = new Matrix(3, 1);
        public double[] speed;
        public bool X_axis = false;
        public bool Y_axis = false;
        public bool Z_axis = false;
        #endregion

        public void analysis()
        {

            parameterInit();
            for (int i = 0; i < 2; i++)
            {
                if (i == 0)
                {
                    matrix_Calculation(A1, B1);
                    jacobian_matrix_J(Zi3, Qi);
                        Z13_T_Q1 = Zi3_T_Qi;
                    }
                    else if (i == 1)
                    {
                        matrix_Calculation(A2, B2);
                        jacobian_matrix_J(Zi3, Qi);
                        Z23_T_Q2 = Zi3_T_Qi;
                    }
            }
            J_(Z13_T_Q1, Z23_T_Q2);

        }

        private void parameterInit()
        {
            angle_x1 = (float)(degree_x / 180 * Math.PI); // 转为弧度
            angle_y1 = (float)(degree_y / 180 * Math.PI);
            angle_z1 = (float)(degree_z / 180 * Math.PI); // 转为弧度           
            matriInit(angle_x1, angle_y1, angle_z1);
        }
        private void matriInit(float angle_x1, float angle_y1, float angle_z1)
        {
            Rz0 = new double[,]                 //二维嵌套数组
                                    { { Math.Cos(angle_z1),-Math.Sin(angle_z1), 0},
                                      {Math.Sin(angle_z1) , Math.Cos(angle_z1), 0},
                                      {0                  ,                  0, 1}};
            Rx0 = new double[,]
                                    { {1,                  0,                  0},
                                      {0, Math.Cos(angle_x1),-Math.Sin(angle_x1)},
                                      {0, Math.Sin(angle_x1) ,Math.Cos(angle_x1)}};
            Ry0 = new double[,]
                                    { { Math.Cos(angle_y1),0,Math.Sin(angle_y1)},
                                      {0                  ,1,                 0},
                                      {-Math.Sin(angle_y1),0,Math.Cos(angle_y1)}};
            Rz.Detail = Rz0;
            Rx.Detail = Rx0;
            Ry.Detail = Ry0;
            ROM = MatrixOperator.MatrixMulti(Rz, Rx);
            ROM = MatrixOperator.MatrixMulti(ROM, Ry);
        }
        private void matrix_Calculation(double[,] Ai, double[,] Bi)
        {
            R01_1 = new double[,]
                                   {{0,-1,0},
                                    {1,0,0 },
                                    {0,0,1 }};
            R01.Detail = R01_1;//定义R01数组
            inverse_of_R01 = MatrixOperator.MatrixInvByCom(R01);//求R01的逆矩阵

            //求Rop*Ai矩阵乘积
            ai.Detail = Ai;
            bi.Detail = Bi;
            RAi = MatrixOperator.MatrixMulti(ROM, ai);
            //求矩阵Rop*Ai-Bi
            Rop_Ai_Bi = MatrixOperator.MatrixSub(RAi, bi);
            //求Rop*Ai-Bi的行列式
            Rop_Ai_Bi_determinant = Math.Sqrt(Math.Pow(Rop_Ai_Bi.Detail[0, 0], 2.0) + Math.Pow(Rop_Ai_Bi.Detail[1, 0], 2.0) + Math.Pow(Rop_Ai_Bi.Detail[2, 0], 2.0));
            //求ki1、ki2、ki3
            Ki = MatrixOperator.MatrixMulti(inverse_of_R01, Rop_Ai_Bi);
            Ki = MatrixOperator.MatrixSimpleMulti(1 / Rop_Ai_Bi_determinant, Ki);
            ki1 = Ki.Detail[0, 0];
            ki2 = Ki.Detail[1, 0];
            ki3 = Ki.Detail[2, 0];
            define_Array(ki1, ki2, ki3);
            solve_Q(RAi);
        }

        private void define_Array(double ki1, double ki2, double ki3)
        {
            angle_i2 = Math.Atan(ki1 / Math.Sqrt(Math.Pow(ki2, 2.0) + Math.Pow(ki3, 2.0)));
            angle_i1 = Math.Atan(-(ki2 / ki3));

            Ri_12 = new double[,]
                                   {{Math.Cos(angle_i2),0,Math.Sin(angle_i2)},
                                    { Math.Sin(angle_i1)*Math.Sin(angle_i2),Math.Cos(angle_i1),-Math.Cos(angle_i2)*Math.Sin(angle_i1)},
                                    {-Math.Cos(angle_i1)*Math.Sin(angle_i2),Math.Sin(angle_i1),Math.Cos(angle_i1)*Math.Cos(angle_i2) }};

            R12_i.Detail = Ri_12;
            Ez.Detail = E;//(0,0,1)
            R02_i = MatrixOperator.MatrixMulti(R01, R12_i);
            Zi3 = MatrixOperator.MatrixMulti(R02_i, Ez);
        }

        private void solve_Q(Matrix RAi)
        {
            Ez.Detail = E;
            Ez1.Detail = E1;
            Ez2.Detail = E2;
            Et = MatrixOperator.MatrixTrans(Ez);//(0,0,1)
            Et1 = MatrixOperator.MatrixTrans(Ez1);//(0,1,0)
            Et2 = MatrixOperator.MatrixTrans(Ez2);//(1,0,0)

            Qi_12 = MatrixOperator.MatrixMulti(Et, RAi).Detail[0, 0];
            Qi_13 = MatrixOperator.MatrixMulti(Et1, RAi).Detail[0, 0];
            Qi_21 = MatrixOperator.MatrixMulti(Et, RAi).Detail[0, 0];
            Qi_23 = MatrixOperator.MatrixMulti(Et2, RAi).Detail[0, 0];
            Qi_31 = MatrixOperator.MatrixMulti(Et1, RAi).Detail[0, 0];
            Qi_32 = MatrixOperator.MatrixMulti(Et2, RAi).Detail[0, 0];

            Qi = new double[,]
                                    {{0,Qi_12,-Qi_13},
                                    {-Qi_21,0,Qi_23 },
                                    {Qi_31,-Qi_32,0 }};

        }
        private void J_(Matrix Z13_T_Q1, Matrix Z23_T_Q2)
        {
            J = new double[,]
                            {{ Z13_T_Q1.Detail[0,0],Z13_T_Q1.Detail[0,1],Z13_T_Q1.Detail[0,2]},
                             {Z23_T_Q2.Detail[0,0],Z23_T_Q2.Detail[0,1],Z23_T_Q2.Detail[0,2]},
                             {0,0,1 }};
            J_1.Detail = J;

            speed_w = new double[,] { { speed_x }, { speed_y }, { speed_z } };
            W.Detail = speed_w;

            //wx = new double[,] { { w }, { 0 }, { 0 } };
            //wy = new double[,] { { 0 }, { w }, { 0 } };
            //wz = new double[,] { { 0 }, { 0 }, { w } };
            //if (X_axis)
            //{
            //    W.Detail = wx;
            //}
            //else if (Y_axis)
            //{
            //    W.Detail = wy;
            //}
            //else if (Z_axis)
            //{
            //    W.Detail = wz;
            //}
            Speed = MatrixOperator.MatrixMulti(J_1, W);//w是一个3*1的向量
            speed = new double[3] { Speed.Detail[0, 0], Speed.Detail[1, 0], Speed.Detail[2, 0] };
            //Console.WriteLine("speed={0},{1},{2}", speed[0], speed[1], speed[2]);

        }

        private void jacobian_matrix_J(Matrix Zi3, double[,] Qi)
        {
            Zi3_T = MatrixOperator.MatrixTrans(Zi3);
            Qi_1.Detail = Qi;
            Zi3_T_Qi = MatrixOperator.MatrixMulti(Zi3_T, Qi_1);
        }
    }


}
