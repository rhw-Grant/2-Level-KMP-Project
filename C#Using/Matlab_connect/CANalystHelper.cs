/*
 * @Author:Intron
 * @Data:2021.05.15
 * @Edition:V0.9.0
 * @Describe:
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Matlab_connect;



public struct VCI_BOARD_INFO
{
    public UInt16 hw_Version;   //硬件版本号，用16进制表示。比如0x0100表示V1.00。

    public UInt16 fw_Version;   //固件版本号，用16进制表示。比如0x0100表示V1.00。
    public UInt16 dr_Version;   //驱动程序版本号，用16进制表示。比如0x0100表示V1.00。
    public UInt16 in_Version;   //接口库版本号，用16进制表示。比如0x0100表示V1.00。
    public UInt16 irq_Num;      //保留参数。
    public byte can_Num;        //表示有几路CAN通道。
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)] public byte[] str_Serial_Num; //此板卡的序列号。
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 40)]
    public byte[] str_hw_Type;  //硬件类型，比如“USBCAN V1.00”（注意：包括字符串结束符’\0’）
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public byte[] Reserved;     //系统保留。
}

/////////////////////////////////////////////////////
//2.定义CAN信息帧的数据类型。
unsafe public struct VCI_CAN_OBJ  //使用不安全代码
{
    public uint ID;               //帧ID。32位变量，数据格式为靠右对齐。
    public uint TimeStamp;        //时间标识。设备接收到某一帧的时间标识。时间标示从CAN卡上电开始计时，计时单位为0.1ms。
    public byte TimeFlag;         //是否使用时间标识，为1时TimeStamp有效，TimeFlag和TimeStamp只在此帧为接收帧时有意义。
    public byte SendType;         //发送标志。=0时为正常发送（发送失败会自动重发，重发时间为4秒，4秒内没有发出则取消）；=1时为单次发送（只发送一次，发送失败不会自动重发，总线只产生一帧数据）；
    public byte RemoteFlag;       //是否是远程帧。=0时为为数据帧，=1时为远程帧（数据段空）。
    public byte ExternFlag;       //是否是扩展帧。=0时为标准帧（11位ID），=1时为扩展帧（29位ID）。
    public byte DataLen;          //数据长度，DLC (<=8)，即CAN帧Data有几个字节。约束了后面Data[8]中的有效字节。
    public fixed byte Data[8];    //数据。由于CAN规定了最大是8个字节，所以这里预留了8个字节的空间，受DataLen约束。如DataLen定义为3，即Data[0]、Data[1]、Data[2]是有效的。
    public fixed byte Reserved[3];//保留位


}

//3.定义初始化CAN的数据类型
public struct VCI_INIT_CONFIG
{
    public UInt32 AccCode;//验收码。SJA1000的帧过滤验收码。对经过屏蔽码过滤为“有关位”进行匹配，全部匹配成功后，此帧可以被接收。否则不接收。详见VCI_InitCAN。
    public UInt32 AccMask;//屏蔽码。SJA1000的帧过滤屏蔽码。对接收的CAN帧ID进行过滤，对应位为0的是“有关位”，对应位为1的是“无关位”。屏蔽码推荐设置为0xFFFFFFFF，即全部接收。
    public UInt32 Reserved;//
    public byte Filter;   //0或1接收所有帧。2标准帧滤波，3是扩展帧滤波。
    public byte Timing0;  //波特率参数，具体配置，请查看下面的表格
    public byte Timing1;
    public byte Mode;     //模式，0表示正常模式，1表示只听模式,2自测模式

    /*
    //用的就是500kbs
     * 波特率	10 Kbps	20 Kbps	40 Kbps	50 Kbps	80 Kbps	100 Kbps
     *  BTR0	0x31	0x18	0x87	0x09	0x83	0x04
     *  BTR1	0x1C	0x1C	0xFF	0x1C	0xFF	0x1C

     *  波特率	125 Kbps	200 Kbps	250 Kbps	400 Kbps	500 Kbps	666 Kbps
     *  BTR0	0x03	0x81	0x01	0x80	0x00	0x80
     *  BTR1	0x1C	0xFA	0x1C	0xFA	0x1C	0xB6

     *  波特率	800 Kbps	1000 Kbps	33.33 Kbps	66.66 Kbps	83.33 Kbps	
     *  BTR0	0x00	0x00	0x09	0x04	0x03	
     *  BTR1	0x16	0x14	0x6F	0x6F	0x6F	

 */
}

/*------------其他数据结构描述---------------------------------*/
//4.USB-CAN总线适配器板卡信息的数据类型1，该类型为VCI_FindUsbDevice函数的返回参数。
public struct VCI_BOARD_INFO1
{
    public UInt16 hw_Version;//硬件版本号，用16进制表示。比如0x0100表示V1.00。
    public UInt16 fw_Version;//固件版本号，用16进制表示。比如0x0100表示V1.00。
    public UInt16 dr_Version;//驱动程序版本号，用16进制表示。比如0x0100表示V1.00。
    public UInt16 in_Version;//接口库版本号，用16进制表示。比如0x0100表示V1.00。
    public UInt16 irq_Num;//保留参数。
    public byte can_Num;//表示有几路CAN通道。
    public byte Reserved;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
    public byte[] str_Serial_Num;//此板卡的序列号。
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
    public byte[] str_hw_Type;//硬件类型，比如“USBCAN V1.00”（注意：包括字符串结束符’\0’）
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
    public byte[] str_Usb_Serial;//
}

/*------------数据结构描述完成---------------------------------*/

public struct CHGDESIPANDPORT
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
    public byte[] szpwd;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
    public byte[] szdesip;
    public Int32 desport;

    public void Init()
    {
        szpwd = new byte[10];
        szdesip = new byte[20];
    }
}


//修改的和创建时的文件名一致
namespace CANNalyst     //------------------>注意：这个命名空间最好修改一下<------------------------\\
{
    class CANalystHelper
    {

        /*
         * 接口库函数使用流程
         * 
         * 
                                +----------------+
                                | VCI_OpenDevice |
                                +----------------+
                                        |
                                +-------v--------+
                                |  VCI_InitCAN   |
                                +----------------+
         +------------------+           |
         |VCI_GetReceiveNum +---------->+             +---------------+
         +------------------+           | <-----------+VCI_ClearBuffer|
         +------------------+           |             +---------------+
         | VCI_ReadBoardInfo+---------->+
         +------------------+           |                   
                                        |
     +------------------------------------------------------------------+
     |    运行主框架                    |                               |
     |                                  +<-------------------+          |
     |                                  |                    |          |
     |                          +-------v--------+           |          |
     |                          |  VCI_StartCAN  |           |          |
     |                          +----------------+           |          |
     |                                  |             +------------+    |
     |                                  | <-----------+VCI_ResetCAN|    |
     |                                  |             +------------+    |
     |    +--------------+              |                               |
     |    | VCI_Transmit |------------->+             +------------+    |
     |    +--------------+              | <-----------+ VCI_Recive |    |
     |                                  v             +------------+    |
     |                          +---------------+                       |
     |                          |VCI_CloseDevice|                       |
     |                          +---------------+                       |
     |                                                                  |
     +------------------------------------------------------------------+

         * 
         */


        /*------------分析仪设备类型声明---------------------------------*/
        public const int DEV_USBCAN = 3;
        public const int DEV_USBCAN2 = 4;



        /*------------函数声明描述---------------------------------*/
        /// <summary>
        /// 此函数用以打开设备。注意一个设备只能打开一次。
        /// </summary>
        /// <param name="DeviceType">设备类型。对应不同的产品型号</param>
        /// <param name="DeviceInd">设备索引，比如当只有一个USB-CAN适配器时，索引号为0，这时再插入一个USB-CAN适配器那么后面插入的这个设备索引号就是1，以此类推。</param>
        /// <param name="Reserved">保留参数，通常为0。</param>
        /// <returns>返回值=1，表示操作成功；=0表示操作失败</returns>
        [DllImport("controlcan.dll")]
        public static extern UInt32 VCI_OpenDevice(UInt32 DeviceType, UInt32 DeviceInd, UInt32 Reserved);

        /// <summary>
        /// 此函数用以关闭设备。
        /// </summary>
        /// <param name="DeviceType">设备类型。对应不同的产品型号</param>
        /// <param name="DeviceInd">设备索引，比如当只有一个USB-CAN适配器时，索引号为0，这时再插入一个USB-CAN适配器那么后面插入的这个设备索引号就是1，以此类推。对应已经打开的设备。</param>
        /// <returns>返回值=1，表示操作成功；=0表示操作失败</returns>
        [DllImport("controlcan.dll")]
        /// <returns>返回值=1，表示操作成功；=0表示操作失败</returns>
        public static extern UInt32 VCI_CloseDevice(UInt32 DeviceType, UInt32 DeviceInd);

        /// <summary>
        /// 此函数用以初始化指定的CAN通道。有多个CAN通道时，需要多次调用。
        /// </summary>
        /// <param name="DeviceType">设备类型。对应不同的产品型号</param>
        /// <param name="DeviceInd">设备索引，比如当只有一个USB-CAN适配器时，索引号为0，这时再插入一个USB-CAN适配器那么后面插入的这个设备索引号就是1，以此类推。</param>
        /// <param name="CANInd">CAN通道索引。第几路CAN。即对应卡的CAN通道号，CAN1为0，CAN2为1。</param>
        /// <param name="pInitConfig">初始化参数结构。</param>
        /// <returns></returns>
        [DllImport("controlcan.dll")]
        public static extern UInt32 VCI_InitCAN(UInt32 DeviceType, UInt32 DeviceInd, UInt32 CANInd, ref VCI_INIT_CONFIG pInitConfig);

        /// <summary>
        /// 此函数用以获取设备信息。
        /// </summary>
        /// <param name="DeviceType">设备类型。对应不同的产品型号</param>
        /// <param name="DeviceInd">设备索引，比如当只有一个USB-CAN适配器时，索引号为0，这时再插入一个USB-CAN适配器那么后面插入的这个设备索引号就是1，以此类推。</param>
        /// <param name="pInfo">用来存储设备信息的VCI_BOARD_INFO结构指针。</param>
        /// <returns>返回值=1，表示操作成功；=0表示操作失败</returns>
        [DllImport("controlcan.dll")]
        public static extern UInt32 VCI_ReadBoardInfo(UInt32 DeviceType, UInt32 DeviceInd, ref VCI_BOARD_INFO pInfo);

        /// <summary>
        /// 此函数用以获取指定CAN通道的接收缓冲区中，接收到但尚未被读取的帧数量。主要用途是配合VCI_Receive使用，即缓冲区有数据，再接收。实际应用中，用户可以忽略该函数，直接循环调用VCI_Receive，可以节约PC系统资源，提高程序效率。
        /// </summary>
        /// <param name="DeviceType">设备类型。对应不同的产品型号</param>
        /// <param name="DeviceInd">设备索引，比如当只有一个USB-CAN适配器时，索引号为0，这时再插入一个USB-CAN适配器那么后面插入的这个设备索引号就是1，以此类推。</param>
        /// <param name="CANInd">CAN通道索引。第几路CAN。即对应卡的CAN通道号，CAN1为0，CAN2为1。</param>
        /// <returns>返回尚未被读取的帧数，=-1表示USB-CAN设备不存在或USB掉线。</returns>
        [DllImport("controlcan.dll")]
        public static extern UInt32 VCI_GetReceiveNum(UInt32 DeviceType, UInt32 DeviceInd, UInt32 CANInd);

        /// <summary>
        /// 此函数用以清空指定CAN通道的缓冲区。主要用于需要清除接收缓冲区数据的情况,同时发送缓冲区数据也会一并清除。
        /// </summary>
        /// <param name="DeviceType">设备类型。对应不同的产品型号</param>
        /// <param name="DeviceInd">设备索引，比如当只有一个USB-CAN适配器时，索引号为0，这时再插入一个USB-CAN适配器那么后面插入的这个设备索引号就是1，以此类推。</param>
        /// <param name="CANInd">CAN通道索引。第几路CAN。即对应卡的CAN通道号，CAN1为0，CAN2为1。</param>
        /// <returns>返回值=1，表示操作成功；=0表示操作失败</returns>
        [DllImport("controlcan.dll")]
        public static extern UInt32 VCI_ClearBuffer(UInt32 DeviceType, UInt32 DeviceInd, UInt32 CANInd);

        /// <summary>
        /// 此函数用以启动CAN卡的某一个CAN通道。有多个CAN通道时，需要多次调用。
        /// </summary>
        /// <param name="DeviceType">设备类型。对应不同的产品型号</param>
        /// <param name="DeviceInd">设备索引，比如当只有一个USB-CAN适配器时，索引号为0，这时再插入一个USB-CAN适配器那么后面插入的这个设备索引号就是1，以此类推。</param>
        /// <param name="CANInd">CAN通道索引。第几路CAN。即对应卡的CAN通道号，CAN1为0，CAN2为1。</param>
        /// <returns>返回值=1，表示操作成功；=0表示操作失败</returns>
        [DllImport("controlcan.dll")]
        public static extern UInt32 VCI_StartCAN(UInt32 DeviceType, UInt32 DeviceInd, UInt32 CANInd);

        /// <summary>
        /// 此函数用以复位 CAN。主要用与 VCI_StartCAN配合使用，无需再初始化，即可恢复CAN卡的正常状态。比如当CAN卡进入总线关闭状态时，可以调用这个函数。
        /// </summary>
        /// <param name="DeviceType">设备类型。对应不同的产品型号</param>
        /// <param name="DeviceInd">设备索引，比如当只有一个USB-CAN适配器时，索引号为0，这时再插入一个USB-CAN适配器那么后面插入的这个设备索引号就是1，以此类推。</param>
        /// <param name="CANInd">CAN通道索引。第几路CAN。即对应卡的CAN通道号，CAN1为0，CAN2为1。</param>
        /// <returns>返回值=1，表示操作成功；=0表示操作失败</returns>
        [DllImport("controlcan.dll")]
        public static extern UInt32 VCI_ResetCAN(UInt32 DeviceType, UInt32 DeviceInd, UInt32 CANInd);

        /// <summary>
        /// 发送函数。返回值为实际发送成功的帧数。
        /// </summary>
        /// <param name="DeviceType">设备类型。对应不同的产品型号</param>
        /// <param name="DeviceInd">设备索引，比如当只有一个USB-CAN适配器时，索引号为0，这时再插入一个USB-CAN适配器那么后面插入的这个设备索引号就是1，以此类推。</param>
        /// <param name="CANInd">CAN通道索引。第几路CAN。即对应卡的CAN通道号，CAN1为0，CAN2为1。</param>
        /// <param name="pSend">要发送的帧结构体 VCI_CAN_OBJ数组的首指针。</param>
        /// <param name="Len">要发送的帧结构体数组的长度（发送的帧数量）。最大为1000，建议设为1，每次发送单帧，以提高发送效率。</param>
        /// <returns>返回实际发送的帧数，=-1表示USB-CAN设备不存在或USB掉线。</returns>
        [DllImport("controlcan.dll")]
        public static extern UInt32 VCI_Transmit(UInt32 DeviceType, UInt32 DeviceInd, UInt32 CANInd, ref VCI_CAN_OBJ pSend, UInt32 Len);

        /// <summary>
        /// 接收函数。此函数从指定的设备CAN通道的接收缓冲区中读取数据。
        /// </summary>
        /// <param name="DeviceType">设备类型。对应不同的产品型号</param>
        /// <param name="DeviceInd">设备索引，比如当只有一个USB-CAN适配器时，索引号为0，这时再插入一个USB-CAN适配器那么后面插入的这个设备索引号就是1，以此类推。</param>
        /// <param name="CANInd">CAN通道索引。第几路CAN。即对应卡的CAN通道号，CAN1为0，CAN2为1。</param>
        /// <param name="pReceive">用来接收的帧结构体VCI_CAN_OBJ数组的首指针。注意：数组的大小一定要比下面的len参数大，否则会出现内存读写错误。</param>
        /// <param name="Len">用来接收的帧结构体数组的长度（本次接收的最大帧数，实际返回值小于等于这个值）。该值为所提供的存储空间大小，适配器中为每个通道设置了2000帧左右的接收缓存区，用户根据自身系统和工作环境需求，在1到2000之间选取适当的接收数组长度。</param>
        /// <param name="WaitTime">保留参数。</param>
        /// <returns></returns>
        [DllImport("controlcan.dll")]
        public static extern UInt32 VCI_Receive(UInt32 DeviceType, UInt32 DeviceInd, UInt32 CANInd, ref VCI_CAN_OBJ pReceive, UInt32 Len, Int32 WaitTime);

        /*------------其他函数描述---------------------------------*/
        /// <summary>
        /// 8 
        /// </summary>
        /// <param name="DevType"></param>
        /// <param name="DevIndex"></param>
        /// <returns></returns>
        [DllImport("controlcan.dll")]
        public static extern UInt32 VCI_ConnectDevice(UInt32 DevType, UInt32 DevIndex);

        /// <summary>
        /// 复位USB-CAN适配器，复位后需要重新使用VCI_OpenDevice打开设备。等同于插拔一次USB设备。
        /// </summary>
        /// <param name="DevType">设备类型。对应不同的产品型号</param>
        /// <param name="DevIndex">设备索引，比如当只有一个USB-CAN适配器时，索引号为0，当有多个时，索引号从0开始依次递增。</param>
        /// <param name="Reserved">保留。</param>
        /// <returns>返回值=1，表示操作成功；=0表示操作失败</returns>
        [DllImport("controlcan.dll")]
        public static extern UInt32 VCI_UsbDeviceReset(UInt32 DevType, UInt32 DevIndex, UInt32 Reserved);

        /// <summary>
        /// 当同一台PC上使用多个USB-CAN的时候，可用此函数查找当前的设备，并获取所有设备的序列号。最多支持50个设备。
        /// </summary>
        /// <param name="pInfo">结构体数组首地址，用来存储设备序列号等信息的结构体数组。数组长度建议定义为50，如：VCI_BOARD_INFO pInfo[50]。</param>
        /// <returns>返回计算机中已插入的USB-CAN适配器的数量。</returns>
        [DllImport("controlcan.dll")]
        public static extern UInt32 VCI_FindUsbDevice(ref VCI_BOARD_INFO1 pInfo);
        /*------------函数描述结束---------------------------------*/


        /*------------默认CAN分析仪参数配置---------------------------------*/
        public static UInt32 m_devtype = DEV_USBCAN2;//USBCAN2

        public UInt32 m_bOpen = 0;
        public UInt32 m_devind = 0;//设备索引号0；比如当只有一个USB-CAN适配器时，索引号为0，这时再插入一个USB-CAN适配器那么后面插入的这个设备索引号就是1，以此类推。
        public UInt32 m_canind = 0;//CAN通道索引。第几路CAN。即对应卡的CAN通道号，CAN1为0，CAN2为1。

        VCI_CAN_OBJ[] m_recobj = new VCI_CAN_OBJ[1000];

        UInt32[] m_arrdevtype = new UInt32[20];


        public string AccCode = "00000000";//验收码
        public string AccMask = "FFFFFFFF";//屏蔽码
        public string Timing0 = "00";
        public string Timing1 = "1C";

        public Byte CANFilter = 1;//滤波方式：1--接受全部；2--只接收标准帧；3--只接收扩展帧；
        public Byte CANMode = 0;//接收模式：0--正常；1--只收；2--回环；

        System.Timers.Timer RevTimer = new System.Timers.Timer(2000);
        static bool IsAdditionalClear = true;//额外清除buffer开关


        /*------------默认CAN分析仪参数配置结束---------------------------------*/




        /*
         * 错误代码：
         * 0x00---00---000
         * 前两位：错误过程
         * 中间两位：错误结果
         * 最后三位：错误位置
         * 
         */
        /// <summary>
        /// 类初始化
        /// </summary>
        public CANalystHelper()
        {
            RevTimer.Elapsed += new System.Timers.ElapsedEventHandler(Rev2Timer);

        }


        public void GetBordInfo()
        {
            VCI_BOARD_INFO info = new VCI_BOARD_INFO();
            VCI_ReadBoardInfo(m_devtype, m_devind, ref info);
            Console.WriteLine(info);
        }


        /// <summary>
        /// 配置CAN分析仪参数
        /// </summary>
        /// <param name="Acccode">验收码</param>
        /// <param name="Accmask">屏蔽码</param>
        /// <param name="T0">时间参数1</param>
        /// <param name="T1">时间参数2</param>
        /// <param name="Fliter">滤波方式：1--接受全部；2--只接收标准帧；3--只接收扩展帧</param>
        /// <param name="Mode">接收模式：0--正常；1--只收；2--回环</param>
        /// <param name="DevID">设备索引号0；比如当只有一个USB-CAN适配器时，索引号为0，这时再插入一个USB-CAN适配器那么后面插入的这个设备索引号就是1，以此类推。</param>
        /// <param name="CANID">CAN通道索引。第几路CAN。即对应卡的CAN通道号，CAN1为0，CAN2为1。</param>
        public void SetCANDeviceConfig(string Acccode = "00000000", string Accmask = "FFFFFFFF", string T0 = "00", string T1 = "1C", Byte Fliter = 1, Byte Mode = 0, UInt32 DevID = 0, UInt32 CANID = 0)
        {
            AccCode = Acccode;
            AccMask = Accmask;
            Timing0 = T0;
            Timing1 = T1;
            CANFilter = Fliter;
            CANMode = Mode;
            m_devind = DevID;
            m_canind = CANID;
        }

        /// <summary>
        /// 此函数用以打开设备。注意一个设备只能打开一次。
        /// </summary>
        /// <param name="IsMesgBox">当发生错误时，是否弹窗进行提示</param>
        /// <returns>返回值=true，表示操作成功；=false表示操作失败；</returns>
        public bool Strat(bool IsMesgBox = false)//默认是false
        {
            if (VCI_OpenDevice(m_devtype, m_devind, 0) == 0)
            {
                if (IsMesgBox) MessageBox.Show("打开设备失败,请检查设备类型和设备索引号是否正确", "错误",
                         MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                RevTimer.Stop();
                return false;
            }
            VCI_INIT_CONFIG config = new VCI_INIT_CONFIG();
            config.AccCode = System.Convert.ToUInt32("0x" + AccCode, 16);
            config.AccMask = System.Convert.ToUInt32("0x" + AccMask, 16);
            config.Timing0 = System.Convert.ToByte("0x" + Timing0, 16);
            config.Timing1 = System.Convert.ToByte("0x" + Timing1, 16);
            config.Filter = CANFilter;
            config.Mode = CANMode;

            if (VCI_InitCAN(m_devtype, m_devind, m_canind, ref config) == 0)
            {
                if (IsMesgBox) MessageBox.Show("初始化设备失败,USB-CAN设备不存在或USB掉线,请检查设备类型和设备索引号是否正确", "错误",
                        MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                RevTimer.Stop();
                return false;
            }

            if (VCI_StartCAN(m_devtype, m_devind, m_canind) == 0)
            {
                if (IsMesgBox) MessageBox.Show("启动设备失败,USB-CAN设备不存在或USB掉线,请检查设备类型和设备索引号是否正确", "错误",
                        MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                RevTimer.Stop();
                return false;
            }
            RevTimer.Start();
            return true;

        }

        /// <summary>
        /// 此函数用以关闭设备。
        /// </summary>
        /// <param name="IsMesgBox">当发生错误时，是否弹窗进行提示</param>
        /// <returns>返回值=true，表示操作成功；=false表示操作失败；</returns>
        public bool Close(bool IsMesgBox = false)
        {
            if (VCI_CloseDevice(m_devtype, m_devind) == 0)
            {
                if (IsMesgBox) MessageBox.Show("关闭设备失败,USB-CAN设备不存在或USB掉线,请检查设备类型和设备索引号是否正确", "错误",
                        MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return false;
            }
            RevTimer.Stop();
            return true;
        }

        /// <summary>
        /// 此函数用以清空指定CAN通道的缓冲区。主要用于需要清除接收缓冲区数据的情况,同时发送缓冲区数据也会一并清除。
        /// </summary>
        /// <param name="id">CAN通道索引。第几路CAN。即对应卡的CAN通道号，CAN1为0，CAN2为1。</param>
        /// <returns>返回值=true，表示操作成功；=false表示操作失败；</returns>
        public bool ClearBuffer(UInt32 id = 0, bool IsMesgBox = false)
        {
            if (VCI_ClearBuffer(m_devtype, m_devind, id) == 0)
            {
                if (IsMesgBox) MessageBox.Show("清楚缓冲区失败,USB-CAN设备不存在或USB掉线,请检查设备类型和设备索引号是否正确", "错误",
                        MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return false;
            }
            return true;
        }

        /// <summary>
        /// 发送一条CAN消息
        /// </summary>
        /// <param name="id">CAN ID，格式为"00000000"由八位组成</param>
        /// <param name="Msg">要发送的消息，格式为"00000000"由八位组成</param>
        /// <param name="IsMesgBox">当发生错误时，是否弹窗进行提示</param>
        /// <returns>返回值=true，表示操作成功；=false表示操作失败；</returns>
        unsafe public bool SendOneMsg(string id, string Msg, bool IsMesgBox = false)
        {
            VCI_CAN_OBJ sendobj = new VCI_CAN_OBJ();
            sendobj.RemoteFlag = (byte)0;
            sendobj.ExternFlag = (byte)0;
            sendobj.ID = System.Convert.ToUInt32("0x" + id, 16);
            int len = (Msg.Length + 1) / 3;
            sendobj.DataLen = System.Convert.ToByte(len);
            String strdata = Msg;
            int i = -1;
            try
            {
                if (i++ < len - 1)
                    sendobj.Data[0] = System.Convert.ToByte("0x" + strdata.Substring(i * 3, 2), 16);
                if (i++ < len - 1)
                    sendobj.Data[1] = System.Convert.ToByte("0x" + strdata.Substring(i * 3, 2), 16);
                if (i++ < len - 1)
                    sendobj.Data[2] = System.Convert.ToByte("0x" + strdata.Substring(i * 3, 2), 16);
                if (i++ < len - 1)
                    sendobj.Data[3] = System.Convert.ToByte("0x" + strdata.Substring(i * 3, 2), 16);
                if (i++ < len - 1)
                    sendobj.Data[4] = System.Convert.ToByte("0x" + strdata.Substring(i * 3, 2), 16);
                if (i++ < len - 1)
                    sendobj.Data[5] = System.Convert.ToByte("0x" + strdata.Substring(i * 3, 2), 16);
                if (i++ < len - 1)
                    sendobj.Data[6] = System.Convert.ToByte("0x" + strdata.Substring(i * 3, 2), 16);
                if (i++ < len - 1)
                    sendobj.Data[7] = System.Convert.ToByte("0x" + strdata.Substring(i * 3, 2), 16);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }


            if (VCI_Transmit(m_devtype, m_devind, m_canind, ref sendobj, 1) == 0)
            {
                if (IsMesgBox) MessageBox.Show("发送失败", "错误",
                        MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return false;
            }
            else { 
                //Console.WriteLine("[OK]" + "\t" + sendobj.ID + "\t" + strdata); 
                return true; }
        }
        public string b;
        unsafe public void A()
        {

        }

        public float angle = 10;
        public float angle1 = 10;
        public float angle2;
        public float angle3;
        public string wer;
        public double sudu;
        public string weizhi;
        public string weizhi1;
        public string weizhi2;
        public string weizhi3;
        public string weizhi4;
        public string askingposition1;
        public string sudu1;
        public string sudu2;
        public string sudu3;
        public string sudu4;
        public string sudua;
        public string zzz;
        public double adc;
        public int fadc = 0;
        public int fadc1 = 0;
        public int biaozhi;//bioazhi:等于1时，显示速度；等于2时，显示位置
        /// <summary>
        /// 调用此函数可以实现读取所有收到的消息。
        /// </summary>
        /// <returns></returns>
        unsafe public Queue RevAllMsg()
        {
            UInt32 res = new UInt32();

            Queue Rev = new Queue(2000);

            res = VCI_Receive(m_devtype, m_devind, m_canind, ref m_recobj[0], 1000, 100);

            if (res == 0xFFFFFFFF) res = 0;//当设备未初始化时，返回0xFFFFFFFF，不进行列表显示。
            String str = "";

            for (UInt32 i = 0; i < res; i++)
            {

                str = "接收到数据: ";
                str += "  帧ID:0x" + System.Convert.ToString(m_recobj[i].ID, 16);
                str += "  帧格式:";
                if (m_recobj[i].RemoteFlag == 0)
                    str += "数据帧 ";
                else
                    str += "远程帧 ";
                if (m_recobj[i].ExternFlag == 0)
                    str += "标准帧 ";
                else
                    str += "扩展帧 ";

                //////////////////////////////////////////
                if (m_recobj[i].RemoteFlag == 0)
                {
                    str += "数据: ";
                    byte len = (byte)(m_recobj[i].DataLen % 9);
                    byte j = 0;
                    fixed (VCI_CAN_OBJ* m_recobj1 = &m_recobj[i])
                    {
                        if (j++ < len)
                            str += " " + System.Convert.ToString(m_recobj1->Data[0], 16);
                        askingposition1 = System.Convert.ToString(m_recobj1->Data[0], 16);
                        if (j++ < len)
                            str += " " + System.Convert.ToString(m_recobj1->Data[1], 16);
                        askingposition1 += System.Convert.ToString(m_recobj1->Data[1], 16);
                        if (j++ < len)
                            str += " " + System.Convert.ToString(m_recobj1->Data[2], 16);
                        askingposition1 += System.Convert.ToString(m_recobj1->Data[2], 16);
                        if (j++ < len)
                            str += " " + System.Convert.ToString(m_recobj1->Data[3], 16);
                        askingposition1 += System.Convert.ToString(m_recobj1->Data[3], 16);
                        if (j++ < len)
                            str += " " + System.Convert.ToString(m_recobj1->Data[4], 16);
                        if (askingposition1 == "4364600")
                        {
                            weizhi1 = System.Convert.ToString(m_recobj1->Data[4], 16);
                            if (weizhi1.Length == 1) weizhi1 = "0" + weizhi1;
                        }
                        else if (askingposition1 == "436c600")
                        {
                            sudu1 = System.Convert.ToString(m_recobj1->Data[4], 16);
                            //if (sudu1.Length == 1) sudu1 = "0" + sudu1;
                        }
                        if (j++ < len)
                            str += " " + System.Convert.ToString(m_recobj1->Data[5], 16);
                        if (askingposition1 == "4364600")
                        {
                            weizhi2 = System.Convert.ToString(m_recobj1->Data[5], 16);
                            if (weizhi2.Length == 1) weizhi2 = "0" + weizhi2;
                        }
                        else if (askingposition1 == "436c600")
                        {
                            sudu2 = System.Convert.ToString(m_recobj1->Data[5], 16);
                            //if (sudu2.Length == 1) sudu2 = "0" + sudu2;
                        }
                        if (j++ < len)
                            str += " " + System.Convert.ToString(m_recobj1->Data[6], 16);
                        if (askingposition1 == "4364600")
                        {
                            weizhi3 = System.Convert.ToString(m_recobj1->Data[6], 16);
                            if (weizhi3.Length == 1) weizhi3 = "0" + weizhi3;
                        }
                        else if (askingposition1 == "436c600")
                        {
                            sudu3 = System.Convert.ToString(m_recobj1->Data[6], 16);
                            //if (sudu3.Length == 1) sudu3 = "0" + sudu3;
                        }
                        if (j++ < len)
                            str += " " + System.Convert.ToString(m_recobj1->Data[7], 16);
                        if (askingposition1 == "4364600")
                        {
                            weizhi4 = System.Convert.ToString(m_recobj1->Data[7], 16);
                            //if (weizhi4.Length == 1) weizhi4 = "0" + weizhi4;
                            zzz = weizhi4 + weizhi3 + weizhi2 + weizhi1;
                            Console.WriteLine("ID是{0}", m_recobj[i].ID);
                            switch (m_recobj[i].ID)
                            {

                            }

                            //Console.WriteLine("zzz=" + zzz);

                            //if (askingposition1 == "4364600") Console.WriteLine("位置是" + zzz);
                            //else Console.WriteLine("不是发送的询问位置");
                            //angle2 = fadc / 5461;
                            //angle = (float)Math.Ceiling(angle2);
                        }
                        else if (askingposition1 == "436c600")
                        {
                            sudu4 = System.Convert.ToString(m_recobj1->Data[7], 16);
                            if (sudu4.Length == 1) sudu4 = "0" + sudu4;
                            sudua = sudu4 + sudu3 + sudu2 + sudu1;
                            sudu = Convert.ToInt32(sudua, 16);
                            Console.WriteLine("速度=" + sudu);
                            //if (askingposition1 == "4364600") Console.WriteLine("位置是" + zzz);
                            //else Console.WriteLine("不是发送的询问位置");
                            //angle2 = fadc / 5461;
                            //angle = (float)Math.Ceiling(angle2);
                        }
                    }
                }
                //Console.WriteLine(str);
                Rev.Enqueue(str);
            }

            return Rev;
        }

        private void Rev2Timer(object sender, System.Timers.ElapsedEventArgs e)//定时器一秒读一次
        {
            if (IsAdditionalClear)
            {
                RevAllMsg();
            }
        }

    }



    class EposDriver
    {
        /*------------CANOpen报文---------------------------------*/

        /*
         * 控制字命令
         */

        //public const string CTR_WORD_SWITCH_ON = "2B 40 60 00 07 00 00 00";
        //public const string CTR_WORD_FAULT_RESET = "2B 40 60 00 40 00 00 00";
        public const string CTR_WORD_ENABLE = "2B 40 60 00 00 00 00 00";
        public const string CTR_WORD_ENABLE_OPERATION = "2B 40 60 00 0F 00 00 00";
        public const string CTR_WORD_SWITCH_DISABLE = "2B 40 60 00 06 00 00 00";
        public const string CTR_WORD_ENABLE_IP_OP = "2B 40 60 00 1F 00 00 00";
        public const string CTR_WORD_DISENABLE_IP = "2B 40 60 00 0F 00 00 00";
        public const string CTR_WORD_QUICK_STOP = "2B 40 60 00 0B 00 00 00";
        public const string Max_Profile_Velocity = "23 7F 60 00 6A 00 B8 B8";
        public const string Profile_Acc = "23 83 60 00 E8 E8 88 00";
        public const string Profile_Dec = "23 84 60 00 E8 88 00 00";
        public const string Quick_Stop_Dec = "23 85 60 00 00 00 00 00";
        public const string Motion_Profile_Type = "23 86 60 00 88 00 00 00";
        /*
         * 电机运动模式
         */
        public const string MODE_WORD_IP = "2F 60 60 00 07 00 00 00";
        public const string MODE_WORD_VELOCITY = "2F 60 60 00 03 00 00 00";
        public const string MODE_WORD_POSITION = "2F 60 60 00 01 00 00 00";


        /*
         * 插补运动模式配置
         */


        public const string IP_MODE_Profile_Velocity_SET = "23 81 60 00 E8 03 00 00";
        public const string IP_MODE_UNKOWN_1 = "2B 40 60 00 80 00 00 00";
        public const string IP_MODE_PVT_Buffer_1 = "2F C4 60 05 08 00 00 00";
        public const string IP_MODE_PVT_Buffer_2 = "2F C4 60 06 00 00 00 00";
        public const string IP_MODE_UNKOWN_2 = "2F C4 60 06 01 00 00 00";

        /*
         * 速度运动模式配置
         */
        public const string VELOCITY_MODE_ENABLE = "2B 40 60 00 0F 00 00 00";

        /*
         * 位置运动模式配置
         */
        public const string POS_MODE_ENABLE = "2B 40 60 00 0F 00 00 00";
        public const string POS_MODE_CHANGE_IMMEDIATELY = "2B 40 60 00 3F 00 00 00";


        private string ID = "00000601";
        private string ID_2 = "00000602";
        private string ID_3 = "00000603";
        private string ID_4 = "00000604";
        private string ID_5 = "00000605";
        private string ID_6 = "00000606";

        private string ID_1 = "00000301";
        private string ID_7 = "00000302";
        private string ID_8 = "00000303";
        private string ID_9 = "00000304";
        private string ID_10 = "00000305";
        private string ID_11 = "00000306";

        CANalystHelper CANalyst;
        Thread SendMsg;

        /// <summary>
        /// 类初始化函数
        /// </summary>
        /// <param name="id">控制ID，基本结构为"00000601"</param>
        /// <param name="CAN">CAN分析仪接口类</param>
        public EposDriver(string id, CANalystHelper CAN)//驱动器类初始化函数
        {
            ID = id;
            CANalyst = CAN;
        }

        /// <summary>
        /// 配置驱动器ID
        /// </summary>
        /// <param name="id">CAN ID，格式为"00000601"</param>
        public void SetEPOSID(string id)//配置驱动器ID
        {
            ID = id;
        }

        /// <summary>
        /// 获取当前ID
        /// </summary>
        /// <returns></returns>
        public string GetEPOSID()//获取当前ID
        {
            return ID;
        }
        /// <summary>
        /// HDT驱动器的初始化操作
        /// </summary>
        /// <returns></returns>
        public bool EPOSInit()//驱动器的初始化操作
        {
            SendSYNC();
            CANalyst.SendOneMsg(ID, MODE_WORD_VELOCITY);
            CANalyst.SendOneMsg(ID, CTR_WORD_SWITCH_DISABLE);
            CANalyst.SendOneMsg(ID, CTR_WORD_ENABLE_OPERATION);
            CANalyst.SendOneMsg(ID, Max_Profile_Velocity);
            CANalyst.SendOneMsg(ID, Profile_Acc);
            CANalyst.SendOneMsg(ID, Profile_Dec);
            CANalyst.SendOneMsg(ID, Quick_Stop_Dec);
            CANalyst.SendOneMsg(ID, Motion_Profile_Type);
            return true;

        }
        /// <summary>
        /// 发送同步报文
        /// </summary>
        public void SendSYNC()//发送同步报文
        {
            CANalyst.SendOneMsg("00000000", "01 00 ");
        }

        public void StartPositionVelocity(Int32 spv)//位置模式速度设置
        {
            string str = Int2Hex(spv);  //针对1、2号电机

            spv = spv * 524288 / 60;//针对3号电机
            string str1 = Int2Hex(spv);
            //1号电机
            CANalyst.SendOneMsg(ID, "23 81 60 00" + " " + str);
            //2号电机
            CANalyst.SendOneMsg(ID_2, "23 81 60 00" + " " + str);
            //3号电机
            CANalyst.SendOneMsg(ID_3, "23 81 60 00" + " " + str1);
        }


        #region 速度模式
        /// <summary>
        /// 速度模式初始化
        /// </summary>
        public void VelocityInit()// 速度模式初始化
        {
            SendSYNC();
            //1号电机
            CANalyst.SendOneMsg(ID, CTR_WORD_SWITCH_DISABLE);
            System.Threading.Thread.Sleep(30);
            CANalyst.SendOneMsg(ID, "2B 40 60 00 07 00 00 00");
            System.Threading.Thread.Sleep(30);
            CANalyst.SendOneMsg(ID, CTR_WORD_ENABLE_OPERATION);
            System.Threading.Thread.Sleep(30);
            CANalyst.SendOneMsg(ID, MODE_WORD_VELOCITY);//选择速度模式
            System.Threading.Thread.Sleep(10);          //延时留出设备响应时间
            CANalyst.SendOneMsg(ID, "23 ff 60 00 00 00 00 00");//设置速度为零（初始化）
            System.Threading.Thread.Sleep(10);
            CANalyst.SendOneMsg(ID, "23 83 60 00 AA AA 42 00");//设置轮廓加速度（初始化）
            System.Threading.Thread.Sleep(10);
            CANalyst.SendOneMsg(ID, "23 84 60 00 55 55 85 00");//设置轮廓减速度（初始化）
            System.Threading.Thread.Sleep(10);
            CANalyst.SendOneMsg(ID, "23 7F 60 00 DD DD DF 01");//设置最大轮廓速度（初始化）
            System.Threading.Thread.Sleep(10);
            CANalyst.SendOneMsg(ID, CTR_WORD_SWITCH_DISABLE);
            System.Threading.Thread.Sleep(10);
            //CANalyst.SendOneMsg(ID, "2B 40 60 00 07 00 00 00");
            //System.Threading.Thread.Sleep(10);
            //CANalyst.SendOneMsg(ID, CTR_WORD_ENABLE_OPERATION);
            //2号电机
            CANalyst.SendOneMsg(ID, CTR_WORD_SWITCH_DISABLE);
            System.Threading.Thread.Sleep(10);
            CANalyst.SendOneMsg(ID_2, MODE_WORD_VELOCITY);//选择速度模式
            System.Threading.Thread.Sleep(10);          //延时留出设备响应时间
            CANalyst.SendOneMsg(ID_2, "23 ff 60 00 00 00 00 00");//设置速度为零（初始化）
            System.Threading.Thread.Sleep(10);
            CANalyst.SendOneMsg(ID_2, "23 83 60 00 AA AA 42 00");//设置轮廓加速度（初始化）
            System.Threading.Thread.Sleep(10);
            CANalyst.SendOneMsg(ID_2, "23 84 60 00 55 55 85 00");//设置轮廓减速度（初始化）
            System.Threading.Thread.Sleep(10);
            CANalyst.SendOneMsg(ID_2, "23 7F 60 00 DD DD DF 01");//设置最大轮廓速度（初始化）
            System.Threading.Thread.Sleep(10);
            CANalyst.SendOneMsg(ID_2, CTR_WORD_SWITCH_DISABLE);
            System.Threading.Thread.Sleep(10);
            CANalyst.SendOneMsg(ID_2, "2B 40 60 00 07 00 00 00");
            System.Threading.Thread.Sleep(10);
            CANalyst.SendOneMsg(ID_2, CTR_WORD_ENABLE_OPERATION);
            //3号电机
            CANalyst.SendOneMsg(ID, CTR_WORD_SWITCH_DISABLE);
            System.Threading.Thread.Sleep(10);
            CANalyst.SendOneMsg(ID_3, MODE_WORD_VELOCITY);//选择速度模式
            System.Threading.Thread.Sleep(10);          //延时留出设备响应时间
            CANalyst.SendOneMsg(ID_3, "23 ff 60 00 00 00 00 00");//设置速度为零（初始化）
            System.Threading.Thread.Sleep(10);
            CANalyst.SendOneMsg(ID_3, "23 83 60 00 AA AA 42 00");//设置轮廓加速度（初始化）
            System.Threading.Thread.Sleep(10);
            CANalyst.SendOneMsg(ID_3, "23 84 60 00 55 55 85 00");//设置轮廓减速度（初始化）
            System.Threading.Thread.Sleep(10);
            CANalyst.SendOneMsg(ID_3, "23 7F 60 00 DD DD DF 01");//设置最大轮廓速度（初始化）
            System.Threading.Thread.Sleep(10);
            CANalyst.SendOneMsg(ID_3, CTR_WORD_SWITCH_DISABLE);
            System.Threading.Thread.Sleep(10);
            CANalyst.SendOneMsg(ID_3, "2B 40 60 00 07 00 00 00");
            System.Threading.Thread.Sleep(10);
            CANalyst.SendOneMsg(ID_3, CTR_WORD_ENABLE_OPERATION);
            //4号电机
            /*CANalyst.SendOneMsg(ID_4, MODE_WORD_VELOCITY);
            CANalyst.SendOneMsg(ID_4, CTR_WORD_SWITCH_DISABLE);
            CANalyst.SendOneMsg(ID_4, CTR_WORD_ENABLE_OPERATION);
            //5号电机
            CANalyst.SendOneMsg(ID_5, MODE_WORD_VELOCITY);
            CANalyst.SendOneMsg(ID_5, CTR_WORD_SWITCH_DISABLE);
            CANalyst.SendOneMsg(ID_5, CTR_WORD_ENABLE_OPERATION);
            //6号电机
            CANalyst.SendOneMsg(ID_6, MODE_WORD_VELOCITY);
            CANalyst.SendOneMsg(ID_6, CTR_WORD_SWITCH_DISABLE);
            CANalyst.SendOneMsg(ID_6, CTR_WORD_ENABLE_OPERATION);
            //SetVelocity(0);*/
        }

        public void Single_Motor_VelocityInit(string ID)// 速度模式初始化
        {
            //1号电机
            CANalyst.SendOneMsg(ID, CTR_WORD_SWITCH_DISABLE);
            System.Threading.Thread.Sleep(30);
            CANalyst.SendOneMsg(ID, "2B 40 60 00 07 00 00 00");
            System.Threading.Thread.Sleep(30);
            CANalyst.SendOneMsg(ID, CTR_WORD_ENABLE_OPERATION);
            System.Threading.Thread.Sleep(30);
            CANalyst.SendOneMsg(ID, MODE_WORD_VELOCITY);//选择速度模式
            System.Threading.Thread.Sleep(10);          //延时留出设备响应时间
            CANalyst.SendOneMsg(ID, "23 ff 60 00 00 00 00 00");//设置速度为零（初始化）
            System.Threading.Thread.Sleep(10);
            CANalyst.SendOneMsg(ID, "23 83 60 00 AA AA 42 00");//设置轮廓加速度（初始化）
            System.Threading.Thread.Sleep(10);
            CANalyst.SendOneMsg(ID, "23 84 60 00 55 55 85 00");//设置轮廓减速度（初始化）
            System.Threading.Thread.Sleep(10);
            CANalyst.SendOneMsg(ID, "23 7F 60 00 DD DD DF 01");//设置最大轮廓速度（初始化）
            System.Threading.Thread.Sleep(10);
            CANalyst.SendOneMsg(ID, CTR_WORD_SWITCH_DISABLE);
            System.Threading.Thread.Sleep(10);
        }

        /// <summary>
        /// 设置速度模式的速度
        /// </summary>
        /// <param name="vel">速度</param>
        public void SetVelocity(Int32 vel)//设置速度模式的速度
        {
            // 单位转换 RPM转换为P/s  ： vel * 脉冲数 / 60
            int vel1 = vel * 524288 / 60;
            string str = Int2Hex(vel);
            
            string str1 = Int2Hex(vel1);
            string str2 = Int2Hex((-1) * vel);
            //1号电机
            if (Matlab_connect.MachineControl.Motor_1)
            {
                CANalyst.SendOneMsg(ID, "23 FF 60 00" + " " + str2);
                //CANalyst.SendOneMsg(ID, "23 FF 60 00" + " " + "03 00 00 00");
                CANalyst.SendOneMsg(ID, VELOCITY_MODE_ENABLE);
            }
            //2号电机
            if (Matlab_connect.MachineControl.Motor_2)
            {
                CANalyst.SendOneMsg(ID_2, "23 FF 60 00 " + str);
                CANalyst.SendOneMsg(ID_2, VELOCITY_MODE_ENABLE);
            }
            //3号电机
            if (Matlab_connect.MachineControl.Motor_3)

            {
                CANalyst.SendOneMsg(ID_3, "23 FF 60 00 " + str1);
                CANalyst.SendOneMsg(ID_3, VELOCITY_MODE_ENABLE);
            }
            /*//4号电机
            CANalyst.SendOneMsg(ID_4, "23 FF 60 00 " + str);
            CANalyst.SendOneMsg(ID_4, VELOCITY_MODE_ENABLE);
            //5号电机
            CANalyst.SendOneMsg(ID_5, "23 FF 60 00 " + str);
            CANalyst.SendOneMsg(ID_5, VELOCITY_MODE_ENABLE);
            //6号电机
            CANalyst.SendOneMsg(ID_6, "23 FF 60 00 " + str);
            CANalyst.SendOneMsg(ID_6, VELOCITY_MODE_ENABLE);*/
        }



        /// <summary>
        /// 失能电机
        /// </summary>
        public void Reseat()
        {
            if (Matlab_connect.MachineControl.Motor_1)
            {
                CANalyst.SendOneMsg(ID, CTR_WORD_SWITCH_DISABLE);
                System.Threading.Thread.Sleep(10);
            }
            if (Matlab_connect.MachineControl.Motor_2)
            {
                CANalyst.SendOneMsg(ID_2, CTR_WORD_SWITCH_DISABLE);
                System.Threading.Thread.Sleep(10);
            }
            if (Matlab_connect.MachineControl.Motor_3)
            {
                CANalyst.SendOneMsg(ID_3, CTR_WORD_SWITCH_DISABLE);
                System.Threading.Thread.Sleep(10);
            }
        }
        #endregion


        #region 位置模式
        public void PositInit()//位置模式初始化
        {
            SendSYNC();
            //用于设置轮廓速度
            //int vel = 50;
            //vel = vel * 524288 / 60;
            //string str = Int2Hex(vel);

            //1号电机
            //CANalyst.SendOneMsg(ID, "2b 40 60 00 06 00 00 00");//失能电机
            //System.Threading.Thread.Sleep(10);
            CANalyst.SendOneMsg(ID, MODE_WORD_POSITION);//位置模式
            System.Threading.Thread.Sleep(10);
            CANalyst.SendOneMsg(ID, CTR_WORD_SWITCH_DISABLE);
            CANalyst.SendOneMsg(ID, "23 40 60 00 07 00 00 00");
            System.Threading.Thread.Sleep(10);
            CANalyst.SendOneMsg(ID, CTR_WORD_ENABLE_OPERATION);
            System.Threading.Thread.Sleep(10);
            //CANalyst.SendOneMsg(ID, "23 7A 60 00 00 00 00 00");//设定初始位置
            //System.Threading.Thread.Sleep(10);
            //CANalyst.SendOneMsg(ID, "23 81 60 00" + " " + str);//设定轮廓速度
            //System.Threading.Thread.Sleep(10);
            //CANalyst.SendOneMsg(ID, "23 7F 60 00 DD DD DF 01");//设定最大轮廓速度
            System.Threading.Thread.Sleep(10);
            CANalyst.SendOneMsg(ID, "23 83 60 00 F4 01 00 00");//设置轮廓加速度（初始化 500）
            System.Threading.Thread.Sleep(10);
            CANalyst.SendOneMsg(ID, "23 84 60 00 E8 03 00 00");//设置轮廓减速度（初始化 1000）
            System.Threading.Thread.Sleep(10);
            CANalyst.SendOneMsg(ID, "2b 40 60 00 04 00 00 00");//对应急停位
            System.Threading.Thread.Sleep(10);
            CANalyst.SendOneMsg(ID, "23 40 60 00 2f 00 00 00");
            System.Threading.Thread.Sleep(10);
            CANalyst.SendOneMsg(ID, "23 40 60 00 3f 00 00 00");
            System.Threading.Thread.Sleep(10);

            //CANalyst.SendOneMsg(ID, "23 40 60 00 2f 00 00 00");
            //System.Threading.Thread.Sleep(10);
            //CANalyst.SendOneMsg(ID, "23 40 60 00 3f 00 00 00");
            //System.Threading.Thread.Sleep(10);
            //2号电机
            //CANalyst.SendOneMsg(ID_2, "2b 40 60 00 06 00 00 00");//失能电机
            //System.Threading.Thread.Sleep(10);
            CANalyst.SendOneMsg(ID_2, MODE_WORD_POSITION);//位置模式
            System.Threading.Thread.Sleep(10);
            CANalyst.SendOneMsg(ID_2, CTR_WORD_SWITCH_DISABLE);
            CANalyst.SendOneMsg(ID_2, "23 40 60 00 07 00 00 00");
            System.Threading.Thread.Sleep(10);
            CANalyst.SendOneMsg(ID_2, CTR_WORD_ENABLE_OPERATION);
            System.Threading.Thread.Sleep(10);
            //CANalyst.SendOneMsg(ID_2, "23 7A 60 00 00 00 00 00");//设定初始位置
            //System.Threading.Thread.Sleep(10);
            //CANalyst.SendOneMsg(ID, "23 81 60 00" + " " + str);//设定轮廓速度
            //System.Threading.Thread.Sleep(10);
            //CANalyst.SendOneMsg(ID, "23 7F 60 00 DD DD DF 01");//设定最大轮廓速度
            System.Threading.Thread.Sleep(10);
            CANalyst.SendOneMsg(ID_2, "23 83 60 00 F4 01 00 00");//设置轮廓加速度（初始化 500）
            System.Threading.Thread.Sleep(10);
            CANalyst.SendOneMsg(ID_2, "23 84 60 00 E8 03 00 00");//设置轮廓减速度（初始化 1000）
            System.Threading.Thread.Sleep(10);
            CANalyst.SendOneMsg(ID_2, "2b 40 60 00 04 00 00 00");//对应急停位
            System.Threading.Thread.Sleep(10);
            CANalyst.SendOneMsg(ID_2, "23 40 60 00 2f 00 00 00");
            System.Threading.Thread.Sleep(10);
            CANalyst.SendOneMsg(ID_2, "23 40 60 00 3f 00 00 00");
            System.Threading.Thread.Sleep(10);
            //3号电机
            CANalyst.SendOneMsg(ID_3, CTR_WORD_SWITCH_DISABLE);
            System.Threading.Thread.Sleep(10);
            CANalyst.SendOneMsg(ID_3, "2F 60 60 00 06 00 00 00");//选择HM模式

            System.Threading.Thread.Sleep(10);
            CANalyst.SendOneMsg(ID_3, "2f 98 60 00 23 00 00 00");//设置当前位置为零点（初始化）
            System.Threading.Thread.Sleep(10);
            CANalyst.SendOneMsg(ID_3, "2B 40 60 00 1f 00 00 00");//开始回零
            Thread.Sleep(10);
            CANalyst.SendOneMsg(ID_3, MODE_WORD_POSITION);       //选择位置模式
            System.Threading.Thread.Sleep(10);
            CANalyst.SendOneMsg(ID_3, "23 83 60 00 AA AA 42 00");//设置轮廓加速度（初始化）
            System.Threading.Thread.Sleep(10);
            CANalyst.SendOneMsg(ID_3, "23 84 60 00 55 55 85 00");//设置轮廓减速度（初始化）
            System.Threading.Thread.Sleep(10);
            CANalyst.SendOneMsg(ID_3, "23 7F 60 00 DD DD DF 01");//设置最大轮廓速度（初始化）
            System.Threading.Thread.Sleep(10);
            //CANalyst.SendOneMsg(ID_3, "2b 40 60 00 04 00 00 00");//对应急停位
            //System.Threading.Thread.Sleep(10);
            //CANalyst.SendOneMsg(ID_3, CTR_WORD_SWITCH_DISABLE);
            //System.Threading.Thread.Sleep(10);
            //CANalyst.SendOneMsg(ID_3, "23 40 60 00 07 00 00 00");
            //System.Threading.Thread.Sleep(10);
            //CANalyst.SendOneMsg(ID_3, CTR_WORD_ENABLE_OPERATION);
            //System.Threading.Thread.Sleep(10);

            /*//4号电机
            CANalyst.SendOneMsg(ID_4, MODE_WORD_POSITION);
            CANalyst.SendOneMsg(ID_4, CTR_WORD_SWITCH_DISABLE);
            CANalyst.SendOneMsg(ID_4, CTR_WORD_ENABLE_OPERATION);
            //5号电机
            CANalyst.SendOneMsg(ID_5, MODE_WORD_POSITION);
            CANalyst.SendOneMsg(ID_5, CTR_WORD_SWITCH_DISABLE);
            CANalyst.SendOneMsg(ID_5, CTR_WORD_ENABLE_OPERATION);
            //6号电机
            CANalyst.SendOneMsg(ID_6, MODE_WORD_POSITION);
            CANalyst.SendOneMsg(ID_6, CTR_WORD_SWITCH_DISABLE);
            CANalyst.SendOneMsg(ID_6, CTR_WORD_ENABLE_OPERATION);*/
        }

        public void SetPosition(Int32 pos)//位置模式设置位置
        {
            string str = Int2Hex(pos);
            //pos = pos * 524288 / 60;
            //string str1 = Int2Hex(pos);

            //byte[] databuffer = new byte[4];

            //databuffer = BitConverter.GetBytes(pos);
            //string a = Convert.ToString(databuffer[0]);
            //string a1 = Convert.ToString(databuffer[1]);
            //string a2 = Convert.ToString(databuffer[2]);
            //string a3 = Convert.ToString(databuffer[3]);
            //1号电机
            if (Matlab_connect.MachineControl.Motor_1)
            {
                //CANalyst.SendOneMsg(ID, "23 7A 60 00" + " " + a + " " + a1 + " " + a2 + " " + a3);
                CANalyst.SendOneMsg(ID, "23 7A 60 00" + " " + str);
                //CANalyst.SendOneMsg(ID, POS_MODE_CHANGE_IMMEDIATELY);
                //CANalyst.SendOneMsg(ID, POS_MODE_ENABLE);
            }
            //2号电机
            if (Matlab_connect.MachineControl.Motor_2)
            {
                CANalyst.SendOneMsg(ID_2, "23 7A 60 00" + " " + str);
                //CANalyst.SendOneMsg(ID_2, POS_MODE_CHANGE_IMMEDIATELY);
                //CANalyst.SendOneMsg(ID_2, POS_MODE_ENABLE);
            }
            //3号电机
            if (Matlab_connect.MachineControl.Motor_3)
            {
                CANalyst.SendOneMsg(ID_3, "23 7A 60 00" + " " + str);
                //CANalyst.SendOneMsg(ID_3, POS_MODE_CHANGE_IMMEDIATELY);
                //CANalyst.SendOneMsg(ID_3, POS_MODE_ENABLE);
            }

            ////4号电机
            //CANalyst.SendOneMsg(ID_4, "23 7A 60 00 " + str);
            //CANalyst.SendOneMsg(ID_4, POS_MODE_CHANGE_IMMEDIATELY);
            //CANalyst.SendOneMsg(ID_4, POS_MODE_ENABLE);
            ////5号电机
            //CANalyst.SendOneMsg(ID_5, "23 7A 60 00 " + str);
            //CANalyst.SendOneMsg(ID_5, POS_MODE_CHANGE_IMMEDIATELY);
            //CANalyst.SendOneMsg(ID_5, POS_MODE_ENABLE);
            ////6号电机
            //CANalyst.SendOneMsg(ID_6, "23 7A 60 00 " + str);
            //CANalyst.SendOneMsg(ID_6, POS_MODE_CHANGE_IMMEDIATELY);
            //CANalyst.SendOneMsg(ID_6, POS_MODE_ENABLE);
        }

        public void StartPosition()//位置模式开始按键
        {
            //string str = Int2Hex(pos);

            //1号电机
            if (Matlab_connect.MachineControl.Motor_1)
            {
                CANalyst.SendOneMsg(ID, "23 40 60 00 2f 00 00 00");
                System.Threading.Thread.Sleep(10);
                CANalyst.SendOneMsg(ID, "23 40 60 00 3f 00 00 00");
            }
            //2号电机
            if (Matlab_connect.MachineControl.Motor_2)
            {
                CANalyst.SendOneMsg(ID_2, "23 40 60 00 2f 00 00 00");
                System.Threading.Thread.Sleep(10);
                CANalyst.SendOneMsg(ID_2, "23 40 60 00 3f 00 00 00");
            }
            //3号电机
            if (Matlab_connect.MachineControl.Motor_3)
            {
                CANalyst.SendOneMsg(ID_3, CTR_WORD_ENABLE_OPERATION);
                System.Threading.Thread.Sleep(10);
                //CANalyst.SendOneMsg(ID_3, "23 40 60 00 2f 00 00 00");
                //System.Threading.Thread.Sleep(10);
                CANalyst.SendOneMsg(ID_3, "2b 40 60 00 3f 00 00 00");
            }
        }

        public void StartPositionVelocity1(Int32 spv)//位置模式速度设置
        {
            if (spv < 0)
            {
                spv = -spv;
            }
            string str = Int2Hex(spv);  //针对1、2号电机

            spv = spv * 524288 / 60;//针对3号电机
            string str1 = Int2Hex(spv);
            //1号电机
            if (Matlab_connect.MachineControl.Motor_1)
            {
                CANalyst.SendOneMsg(ID, "23 81 60 00" + " " + str);
            }
            //2号电机
            if (Matlab_connect.MachineControl.Motor_2)
            {
                CANalyst.SendOneMsg(ID_2, "23 81 60 00" + " " + str);
            }
            //3号电机
            if (Matlab_connect.MachineControl.Motor_3)
            {
                CANalyst.SendOneMsg(ID_3, "23 81 60 00" + " " + str1);
            }
        }





        //位置访问
        public void location_Access()
        {
            if (Matlab_connect.MachineControl.Motor_1)
            {
                CANalyst.SendOneMsg(ID, "40 64 60 00 00 00 00 00");
            }
            //System.Threading.Thread.Sleep(10);
            if (Matlab_connect.MachineControl.Motor_2)
            {
                CANalyst.SendOneMsg(ID_2, "40 64 60 00 00 00 00 00");
            }
            //System.Threading.Thread.Sleep(10);
            if (Matlab_connect.MachineControl.Motor_3)
            {
                CANalyst.SendOneMsg(ID_3, "40 64 60 00 00 00 00 00");
            }
        }
        #endregion
        /// <summary>
        /// 位置模式停止运行
        /// </summary>
        public void StopPosition()
        {
            CANalyst.SendOneMsg(ID, "2B 40 60 00 3F 01 00 00");
            CANalyst.SendOneMsg(ID_2, "2B 40 60 00 3F 01 00 00");
            CANalyst.SendOneMsg(ID_3, "2B 40 60 00 3F 01 00 00");
        }


        #region 数值转换函数
        public string Float2ByteStr(float num)
        {
            byte[] HexVal = BitConverter.GetBytes(num);
            return ByteArrayToHexString(HexVal);

        }

        public static string ByteArrayToHexString(byte[] data)//小数转换成16进制
        {
            StringBuilder sb = new StringBuilder(data.Length * 3);
            foreach (byte b in data)
            {
                sb.Append(Convert.ToString(b, 16).PadLeft(2, '0').PadRight(3, ' '));
            }
            return sb.ToString().ToUpper().Trim(' ');
        }

        public string Int2Hex(int num)//整数转化16进制数
        {

            string a = Convert.ToString(num, 16);
            int b = 8 - a.Length;
            for (int i = 0; i < b; i++)
            {
                a = "0" + a;
            }
            a = a.ToUpper();
            //Console.WriteLine(a[6].ToString() + a[7].ToString() + " " + a[4].ToString() + a[5].ToString() + " " + a[2].ToString() + a[3].ToString() + " " + a[0].ToString() + a[1].ToString());
            return a[6].ToString() + a[7].ToString() + " " + a[4].ToString() + a[5].ToString() + " " + a[2].ToString() + a[3].ToString() + " " + a[0].ToString() + a[1].ToString();
        }
        #endregion
    }
    class HDTDriver
    {
        /*------------CANOpen报文---------------------------------*/

        /*
         * 控制字命令
         */
        public const string CTR_WORD_ENABLE = "2B 40 60 00 00 00 00 00";
        public const string CTR_WORD_SWITCH_DISABLE = "2B 40 60 00 06 00 00 00";
        public const string CTR_WORD_SWITCH_ON = "2B 40 60 00 07 00 00 00";
        public const string CTR_WORD_ENABLE_OPERATION = "2B 40 60 00 0F 00 00 00";
        public const string CTR_WORD_ENABLE_IP_OP = "2B 40 60 00 1F 00 00 00";
        public const string CTR_WORD_DISENABLE_IP = "2B 40 60 00 0F 00 00 00";
        public const string CTR_WORD_FAULT_RESET = "2B 40 60 00 40 00 00 00";

        /*
         * 电机运动模式
         */
        public const string MODE_WORD_IP = "2F 60 60 00 07 00 00 00";
        public const string MODE_WORD_VELOCITY = "2F 60 60 00 03 00 00 00";
        public const string MODE_WORD_POSITION = "2F 60 60 00 01 00 00 00";


        /*
         * 插补运动模式配置
         */
        public const string IP_MODE_RPDO_ENTRIES_CLEAR = "2F 03 16 00 00 00 00 00";
        public const string IP_MODE_RPDO_Mapping = "23 03 16 01 20 01 C1 60";
        public const string IP_MODE_RPDO_ENTRIES_SET = "2F 03 16 00 01 00 00 00";
        public const string IP_MODE_RPDO_TRANSMISSION_TYPE = "2F 03 14 02 01 00 00 00";

        //public const string IP_MODE_SUBMODE_CUBICSPINE_SET = "2F C0 60 00 FF 00 00 00";//-1 三次样条插补
        public const string IP_MODE_SUBMODE_CUBICSPINE_SET = "2F C0 60 00 00 00 00 00";//0 直线样条插补

        public const string IP_MODE_TIME_PERIOD_SET = "23 C2 60 01 32 00 00 00";//50ms
        public const string IP_MODE_TIME_INDEX_SET = "23 C2 60 02 FD 00 00 00";//-3



        /*
         * 速度运动模式配置
         */








        /*
         * 位置运动模式配置
         */
        public const string POS_MODE_ENABLE = "2B 40 60 00 0F 00 00 00";
        public const string POS_MODE_CHANGE_IMMEDIATELY = "2B 40 60 00 1F 00 00 00";




        private string ID = "00000601";
        CANalystHelper CANalyst;


        /// <summary>
        /// 类初始化函数
        /// </summary>
        /// <param name="id">控制ID，基本结构为"00000601"</param>
        /// <param name="CAN">CAN分析仪接口类</param>
        public HDTDriver(string id, CANalystHelper CAN)
        {
            ID = id;
            CANalyst = CAN;
        }

        /// <summary>
        /// 配置驱动器ID
        /// </summary>
        /// <param name="id">CAN ID，格式为"00000601"</param>
        public void SetHDTID(string id)
        {
            ID = id;
        }

        /// <summary>
        /// 获取当前ID
        /// </summary>
        /// <returns></returns>
        public string GetHDTID()
        {
            return ID;
        }

        /// <summary>
        /// HDT驱动器的初始化操作
        /// </summary>
        /// <returns></returns>
        public bool HDTInit()
        {

            CANalyst.SendOneMsg(ID, CTR_WORD_FAULT_RESET);
            CANalyst.SendOneMsg(ID, CTR_WORD_SWITCH_DISABLE);
            CANalyst.SendOneMsg(ID, CTR_WORD_SWITCH_ON);
            CANalyst.SendOneMsg(ID, CTR_WORD_ENABLE_OPERATION);
            CANalyst.SendOneMsg(ID, CTR_WORD_ENABLE_IP_OP);
            CANalyst.SendOneMsg(ID, IP_MODE_RPDO_ENTRIES_CLEAR);
            CANalyst.SendOneMsg(ID, IP_MODE_RPDO_ENTRIES_SET);
            CANalyst.SendOneMsg(ID, IP_MODE_RPDO_TRANSMISSION_TYPE);
            CANalyst.SendOneMsg(ID, MODE_WORD_IP);
            return true;

        }

        /// <summary>
        /// 发送同步报文
        /// </summary>
        public void SendSYNC()
        {
            CANalyst.SendOneMsg("00000080", " ");
        }

        /// <summary>
        /// 插补模式初始化操作
        /// </summary>
        public void IPInit()
        {
            CANalyst.SendOneMsg(ID, CTR_WORD_FAULT_RESET);
            CANalyst.SendOneMsg(ID, CTR_WORD_ENABLE);
            CANalyst.SendOneMsg(ID, CTR_WORD_SWITCH_DISABLE);
            CANalyst.SendOneMsg(ID, CTR_WORD_SWITCH_ON);
            CANalyst.SendOneMsg(ID, CTR_WORD_ENABLE_OPERATION);
            CANalyst.SendOneMsg(ID, CTR_WORD_ENABLE_IP_OP);
            CANalyst.SendOneMsg(ID, IP_MODE_RPDO_ENTRIES_CLEAR);
            CANalyst.SendOneMsg(ID, IP_MODE_RPDO_Mapping);
            CANalyst.SendOneMsg(ID, IP_MODE_RPDO_ENTRIES_SET);
            CANalyst.SendOneMsg(ID, IP_MODE_RPDO_TRANSMISSION_TYPE);
            CANalyst.SendOneMsg(ID, MODE_WORD_IP);
            CANalyst.SendOneMsg(ID, CTR_WORD_ENABLE_IP_OP);
            CANalyst.SendOneMsg(ID, IP_MODE_SUBMODE_CUBICSPINE_SET);
            CANalyst.SendOneMsg(ID, IP_MODE_TIME_PERIOD_SET);
            CANalyst.SendOneMsg(ID, IP_MODE_TIME_INDEX_SET);
            CANalyst.SendOneMsg(ID, CTR_WORD_ENABLE_IP_OP);
        }

        /// <summary>
        /// 设置插补模式的位置，已包含同步报文
        /// </summary>
        /// <param name="Pos">要运动的位置</param>
        public void SetIPosition(int Pos)
        {
            string str = Int2Hex(Pos);
            CANalyst.SendOneMsg(ID, "23 C1 60 01 " + str);
            SendSYNC();

        }

        /// <summary>
        /// 停止插补运动
        /// </summary>
        public void IPStop()
        {
            CANalyst.SendOneMsg(ID, CTR_WORD_ENABLE_OPERATION);
        }

        /// <summary>
        /// 速度模式初始化
        /// </summary>
        public void VelocityInit()
        {
            CANalyst.SendOneMsg(ID, CTR_WORD_FAULT_RESET);
            CANalyst.SendOneMsg(ID, CTR_WORD_SWITCH_DISABLE);
            CANalyst.SendOneMsg(ID, CTR_WORD_SWITCH_ON);
            CANalyst.SendOneMsg(ID, CTR_WORD_ENABLE_OPERATION);

            CANalyst.SendOneMsg(ID, MODE_WORD_VELOCITY);
            SetVelocity(0);
        }

        /// <summary>
        /// 设置速度模式的速度
        /// </summary>
        /// <param name="vel">速度</param>
        public void SetVelocity(Int32 vel)
        {
            string str = Int2Hex(vel);
            CANalyst.SendOneMsg(ID, "23 FF 60 00 " + str);
        }



        /// <summary>
        /// 位置模式初始化
        /// </summary>
        public void PositInit()
        {
            CANalyst.SendOneMsg(ID, CTR_WORD_FAULT_RESET);
            CANalyst.SendOneMsg(ID, CTR_WORD_SWITCH_DISABLE);
            CANalyst.SendOneMsg(ID, CTR_WORD_SWITCH_ON);
            CANalyst.SendOneMsg(ID, CTR_WORD_ENABLE_OPERATION);

            CANalyst.SendOneMsg(ID, MODE_WORD_POSITION);
        }

        /// <summary>
        /// 设置位置模式的位置
        /// </summary>
        /// <param name="pos">位置</param>
        public void SetPosition(Int32 pos)
        {
            string str = Int2Hex(pos);
            CANalyst.SendOneMsg(ID, "23 7A 60 00 " + str);
            CANalyst.SendOneMsg(ID, POS_MODE_ENABLE);
            CANalyst.SendOneMsg(ID, POS_MODE_CHANGE_IMMEDIATELY);
        }




        public string Float2ByteStr(float num)
        {
            byte[] HexVal = BitConverter.GetBytes(num);
            return ByteArrayToHexString(HexVal);

        }


        public static string ByteArrayToHexString(byte[] data)
        {
            StringBuilder sb = new StringBuilder(data.Length * 3);
            foreach (byte b in data)
            {
                sb.Append(Convert.ToString(b, 16).PadLeft(2, '0').PadRight(3, ' '));
            }
            return sb.ToString().ToUpper().Trim(' ');
        }


        public string Int2Hex(int num)
        {

            string a = Convert.ToString(num, 16);
            int b = 8 - a.Length;
            for (int i = 0; i < b; i++)
            {
                a = "0" + a;
            }
            a = a.ToUpper();
            //Console.WriteLine(a[6].ToString() + a[7].ToString() + " " + a[4].ToString() + a[5].ToString() + " " + a[2].ToString() + a[3].ToString() + " " + a[0].ToString() + a[1].ToString());
            return a[6].ToString() + a[7].ToString() + " " + a[4].ToString() + a[5].ToString() + " " + a[2].ToString() + a[3].ToString() + " " + a[0].ToString() + a[1].ToString();
        }


    }
}

