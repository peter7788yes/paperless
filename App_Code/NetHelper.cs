using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;

    /// <summary>
    /// 網絡操作相關的類
    /// </summary>    
    public class NetHelper
   {
        #region 檢查設置的IP地址是否正確，返回正確的IP地址
        /// <summary>
        /// 檢查設置的IP地址是否正確，並返回正確的IP地址,無效IP地址返回"-1"。
        /// </summary>
        /// <param name="ip">設置的IP地址</param>
        //public static string GetValidIP(string ip)
        //{
        //    if (PageValidate.IsIP(ip))
        //    {
        //        return ip;
        //    }
        //    else
        //    {
        //        return "-1";
        //    }
        //}
        #endregion
  
        #region 檢查設置的端口號是否正確，返回正確的端口號
        /// <summary>
        /// 檢查設置的端口號是否正確，並返回正確的端口號,無效端口號返回-1。
        /// </summary>
        /// <param name="port">設置的端口號</param>        
        public static int GetValidPort(string port)
        {
            //聲明返回的正確端口號
            int validPort = -1;
            //最小有效端口號
            const int MINPORT = 0;
            //最大有效端口號
            const int MAXPORT = 65535;
  
            //檢測端口號
            try
            {
                //傳入的端口號為空則拋出異常
                if (port == "")
                {
                    throw new Exception("端口號不能為空！");
                }
  
                //檢測端口範圍
                if ((Convert.ToInt32(port) < MINPORT) || (Convert.ToInt32(port) > MAXPORT))
                {
                    throw new Exception("端口號範圍無效！");
                }
  
                //為端口號賦值
                validPort = Convert.ToInt32(port);
            }
            catch (Exception ex)
            {
                string errMessage = ex.Message;
            }
            return validPort;
        }
        #endregion
  
        #region 將字符串形式的IP地址轉換成IPAddress對象
        /// <summary>
        /// 將字符串形式的IP地址轉換成IPAddress對象
        /// </summary>
        /// <param name="ip">字符串形式的IP地址</param>        
        public static IPAddress StringToIPAddress(string ip)
        {
            return IPAddress.Parse(ip);
        }
        #endregion
  
        #region 獲取本機的計算機名
        /// <summary>
        /// 獲取本機的計算機名
        /// </summary>
        public static string LocalHostName
        {
            get
            {
                return Dns.GetHostName();
            }
        }
        #endregion
  
        #region 獲取本機的局域網IP
        /// <summary>
        /// 獲取本機的局域網IP
        /// </summary>        
        public static string LANIP
        {
            get
            {
                //獲取本機的IP列表,IP列表中的第一項是局域網IP，第二項是廣域網IP
                IPAddress[] addressList = Dns.GetHostEntry(Dns.GetHostName()).AddressList;
  
                //如果本機IP列表為空，則返回空字符串
                if (addressList.Length < 1)
                {
                    return "";
                }
  
                //返回本機的局域網IP
                return addressList[0].ToString();
            }
        }
        #endregion
  
        #region 獲取本機在Internet網絡的廣域網IP
        /// <summary>
        /// 獲取本機在Internet網絡的廣域網IP
        /// </summary>        
        public static string WANIP
        {
            get
            {
                //獲取本機的IP列表,IP列表中的第一項是局域網IP，第二項是廣域網IP
                IPAddress[] addressList = Dns.GetHostEntry(Dns.GetHostName()).AddressList;
  
                //如果本機IP列表小於2，則返回空字符串
                if (addressList.Length < 2)
                {
                    return "";
                }
  
                //返回本機的廣域網IP
                return addressList[1].ToString();
            }
        }
        #endregion
  
        #region 獲取遠程客戶機的IP地址
        /// <summary>
        /// 獲取遠程客戶機的IP地址
        /// </summary>
        /// <param name="clientSocket">客戶端的socket對象</param>        
        public static string GetClientIP(Socket clientSocket)
        {
            IPEndPoint client = (IPEndPoint)clientSocket.RemoteEndPoint;
            return client.Address.ToString();
        }
        #endregion
  
        #region 創建一個IPEndPoint對象
        /// <summary>
        /// 創建一個IPEndPoint對象
        /// </summary>
        /// <param name="ip">IP地址</param>
        /// <param name="port">端口號</param>        
        public static IPEndPoint CreateIPEndPoint(string ip, int port)
        {
            IPAddress ipAddress = StringToIPAddress(ip);
            return new IPEndPoint(ipAddress, port);
        }
        #endregion
  
        #region 創建一個TcpListener對象
        /// <summary>
        /// 創建一個自動分配IP和端口的TcpListener對象
        /// </summary>        
        public static TcpListener CreateTcpListener()
        {
            //創建一個自動分配的網絡節點
            IPAddress ipAddress = IPAddress.Any;
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 0);
  
            return new TcpListener(localEndPoint);
        }
        /// <summary>
        /// 創建一個TcpListener對象
        /// </summary>
        /// <param name="ip">IP地址</param>
        /// <param name="port">端口</param>        
        public static TcpListener CreateTcpListener(string ip, int port)
        {
            //創建一個網絡節點
            IPAddress ipAddress = StringToIPAddress(ip);
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, port);
  
            return new TcpListener(localEndPoint);
        }
        #endregion
  
        #region 創建一個基於TCP協議的Socket對象
        /// <summary>
        /// 創建一個基於TCP協議的Socket對象
        /// </summary>        
        public static Socket CreateTcpSocket()
        {
            return new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }
        #endregion
  
        #region 創建一個基於UDP協議的Socket對象
        /// <summary>
        /// 創建一個基於UDP協議的Socket對象
        /// </summary>        
        public static Socket CreateUdpSocket()
        {
            return new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        }
        #endregion
  
        #region 獲取本地終結點
  
        #region 獲取TcpListener對象的本地終結點
        /// <summary>
        /// 獲取TcpListener對象的本地終結點
        /// </summary>
        /// <param name="tcpListener">TcpListener對象</param>        
        public static IPEndPoint GetLocalPoint(TcpListener tcpListener)
        {
            return (IPEndPoint)tcpListener.LocalEndpoint;
        }
  
        /// <summary>
        /// 獲取TcpListener對象的本地終結點的IP地址
        /// </summary>
        /// <param name="tcpListener">TcpListener對象</param>        
        public static string GetLocalPoint_IP(TcpListener tcpListener)
        {
            IPEndPoint localEndPoint = (IPEndPoint)tcpListener.LocalEndpoint;
            return localEndPoint.Address.ToString();
        }
  
        /// <summary>
        /// 獲取TcpListener對象的本地終結點的端口號
        /// </summary>
        /// <param name="tcpListener">TcpListener對象</param>        
        public static int GetLocalPoint_Port(TcpListener tcpListener)
        {
            IPEndPoint localEndPoint = (IPEndPoint)tcpListener.LocalEndpoint;
            return localEndPoint.Port;
        }
        #endregion
  
        #region 獲取Socket對象的本地終結點
        /// <summary>
        /// 獲取Socket對象的本地終結點
        /// </summary>
        /// <param name="socket">Socket對象</param>        
        public static IPEndPoint GetLocalPoint(Socket socket)
        {
            return (IPEndPoint)socket.LocalEndPoint;
        }
  
        /// <summary>
        /// 獲取Socket對象的本地終結點的IP地址
        /// </summary>
        /// <param name="socket">Socket對象</param>        
        public static string GetLocalPoint_IP(Socket socket)
        {
            IPEndPoint localEndPoint = (IPEndPoint)socket.LocalEndPoint;
            return localEndPoint.Address.ToString();
        }
  
        /// <summary>
        /// 獲取Socket對象的本地終結點的端口號
        /// </summary>
        /// <param name="socket">Socket對象</param>        
        public static int GetLocalPoint_Port(Socket socket)
        {
            IPEndPoint localEndPoint = (IPEndPoint)socket.LocalEndPoint;
            return localEndPoint.Port;
        }
        #endregion
  
        #endregion
  
        #region 綁定終結點
        /// <summary>
        /// 綁定終結點
        /// </summary>
        /// <param name="socket">Socket對象</param>
        /// <param name="endPoint">要綁定的終結點</param>
        public static void BindEndPoint(Socket socket, IPEndPoint endPoint)
        {
            if (!socket.IsBound)
            {
                socket.Bind(endPoint);
            }
        }
  
        /// <summary>
        /// 綁定終結點
        /// </summary>
        /// <param name="socket">Socket對象</param>        
        /// <param name="ip">服務器IP地址</param>
        /// <param name="port">服務器端口</param>
        public static void BindEndPoint(Socket socket, string ip, int port)
        {
            //創建終結點
            IPEndPoint endPoint = CreateIPEndPoint(ip, port);
  
            //綁定終結點
            if (!socket.IsBound)
            {
                socket.Bind(endPoint);
            }
        }
        #endregion
  
        #region 指定Socket對象執行監聽
        /// <summary>
        /// 指定Socket對象執行監聽，默認允許的最大掛起連接數為100
        /// </summary>
        /// <param name="socket">執行監聽的Socket對象</param>
        /// <param name="port">監聽的端口號</param>
        public static void StartListen(Socket socket, int port)
        {
            //創建本地終結點
            IPEndPoint localPoint = CreateIPEndPoint(NetHelper.LocalHostName, port);
  
            //綁定到本地終結點
            BindEndPoint(socket, localPoint);
  
            //開始監聽
            socket.Listen(100);
        }
  
        /// <summary>
        /// 指定Socket對象執行監聽
        /// </summary>
        /// <param name="socket">執行監聽的Socket對象</param>
        /// <param name="port">監聽的端口號</param>
        /// <param name="maxConnection">允許的最大掛起連接數</param>
        public static void StartListen(Socket socket, int port, int maxConnection)
        {
            //創建本地終結點
            IPEndPoint localPoint = CreateIPEndPoint(NetHelper.LocalHostName, port);
  
            //綁定到本地終結點
            BindEndPoint(socket, localPoint);
  
            //開始監聽
            socket.Listen(maxConnection);
        }
  
        /// <summary>
        /// 指定Socket對象執行監聽
        /// </summary>
        /// <param name="socket">執行監聽的Socket對象</param>
        /// <param name="ip">監聽的IP地址</param>
        /// <param name="port">監聽的端口號</param>
        /// <param name="maxConnection">允許的最大掛起連接數</param>
        public static void StartListen(Socket socket, string ip, int port, int maxConnection)
        {
            //綁定到本地終結點
            BindEndPoint(socket, ip, port);
  
            //開始監聽
            socket.Listen(maxConnection);
        }
        #endregion
  
        #region 連接到基於TCP協議的服務器
        /// <summary>
        /// 連接到基於TCP協議的服務器,連接成功返回true，否則返回false
        /// </summary>
        /// <param name="socket">Socket對象</param>
        /// <param name="ip">服務器IP地址</param>
        /// <param name="port">服務器端口號</param>     
        public static bool Connect(Socket socket, string ip, int port)
        {
            try
            {
                //連接服務器
                socket.Connect(ip, port);
  
                //檢測連接狀態
                return socket.Poll(-1, SelectMode.SelectWrite);
            }
            catch (SocketException ex)
            {
                throw new Exception(ex.Message);
                //LogHelper.WriteTraceLog(TraceLogLevel.Error, ex.Message);
            }
        }
        #endregion
  
        #region 以同步方式發送消息
        /// <summary>
        /// 以同步方式向指定的Socket對象發送消息
        /// </summary>
        /// <param name="socket">socket對象</param>
        /// <param name="msg">發送的消息</param>
        public static void SendMsg(Socket socket, byte[] msg)
        {
            //發送消息
            socket.Send(msg, msg.Length, SocketFlags.None);
        }
  
        /// <summary>
        /// 使用UTF8編碼格式以同步方式向指定的Socket對象發送消息
        /// </summary>
        /// <param name="socket">socket對象</param>
        /// <param name="msg">發送的消息</param>
        public static void SendMsg(Socket socket, string msg)
        {            
            //將字符串消息轉換成字符數組
            byte[] buffer =Encoding.Default.GetBytes(msg);
  
            //發送消息
            socket.Send(buffer, buffer.Length, SocketFlags.None);
        }
        #endregion
  
        #region 以同步方式接收消息
        /// <summary>
        /// 以同步方式接收消息
        /// </summary>
        /// <param name="socket">socket對象</param>
        /// <param name="buffer">接收消息的緩衝區</param>
        public static void ReceiveMsg(Socket socket, byte[] buffer)
        {
            socket.Receive(buffer);
        }
  
        /// <summary>
        /// 以同步方式接收消息，並轉換為UTF8編碼格式的字符串,使用5000字節的默認緩衝區接收。
        /// </summary>
        /// <param name="socket">socket對象</param>        
        public static string ReceiveMsg(Socket socket)
        {
            //定義接收緩衝區
            byte[] buffer = new byte[5000];
            //接收數據，獲取接收到的字節數
            int receiveCount = socket.Receive(buffer);
  
            //定義臨時緩衝區
            byte[] tempBuffer = new byte[receiveCount];
            //將接收到的數據寫入臨時緩衝區
            Buffer.BlockCopy(buffer, 0, tempBuffer, 0, receiveCount);
            //轉換成字符串，並將其返回
            //return ConvertHelper.BytesToString(tempBuffer, Encoding.Default);
            return "";
        }
        #endregion
  
        #region 關閉基於Tcp協議的Socket對象
        /// <summary>
        /// 關閉基於Tcp協議的Socket對象
        /// </summary>
        /// <param name="socket">要關閉的Socket對象</param>
        public static void Close(Socket socket)
        {
            try
            {
                //禁止Socket對象接收和發送數據
                socket.Shutdown(SocketShutdown.Both);
            }
            catch (SocketException ex)
            {
                throw ex;
            }
            finally
            {
                //關閉Socket對象
                socket.Close();
            }
        }
        #endregion
  
        #region 發送電子郵件
        /// <summary>
        /// 發送電子郵件,所有SMTP配置信息均在config配置文件中system.net節設置.
        /// </summary>
        /// <param name="receiveEmail">接收電子郵件的地址</param>
        /// <param name="msgSubject">電子郵件的標題</param>
        /// <param name="msgBody">電子郵件的正文</param>
        /// <param name="IsEnableSSL">是否開啟SSL</param>
        public static bool SendEmail(string receiveEmail, string msgSubject, string msgBody, bool IsEnableSSL)
        {
            //創建電子郵件對象
            MailMessage email = new MailMessage();
            //設置接收人的電子郵件地址
            email.To.Add(receiveEmail);
            //設置郵件的標題
            email.Subject = msgSubject;
            //設置郵件的正文
            email.Body = msgBody;
            //設置郵件為HTML格式
            email.IsBodyHtml = true;
  
            //創建SMTP客戶端，將自動從配置文件中獲取SMTP服務器信息
            SmtpClient smtp = new SmtpClient();
            //開啟SSL
            smtp.EnableSsl = IsEnableSSL;
  
            try
            {
                //發送電子郵件
                smtp.Send(email);
  
                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
  
        #endregion  #region  這招非常有用，檢測本機是否聯網（互聯網）
  
        [DllImport("wininet")]
        private extern static bool InternetGetConnectedState(out int connectionDescription, int reservedValue);
  
  
        /// <summary>
        /// 檢測本機是否聯網
        /// </summary>
        /// <returns></returns>
        public static bool IsConnectedInternet()
        {
            int i = 0;
            if (InternetGetConnectedState(out i, 0))
            {
                //已聯網
                return true;
            }
            else
            {
                //未聯網
                return false;
            }
  
        }
  
        
    }