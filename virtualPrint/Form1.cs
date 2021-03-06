﻿using System;
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
using virtualPrint.printerDev;
using System.Xml.Serialization;
using System.Collections.Concurrent;

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

        //static FileStream file = new FileStream(@"./wenben/log.txt", FileMode.OpenOrCreate);
        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                if (Print.dic.Count == 0)
                {
                    if (textBox1.Text != "" && textBox2.Text != "" && txb_startNnm.Text != "")
                    {
                        string dataS = "";
                        string dataE = "";
                        if (txb_endNum.Text == "")
                        {
                            txb_endNum.Text = txb_startNnm.Text;
                            dataS = txb_startNnm.Text.Substring(txb_startNnm.Text.Length - 6);
                            dataE = txb_endNum.Text.Substring(txb_endNum.Text.Length - 6);
                        }
                        else
                        {
                            dataS = txb_startNnm.Text.Substring(txb_startNnm.Text.Length - 6);
                            dataE = txb_endNum.Text.Substring(txb_endNum.Text.Length - 6);
                            if (Convert.ToUInt32(dataE, 16) < Convert.ToUInt32(dataS, 16))
                            {
                                MessageBox.Show("编号不能小于设置的最小编号值！");
                                return;
                            }
                        }
                        Print.model = txb_model.Text;
                        uint numPrinters = (uint)(Convert.ToUInt32(dataE, 16) - Convert.ToUInt32(dataS, 16) + 1);
                        this.lb_num.Text = numPrinters.ToString();
                        IPAddress ip = IPAddress.Parse(textBox1.Text);
                        int controlPort = Int32.Parse(textBox2.Text);
                        int dataPort = Int32.Parse(textBox4.Text);
                        int jinPort = Int32.Parse(txb_jinzhi.Text);
                        int needNum = Int32.Parse(lb_num.Text);
                        string number = txb_startNnm.Text;
                        string prefix = txb_startNnm.Text.Substring(0, txb_startNnm.TextLength - 6);
                        (new Thread(() =>
                        {
                            Random ra = new Random();
                            string numstr = number.Substring(number.Length - 6);
                            uint num = Convert.ToUInt32(numstr, 16);
                            for (int i = 0; i < needNum; i++)
                            {
                                uint n = (uint)(num + i);
                                int sn = ra.Next(10000000, 90000000);
                                string actualNumber = prefix + string.Format("{0:X6}", n);
                                if (string.IsNullOrWhiteSpace(actualNumber))
                                {
                                    string msg = "遭遇字符串为空白或空串。\r\n";
                                    msg += "prefix = " + (prefix ?? "(null)") + "\r\n";
                                    msg += "n = " + (string.Format("{0:X6}", n) ?? "(null)") + "\r\n";
                                    msg += "actualNumber = " + (actualNumber ?? "(null)");
                                    MessageBox.Show(msg);
                                }
                                if (i % 3000 == 0)
                                {
                                    Thread.Sleep(3000);
                                }
                                new Print(sn, ip, controlPort, dataPort, addTextAsync,
                                    actualNumber);

                            }
                        })).Start();
                        using (FileStream file = new FileStream("./cfg.xml", FileMode.OpenOrCreate))
                        {
                            if (file.Length > 0)
                            {
                                file.SetLength(0);
                                file.Seek(0, 0);
                            }
                            XmlSerializer xml = new XmlSerializer(typeof(ipCfg));
                            ipCfg fg = new ipCfg()
                            {
                                ip = textBox1.Text,
                                contorlPort = textBox2.Text,
                                dataPort = textBox4.Text,
                                model = txb_model.Text
                            };
                            xml.Serialize(file, fg);

                        }

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
            addText("执行关闭操作\r\n");
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
                    Print.connCount = 0;
                    Print.dic.Clear();
                }
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.lb_banben.Text = "V5.1.18";
            ToolTip tool = new ToolTip();
            tool.SetToolTip(this.txb_endNum, "如果设置为空则表示选择一台打印机！");
            tool.SetToolTip(this.button1, "如果重连请先等服务器将原来的数据处理完毕之后再重连！！！");
            ulong numPrinters = Convert.ToUInt64(txb_endNum.Text, 16) - Convert.ToUInt64(txb_startNnm.Text, 16) + 1;
            this.lb_num.Text = numPrinters.ToString();
            cmb_mState.SelectedIndex = 0;
            using (FileStream file = new FileStream("./cfg.xml", FileMode.OpenOrCreate))
            {
                if (file.Length > 0)
                {
                    XmlSerializer xml = new XmlSerializer(typeof(ipCfg));
                    var result = xml.Deserialize(file) as ipCfg;
                    textBox1.Text = result.ip;
                    textBox2.Text = result.contorlPort;
                    textBox4.Text = result.dataPort;
                    txb_model.Text = result.model;
                }
            }


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
            //file.Flush();
            //file.Dispose();
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
                    //file.Write(strByte, 0, strByte.Length);
                }
            }
        }
        public static void setLog2(byte[] data, string sn, BinaryWriter bw)
        {
            //以二进制的方式写的一个二进制文件
            byte[] dataNew = new byte[data.Length - 20];
            for (int i = 0; i < dataNew.Length; i++)
            {
                dataNew[i] = data[i + 20];
            }
            bw.Write(dataNew);
        }

        public void addText(string str)
        {
            try
            {
                textBox3.AppendText(str);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }



        public void addTextAsync(string str)
        {

            BeginInvoke(new retext(addText), str);

        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (textBox3.TextLength > 4000)
                {
                    textBox3.ClearUndo();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }


        public class Print
        {
            public Print(int index, IPAddress ip, int port1, int port2, retext logger, string number)
            {
                this.ip = ip;
                this.index = index;
                this.port1 = port1;
                this.port2 = port2;
                OnPrintLog = logger;
                client = new TcpClient();
                receiveBuffer = new byte[1024];
                received = new List<byte>();
                closed = false;
                this.number = number;
                Interlocked.Increment(ref connCount);
                // BeginConnect 必须是构造函数中最后一个操作。
                client.BeginConnect(ip, port1, OnConnectComplete, null);
            }

            public static List<Print> dic = new List<Print>();
            public static Random RD = new Random();
            public int index;
            public int port1;
            public TcpClient client;
            public NetworkStream stream;
            public List<byte> received;
            public byte[] receiveBuffer;
            public static volatile int openCount = 0;
            public bool state = false;
            public static volatile int conncedCount = 0;
            public int port2;
            public IPAddress ip;
            public bool isBeat = false;
            public event retext OnPrintLog;
            public static readonly int HEADER_LENGTH = 20;
            private volatile bool closed;
            public int count = 0;
            public readonly string number;
            public static volatile int connCount = 0;
            public static string model = "";
            public static volatile int printSendCount = 0;
            public string sn()
            {
                return index.ToString();
            }
            System.Timers.Timer ti = new System.Timers.Timer();

            public void Close()
            {
                closed = true;
                client.Close();
                stream.Close();
                stream.Dispose();
            }

            public void log(string l)
            {
                try
                {
                    var x = OnPrintLog;
                    if (x != null)
                    {
                        //if (number.Equals("000AF92CE7010006"))
                        {
                            x(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + ":" + l);
                            //x(number + "心跳发送时间：" + dateHeart+"\r\n");
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
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
                catch
                {
                    log("服务器无法连接上！");
                    if (!closed)
                    {
                        Interlocked.Decrement(ref connCount);
                    }
                    return;
                }
                try
                {
                    HandleAuthentication();
                    stream.BeginRead(receiveBuffer, 0, receiveBuffer.Length, OnReadComplete, this);
                }
                catch
                {
                    Interlocked.Decrement(ref openCount);
                }
            }

            private void getConnec()
            {
                if (stream != null)
                {
                    client.Close();
                    client = new TcpClient();
                    stream.Dispose();
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
                catch
                {
                    log("写入数据失败！");
                    client.Close();
                    stream.Dispose();
                    lock (objectLock)
                        dic.Remove(this);
                    //getConnec();

                }
            }
            /// <summary>
            /// 异步读取
            /// </summary>
            /// <param name="ar"></param>
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
                        log("服务器无数据！本地端口：" + client.Client.LocalEndPoint.ToString() +"\r\n");
                        stream.Dispose();
                        client.Close();
                        (client as IDisposable).Dispose();
                        Interlocked.Decrement(ref openCount);
                        return;
                    }
                    bytesReceived += read;
                    readCompleteCount += 1;
                    readcount = read;
                    var tmp = new byte[read];
                    {
                        Array.Copy(receiveBuffer, tmp, read);
                        received.AddRange(tmp);
                    }
                }
                catch (Exception ex)
                {
                    log("已经断开TCP连接" + " 异常：" + ex.Message + "\r\n");
                    Interlocked.Decrement(ref openCount);
                    client.Close();
                    stream.Dispose();
                    lock (objectLock)
                    {
                        dic.Remove(this);
                    }
                    //getConnec();
                }
                try
                {
                    HandleNormalData(readcount);
                }
                catch (Exception ex)
                {
                    log("出现一个未知的错误信息！" + string.Format("异常{0}，追踪{1}", ex, ex.StackTrace));
                    Interlocked.Decrement(ref openCount);
                }
                try
                {
                    stream.BeginRead(receiveBuffer, 0, receiveBuffer.Length, OnReadComplete, this);
                }
                catch (Exception ex)
                {
                    log("已经关闭了该连接！" + " 异常：" + ex.Message + "\r\n");
                    Interlocked.Decrement(ref openCount);
                }

            }

            private DateTime lastTimeoutCheck = DateTime.Now;

            /// <summary>
            /// 返回数据信息验证结果
            /// </summary>
            /// <param name="received"></param>
            private void OnAuthenticationResponse(byte[] received)
            {
                if (received.Length != 24)
                {
                    log("服务器发送了不完整的认证信息！长度应当为24，实际为" + received.Length + "\r\n");
                    client.Close();
                    stream.Close();
                    stream.Dispose();
                }

                if (received[13] != 2)
                {
                    log("服务器认证消息的“设备类型”字段应当固定为2，此处收到" + received[13] + "\r\n");
                }

                isBeat = true;
                lastTimeoutCheck = DateTime.Now;
                int time = ((received[23] << 8) + received[22]) * 3000;
                ti.Enabled = true;
                ti.Interval = time;
                ti.Elapsed += ((o, e) =>
                {
                    if (!closed && ti.Enabled)
                    {
                        if (!isBeat)
                        {
                            log("设备：" + number + " 最后认证时间：" + lastTimeoutCheck + "超时：" + DateTime.Now + "\r\n");
                            client.Close();
                            stream.Close();
                            stream.Dispose();
                            ti.Dispose();
                        }
                        else
                        {
                            isBeat = false;

                        }
                    }
                });
                count = (received[21] << 8) + received[20];
                switch (count)
                {
                    case 0:
                        log("验证成功！\r\n");
                        Interlocked.Increment(ref conncedCount);
                        break;
                    case 1:
                        log("设备ID未注册\r\n");
                        break;
                    case 2:
                        log("信息获取失败!\r\n");
                        break;
                    case 3:
                        log("该设备已登录\r\n");
                        break;
                    case 4:
                        log("其他错误，未定义\r\n");
                        break;
                }
            }
            private static ConcurrentDictionary<string, dataPrint> cdic = new ConcurrentDictionary<string, dataPrint>();
            public static List<string> liNumber = new List<string>();
            /// <summary>
            /// 打印通知开启数据通道
            /// </summary>
            /// <param name="received"></param>
            private void OnPrintRequest(byte[] received)
            {
                if (received.Length != HEADER_LENGTH)
                {
                    log("服务器发送了超长的打印通道开启请求！长度应当为20，实际为" + received.Length + "\r\n");
                }
                if (!liNumber.Contains(number))
                {
                    Interlocked.Increment(ref printSendCount);
                    liNumber.Add(number);
                    var dp=new dataPrint(ip, port2, this.OnPrintLog, number);
                    cdic.TryAdd(number, dp);
                }else
                {
                    Interlocked.Decrement(ref printSendCount);
                    dataPrint dp;
                    cdic.TryRemove(number, out dp);
                    dp.closeThread();
                    var ndp=new dataPrint(ip, port2, this.OnPrintLog, number);
                    cdic.TryAdd(number, ndp);
                }
            }
            string dateHeart = "";
            /// <summary>
            /// 发送心跳内容
            /// </summary>
            /// <param name="received"></param>
            private void OnCommandOrHeartbeat(byte[] received)
            {
                isBeat = true;
                var p = new Printershar(received);
                var data = p.getReData();
                if (p.contorlTo)
                {
                    log(number + ":" + "{" + BitConverter.ToString(received) + "}\r\n");
                    //setLog(received, 2, number);
                }
                byte[] dataAll1 = new byte[HEADER_LENGTH + data.Length];
                Array.Copy(received, 0, dataAll1, 0, HEADER_LENGTH);
                Array.Copy(data, 0, dataAll1, HEADER_LENGTH, data.Length);
                dataAll1[8] = (byte)(data.Length & 0xFF);
                dataAll1[9] = (byte)((data.Length & 0xFF00) >> 8);
                dataAll1[10] = (byte)((data.Length & 0xFF0000) >> 16);
                dataAll1[11] = (byte)((data.Length & 0xFF000000) >> 24);
                try
                {
                    dateHeart = DateTime.Now.ToString();
                    stream.BeginWrite(dataAll1, 0, dataAll1.Length, OnWriteComplete, this);
                    //log("心跳回复已发送。\r\n");
                }
                catch
                {
                    log("写入数据失败!\r\n");
                }
            }
            /// <summary>
            /// 标头固定值是否一致
            /// </summary>
            /// <returns></returns>
            private bool IsValidHeaderSignature()
            {
                return received[0] == 0x40 && received[1] == 0x41 && received[2] == 0x2f && received[3] == 0x3f;
            }

            private int bytesReceived = 0;

            private int readCompleteCount = 0;
            /// <summary>
            /// 读取到数据进行分类处理
            /// </summary>
            /// <param name="read"></param>
            public void HandleNormalData(int read)
            {
                int bodySize = 0;
                isBeat = true;
                lastTimeoutCheck = DateTime.Now;
                while (HasCompleteMessage(ref bodySize))
                {

                    if (!IsValidHeaderSignature())
                    {
                        log("消息头标记不正确：" + string.Format("0x{0:X2} 0x{1:X2} 0x{2:X2} 0x{3:X2}",
                            received[0], received[1], received[2], received[3]));
                    }

                    var message = received.Take(HEADER_LENGTH + bodySize).ToArray();
                    switch (received[12])
                    {
                        case virtuP.sureIndex:
                            OnAuthenticationResponse(message);
                            break;
                        case virtuP.printIndex:
                            OnPrintRequest(message);
                            break;
                        case virtuP.conmendIndex:
                            OnCommandOrHeartbeat(message);
                            break;
                        default:
                            {
                                //TODO : 记录错误日志。
                                log("收到一条非控制命令，命令码" + received[12]);
                            }
                            break;
                    }
                    received.RemoveRange(0, HEADER_LENGTH + bodySize);
                }

            }
            /// <summary>
            /// 验证是否是一个完整的消息结构
            /// </summary>
            /// <param name="bodySize"></param>
            /// <returns></returns>
            public bool HasCompleteMessage(ref int bodySize)
            {
                if (received.Count < HEADER_LENGTH)
                {
                    return false;
                }

                bodySize =
                   received[8] +
                   received[9] * 256 +
                   received[10] * 256 * 256 +
                   received[11] * 256 * 256 * 256;

                return received.Count >= HEADER_LENGTH + bodySize;
            }

            /// <summary>
            /// 认证发送信息
            /// </summary>
            public void HandleAuthentication()
            {
                try
                {
                    var n = Encoding.GetEncoding("UTF-8").GetBytes(number.Substring(number.Length - 4));
                    byte[] dataHeard = new byte[20];
                    dataHeard[0] = 0x40;
                    dataHeard[1] = 0x41;
                    dataHeard[2] = 0x2f;
                    dataHeard[3] = 0x3f;
                    dataHeard[4] = n[0];
                    dataHeard[5] = n[1];
                    dataHeard[6] = n[2];
                    dataHeard[7] = n[3];
                    dataHeard[12] = virtuP.sureIndex;
                    dataHeard[13] = 2;
                    int hreadall = 8;
                    //通用
                    int hreadWife = 3;
                    string sWife = model + ";" + number + ";1.2;1.3;2.3;1.2;";
                    var sDbyte = Encoding.UTF8.GetBytes(sWife);
                    //系统..... 固定6个字节
                    int infolength = 6;
                    //用户
                    int hreadUser = 2;
                    byte[] ssbytes = new byte[11];
                    string s = "";
                    for (int i = 0; i < number.Length; i++)
                    {
                        if (i % 2 != 0)
                        {
                            s += number[i];
                            int index = (i + 1) / 2;
                            ssbytes[index] = Convert.ToByte(s, 16);
                            s = "";
                        }
                        else
                        {
                            s += number[i];
                        }
                    }
                    ssbytes[0] = 8;
                    ssbytes[9] = 1;
                    ssbytes[10] = 1;
                    int len = sDbyte.Length + ssbytes.Length + hreadall + hreadUser + hreadWife + infolength;
                    var sendBuffer = new byte[HEADER_LENGTH + len];
                    //头
                    Array.Copy(dataHeard, 0, sendBuffer, 0, HEADER_LENGTH);
                    //通用
                    Array.Copy(sDbyte, 0, sendBuffer, HEADER_LENGTH + hreadall + hreadWife, sDbyte.Length);
                    //系统
                    int len1 = HEADER_LENGTH + hreadall + hreadWife + sDbyte.Length;
                    sendBuffer[len1] = 0x06;
                    sendBuffer[len1 + 1] = 0x02;
                    sendBuffer[len1 + 2] = (byte)((len1 & 0xFF000000) >> 24);
                    sendBuffer[len1 + 3] = (byte)((len1 & 0xFF0000) >> 16);
                    sendBuffer[len1 + 4] = (byte)((len1 & 0xFF00) >> 8);
                    sendBuffer[len1 + 5] = (byte)(len1 & 0xFF);
                    //用户
                    Array.Copy(ssbytes, 0, sendBuffer, HEADER_LENGTH + sDbyte.Length + hreadall + hreadWife + infolength + hreadUser, ssbytes.Length);

                    //所有信息返回消息头
                    sendBuffer[20] = 0x10;
                    sendBuffer[21] = 0x0c;
                    sendBuffer[22] = (byte)(len - 4);
                    sendBuffer[23] = 0x67;
                    sendBuffer[24] = 0;
                    //所有
                    sendBuffer[25] = 0x80;
                    sendBuffer[26] = 0x03;
                    //通用
                    sendBuffer[27] = 0;
                    sendBuffer[28] = (byte)(sDbyte.Length + hreadWife);
                    sendBuffer[29] = 0x01;
                    sendBuffer[30] = 0x3B;
                    //用户
                    int len2 = HEADER_LENGTH + sDbyte.Length + hreadall + hreadWife + infolength;
                    sendBuffer[len2] = (byte)(hreadUser + ssbytes.Length);
                    sendBuffer[len2 + 1] = 0x03;


                    //获取设备信息内容
                    //设备前部分8个字节
                    int devHeard = 8;
                    //主体01H
                    int OneHeard = 3;
                    string sDev = model + ";123656317;1.2;1.3;2.3;1.2;";
                    var OneBody = Encoding.UTF8.GetBytes(sDev);
                    byte[] OneBodyAll = new byte[OneHeard + OneBody.Length];
                    OneBodyAll[0] = (byte)(OneBody.Length + OneHeard);
                    OneBodyAll[1] = 0x1;
                    OneBodyAll[2] = 0x3B;
                    Array.Copy(OneBody, 0, OneBodyAll, OneHeard, OneBody.Length);

                    //主体02H
                    int TwoLen = 11;
                    int buffer = 300;
                    int frame = 500;
                    byte[] TwoBodyAll = new byte[TwoLen];
                    TwoBodyAll[0] = (byte)TwoLen;
                    TwoBodyAll[1] = 0x02;
                    TwoBodyAll[2] = (byte)(buffer >> 24);
                    TwoBodyAll[3] = (byte)(buffer >> 16);
                    TwoBodyAll[4] = (byte)(buffer >> 8);
                    TwoBodyAll[5] = (byte)buffer;
                    TwoBodyAll[6] = (byte)(frame >> 24);
                    TwoBodyAll[7] = (byte)(frame >> 16);
                    TwoBodyAll[8] = (byte)(frame >> 8);
                    TwoBodyAll[9] = (byte)frame;
                    TwoBodyAll[10] = 0x1;

                    //主体03H
                    int ThreeLen = 15;
                    int pixW = 200;
                    int pixH = 500;
                    int pixB = 400;
                    byte[] ThreeBodyAll = new byte[ThreeLen];
                    ThreeBodyAll[0] = (byte)ThreeLen;
                    ThreeBodyAll[1] = 0x03;
                    ThreeBodyAll[2] = (byte)(pixW >> 8);
                    ThreeBodyAll[3] = (byte)pixW;
                    ThreeBodyAll[4] = (byte)(pixH >> 8);
                    ThreeBodyAll[5] = (byte)pixH;
                    ThreeBodyAll[6] = (byte)(pixB >> 24);
                    ThreeBodyAll[7] = (byte)(pixB >> 16);
                    ThreeBodyAll[8] = (byte)(pixB >> 8);
                    ThreeBodyAll[9] = (byte)pixB;
                    ThreeBodyAll[10] = 0x26;
                    ThreeBodyAll[11] = 0x23;
                    ThreeBodyAll[12] = 0x1;
                    ThreeBodyAll[13] = 1;
                    ThreeBodyAll[14] = 1;

                    byte[] sendDev = new byte[devHeard + OneBodyAll.Length + TwoLen + ThreeLen];
                    Array.Copy(OneBodyAll, 0, sendDev, devHeard, OneBodyAll.Length);
                    Array.Copy(TwoBodyAll, 0, sendDev, devHeard + OneBodyAll.Length, TwoLen);
                    Array.Copy(ThreeBodyAll, 0, sendDev, devHeard + OneBodyAll.Length + TwoLen, ThreeLen);
                    sendDev[0] = 0x10;
                    sendDev[1] = 0x0C;
                    sendDev[2] = (byte)(4 + OneBodyAll.Length + TwoLen + ThreeLen);
                    sendDev[3] = (byte)(4 + OneBodyAll.Length + TwoLen + ThreeLen);
                    sendDev[4] = 0;
                    sendDev[5] = 1;
                    sendDev[6] = 3;
                    sendDev[7] = 0;

                    //发送总内容
                    byte[] sendBufferAll = new byte[sendBuffer.Length + sendDev.Length];
                    Array.Copy(sendBuffer, 0, sendBufferAll, 0, sendBuffer.Length);
                    Array.Copy(sendDev, 0, sendBufferAll, sendBuffer.Length, sendDev.Length);

                    //消息体总长度
                    sendBufferAll[8] = (byte)(len + sendDev.Length);
                    sendBufferAll[9] = (byte)((len + sendDev.Length) >> 8);
                    sendBufferAll[10] = (byte)((len + sendDev.Length) >> 16);
                    sendBufferAll[11] = (byte)((len + sendDev.Length) >> 24);


                    //sendBuffer[8] = (byte)(len );
                    //sendBuffer[9] = (byte)((len) >> 8);
                    //sendBuffer[10] = (byte)((len) >> 16);
                    //sendBuffer[11] = (byte)((len) >> 24);

                    //setLog(sendBufferAll, 1, number);
                    stream.BeginWrite(sendBufferAll, 0, sendBufferAll.Length, OnWriteComplete, this);
                }
                catch
                {
                    Interlocked.Decrement(ref openCount);
                }
            }



        }


        public class dataPrint
        {
            public dataPrint(IPAddress ip, int port2, retext logger, string number)
            {
                this.ip = ip;
                this.port2 = port2;
                this.logger = logger;
                this.number = number;
                //st = new FileStream(@"./wenben/" + number + "_" + DateTime.Now.ToString("yyyy-MM-dd.HH.mm.ss") + ".dat", FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
                //this.bw = new BinaryWriter(st);
                client.BeginConnect(ip, port2, onConnectCall, this);
            }
            IPAddress ip;
            int port2;
            event retext logger;
            void log(string str)
            {
                var x = logger;
                x(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + ":" + str);
            }
            string number;
            TcpClient client = new TcpClient();
            NetworkStream stream;
            List<byte> reciced = new List<byte>();
            byte[] buffer = new byte[2000];
            //BinaryWriter bw;
            //Stream st;
            public static volatile int printSuerCount = 0;

            public void closeThread()
            {
                var thread = Thread.CurrentThread;
                client.Close();
                stream.Close();
                stream.Flush();
                stream.Dispose();
                thread.Abort();
            }


            private void onConnectCall(IAsyncResult ar)
            {
                try
                {
                    client.EndConnect(ar);
                    stream = client.GetStream();
                    if (stream != null)
                    {
                        Interlocked.Increment(ref printSuerCount);
                        log("设备" + number + "：打开数据通道成功！\r\n");
                        stream.BeginRead(buffer, 0, buffer.Length, onReadCall, this);
                    }
                }
                catch (Exception ex)
                {
                    log("数据通道打开失败！" + " 异常：" + ex.Message + "\r\n");
                    Print.liNumber.Remove(number);
                    return;
                }
            }

            private void onReadCall(IAsyncResult ar)
            {
                try
                {
                    int readCount = stream.EndRead(ar);
                    if (readCount == 0)
                    {
                        log("设备" + number + "数据通道关闭！" + " 端口：" + client.Client.LocalEndPoint + "\r\n");
                        //bw.Close();
                        //st.Close();
                        Print.liNumber.Remove(number);
                        return;
                    }
                    byte[] buf = new byte[readCount];
                    Array.Copy(buffer, 0, buf, 0, readCount);
                    reciced.AddRange(buf);
                }
                catch (Exception ex)
                {
                    log("设备" + number + "数据通道读取失败！" + " 异常：" + ex.Message + "\r\n");
                    Print.liNumber.Remove(number);
                    return;
                }
                try
                {
                    DataReSend();
                }
                catch
                {
                    log("设备" + number + "数据通道发送数据结构内容出现错误！");
                    Print.liNumber.Remove(number);
                    return;
                }
                try
                {
                    stream.BeginRead(buffer, 0, buffer.Length, onReadCall, this);
                }
                catch (Exception ex)
                {
                    log("设备" + number + "数据通道再次读取数据信息失败！" + " 异常：" + ex.Message + "\r\n");
                    Print.liNumber.Remove(number);
                    return;
                }
            }


            private void DataReSend()
            {
                int bodySize = 0;
                while (HasCompleteMessage(ref bodySize))
                {
                    if (reciced[12] == virtuP.rePrinIndex)
                    {
                        byte[] dataNum = new byte[8];
                        string s = "";
                        for (int i = 0; i < number.Length; i++)
                        {
                            if (i % 2 != 0)
                            {
                                s += number[i];
                                int index = (i + 1) / 2;
                                dataNum[index - 1] = Convert.ToByte(s, 16);
                                s = "";
                            }
                            else
                            {
                                s += number[i];
                            }
                        }
                        byte[] dataHeard = new byte[Print.HEADER_LENGTH];
                        dataHeard[0] = 0x40;
                        dataHeard[1] = 0x41;
                        dataHeard[2] = 0x2f;
                        dataHeard[3] = 0x3f;
                        dataHeard[4] = buffer[4];
                        dataHeard[5] = buffer[5];
                        dataHeard[6] = buffer[6];
                        dataHeard[7] = buffer[7];
                        dataHeard[8] = (byte)(dataNum.Length & 0xFF);
                        dataHeard[9] = (byte)((dataNum.Length & 0xFF00) >> 8);
                        dataHeard[10] = (byte)((dataNum.Length & 0xFF0000) >> 16);
                        dataHeard[11] = (byte)((dataNum.Length & 0xFF000000) >> 24);
                        dataHeard[12] = virtuP.rePrinIndex;
                        dataHeard[13] = 2;
                        byte[] dataAll = new byte[Print.HEADER_LENGTH + dataNum.Length];
                        Array.Copy(dataHeard, 0, dataAll, 0, Print.HEADER_LENGTH);
                        Array.Copy(dataNum, 0, dataAll, Print.HEADER_LENGTH, dataNum.Length);
                        //setLog(dataAll, 5, number);
                        stream.BeginWrite(dataAll, 0, dataAll.Length, onEndWritCall, this);
                    }
                    else if (reciced[12] == virtuP.printConmendIndex)
                    {
                        //默认wife状态
                        var p = new Printershar();
                        byte[] dataStr = p.getReData();
                        var data = new byte[Print.HEADER_LENGTH + dataStr.Length];
                        for (int i = 0; i < data.Length; i++)
                        {
                            if (i < Print.HEADER_LENGTH)
                            {
                                data[i] = reciced[i];
                            }
                            else
                            {
                                data[i] = dataStr[i - Print.HEADER_LENGTH];
                            }
                        }
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
                            data[11 - (le / 2)] = (byte)Convert.ToInt32(stl, 16);
                        }
                        //setLog(data, 3, number);

                        stream.BeginWrite(data, 0, data.Length, onEndWritCall, this);
                    }
                    if (reciced[12] == virtuP.printConmendIndex)
                    {
                        byte[] buffernew = new byte[Print.HEADER_LENGTH + bodySize];
                        for (int i = 0; i < Print.HEADER_LENGTH + bodySize; i++)
                        {
                            buffernew[i] = reciced[i];
                        }
                        //setLog(buffernew, 4, number);
                        //setLog2(buffernew, number, bw);
                    }
                    reciced.RemoveRange(0, Print.HEADER_LENGTH + bodySize);
                }

            }

            public bool HasCompleteMessage(ref int bodySize)
            {
                if (reciced.Count < Print.HEADER_LENGTH)
                {
                    return false;
                }

                bodySize =
                   reciced[8] +
                   reciced[9] * 256 +
                   reciced[10] * 256 * 256 +
                   reciced[11] * 256 * 256 * 256;

                return reciced.Count >= Print.HEADER_LENGTH + bodySize;
            }

            private void onEndWritCall(IAsyncResult ar)
            {
                stream.EndWrite(ar);
            }
        }






        private void button3_Click(object sender, EventArgs e)
        {
            BeginInvoke(new retext(textBox3.AppendText), "连接总数：" + Print.openCount + " 已认证数:" + Print.conncedCount + " Tcp实际打开数:" + Print.connCount
                + "数据通道连接成功数：" + dataPrint.printSuerCount + " 控制发送打开数据的实际数：" + Print.printSendCount + "\r\n");
        }

        private void txb_endNum_KeyPress(object sender, KeyPressEventArgs e)
        {
            //if ((e.KeyChar < 48 || e.KeyChar > 57) && e.KeyChar != 8)
            //{
            //    e.Handled = true;
            //}
        }

        private void txb_endNum_TextChanged(object sender, EventArgs e)
        {
            //if (txb_endNum.Text.Length > 14)
            //{

            //    string dataS=txb_startNnm.Text.Substring(txb_startNnm.Text.Length - 6);
            //    string dataE=txb_endNum.Text.Substring(txb_endNum.Text.Length - 6);
            //   if (Convert.ToUInt16(dataE,16)< Convert.ToUInt16(dataS, 16))
            //    {
            //        MessageBox.Show("编号不能小于设置的最小编号值！");
            //        return;
            //    }
            //    ushort numPrinters =(ushort)(Convert.ToUInt16(dataE, 16) - Convert.ToUInt16(dataS, 16) + 1);
            //    this.lb_num.Text = numPrinters.ToString();
            //}
        }

        private void cmb_mState_SelectedIndexChanged(object sender, EventArgs e)
        {
            cmb_cState.Items.Clear();
            int mState = 0;
            switch (cmb_mState.SelectedIndex)
            {
                case 0:
                    cmb_cState.Items.Add("");
                    mState = 2;//ready
                    break;
                case 1:
                    cmb_cState.Items.Add("");
                    mState = 3;
                    break;
                case 2:
                    mState = 0xFF;
                    cmb_cState.Items.Add("errDoorOpen");
                    cmb_cState.Items.Add("errMediaEmpty");
                    cmb_cState.Items.Add("errMarkerSupplyEmpty");
                    cmb_cState.Items.Add("errMediaJam");
                    break;
                case 3:
                    mState = 4;
                    cmb_cState.Items.Add("warnMediaLow");
                    cmb_cState.Items.Add("warnMarkerSupplyLow");
                    cmb_cState.Items.Add("warnHeadTooHot");
                    cmb_cState.Items.Add("warnNeedClean");
                    break;
            }
            cmb_cState.SelectedIndex = 0;
            Printershar.mState = mState;
        }

        private void cmb_cState_SelectedIndexChanged(object sender, EventArgs e)
        {
            Printershar.cState = cmb_cState.SelectedIndex;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            Print.printSendCount = 0;
            dataPrint.printSuerCount = 0;
        }
    }
    class virtuP
    {
        public const int sureIndex = 1;
        public const int conmendIndex = 5;
        public const int printIndex = 3;
        public const int rePrinIndex = 4;
        public const int printConmendIndex = 2;
        public const int DevIndex = 1;
    }
}
