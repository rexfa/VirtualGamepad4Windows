using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Media;

namespace VirtualGamepad4Windows
{
    public class ControlService
    {
        public X360Device x360Bus;
        public VGDevice[] DS4Controllers = new VGDevice[4];
        public Mouse[] touchPad = new Mouse[4];
        private bool running = false;
        private VGState[] MappedState = new VGState[4];
        private VGState[] CurrentState = new VGState[4];
        private VGState[] PreviousState = new VGState[4];
        public VGStateExposed[] ExposedState = new VGStateExposed[4];
        public bool recordingMacro = false;
        public event EventHandler<DebugEventArgs> Debug = null;
        public bool eastertime = false;
        private int eCode = 0;
        bool[] buttonsdown = { false, false, false, false };
        List<VGControls> dcs = new List<VGControls>();
        bool[] held = new bool[4];
        int[] oldmouse = new int[4] { -1, -1, -1, -1 };
        SoundPlayer sp = new SoundPlayer();

        #region 360 数据
        private class X360Data
        {
            public byte[] Report = new byte[28];
            public byte[] Rumble = new byte[8];
        }
        private X360Data[] processingData = new X360Data[4];
        #endregion

        public ControlService()
        {
            sp.Stream = Properties.Resources.EE;
            x360Bus = new X360Device();
            AddtoVGList();
            for (int i = 0; i < DS4Controllers.Length; i++)
            {
                processingData[i] = new X360Data();
                MappedState[i] = new VGState();
                CurrentState[i] = new VGState();
                PreviousState[i] = new VGState();
                ExposedState[i] = new VGStateExposed(CurrentState[i]);
            }
        }
        void AddtoVGList()
        {
            dcs.Add(VGControls.Cross);
            dcs.Add(VGControls.Cross);
            dcs.Add(VGControls.Circle);
            dcs.Add(VGControls.Square);
            dcs.Add(VGControls.Triangle);
            dcs.Add(VGControls.Options);
            dcs.Add(VGControls.Share);
            dcs.Add(VGControls.DpadUp);
            dcs.Add(VGControls.DpadDown);
            dcs.Add(VGControls.DpadLeft);
            dcs.Add(VGControls.DpadRight);
            dcs.Add(VGControls.PS);
            dcs.Add(VGControls.L1);
            dcs.Add(VGControls.R1);
            dcs.Add(VGControls.L2);
            dcs.Add(VGControls.R2);
            dcs.Add(VGControls.L3);
            dcs.Add(VGControls.R3);
            dcs.Add(VGControls.LXPos);
            dcs.Add(VGControls.LXNeg);
            dcs.Add(VGControls.LYPos);
            dcs.Add(VGControls.LYNeg);
            dcs.Add(VGControls.RXPos);
            dcs.Add(VGControls.RXNeg);
            dcs.Add(VGControls.RYPos);
            dcs.Add(VGControls.RYNeg);
            dcs.Add(VGControls.SwipeUp);
            dcs.Add(VGControls.SwipeDown);
            dcs.Add(VGControls.SwipeLeft);
            dcs.Add(VGControls.SwipeRight);
        }
        public bool Start(bool showlog = true)
        {
            return false;
        }
        public bool HotPlug()
        {
            if (running)
            {
                //findControllers
                //foreach (DS4Device device in devices)
                //{
                //    if (device.IsDisconnecting)
                //    { }
                //}

            }
            return true;
        }
    }
}
