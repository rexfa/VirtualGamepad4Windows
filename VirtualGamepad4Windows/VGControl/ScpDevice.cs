using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;

using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using System.Drawing;

namespace VirtualGamepad4Windows
{
    public partial class ScpDevice : Component
    {
        public virtual Boolean IsActive
        {
            get { return m_IsActive; }
        }

        public virtual String Path
        {
            get { return m_Path; }
        }


        public ScpDevice()
        {
            InitializeComponent();
        }

        public ScpDevice(IContainer container)
        {
            container.Add(this);

            InitializeComponent();
        }

        public ScpDevice(String Class)
        {
            InitializeComponent();

            this.m_Class = new Guid(Class);
        }


        public virtual Boolean Open(Int32 Instance = 0)
        {
            String DevicePath = String.Empty;
            m_WinUsbHandle = (IntPtr)INVALID_HANDLE_VALUE;

            if (Find(m_Class, ref DevicePath, Instance))
            {
                Open(DevicePath);
            }

            return m_IsActive;
        }

        public virtual Boolean Open(String DevicePath)
        {
            m_Path = DevicePath.ToUpper();
            m_WinUsbHandle = (IntPtr)INVALID_HANDLE_VALUE;

            if (GetDeviceHandle(m_Path))
            {
                if (WinUsb_Initialize(m_FileHandle, ref m_WinUsbHandle))
                {
                    if (InitializeDevice())
                    {
                        m_IsActive = true;
                    }
                    else
                    {
                        WinUsb_Free(m_WinUsbHandle);
                        m_WinUsbHandle = (IntPtr)INVALID_HANDLE_VALUE;
                    }
                }
                else
                {
                    m_FileHandle.Close();
                }
            }

            return m_IsActive;
        }

        public virtual Boolean Start()
        {
            return m_IsActive;
        }

        public virtual Boolean Stop()
        {
            m_IsActive = false;

            if (!(m_WinUsbHandle == (IntPtr)INVALID_HANDLE_VALUE))
            {
                WinUsb_AbortPipe(m_WinUsbHandle, m_IntIn);
                WinUsb_AbortPipe(m_WinUsbHandle, m_BulkIn);
                WinUsb_AbortPipe(m_WinUsbHandle, m_BulkOut);

                WinUsb_Free(m_WinUsbHandle);
                m_WinUsbHandle = (IntPtr)INVALID_HANDLE_VALUE;
            }

            if (m_FileHandle != null && !m_FileHandle.IsInvalid && !m_FileHandle.IsClosed)
            {
                m_FileHandle.Close();
                m_FileHandle = null;
            }

            return true;
        }

        public virtual Boolean Close()
        {
            return Stop();
        }


        public virtual Boolean ReadIntPipe(Byte[] Buffer, Int32 Length, ref Int32 Transfered)
        {
            if (!m_IsActive) return false;

            return WinUsb_ReadPipe(m_WinUsbHandle, m_IntIn, Buffer, Length, ref Transfered, IntPtr.Zero);
        }

        public virtual Boolean ReadBulkPipe(Byte[] Buffer, Int32 Length, ref Int32 Transfered)
        {
            if (!m_IsActive) return false;

            return WinUsb_ReadPipe(m_WinUsbHandle, m_BulkIn, Buffer, Length, ref Transfered, IntPtr.Zero);
        }

        public virtual Boolean WriteIntPipe(Byte[] Buffer, Int32 Length, ref Int32 Transfered)
        {
            if (!m_IsActive) return false;

            return WinUsb_WritePipe(m_WinUsbHandle, m_IntOut, Buffer, Length, ref Transfered, IntPtr.Zero);
        }

        public virtual Boolean WriteBulkPipe(Byte[] Buffer, Int32 Length, ref Int32 Transfered)
        {
            if (!m_IsActive) return false;

            return WinUsb_WritePipe(m_WinUsbHandle, m_BulkOut, Buffer, Length, ref Transfered, IntPtr.Zero);
        }


        public virtual Boolean SendTransfer(Byte RequestType, Byte Request, UInt16 Value, Byte[] Buffer, ref Int32 Transfered)
        {
            if (!m_IsActive) return false;

            WINUSB_SETUP_PACKET Setup = new WINUSB_SETUP_PACKET();

            Setup.RequestType = RequestType;
            Setup.Request = Request;
            Setup.Value = Value;
            Setup.Index = 0;
            Setup.Length = (UInt16)Buffer.Length;

            return WinUsb_ControlTransfer(m_WinUsbHandle, Setup, Buffer, Buffer.Length, ref Transfered, IntPtr.Zero);
        }


        #region Constant and Structure Definitions
        public const Int32 SERVICE_CONTROL_STOP = 0x00000001;
        public const Int32 SERVICE_CONTROL_SHUTDOWN = 0x00000005;
        public const Int32 SERVICE_CONTROL_DEVICEEVENT = 0x0000000B;
        public const Int32 SERVICE_CONTROL_POWEREVENT = 0x0000000D;

        public const Int32 DBT_DEVICEARRIVAL = 0x8000;
        public const Int32 DBT_DEVICEQUERYREMOVE = 0x8001;
        public const Int32 DBT_DEVICEREMOVECOMPLETE = 0x8004;
        public const Int32 DBT_DEVTYP_DEVICEINTERFACE = 0x0005;
        public const Int32 DBT_DEVTYP_HANDLE = 0x0006;

        public const Int32 PBT_APMRESUMEAUTOMATIC = 0x0012;
        public const Int32 PBT_APMSUSPEND = 0x0004;

        public const Int32 DEVICE_NOTIFY_WINDOW_HANDLE = 0x0000;
        public const Int32 DEVICE_NOTIFY_SERVICE_HANDLE = 0x0001;
        public const Int32 DEVICE_NOTIFY_ALL_INTERFACE_CLASSES = 0x0004;

        public const Int32 WM_DEVICECHANGE = 0x0219;

        public const Int32 DIGCF_PRESENT = 0x0002;
        public const Int32 DIGCF_DEVICEINTERFACE = 0x0010;

        public delegate Int32 ServiceControlHandlerEx(Int32 Control, Int32 Type, IntPtr Data, IntPtr Context);

        [StructLayout(LayoutKind.Sequential)]
        public class DEV_BROADCAST_DEVICEINTERFACE
        {
            internal Int32 dbcc_size;
            internal Int32 dbcc_devicetype;
            internal Int32 dbcc_reserved;
            internal Guid dbcc_classguid;
            internal Int16 dbcc_name;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public class DEV_BROADCAST_DEVICEINTERFACE_M
        {
            public Int32 dbcc_size;
            public Int32 dbcc_devicetype;
            public Int32 dbcc_reserved;

            [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 16)]
            public Byte[] dbcc_classguid;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 255)]
            public Char[] dbcc_name;
        }

        [StructLayout(LayoutKind.Sequential)]
        public class DEV_BROADCAST_HDR
        {
            public Int32 dbch_size;
            public Int32 dbch_devicetype;
            public Int32 dbch_reserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        protected struct SP_DEVICE_INTERFACE_DATA
        {
            internal Int32 cbSize;
            internal Guid InterfaceClassGuid;
            internal Int32 Flags;
            internal IntPtr Reserved;
        }

        protected const UInt32 FILE_ATTRIBUTE_NORMAL = 0x80;
        protected const UInt32 FILE_FLAG_OVERLAPPED = 0x40000000;
        protected const UInt32 FILE_SHARE_READ = 1;
        protected const UInt32 FILE_SHARE_WRITE = 2;
        protected const UInt32 GENERIC_READ = 0x80000000;
        protected const UInt32 GENERIC_WRITE = 0x40000000;
        protected const Int32 INVALID_HANDLE_VALUE = -1;
        protected const UInt32 OPEN_EXISTING = 3;
        protected const UInt32 DEVICE_SPEED = 1;
        protected const Byte USB_ENDPOINT_DIRECTION_MASK = 0x80;

        protected enum POLICY_TYPE
        {
            SHORT_PACKET_TERMINATE = 1,
            AUTO_CLEAR_STALL = 2,
            PIPE_TRANSFER_TIMEOUT = 3,
            IGNORE_SHORT_PACKETS = 4,
            ALLOW_PARTIAL_READS = 5,
            AUTO_FLUSH = 6,
            RAW_IO = 7,
        }

        protected enum USBD_PIPE_TYPE
        {
            UsbdPipeTypeControl = 0,
            UsbdPipeTypeIsochronous = 1,
            UsbdPipeTypeBulk = 2,
            UsbdPipeTypeInterrupt = 3,
        }

        protected enum USB_DEVICE_SPEED
        {
            UsbLowSpeed = 1,
            UsbFullSpeed = 2,
            UsbHighSpeed = 3,
        }

        [StructLayout(LayoutKind.Sequential)]
        protected struct USB_CONFIGURATION_DESCRIPTOR
        {
            internal Byte bLength;
            internal Byte bDescriptorType;
            internal UInt16 wTotalLength;
            internal Byte bNumInterfaces;
            internal Byte bConfigurationValue;
            internal Byte iConfiguration;
            internal Byte bmAttributes;
            internal Byte MaxPower;
        }

        [StructLayout(LayoutKind.Sequential)]
        protected struct USB_INTERFACE_DESCRIPTOR
        {
            internal Byte bLength;
            internal Byte bDescriptorType;
            internal Byte bInterfaceNumber;
            internal Byte bAlternateSetting;
            internal Byte bNumEndpoints;
            internal Byte bInterfaceClass;
            internal Byte bInterfaceSubClass;
            internal Byte bInterfaceProtocol;
            internal Byte iInterface;
        }

        [StructLayout(LayoutKind.Sequential)]
        protected struct WINUSB_PIPE_INFORMATION
        {
            internal USBD_PIPE_TYPE PipeType;
            internal Byte PipeId;
            internal UInt16 MaximumPacketSize;
            internal Byte Interval;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        protected struct WINUSB_SETUP_PACKET
        {
            internal Byte RequestType;
            internal Byte Request;
            internal UInt16 Value;
            internal UInt16 Index;
            internal UInt16 Length;
        }

        protected const Int32 DIF_PROPERTYCHANGE = 0x12;
        protected const Int32 DICS_ENABLE = 1;
        protected const Int32 DICS_DISABLE = 2;
        protected const Int32 DICS_PROPCHANGE = 3;
        protected const Int32 DICS_FLAG_GLOBAL = 1;

        [StructLayout(LayoutKind.Sequential)]
        protected struct SP_CLASSINSTALL_HEADER
        {
            internal Int32 cbSize;
            internal Int32 InstallFunction;
        }

        [StructLayout(LayoutKind.Sequential)]
        protected struct SP_PROPCHANGE_PARAMS
        {
            internal SP_CLASSINSTALL_HEADER ClassInstallHeader;
            internal Int32 StateChange;
            internal Int32 Scope;
            internal Int32 HwProfile;
        }
        #endregion

        #region Protected Data Members
        protected Guid m_Class = Guid.Empty;
        protected String m_Path = String.Empty;

        protected SafeFileHandle m_FileHandle = null;
        protected IntPtr m_WinUsbHandle = IntPtr.Zero;

        protected Byte m_IntIn = 0xFF;
        protected Byte m_IntOut = 0xFF;
        protected Byte m_BulkIn = 0xFF;
        protected Byte m_BulkOut = 0xFF;

        protected Boolean m_IsActive = false;
        #endregion

        #region Static Helper Methods
        public enum Notified { Ignore = 0x0000, Arrival = 0x8000, QueryRemove = 0x8001, Removal = 0x8004 };

        public static Boolean RegisterNotify(IntPtr Form, Guid Class, ref IntPtr Handle, Boolean Window = true)
        {
            IntPtr devBroadcastDeviceInterfaceBuffer = IntPtr.Zero;

            try
            {
                DEV_BROADCAST_DEVICEINTERFACE devBroadcastDeviceInterface = new DEV_BROADCAST_DEVICEINTERFACE();
                Int32 Size = Marshal.SizeOf(devBroadcastDeviceInterface);

                devBroadcastDeviceInterface.dbcc_size = Size;
                devBroadcastDeviceInterface.dbcc_devicetype = DBT_DEVTYP_DEVICEINTERFACE;
                devBroadcastDeviceInterface.dbcc_reserved = 0;
                devBroadcastDeviceInterface.dbcc_classguid = Class;

                devBroadcastDeviceInterfaceBuffer = Marshal.AllocHGlobal(Size);
                Marshal.StructureToPtr(devBroadcastDeviceInterface, devBroadcastDeviceInterfaceBuffer, true);

                Handle = RegisterDeviceNotification(Form, devBroadcastDeviceInterfaceBuffer, Window ? DEVICE_NOTIFY_WINDOW_HANDLE : DEVICE_NOTIFY_SERVICE_HANDLE);

                Marshal.PtrToStructure(devBroadcastDeviceInterfaceBuffer, devBroadcastDeviceInterface);

                return Handle != IntPtr.Zero;
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} {1}", ex.HelpLink, ex.Message);
                throw;
            }
            finally
            {
                if (devBroadcastDeviceInterfaceBuffer != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(devBroadcastDeviceInterfaceBuffer);
                }
            }
        }

        public static Boolean UnregisterNotify(IntPtr Handle)
        {
            try
            {
                return UnregisterDeviceNotification(Handle);
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} {1}", ex.HelpLink, ex.Message);
                throw;
            }
        }
        #endregion

        #region Protected Methods
        protected virtual Boolean Find(Guid Target, ref String Path, Int32 Instance = 0)
        {
            IntPtr detailDataBuffer = IntPtr.Zero;
            IntPtr deviceInfoSet = IntPtr.Zero;

            try
            {
                SP_DEVICE_INTERFACE_DATA DeviceInterfaceData = new SP_DEVICE_INTERFACE_DATA(), da = new SP_DEVICE_INTERFACE_DATA();
                Int32 bufferSize = 0, memberIndex = 0;

                deviceInfoSet = SetupDiGetClassDevs(ref Target, IntPtr.Zero, IntPtr.Zero, DIGCF_PRESENT | DIGCF_DEVICEINTERFACE);

                DeviceInterfaceData.cbSize = da.cbSize = Marshal.SizeOf(DeviceInterfaceData);

                while (SetupDiEnumDeviceInterfaces(deviceInfoSet, IntPtr.Zero, ref Target, memberIndex, ref DeviceInterfaceData))
                {
                    SetupDiGetDeviceInterfaceDetail(deviceInfoSet, ref DeviceInterfaceData, IntPtr.Zero, 0, ref bufferSize, ref da);
                    {
                        detailDataBuffer = Marshal.AllocHGlobal(bufferSize);

                        Marshal.WriteInt32(detailDataBuffer, (IntPtr.Size == 4) ? (4 + Marshal.SystemDefaultCharSize) : 8);

                        if (SetupDiGetDeviceInterfaceDetail(deviceInfoSet, ref DeviceInterfaceData, detailDataBuffer, bufferSize, ref bufferSize, ref da))
                        {
                            IntPtr pDevicePathName = new IntPtr(IntPtr.Size == 4 ? detailDataBuffer.ToInt32() + 4 : detailDataBuffer.ToInt64() + 4);

                            Path = Marshal.PtrToStringAuto(pDevicePathName).ToUpper();
                            Marshal.FreeHGlobal(detailDataBuffer);

                            if (memberIndex == Instance) return true;
                        }
                        else Marshal.FreeHGlobal(detailDataBuffer);
                    }

                    memberIndex++;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} {1}", ex.HelpLink, ex.Message);
                throw;
            }
            finally
            {
                if (deviceInfoSet != IntPtr.Zero)
                {
                    SetupDiDestroyDeviceInfoList(deviceInfoSet);
                }
            }

            return false;
        }

        protected virtual Boolean GetDeviceInstance(ref String Instance)
        {
            IntPtr detailDataBuffer = IntPtr.Zero;
            IntPtr deviceInfoSet = IntPtr.Zero;

            try
            {
                SP_DEVICE_INTERFACE_DATA DeviceInterfaceData = new SP_DEVICE_INTERFACE_DATA(), da = new SP_DEVICE_INTERFACE_DATA();
                Int32 bufferSize = 0, memberIndex = 0;

                deviceInfoSet = SetupDiGetClassDevs(ref m_Class, IntPtr.Zero, IntPtr.Zero, DIGCF_PRESENT | DIGCF_DEVICEINTERFACE);

                DeviceInterfaceData.cbSize = da.cbSize = Marshal.SizeOf(DeviceInterfaceData);

                while (SetupDiEnumDeviceInterfaces(deviceInfoSet, IntPtr.Zero, ref m_Class, memberIndex, ref DeviceInterfaceData))
                {
                    SetupDiGetDeviceInterfaceDetail(deviceInfoSet, ref DeviceInterfaceData, IntPtr.Zero, 0, ref bufferSize, ref da);
                    {
                        detailDataBuffer = Marshal.AllocHGlobal(bufferSize);

                        Marshal.WriteInt32(detailDataBuffer, (IntPtr.Size == 4) ? (4 + Marshal.SystemDefaultCharSize) : 8);

                        if (SetupDiGetDeviceInterfaceDetail(deviceInfoSet, ref DeviceInterfaceData, detailDataBuffer, bufferSize, ref bufferSize, ref da))
                        {
                            IntPtr pDevicePathName = new IntPtr(IntPtr.Size == 4 ? detailDataBuffer.ToInt32() + 4 : detailDataBuffer.ToInt64() + 4);

                            String Current = Marshal.PtrToStringAuto(pDevicePathName).ToUpper();
                            Marshal.FreeHGlobal(detailDataBuffer);

                            if (Current == Path)
                            {
                                Int32 nBytes = 256;
                                IntPtr ptrInstanceBuf = Marshal.AllocHGlobal(nBytes);

                                CM_Get_Device_ID(da.Flags, ptrInstanceBuf, nBytes, 0);
                                Instance = Marshal.PtrToStringAuto(ptrInstanceBuf).ToUpper();

                                Marshal.FreeHGlobal(ptrInstanceBuf);
                                return true;
                            }
                        }
                        else Marshal.FreeHGlobal(detailDataBuffer);
                    }

                    memberIndex++;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} {1}", ex.HelpLink, ex.Message);
                throw;
            }
            finally
            {
                if (deviceInfoSet != IntPtr.Zero)
                {
                    SetupDiDestroyDeviceInfoList(deviceInfoSet);
                }
            }

            return false;
        }

        protected virtual Boolean GetDeviceHandle(String Path)
        {
            m_FileHandle = CreateFile(Path, (GENERIC_WRITE | GENERIC_READ), FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL | FILE_FLAG_OVERLAPPED, 0);

            return !m_FileHandle.IsInvalid;
        }

        protected virtual Boolean UsbEndpointDirectionIn(Int32 addr)
        {
            return (addr & 0x80) == 0x80;
        }

        protected virtual Boolean UsbEndpointDirectionOut(Int32 addr)
        {
            return (addr & 0x80) == 0x00;
        }

        protected virtual Boolean InitializeDevice()
        {
            try
            {
                USB_INTERFACE_DESCRIPTOR ifaceDescriptor = new USB_INTERFACE_DESCRIPTOR();
                WINUSB_PIPE_INFORMATION pipeInfo = new WINUSB_PIPE_INFORMATION();

                if (WinUsb_QueryInterfaceSettings(m_WinUsbHandle, 0, ref ifaceDescriptor))
                {
                    for (Int32 i = 0; i < ifaceDescriptor.bNumEndpoints; i++)
                    {
                        WinUsb_QueryPipe(m_WinUsbHandle, 0, System.Convert.ToByte(i), ref pipeInfo);

                        if (((pipeInfo.PipeType == USBD_PIPE_TYPE.UsbdPipeTypeBulk) & UsbEndpointDirectionIn(pipeInfo.PipeId)))
                        {
                            m_BulkIn = pipeInfo.PipeId;
                            WinUsb_FlushPipe(m_WinUsbHandle, m_BulkIn);
                        }
                        else if (((pipeInfo.PipeType == USBD_PIPE_TYPE.UsbdPipeTypeBulk) & UsbEndpointDirectionOut(pipeInfo.PipeId)))
                        {
                            m_BulkOut = pipeInfo.PipeId;
                            WinUsb_FlushPipe(m_WinUsbHandle, m_BulkOut);
                        }
                        else if ((pipeInfo.PipeType == USBD_PIPE_TYPE.UsbdPipeTypeInterrupt) & UsbEndpointDirectionIn(pipeInfo.PipeId))
                        {
                            m_IntIn = pipeInfo.PipeId;
                            WinUsb_FlushPipe(m_WinUsbHandle, m_IntIn);
                        }
                        else if ((pipeInfo.PipeType == USBD_PIPE_TYPE.UsbdPipeTypeInterrupt) & UsbEndpointDirectionOut(pipeInfo.PipeId))
                        {
                            m_IntOut = pipeInfo.PipeId;
                            WinUsb_FlushPipe(m_WinUsbHandle, m_IntOut);
                        }
                    }

                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} {1}", ex.HelpLink, ex.Message);
                throw;
            }
        }

        protected virtual Boolean RestartDevice(String InstanceId)
        {
            IntPtr deviceInfoSet = IntPtr.Zero;

            try
            {
                SP_DEVICE_INTERFACE_DATA deviceInterfaceData = new SP_DEVICE_INTERFACE_DATA();

                deviceInterfaceData.cbSize = Marshal.SizeOf(deviceInterfaceData);
                deviceInfoSet = SetupDiGetClassDevs(ref m_Class, IntPtr.Zero, IntPtr.Zero, DIGCF_PRESENT | DIGCF_DEVICEINTERFACE);

                if (SetupDiOpenDeviceInfo(deviceInfoSet, InstanceId, IntPtr.Zero, 0, ref deviceInterfaceData))
                {
                    SP_PROPCHANGE_PARAMS props = new SP_PROPCHANGE_PARAMS();

                    props.ClassInstallHeader = new SP_CLASSINSTALL_HEADER();
                    props.ClassInstallHeader.cbSize = Marshal.SizeOf(props.ClassInstallHeader);
                    props.ClassInstallHeader.InstallFunction = DIF_PROPERTYCHANGE;

                    props.Scope = DICS_FLAG_GLOBAL;
                    props.StateChange = DICS_PROPCHANGE;
                    props.HwProfile = 0x00;

                    if (SetupDiSetClassInstallParams(deviceInfoSet, ref deviceInterfaceData, ref props, Marshal.SizeOf(props)))
                    {
                        return SetupDiChangeState(deviceInfoSet, ref deviceInterfaceData);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} {1}", ex.HelpLink, ex.Message);
                throw;
            }
            finally
            {
                if (deviceInfoSet != IntPtr.Zero)
                {
                    SetupDiDestroyDeviceInfoList(deviceInfoSet);
                }
            }

            return false;
        }
        #endregion

        #region Interop Definitions
        [DllImport("setupapi.dll", SetLastError = true)]
        protected static extern Int32 SetupDiCreateDeviceInfoList(ref System.Guid ClassGuid, Int32 hwndParent);

        [DllImport("setupapi.dll", SetLastError = true)]
        protected static extern Int32 SetupDiDestroyDeviceInfoList(IntPtr DeviceInfoSet);

        [DllImport("setupapi.dll", SetLastError = true)]
        protected static extern Boolean SetupDiEnumDeviceInterfaces(IntPtr DeviceInfoSet, IntPtr DeviceInfoData, ref System.Guid InterfaceClassGuid, Int32 MemberIndex, ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        protected static extern IntPtr SetupDiGetClassDevs(ref System.Guid ClassGuid, IntPtr Enumerator, IntPtr hwndParent, Int32 Flags);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        protected static extern Boolean SetupDiGetDeviceInterfaceDetail(IntPtr DeviceInfoSet, ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData, IntPtr DeviceInterfaceDetailData, Int32 DeviceInterfaceDetailDataSize, ref Int32 RequiredSize, IntPtr DeviceInfoData);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        protected static extern Boolean SetupDiGetDeviceInterfaceDetail(IntPtr DeviceInfoSet, ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData, IntPtr DeviceInterfaceDetailData, Int32 DeviceInterfaceDetailDataSize, ref Int32 RequiredSize, ref SP_DEVICE_INTERFACE_DATA DeviceInfoData);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        protected static extern IntPtr RegisterDeviceNotification(IntPtr hRecipient, IntPtr NotificationFilter, Int32 Flags);

        [DllImport("user32.dll", SetLastError = true)]
        protected static extern Boolean UnregisterDeviceNotification(IntPtr Handle);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        protected static extern SafeFileHandle CreateFile(String lpFileName, UInt32 dwDesiredAccess, UInt32 dwShareMode, IntPtr lpSecurityAttributes, UInt32 dwCreationDisposition, UInt32 dwFlagsAndAttributes, UInt32 hTemplateFile);

        [DllImport("winusb.dll", SetLastError = true)]
        protected static extern Boolean WinUsb_Initialize(SafeFileHandle DeviceHandle, ref IntPtr InterfaceHandle);

        [DllImport("winusb.dll", SetLastError = true)]
        protected static extern Boolean WinUsb_QueryInterfaceSettings(IntPtr InterfaceHandle, Byte AlternateInterfaceNumber, ref USB_INTERFACE_DESCRIPTOR UsbAltInterfaceDescriptor);

        [DllImport("winusb.dll", SetLastError = true)]
        protected static extern Boolean WinUsb_QueryPipe(IntPtr InterfaceHandle, Byte AlternateInterfaceNumber, Byte PipeIndex, ref WINUSB_PIPE_INFORMATION PipeInformation);

        [DllImport("winusb.dll", SetLastError = true)]
        protected static extern Boolean WinUsb_AbortPipe(IntPtr InterfaceHandle, Byte PipeID);

        [DllImport("winusb.dll", SetLastError = true)]
        protected static extern Boolean WinUsb_FlushPipe(IntPtr InterfaceHandle, Byte PipeID);

        [DllImport("winusb.dll", SetLastError = true)]
        protected static extern Boolean WinUsb_ControlTransfer(IntPtr InterfaceHandle, WINUSB_SETUP_PACKET SetupPacket, Byte[] Buffer, Int32 BufferLength, ref Int32 LengthTransferred, IntPtr Overlapped);

        [DllImport("winusb.dll", SetLastError = true)]
        protected static extern Boolean WinUsb_ReadPipe(IntPtr InterfaceHandle, Byte PipeID, Byte[] Buffer, Int32 BufferLength, ref Int32 LengthTransferred, IntPtr Overlapped);

        [DllImport("winusb.dll", SetLastError = true)]
        protected static extern Boolean WinUsb_WritePipe(IntPtr InterfaceHandle, Byte PipeID, Byte[] Buffer, Int32 BufferLength, ref Int32 LengthTransferred, IntPtr Overlapped);

        [DllImport("winusb.dll", SetLastError = true)]
        protected static extern Boolean WinUsb_Free(IntPtr InterfaceHandle);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern IntPtr RegisterServiceCtrlHandlerEx(String ServiceName, ServiceControlHandlerEx Callback, IntPtr Context);

        [DllImport("kernel32.dll", SetLastError = true)]
        protected static extern Boolean DeviceIoControl(SafeFileHandle DeviceHandle, Int32 IoControlCode, Byte[] InBuffer, Int32 InBufferSize, Byte[] OutBuffer, Int32 OutBufferSize, ref Int32 BytesReturned, IntPtr Overlapped);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        protected static extern Int32 CM_Get_Device_ID(Int32 dnDevInst, IntPtr Buffer, Int32 BufferLen, Int32 ulFlags);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        protected static extern Boolean SetupDiOpenDeviceInfo(IntPtr DeviceInfoSet, String DeviceInstanceId, IntPtr hwndParent, Int32 Flags, ref SP_DEVICE_INTERFACE_DATA DeviceInfoData);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        protected static extern Boolean SetupDiChangeState(IntPtr DeviceInfoSet, ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        protected static extern Boolean SetupDiSetClassInstallParams(IntPtr DeviceInfoSet, ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData, ref SP_PROPCHANGE_PARAMS ClassInstallParams, Int32 ClassInstallParamsSize);
        #endregion
    }

    /*
    public class Global
    {
        protected static BackingStore m_Config = new BackingStore();
        protected static Int32 m_IdleTimeout = 600000;
        static string exepath = Directory.GetParent(Assembly.GetExecutingAssembly().Location).FullName;
        public static string appdatapath;
        public static string[] tempprofilename = new string[5] { string.Empty, string.Empty, string.Empty, string.Empty, string.Empty };

        public static void SaveWhere(string path)
        {
            appdatapath = path;
            m_Config.m_Profile = appdatapath + "\\Profiles.xml";
            m_Config.m_Actions = appdatapath + "\\Actions.xml";
        }
        /// <summary>
        /// Check if Admin Rights are needed to write in Appliplation Directory
        /// </summary>
        /// <returns></returns>
        public static bool AdminNeeded()
        {
            try
            {
                File.WriteAllText(exepath + "\\test.txt", "test");
                File.Delete(exepath + "\\test.txt");
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return true;
            }
        }

        public static bool IsAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        public static event EventHandler<EventArgs> ControllerStatusChange; // called when a controller is added/removed/battery or touchpad mode changes/etc.
        public static void ControllerStatusChanged(object sender)
        {
            if (ControllerStatusChange != null)
                ControllerStatusChange(sender, EventArgs.Empty);
        }

        //general values
        public static bool UseExclusiveMode
        {
            set { m_Config.useExclusiveMode = value; }
            get { return m_Config.useExclusiveMode; }
        }

        public static DateTime LastChecked
        {
            set { m_Config.lastChecked = value; }
            get { return m_Config.lastChecked; }
        }

        public static int CheckWhen
        {
            set { m_Config.CheckWhen = value; }
            get { return m_Config.CheckWhen; }
        }
        public static int Notifications
        {
            set { m_Config.notifications = value; }
            get { return m_Config.notifications; }
        }
        public static bool DCBTatStop
        {
            set { m_Config.disconnectBTAtStop = value; }
            get { return m_Config.disconnectBTAtStop; }
        }
        public static bool SwipeProfiles
        {
            set { m_Config.swipeProfiles = value; }
            get { return m_Config.swipeProfiles; }
        }
        public static bool DS4Mapping
        {
            set { m_Config.ds4Mapping = value; }
            get { return m_Config.ds4Mapping; }
        }
        public static bool QuickCharge
        {
            set { m_Config.quickCharge = value; }
            get { return m_Config.quickCharge; }
        }
        public static int FirstXinputPort
        {
            set { m_Config.firstXinputPort = value; }
            get { return m_Config.firstXinputPort; }
        }
        public static bool CloseMini
        {
            set { m_Config.closeMini = value; }
            get { return m_Config.closeMini; }
        }
        public static bool StartMinimized
        {
            set { m_Config.startMinimized = value; }
            get { return m_Config.startMinimized; }
        }
        public static int FormWidth
        {
            set { m_Config.formWidth = value; }
            get { return m_Config.formWidth; }
        }
        public static int FormHeight
        {
            set { m_Config.formHeight = value; }
            get { return m_Config.formHeight; }
        }
        public static bool DownloadLang
        {
            set { m_Config.downloadLang = value; }
            get { return m_Config.downloadLang; }
        }
        public static bool FlashWhenLate
        {
            set { m_Config.flashWhenLate = value; }
            get { return m_Config.flashWhenLate; }
        }
        public static int FlashWhenLateAt
        {
            set { m_Config.flashWhenLateAt = value; }
            get { return m_Config.flashWhenLateAt; }
        }
        public static bool UseWhiteIcon
        {
            set { m_Config.useWhiteIcon = value; }
            get { return m_Config.useWhiteIcon; }
        }

        //controller/profile specfic values
        public static int[] ButtonMouseSensitivity => m_Config.buttonMouseSensitivity;
        public static byte[] RumbleBoost => m_Config.rumble;
        public static double[] Rainbow => m_Config.rainbow;
        public static bool[] FlushHIDQueue => m_Config.flushHIDQueue;
        public static int[] IdleDisconnectTimeout => m_Config.idleDisconnectTimeout;
        public static byte[] TouchSensitivity => m_Config.touchSensitivity;
        public static byte[] FlashType => m_Config.flashType;
        public static int[] FlashAt => m_Config.flashAt;
        public static bool[] LedAsBatteryIndicator => m_Config.ledAsBattery;
        public static int[] ChargingType => m_Config.chargingType;
        public static bool[] DinputOnly => m_Config.dinputOnly;
        public static bool[] StartTouchpadOff => m_Config.startTouchpadOff;
        public static bool[] UseTPforControls => m_Config.useTPforControls;
        public static bool[] UseSAforMouse => m_Config.useSAforMouse;
        public static string[] SATriggers => m_Config.sATriggers;
        public static int[] GyroSensitivity => m_Config.gyroSensitivity;
        public static int[] GyroInvert => m_Config.gyroInvert;
        public static DS4Color[] MainColor => m_Config.m_Leds;
        public static DS4Color[] LowColor => m_Config.m_LowLeds;
        public static DS4Color[] ChargingColor => m_Config.m_ChargingLeds;
        public static DS4Color[] CustomColor => m_Config.m_CustomLeds;
        public static bool[] UseCustomLed => m_Config.useCustomLeds;

        public static DS4Color[] FlashColor => m_Config.m_FlashLeds;
        public static byte[] TapSensitivity => m_Config.tapSensitivity;
        public static bool[] DoubleTap => m_Config.doubleTap;
        public static int[] ScrollSensitivity => m_Config.scrollSensitivity;
        public static bool[] LowerRCOn => m_Config.lowerRCOn;
        public static bool[] TouchpadJitterCompensation => m_Config.touchpadJitterCompensation;

        public static byte[] L2Deadzone => m_Config.l2Deadzone;
        public static byte[] R2Deadzone => m_Config.r2Deadzone;
        public static double[] SXDeadzone => m_Config.SXDeadzone;
        public static double[] SZDeadzone => m_Config.SZDeadzone;
        public static int[] LSDeadzone => m_Config.LSDeadzone;
        public static int[] RSDeadzone => m_Config.RSDeadzone;
        public static int[] LSCurve => m_Config.lsCurve;
        public static int[] RSCurve => m_Config.rsCurve;
        public static double[] L2Sens => m_Config.l2Sens;
        public static double[] R2Sens => m_Config.r2Sens;
        public static double[] SXSens => m_Config.SXSens;
        public static double[] SZSens => m_Config.SZSens;
        public static double[] LSSens => m_Config.LSSens;
        public static double[] RSSens => m_Config.RSSens;
        public static bool[] MouseAccel => m_Config.mouseAccel;
        public static string[] LaunchProgram => m_Config.launchProgram;
        public static string[] ProfilePath => m_Config.profilePath;
        public static List<string>[] ProfileActions => m_Config.profileActions;

        public static void UpdateDS4CSetting(int deviceNum, string buttonName, bool shift, object action, string exts, DS4KeyType kt, int trigger = 0)
        {
            m_Config.UpdateDS4CSetting(deviceNum, buttonName, shift, action, exts, kt, trigger);
        }
        public static void UpdateDS4Extra(int deviceNum, string buttonName, bool shift, string exts)
        {
            m_Config.UpdateDS4CExtra(deviceNum, buttonName, shift, exts);
        }

        public static object GetDS4Action(int deviceNum, string buttonName, bool shift) => m_Config.GetDS4Action(deviceNum, buttonName, shift);
        public static DS4KeyType GetDS4KeyType(int deviceNum, string buttonName, bool shift) => m_Config.GetDS4KeyType(deviceNum, buttonName, shift);
        public static string GetDS4Extra(int deviceNum, string buttonName, bool shift) => m_Config.GetDS4Extra(deviceNum, buttonName, shift);
        public static int GetDS4STrigger(int deviceNum, string buttonName) => m_Config.GetDS4STrigger(deviceNum, buttonName);
        public static List<DS4ControlSettings> getDS4CSettings(int device) => m_Config.ds4settings[device];
        public static DS4ControlSettings getDS4CSetting(int deviceNum, string control) => m_Config.getDS4CSetting(deviceNum, control);
        public static bool HasCustomAction(int deviceNum) => m_Config.HasCustomActions(deviceNum);
        public static bool HasCustomExtras(int deviceNum) => m_Config.HasCustomExtras(deviceNum);

        public static void SaveAction(string name, string controls, int mode, string details, bool edit, string extras = "")
        {
            m_Config.SaveAction(name, controls, mode, details, edit, extras);
            Mapping.actionDone.Add(new Mapping.ActionState());
        }

        public static void RemoveAction(string name)
        {
            m_Config.RemoveAction(name);
        }

        public static bool LoadActions() => m_Config.LoadActions();

        public static List<SpecialAction> GetActions() => m_Config.actions;

        public static int GetActionIndexOf(string name)
        {
            for (int i = 0; i < m_Config.actions.Count; i++)
                if (m_Config.actions[i].name == name)
                    return i;
            return -1;
        }

        public static SpecialAction GetAction(string name)
        {
            foreach (SpecialAction sA in m_Config.actions)
                if (sA.name == name)
                    return sA;
            return new SpecialAction("null", "null", "null", "null");
        }
        */

    /*public static X360Controls getCustomButton(int device, DS4Controls controlName) => m_Config.GetCustomButton(device, controlName);

    public static ushort getCustomKey(int device, DS4Controls controlName) => m_Config.GetCustomKey(device, controlName);

    public static string getCustomMacro(int device, DS4Controls controlName) => m_Config.GetCustomMacro(device, controlName);

    public static string getCustomExtras(int device, DS4Controls controlName) => m_Config.GetCustomExtras(device, controlName);

    public static DS4KeyType getCustomKeyType(int device, DS4Controls controlName) => m_Config.GetCustomKeyType(device, controlName);

    public static bool getHasCustomKeysorButtons(int device) => m_Config.customMapButtons[device].Count > 0
            || m_Config.customMapKeys[device].Count > 0;

    public static bool getHasCustomExtras(int device) => m_Config.customMapExtras[device].Count > 0;        
    public static Dictionary<DS4Controls, X360Controls> getCustomButtons(int device) => m_Config.customMapButtons[device];        
    public static Dictionary<DS4Controls, ushort> getCustomKeys(int device) => m_Config.customMapKeys[device];        
    public static Dictionary<DS4Controls, string> getCustomMacros(int device) => m_Config.customMapMacros[device];        
    public static Dictionary<DS4Controls, string> getCustomExtras(int device) => m_Config.customMapExtras[device];
    public static Dictionary<DS4Controls, DS4KeyType> getCustomKeyTypes(int device) => m_Config.customMapKeyTypes[device];        

    public static X360Controls getShiftCustomButton(int device, DS4Controls controlName) => m_Config.GetShiftCustomButton(device, controlName);        
    public static ushort getShiftCustomKey(int device, DS4Controls controlName) => m_Config.GetShiftCustomKey(device, controlName);        
    public static string getShiftCustomMacro(int device, DS4Controls controlName) => m_Config.GetShiftCustomMacro(device, controlName);        
    public static string getShiftCustomExtras(int device, DS4Controls controlName) => m_Config.GetShiftCustomExtras(device, controlName);        
    public static DS4KeyType getShiftCustomKeyType(int device, DS4Controls controlName) => m_Config.GetShiftCustomKeyType(device, controlName);        
    public static bool getHasShiftCustomKeysorButtons(int device) => m_Config.shiftCustomMapButtons[device].Count > 0
            || m_Config.shiftCustomMapKeys[device].Count > 0;        
    public static bool getHasShiftCustomExtras(int device) => m_Config.shiftCustomMapExtras[device].Count > 0;        
    public static Dictionary<DS4Controls, X360Controls> getShiftCustomButtons(int device) => m_Config.shiftCustomMapButtons[device];        
    public static Dictionary<DS4Controls, ushort> getShiftCustomKeys(int device) => m_Config.shiftCustomMapKeys[device];        
    public static Dictionary<DS4Controls, string> getShiftCustomMacros(int device) => m_Config.shiftCustomMapMacros[device];        
    public static Dictionary<DS4Controls, string> getShiftCustomExtras(int device) => m_Config.shiftCustomMapExtras[device];        
    public static Dictionary<DS4Controls, DS4KeyType> getShiftCustomKeyTypes(int device) => m_Config.shiftCustomMapKeyTypes[device]; */

    /*
    public static bool Load() => m_Config.Load();

    public static void LoadProfile(int device, bool launchprogram, ControlService control)
    {
        m_Config.LoadProfile(device, launchprogram, control);
        tempprofilename[device] = string.Empty;
    }

    public static void LoadTempProfile(int device, string name, bool launchprogram, ControlService control)
    {
        m_Config.LoadProfile(device, launchprogram, control, appdatapath + @"\Profiles\" + name + ".xml");
        tempprofilename[device] = name;
    }

    public static bool Save()
    {
        return m_Config.Save();
    }

    public static void SaveProfile(int device, string propath)
    {
        m_Config.SaveProfile(device, propath);
    }

    private static byte applyRatio(byte b1, byte b2, double r)
    {
        if (r > 100)
            r = 100;
        else if (r < 0)
            r = 0;
        r /= 100f;
        return (byte)Math.Round((b1 * (1 - r) + b2 * r), 0);
    }
    public static DS4Color getTransitionedColor(DS4Color c1, DS4Color c2, double ratio)
    {//;
        //Color cs = Color.FromArgb(c1.red, c1.green, c1.blue);
        c1.red = applyRatio(c1.red, c2.red, ratio);
        c1.green = applyRatio(c1.green, c2.green, ratio);
        c1.blue = applyRatio(c1.blue, c2.blue, ratio);
        return c1;
    }

    private static Color applyRatio(Color c1, Color c2, uint r)
    {
        float ratio = r / 100f;
        float hue1 = c1.GetHue();
        float hue2 = c2.GetHue();
        float bri1 = c1.GetBrightness();
        float bri2 = c2.GetBrightness();
        float sat1 = c1.GetSaturation();
        float sat2 = c2.GetSaturation();
        float hr = hue2 - hue1;
        float br = bri2 - bri1;
        float sr = sat2 - sat1;
        Color csR;
        if (bri1 == 0)
            csR = HuetoRGB(hue2, sat2, bri2 - br * ratio);
        else
            csR = HuetoRGB(hue2 - hr * ratio, sat2 - sr * ratio, bri2 - br * ratio);
        return csR;
    }

    public static Color HuetoRGB(float hue, float sat, float bri)
    {
        float C = (1 - Math.Abs(2 * bri) - 1) * sat;
        float X = C * (1 - Math.Abs((hue / 60) % 2 - 1));
        float m = bri - C / 2;
        float R, G, B;
        if (0 <= hue && hue < 60)
        { R = C; G = X; B = 0; }
        else if (60 <= hue && hue < 120)
        { R = X; G = C; B = 0; }
        else if (120 <= hue && hue < 180)
        { R = 0; G = C; B = X; }
        else if (180 <= hue && hue < 240)
        { R = 0; G = X; B = C; }
        else if (240 <= hue && hue < 300)
        { R = X; G = 0; B = C; }
        else if (300 <= hue && hue < 360)
        { R = C; G = 0; B = X; }
        else
        { R = 255; G = 0; B = 0; }
        R += m; G += m; B += m;
        R *= 255; G *= 255; B *= 255;
        return Color.FromArgb((int)R, (int)G, (int)B);
    }


}*/
}
