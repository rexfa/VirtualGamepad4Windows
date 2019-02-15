using System;
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
    public partial class WinProgs : Form
    {
        public VGForm form;
        public WinProgs(string[] oc, VGForm main)
        {
            form = main;
            InitializeComponent();
        }
        public void ShowMainWindow()
        {
            form.Show();
            form.WindowState = FormWindowState.Normal;
            form.Focus();
        }
    }
}
