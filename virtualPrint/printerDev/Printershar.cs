using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace virtualPrint.printerDev
{
    public class Printershar
    {
        //1--设备，0--wief
        private int DevType;
        private int codeType;
        private int codeData;
        public static int mState = 0;
        public static int cState = 0;
        public bool contorlTo = false;
        static Random rd = new Random();
        public Printershar(byte[] data)
        {
            if (data.Length >= 24 && data[20] == 0x10 && data[21] == 0x09)
            {
               
                this.DevType = data[13];
                this.codeType = data[21];
                if (data[22] == 1)
                {
                    this.codeData = data[24];
                }
                else
                {
                    this.codeData = 0;
                }
            }
            else
            {
                codeType = 0;
            }
        }
        public Printershar()
        {
            DevType = 0;
            codeType = 0x09;
            codeData = 0x31;
        }
        public byte[] getReData()
        {
            if (codeType == 0x09)
            {
                byte[] data = new byte[0];
                if (DevType == 0)
                {
                    data=getWifeData(codeData);
                }
                else
                {
                    data=getDevData(codeData);
                }
                contorlTo = false;
                return data;
            }
            else
            {
                var index=rd.Next(500,1000);
                byte[] da = Encoding.UTF8.GetBytes(index.ToString());
                contorlTo = true;
                return da;
            }
        }

        private byte[] getDevData(int codeData)
        {
            byte[] data = new byte[]{0x10,0x09,0,0};
            if (codeData == 0x30)
            {
                byte[] dInfo = new byte[] { 0x05, 0x30, 2, 0, 0, };
                dInfo[2] = (byte)mState;
                dInfo[4] = (byte)cState;
                data=new byte[4+dInfo.Length];
                Array.Copy(dInfo,0,data,4,dInfo.Length);
            }
            else if (codeData == 0x34)
            {
                byte[] dData = new byte[] { 0x0D, 0x34, 0, 0x29, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
                int index = rd.Next(9000, 15000);
                dData[5] = (byte)((index & 0xFF000000) >> 24);
                dData[6] = (byte)((index & 0xFF0000) >> 16);
                dData[7] = (byte)((index & 0xFF00) >> 8);
                dData[8] = (byte)(index & 0xFF);
                data=new byte[4+dData.Length];
                Array.Copy(dData,0,data,4,dData.Length);
            }
            data[0] = 0x10;
            data[1] = 0x09;
            data[2] = (byte)(data.Length - 4);
            data[3] = 0x28;
            return data;
        }

        private byte[] getWifeData(int codeData)
        {
            byte[] data = new byte[] { 0x10, 0x09, 0, 0 };
            int len=0;
            //0x30
            byte[] dInfo = new byte[] { 0x0A, 0x30, 0, 0, 0, 0, 0, 1, 2, 2 };
            //0x31
            byte[] dData = new byte[] { 0x0B, 0x31, 0, 0, 0, 0, 0, 0, 0, 0, 0x1A };
            int index=rd.Next(9000, 15000);
            dData[3] = (byte)((index & 0xFF000000) >> 24);
            dData[4] = (byte)((index & 0xFF0000) >> 16);
            dData[5] = (byte)((index & 0xFF00) >> 8);
            dData[6]=(byte)(index&0xFF);
            switch (codeData)
            {
                case 0:
                    data = new byte[8+dInfo.Length+dData.Length];
                    data[5] = 0x80;
                    data[6] = 0x02;
                    len=4+dInfo.Length+dData.Length;
                    Array.Copy(dInfo, 0, data, 8, dInfo.Length);
                    Array.Copy(dData, 0, data, 8+dInfo.Length, dData.Length);
                    break;
                case 0x30:
                    data = new byte[4+dInfo.Length];
                    len=dInfo.Length;
                    Array.Copy(dInfo, 0, data, 4, dInfo.Length);
                    break;
                case 0x31:
                    data = new byte[4+dData.Length];
                    len=dData.Length;
                    Array.Copy(dData, 0, data, 4, dData.Length);
                    break;
            }
            data[0] = 0x10;
            data[1] = 0x09;
            data[2] = (byte)(len);
            data[3] = 0x28;
            return data;
        }
    }
}
