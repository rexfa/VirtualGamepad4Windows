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
        private const String DS3_BUS_CLASS_GUID = "{F679F562-3164-42CE-A4DB-E7DDBE723909}";
        private const int CONTROLLER_OFFSET = 1; // Device 0 is the virtual USB hub itself, and we leave devices 1-10 available for other software (like the Scarlet.Crush DualShock driver itself)

        private int firstController = 1;
        public int FirstController
        {
            get { return firstController; }
            set { firstController = value > 0 ? value : 1; }
        }

        protected Int32 Scale(Int32 Value, Boolean Flip)
        {
            Value -= 0x80;

            if (Value == -128) Value = -127;
            if (Flip) Value *= -1;

            return (Int32)((float)Value * 258.00787401574803149606299212599f);
        }
        public X360Device() : base(DS3_BUS_CLASS_GUID)
        {
            InitializeComponent();
        }

        public X360Device(IContainer container) : base(DS3_BUS_CLASS_GUID)
        {
            container.Add(this);

            InitializeComponent();
        }
    }
}
