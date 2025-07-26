using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAS
{
    class StringHelper
    {
        /// <summary>
        /// 用于测试串口功能的字节转换函数，在具体业务中不使用
        /// </summary>
        /// <param name="data">要转换的数组</param>
        /// <returns>转换完成的字符串</returns>
        public static string ByteArrayToHexString(byte[] data)
        {
            StringBuilder sb = new StringBuilder(data.Length * 3);
            foreach (byte b in data)
            {
                sb.Append(Convert.ToString(b, 16).PadLeft(2, '0').PadRight(3, ' '));
            }
            return sb.ToString().ToUpper();
        }
        /// <summary>
        /// 数据和校验
        /// </summary>
        /// <param name="buffer">需要校验的数据</param>
        /// <returns>校验和结果</returns>
        public static bool IsSUM(byte[] buffer)
        {
            byte bcc = 0;// Initial value
            for (int i = 4; i < 28; i++)
            {
                bcc += buffer[i];
            }
            return bcc == buffer[28];
        }
    }
}
