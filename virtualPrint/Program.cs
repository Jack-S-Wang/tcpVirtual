using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace virtualPrint
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new Form1());
            }
            catch (Exception ex)
            {
                var str = string.Format("抛出了一个异常：{0}，栈跟踪内容：{1}",
                    ex,
                    ex.StackTrace);
                MessageBox.Show(str);
            }
        }
    }
}
