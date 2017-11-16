using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;

namespace virtualPrint
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

        }
        public delegate void retext(string str);
        static object objectLock = new object();
        static System.Timers.Timer ti = new System.Timers.Timer();
        static System.Timers.Timer ti2 = new System.Timers.Timer(10000);
        static FileStream file = new FileStream(@"./wenben/log.txt", FileMode.OpenOrCreate);
        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                if (Print.dic.Count == 0)
                {
                    if (textBox1.Text != "" && textBox2.Text != "" && textBox5.Text != "")
                    {
                        (new Thread(() =>
                        {
                            IPAddress ip = IPAddress.Parse(textBox1.Text);
                            int controlPort = Int32.Parse(textBox2.Text);
                            int dataPort = Int32.Parse(textBox4.Text);
                            int numPrinters = Int32.Parse(textBox5.Text);

                            List<Guid> li = new List<Guid>();
                            Guid sn ;
                            for (int i = 0; i < numPrinters; )
                            {
                                sn = Guid.NewGuid();
                                if (li.Contains(sn))
                                {
                                    continue;
                                }
                                li.Add(sn);
                                i++;
                                new Print(sn, ip, controlPort, dataPort, addTextAsync);
                            }
                        })).Start();
                        
                    }
                    else
                    {
                        MessageBox.Show("内容不能为空值！");
                    }
                }
                else
                {
                    MessageBox.Show("请先断开之前连接的设备！");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }


        private void button2_Click(object sender, EventArgs e)
        {
            lock (objectLock)
            {
                if (Print.dic.Count > 0)
                {
                    foreach (var keyValue in Print.dic)
                    {
                        keyValue.Close();
                    }
                    Print.conncedCount = 0;
                    Print.openCount = 0;
                    Print.dic.Clear();
                }
            }
        }
        static bool showCount = false;
        private void Form1_Load(object sender, EventArgs e)
        {
           
        }

        private void textBox5_KeyPress(object sender, KeyPressEventArgs e)
        {
            if ((e.KeyChar < 48 || e.KeyChar > 57) && e.KeyChar != 8)
            {
                e.Handled = true;
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            lock (objectLock)
            {
                if (Print.dic.Count > 0)
                {
                    foreach (var keyValue in Print.dic)
                    {
                        keyValue.Close();
                      
                    }
                    Print.conncedCount = 0;
                    Print.dic.Clear();
                }
            }
            file.Flush();
            file.Dispose();
            Application.ExitThread();
        }

        public static void setLog(byte[] data, int type, string sn)
        {
            lock (objectLock)
            {
                byte[] strByte = null;

                if (type == 1)
                {
                    strByte = Encoding.UTF8.GetBytes(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "控制数据" + sn + "：{" + BitConverter.ToString(data) + "}\r\n");
                }
                else if (type == 2)
                {
                    strByte = Encoding.UTF8.GetBytes(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "控制服务器数据" + sn + "：{" + BitConverter.ToString(data) + "}\r\n");
                }
                else if (type == 3)
                {

                    strByte = Encoding.UTF8.GetBytes(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "打印数据" + sn + ":{" + BitConverter.ToString(data) + "}\r\n");
                }
                else if (type == 4)
                {
                    strByte = Encoding.UTF8.GetBytes(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "打印服务器数据" + sn + "：{" + BitConverter.ToString(data) + "}\r\n");
                    //显示二进制的文本文件
                    //StringBuilder sendDa=new StringBuilder(data.Length*8);
                    //foreach (byte da in data)
                    //{
                    //    sendDa.Append(Convert.ToString(da, 2).PadLeft(8, '0'));
                    //}
                    //System.IO.File.AppendAllText(@"./wenben/"+sn+"_"+DateTime.Now.ToString("yyyy-MM-dd.HH.mm.ss")+".txt",DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") +sn+":{"+sendDa.ToString()+"}\r\n");

                }
                else if (type == 5)
                {
                    strByte = Encoding.UTF8.GetBytes(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "打印sn数据" + sn + ":{" + BitConverter.ToString(data) + "}\r\n");
                }

                if (strByte != null)
                {
                    file.Write(strByte, 0, strByte.Length);
                }
            }
        }
        public static void setLog2(byte[] data, string sn, BinaryWriter bw)
        {
            //以二进制的方式写的一个二进制文件
            byte[] dataNew = new byte[data.Length - 16];
            for (int i = 0; i < dataNew.Length; i++)
            {
                dataNew[i] = data[i + 16];
            }
            bw.Write(dataNew);
        }

        public void addText(string str)
        {
           
        }

       

        public void addTextAsync(string str)
        {

            BeginInvoke(new retext(addText), str);
            
        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {
            if (textBox3.TextLength > 4000)
            {
                textBox3.Clear();
            }
        }


        public class Print
        {
            public Print(Guid index, IPAddress ip, int port1, int port2, retext logger)
            {
                this.ip = ip;
                this.index = index;
                this.port1 = port1;
                this.port2 = port2;
                OnPrintLog = logger;
                client = new TcpClient();
                receiveBuffer = new byte[1024];
                received = new List<byte>();
                isAuthenticated = false;
                closed = false;
                hearbeat = false;
                client.BeginConnect(ip, port1, OnConnectComplete, null);
                
            }
            public static List<Print> dic = new List<Print>();
            public static Random RD = new Random();
            public Guid index;
            public int port1;
            public TcpClient client;
            public NetworkStream stream;
            public List<byte> received;
            public byte[] receiveBuffer;
            public bool isAuthenticated;
            public static volatile int openCount = 0;
            public  bool hearbeat;
            public bool state = false;
            public static volatile int conncedCount = 0;
            public int port2;
            public IPAddress ip;
            public bool isBeat = false;
            public event retext OnPrintLog;
           
            private volatile bool closed;

            public void Close()
            {
                closed = true;
                client.Close();
                stream.Close();
                stream.Dispose();
            }

            public void log(string l)
            {
                var x = OnPrintLog;
                if (x != null)
                {
                    x(l);
                }
            }

            public void OnConnectComplete(IAsyncResult ar)
            {
                if (closed)
                {
                    return;
                }

                try
                {
                    client.EndConnect(ar);
                    stream = client.GetStream();
                    if (client.Connected && stream != null)
                    {
                        lock (objectLock)
                        {
                            dic.Add(this);
                            Interlocked.Increment(ref openCount);
                        }
                    }
                }
                catch { }
                try
                {
                    stream.BeginRead(receiveBuffer, 0, receiveBuffer.Length, OnReadComplete, this);
                }
                catch (Exception ex)
                {
                    Interlocked.Decrement(ref openCount);
                }
            }

            private void getConnec()
            {
                if (stream == null)
                {
                    if (client != null)
                    {
                        client.Close();
                        client = new TcpClient();
                        stream.Dispose();
                    }
                }
                client.BeginConnect(ip, port1, OnConnectComplete, null);
            }

            public void OnWriteComplete(IAsyncResult ar)
            {
                if (closed)
                {
                    return;
                }

                try
                {
                    stream.EndWrite(ar);
                }
                catch (Exception ex)
                {
                   
                    client.Close();
                    stream.Dispose();
                    lock (objectLock)
                        dic.Remove(this);
                    getConnec();
                    
                }
            }

            public void OnReadComplete(IAsyncResult ar)
            {
                if (closed)
                {
                    return;
                }
                int readcount = 0;
                try
                {
                    var read = stream.EndRead(ar);
                    if (read == 0)
                    {
                        stream.Dispose();
                        client.Close();
                        (client as IDisposable).Dispose();
                        Interlocked.Decrement(ref openCount);
                        return;
                    }
                    readcount = read;

                    {
                        var tmp = new byte[read];
                        Array.Copy(receiveBuffer, tmp, read);
                        received.AddRange(tmp);
                        if (tmp[4] != 3)
                        {
                            setLog(tmp, 2, index.ToString());
                        }
                    }
                }
                    catch (Exception ex)
                {
                  
                    Interlocked.Decrement(ref openCount);
                    client.Close();
                    stream.Dispose();
                    lock (objectLock)
                    {
                        dic.Remove(this);
                    }
                    getConnec();

                }
                try
                {
                    if (isAuthenticated)
                    {
                        HandleNormalData(readcount);
                    }
                    else
                    {
                        HandleAuthentication(readcount);
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }
                try
                {
                    stream.BeginRead(receiveBuffer, 0, receiveBuffer.Length, OnReadComplete, this);
                }
                catch (Exception ex)
                {
                    throw ex;
                }
               
            }

            public void HandleNormalData(int read)
            {

                if (received.Count >= 16)
                {
                    isBeat = true;
                    int bodySize =
                        received[12] +
                        received[13] * 256 +
                        received[14] * 256 * 256 +
                        received[15] * 256 * 256 * 256;
                    if (received.Count >= 16 + bodySize)
                    {
                        var msg = new byte[16 + bodySize];
                        received.CopyTo(0, msg, 0, 16 + bodySize);
                        received.RemoveRange(0, 16 + bodySize);
                        hearbeat = true;
                        stateTo(hearbeat);
                        if (receiveBuffer[4] == 3 || receiveBuffer[4] == 5)
                        {
                            byte[] ssbytes;
                            if (receiveBuffer[4] == 3)
                            {
                                StringBuilder ss = new StringBuilder();
                                ss.Append("ready\r\n");
                                ss.Append("\r\n");
                                ss.Append("" + RD.Next(4000));
                                ssbytes = Encoding.GetEncoding("UTF-8")
                                    .GetBytes(ss.ToString());
                            }
                            else
                            {
                                ssbytes = Encoding.GetEncoding("utf-8").GetBytes(RD.Next(1000).ToString());
                            }
                            var sendBuffer = new byte[16 + ssbytes.Length];
                            Array.Copy(msg, 0, sendBuffer, 0, 16);
                            Array.Copy(ssbytes, 0, sendBuffer, 16, ssbytes.Length);
                            if (receiveBuffer[4] == 3)
                            {
                                sendBuffer[4] = 4;
                            }
                            else
                            {
                                sendBuffer[4] = 6;
                            }
                            sendBuffer[12] = (byte)(ssbytes.Length & 0xFF);
                            sendBuffer[13] = (byte)((ssbytes.Length & 0xFF00) >> 8);
                            sendBuffer[14] = (byte)((ssbytes.Length & 0xFF0000) >> 16);
                            sendBuffer[15] = (byte)((ssbytes.Length & 0xFF000000) >> 24);
                            stream.BeginWrite(sendBuffer, 0, sendBuffer.Length, OnWriteComplete, this);
                            if (sendBuffer[4] != 4)
                            {
                                setLog(sendBuffer, 1, index.ToString());
                            }
                           
                        }
                        else if(receiveBuffer[4]==7)
                        {
                            openTcp2(index.ToString());
                        }
                    }
                }
            }
            private void stateTo(bool tag)
            {

                if (state != tag)
                {
                    Interlocked.Increment(ref conncedCount);
                }
                state = tag;
            }
            public void HandleAuthentication(int read)
            {
                try
                {
                    
                        if (received.Count >= 20)
                        {
                            isBeat = true;
                            int time = ((received[16] << 8) + received[17]) * 2000;
                            ti.Enabled = true;
                            ti.Interval = time;
                            ti.Elapsed += ((o, e) =>
                            {
                                if (!isBeat)
                                {
                                    client.Close();
                                    stream.Close();
                                }
                                else
                                {
                                    isBeat = false;
                                }
                            });
                            int count = (received[19] << 8) + received[18];
                            StringBuilder ss = new StringBuilder();
                            ss.Append("key=" + count + "\r\n");
                            ss.Append("sn=" + index + "\r\n");
                            ss.Append("model=DD-199\r\n");
                            ss.Append("PROTOCOLVER=1.2\r\n");
                            ss.Append("LANGUAGE=ESC\r\n");
                            ss.Append("xdpi=132\r\n");
                            ss.Append("ydpi=365\r\n");
                            ss.Append("pageWidth=981");
                            var ssbytes = Encoding.GetEncoding("UTF-8").GetBytes(ss.ToString());
                            var sendBuffer = new byte[16 + ssbytes.Length];
                            received.CopyTo(0, sendBuffer, 0, 16);
                            Array.Copy(ssbytes, 0, sendBuffer, 16, ssbytes.Length);
                            sendBuffer[4] = 2;
                            sendBuffer[12] = (byte)(ssbytes.Length & 0xFF);
                            sendBuffer[13] = (byte)((ssbytes.Length & 0xFF00) >> 8);
                            sendBuffer[14] = (byte)((ssbytes.Length & 0xFF0000) >> 16);
                            sendBuffer[15] = (byte)((ssbytes.Length & 0xFF000000) >> 24);
                            received.RemoveRange(0, 20);
                            stream.BeginWrite(sendBuffer, 0, sendBuffer.Length, OnWriteComplete, this);
                            setLog(sendBuffer, 1, index.ToString());
                            isAuthenticated = true;
                            
                        }
                }
                catch (Exception ex)
                {
                    Interlocked.Decrement(ref openCount);
                    //MessageBox.Show(ex.Message);
                }
            }

            private void openTcp2(string sn)
            {

                TcpClient tcp2 = new TcpClient();

                tcp2.Connect(ip, port2);
                NetworkStream sendStream2 = tcp2.GetStream();
                Thread thread2 = new Thread(ListenerServer2);
                Stream st = new FileStream(@"./wenben/" + sn + "_" + DateTime.Now.ToString("yyyy-MM-dd.HH.mm.ss") + ".dat", FileMode.Create);
                BinaryWriter bw = new BinaryWriter(st);
                thread2.Start(new object[] { sendStream2, tcp2, thread2, sn, bw, st });

                byte[] data = Encoding.GetEncoding("GBK").GetBytes(sn);
                setLog(data, 5, sn);
                sendStream2.Write(data, 0, data.Length);
            }

            private void ListenerServer2(object Stream)
            {
                object[] obj = Stream as object[];
                NetworkStream sendStream2 = obj[0] as NetworkStream;
                TcpClient tcp2 = obj[1] as TcpClient;
                Thread thread2 = obj[2] as Thread;
                string sn = obj[3] as string;
                BinaryWriter bw = obj[4] as BinaryWriter;
                Stream st = obj[5] as Stream;
                do
                {
                    try
                    {
                        int readSize;
                        byte[] buffer = new byte[8000];
                        lock (sendStream2)
                        {
                            if (tcp2.Connected)
                            {
                                log("客户端曰" + sn + "：已打开数据通道\n");
                            }
                            byte[] data = null;
                            readSize = sendStream2.Read(buffer, 0, 8000);
                            if (readSize == 0)
                            {
                                bw.Close();
                                st.Close();
                                if (tcp2 != null)
                                {

                                    tcp2.Close();
                                    thread2.Abort();
                                }
                                return;
                            }
                            else//说明时打印信息
                            {
                                if (buffer[4] == 9)
                                {
                                    StringBuilder sql = new StringBuilder();
                                    sql.Append("ready\r\n");
                                    sql.Append("\r\n");
                                    Random ran = new Random();
                                    sql.Append("" + ran.Next(4000));
                                    byte[] dataStr = Encoding.GetEncoding("utf-8").GetBytes(sql.ToString());
                                    data = new byte[16 + dataStr.Length];
                                    for (int i = 0; i < data.Length; i++)
                                    {
                                        if (i < 16)
                                        {
                                            data[i] = buffer[i];
                                        }
                                        else
                                        {
                                            data[i] = dataStr[i - 16];
                                        }
                                    }
                                    data[4] = 0x0A;
                                    //长度替换方法
                                    string len = Convert.ToString(dataStr.Length, 16);
                                    if (len.Length < 8)
                                    {
                                        int num = 8 - len.Length;
                                        for (int cl = 0; cl < num; cl++)
                                        {
                                            len = 0 + len;
                                        }
                                    }
                                    for (int le = 0; le < len.Length; le += 2)
                                    {
                                        string stl = len[le] + "" + len[le + 1];
                                        data[15 - (le / 2)] = (byte)Convert.ToInt32(stl, 16);
                                    }
                                    setLog(data, 3, sn);

                                    sendStream2.Write(data, 0, data.Length);
                                }
                            }
                            byte[] buffernew = new byte[readSize];
                            for (int i = 0; i < readSize; i++)
                            {
                                buffernew[i] = buffer[i];
                            }
                            setLog(buffernew, 4, sn);
                            setLog2(buffernew, sn, bw);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);

                    }
                    //将缓存中的数据写入传输流
                } while (true);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            BeginInvoke(new retext(textBox3.AppendText), "连接总数：" + Print.openCount+" 已认证数:"+Print.conncedCount + "\r\n");
        }
    }
}
