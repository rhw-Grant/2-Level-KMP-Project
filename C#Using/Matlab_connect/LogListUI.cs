using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Matlab_connect
{
    internal class LogListUI
    {
        public ListBox LogListBox;

        public void SetListBox(ListBox lb)
        {
            LogListBox = lb;
        }
        /// <summary>
        /// 输出日志
        /// </summary>
        /// <param name="PID">线程ID，建议使用Thread.CurrentThread.ManagedThreadId</param>
        /// <param name="model">模块</param>
        /// <param name="operation">操作</param>
        /// <param name="detail">细节</param>
        public void Log(int PID, String model, String operation, String detail = "")
        {

            LogListBox.Items.Insert(0, "[" + DateTime.Now.TimeOfDay + "]  ID:" + PID.ToString() + "  " + model + ": " + operation + " " + detail);
        }

    }
}
