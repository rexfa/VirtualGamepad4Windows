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
        public Mouse[] touchPad = new Mouse[4];
        private bool running = false;
        public ControlService()
        {

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
