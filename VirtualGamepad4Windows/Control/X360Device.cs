using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VirtualGamepad4Windows
{
    public partial class X360Device : ScpDevice
    {
        public X360Device()
        {
            InitializeComponent();
        }

        public X360Device(IContainer container)
        {
            container.Add(this);

            InitializeComponent();
        }
    }
}
