using System;
using System.Windows.Forms;
using System.Threading;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.ComponentModel;
using System.Globalization;

namespace VirtualGamepad4Windows
{
    static class Program
    {
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();
        private static String SingleAppComEventName = "{47bf37c7-e896-43ac-a731-5c50491ecdb6}";
        //static Mutex mutex = new Mutex(true, "{FI329DM2-DS4W-J2K2-HYES-92H21B3WJARG}");
        private static BackgroundWorker singleAppComThread = null;
        private static EventWaitHandle threadComEvent = null;
        //public static ControlService rootHub;

        [STAThread]
        static void Main(string[] args)
        {
            System.Runtime.GCSettings.LatencyMode = System.Runtime.GCLatencyMode.LowLatency;
            try
            {
                Process.GetCurrentProcess().PriorityClass = System.Diagnostics.ProcessPriorityClass.High;
            }
            catch
            {
                // 忽略提高进程优先级时候的异常
            }
            try
            {
                // 如果OpenExsting返回成功，另一个已经在运行的实例. 关闭自己
                threadComEvent = EventWaitHandle.OpenExisting(SingleAppComEventName);
                threadComEvent.Set();  // 发信号给另外的实例.
                threadComEvent.Close();
                return;    // return immediately.
            }
            catch
            {
                /* 忽略重复打开后关闭时候的异常  */
            }
            //创建事件句柄
            threadComEvent = new EventWaitHandle(false, EventResetMode.AutoReset, SingleAppComEventName);
            CreateInterAppComThread();

            if (mutex.WaitOne(TimeSpan.Zero, true))
            {
                rootHub = new ControlService();
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new VGForm(args));
                mutex.ReleaseMutex();
            }

            // End the communication thread.
            singleAppComThread.CancelAsync();
            while (singleAppComThread.IsBusy)
                Thread.Sleep(50);
            threadComEvent.Close();

        }

        static private void CreateInterAppComThread()
        {
            singleAppComThread = new BackgroundWorker();
            singleAppComThread.WorkerReportsProgress = false;
            singleAppComThread.WorkerSupportsCancellation = true;
            singleAppComThread.DoWork += new DoWorkEventHandler(singleAppComThread_DoWork);
            singleAppComThread.RunWorkerAsync();
        }
        static private void singleAppComThread_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            WaitHandle[] waitHandles = new WaitHandle[] { threadComEvent };

            while (!worker.CancellationPending)
            {
                // 每秒检查一次信号
                if (WaitHandle.WaitAny(waitHandles, 1000) == 0)
                {
                    // 不允许启动其他实例 
                    // 所以当用户试图启动其他实例，将现有实例的用户界面唤醒 
                    // 那么该Form创建在其他实例的话就需要一些同步魔法
                    if (Application.OpenForms.Count > 0)
                    {
                        Form mainForm = Application.OpenForms[0];
                        mainForm.Invoke(new SetFormVisableDelegate(ThreadFormVisable), mainForm);
                    }
                }
            }
        }
        /// <summary>
        /// 调用线程中创建的form
        /// </summary>
        /// <param name="frm"></param>
        private delegate void SetFormVisableDelegate(Form frm);
        static private void ThreadFormVisable(Form frm)
        {
            if (frm != null)
            {
                if (frm is VGForm)
                {
                    // display the form and bring to foreground.
                    frm.WindowState = FormWindowState.Normal;
                    frm.Focus();
                }
                else
                {
                    WinProgs wp = (WinProgs)frm;
                    wp.form.mAllowVisible = true;
                    wp.ShowMainWindow();
                    SetForegroundWindow(wp.form.Handle);
                }
            }
            SetForegroundWindow(frm.Handle);
        }
    }
}
