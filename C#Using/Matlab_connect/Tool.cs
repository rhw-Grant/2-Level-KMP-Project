using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matlab_connect
{
    internal class Tool
    {
        #region CSV文件读取函数
        public bool readCSV(string filePath, out DataTable dt)//从csv读取数据返回table
        {
            dt = new DataTable();
            try
            {
                System.Text.Encoding encoding = Encoding.Default;//GetType(filePath); //
                                                                 // DataTable dt = new DataTable();
                System.IO.FileStream fs = new System.IO.FileStream(filePath, System.IO.FileMode.Open,
                    System.IO.FileAccess.Read);
                System.IO.StreamReader sr = new System.IO.StreamReader(fs, encoding);
                //记录每次读取的一行记录
                string strLine = "";
                //记录每行记录中的各字段内容
                string[] aryLine = null;
                string[] tableHead = null;
                //标示列数
                int columnCount = 0;
                //标示是否是读取的第一行
                bool IsFirst = true;
                //逐行读取CSV中的数据
                while ((strLine = sr.ReadLine()) != null)
                {
                    if (IsFirst == true)
                    {
                        tableHead = strLine.Split(',');
                        IsFirst = false;
                        columnCount = tableHead.Length;
                        //创建列
                        for (int i = 0; i < columnCount; i++)
                        {
                            DataColumn dc = new DataColumn(tableHead[i]);
                            dt.Columns.Add(dc);
                        }
                    }
                    else
                    {
                        aryLine = strLine.Split(',');
                        DataRow dr = dt.NewRow();
                        for (int j = 0; j < columnCount; j++)

                        {
                            dr[j] = aryLine[j];
                        }
                        dt.Rows.Add(dr);
                    }
                }

                if (aryLine != null && aryLine.Length > 0)

                {
                    dt.DefaultView.Sort = tableHead[0] + " " + "asc";
                }
                sr.Close();
                fs.Close();
                return true;
            }

            catch (Exception)
            {
                return false;
            }

        }
        #endregion

        #region 三维状态下计算点与线段之间的最短路径
        public (double res, double[] D) DistanceParallel(double[] A, double[] B, double[] P)
        {
            // AB 为线 1 已知点坐标，P 为线 2 上一点
            double[] AB = { A[0] - B[0], A[1] - B[1], A[2] - B[2] };
            double[] AP = { A[0] - P[0], A[1] - P[1], A[2] - P[2] };

            double ABNormSquared = Math.Pow(AB[0], 2) + Math.Pow(AB[1], 2) + Math.Pow(AB[2], 2);
            double r = DotProduct(AP, AB) / ABNormSquared;
            double[] D;
            double res;
            if (r <= 0)
            {
                res = Norm(AP); // 在 BA 延长线上
                D = A;
            }
            else if (r >= 1)
            {
                double[] BP = { B[0] - P[0], B[1] - P[1], B[2] - P[2] };
                res = Norm(BP); // 在 AB 延长线上
                D = B;
            }
            else
            {
                res = PointIsOnLine(A, B, P); // 使用点到线距离计算方法
                double[] AD = { r * AB[0], r * AB[1], r * AB[2] };
                double[] D_temp ={ AD[0] + A[0], AD[1] + A[1], AD[2] + A[2] };
                D = D_temp;
            }
            return (res, D);
        }

        private static double DotProduct(double[] v1, double[] v2)
        {
            return v1[0] * v2[0] + v1[1] * v2[1] + v1[2] * v2[2];
        }

        private static double Norm(double[] v)
        {
            return Math.Sqrt(v[0] * v[0] + v[1] * v[1] + v[2] * v[2]);
        }

        private static double PointIsOnLine(double[] A, double[] B, double[] P)
        {
            double[] AB = { B[0] - A[0], B[1] - A[1], B[2] - A[2] };
            double[] AP = { P[0] - A[0], P[1] - A[1], P[2] - A[2] };
            double[] crossProduct = CrossProduct(AP, AB);
            return Norm(crossProduct) / Norm(AB);
        }

        private static double[] CrossProduct(double[] v1, double[] v2)
        {
            return new double[]
            {
            v1[1] * v2[2] - v1[2] * v2[1],
            v1[2] * v2[0] - v1[0] * v2[2],
            v1[0] * v2[1] - v1[1] * v2[0]
            };
        }
        #endregion


    }


}
