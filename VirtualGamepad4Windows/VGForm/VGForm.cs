using System;
using System.Runtime.InteropServices;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VirtualGamepad4Windows
{
    public partial class VGForm : Form
    {
        public bool mAllowVisible;
        private static bool systemShutdown = false;
        private static int WM_QUERYENDSESSION = 0x11;
        public VGForm(string[] args)
        {
            InitializeComponent();
        }
        /// <summary>
        /// 发生USB设备变化检查是否是Android设备启动服务
        /// </summary>
        /// <param name="m"></param>
        protected override void WndProc(ref Message m)
        {
            try
            {
                if (m.Msg == ScpDevice.WM_DEVICECHANGE)
                {
                    Int32 Type = m.WParam.ToInt32();
                    lock (this)
                    {
                        Program.controlService.HotPlug();
                    }
                }
            }
            catch { }
            if (m.Msg == WM_QUERYENDSESSION)
                systemShutdown = true;

            // If this is WM_QUERYENDSESSION, the closing event should be
            // raised in the base WndProc.
            try { base.WndProc(ref m); }
            catch { }
        }

        private void btn_Test_Click(object sender, EventArgs e)
        {
            Program.commandService.Start();

            Console.WriteLine(Program.commandService.ConnectionsCount.ToString());
        }
    }
}
