using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matlab_connect
{
    //用于保存需要改变位置的改变信息
    public class routeInfo : IComparable<routeInfo>
    {
        public double route_time;//插入点时间
        public double route_changeAngle;//插入点改变量
        public double route_changeVel;//插入点改变速度
        public int routeflag;//插入点改变方向

        public int CompareTo(routeInfo others)
        {
            if (this.route_time != others.route_time)
                return this.route_time.CompareTo(others.route_time);
            else
                return 0;
        }
    }

  


    public class DataTrasfer
    {
        //设置路径寄存数据
        private double[,] RouteData = new double[1000, 4];
        //设置flag，flag=1时调整x轴，flag=2时调整y轴，flag=3时调整z轴
        private bool[] flag = new bool[3] { false, false, false };
        //设置InsertTime，用来保存需要修正的时间,考虑到修正时间3个方向不相同，设置3个数组
        private double[] InsertTime_x;
        private double[] InsertTime_y;
        private double[] InsertTime_z;

        //定义xyz三个方向的链条数组，分别放xyz，正反方向的路径数据
        List<routeInfo>[] insertPoint_x = new List<routeInfo>[2];
        List<routeInfo>[] insertPoint_y = new List<routeInfo>[2];
        List<routeInfo>[] insertPoint_z = new List<routeInfo>[2];

        public void InitInsertPoint()
        {
            insertPoint_x[0] = new List<routeInfo>();
            insertPoint_x[1] = new List<routeInfo>();
            insertPoint_y[0] = new List<routeInfo>();
            insertPoint_y[1] = new List<routeInfo>();
            insertPoint_z[0] = new List<routeInfo>();
            insertPoint_z[1] = new List<routeInfo>();
        }

        public void setInsertPoint(double route_time, double route_changeAngle, double route_changeVel, int routeflag, int num)
            //packnum表示在这一组数据里，该数据为第几个
        {
            routeInfo temp = new routeInfo();
            temp.route_time = route_time;
            temp.route_changeAngle = route_changeAngle;
            temp.routeflag = routeflag;
            temp.route_changeVel = route_changeVel;
            if (num == 0)
            {
                if (this.insertPoint_x[routeflag].Count ==0)
                {
                    this.insertPoint_x[routeflag].Add(temp);
                    //Console.WriteLine("输入成功，{0}", temp.route_time);
                    //Console.WriteLine("{0}", this.insertPoint_x[routeflag].Count);


                }
                else
                {
                    if (this.insertPoint_x[routeflag][this.insertPoint_x[routeflag].Count-1].route_time != temp.route_time)
                    {
                        //Console.WriteLine("输入成功，{0},{1}", insertPoint_x[routeflag][this.insertPoint_x[routeflag].Count - 5].route_time, temp.route_time);
                        this.insertPoint_x[routeflag].Add(temp);
                        

                    }
                    else
                    {
                        Console.WriteLine("数字重复");
                    }
                }
 
            }
            else if (num == 1)
            {
                this.insertPoint_y[routeflag].Add(temp);
            }
            else if (num == 2)
            {
                this.insertPoint_z[routeflag].Add(temp);
            }
        }
        #region 用于根据插入时间给插入点排序
        public void dtInsertTimeSort()
        {
            insertPoint_x[0].Sort();
            insertPoint_x[1].Sort();
            insertPoint_y[0].Sort();
            insertPoint_y[1].Sort();
            insertPoint_z[0].Sort();
            insertPoint_z[0].Sort();
        }
        #endregion

        public List<routeInfo> getInsertPoint(int num,int flag)
        {
            if (num == 0) { return insertPoint_x[flag]; }
            else if (num == 1) { return insertPoint_y[flag]; }
            else if (num == 2) { return insertPoint_z[flag]; }
            else{ return null;}
        }
        public void cleanRouteData()
        {
            Array.Clear(InsertTime_x, 0, InsertTime_x.Length);
            Array.Clear(InsertTime_y, 0, InsertTime_y.Length);
            Array.Clear(InsertTime_z, 0, InsertTime_z.Length);
        }



        public void setRouteData(double[,] RouteData)
        {
            this.RouteData = RouteData;
        }

        //用来判断是那个轴需要修正
        public void setFlag(int num,bool check)
        {
            if(check==true)
            {
                flag[num] = true;
            }
            else
            {
                flag[num] = false;
            }
            
        }
        public void setInsertTime(double[] InsertTime_x, double[] InsertTime_y, double[] InsertTime_z)
        {
            this.InsertTime_x = InsertTime_x;
            this.InsertTime_y = InsertTime_y;
            this.InsertTime_z = InsertTime_z;
        }

        public double[,] getRouteData()
        {
            return RouteData;
        }
        public bool getFlag(int num)
        {
            return flag[num];
        }
        public double[] getInsertTime(int num)
        {
            if (num == 0 )
            {
                return InsertTime_x;
            }
            else if (num == 1)
            {
                return InsertTime_y;
            }
            else if (num == 2)
            {
                return InsertTime_z;
            }
            else
            {
                Console.WriteLine("没有这个轴");
                return null;
            }

        }

        public void CleanInsertPoint()
        {
            this.insertPoint_x[0].Clear();
            this.insertPoint_x[1].Clear();

            this.insertPoint_y[0].Clear();
            this.insertPoint_y[1].Clear();

            this.insertPoint_z[0].Clear();
            this.insertPoint_z[1].Clear();


        }

    }


}
