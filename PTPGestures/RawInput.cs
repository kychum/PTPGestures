using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace PTPGestures
{
    class RawInputAPI
    {
        public const int RID_INPUT = 0x10000003;
        [StructLayout(LayoutKind.Sequential)]
        public struct RawInputHeader
        {
            public RawInputType Type;
            public int Size;
            public IntPtr Device;
            public IntPtr wParam;
        }

        // Since variable sized arrays don't play well with marshalling, use GetRawHIDDataFromInput to get the byte array.
        [StructLayout(LayoutKind.Sequential)]
        public struct RawInputHID
        {
            public int Size;
            public int Count;
        }

        public static byte[] GetRawHIDDataFromInput(RawInput input){
            byte[] output = new byte[input.HID.Count * input.HID.Size];
            IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(input));
            Marshal.StructureToPtr(input, ptr, false);
            Marshal.Copy((ptr + Marshal.SizeOf(typeof(RawInputHeader)) + Marshal.SizeOf(typeof(RawInputHID))), output, 0, input.HID.Size * input.HID.Count);
            Marshal.FreeHGlobal(ptr);
            return output;
        }
 
        //The MSDN page for GetRawInputBuffer suggests using a field offset of 16+8 for WOW64
        [StructLayout(LayoutKind.Explicit)]
        public struct RawInput
        {
            [FieldOffset(0)]
            public RawInputHeader Header;
            [FieldOffset(16)]        
            public RawMouse Mouse;
            [FieldOffset(16)]
            public RawKeyboard Keyboard;
            [FieldOffset(16)]
            public RawInputHID HID;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RawMouse
        {
            public RawMouseFlags Flags;
            [StructLayout(LayoutKind.Explicit)]
            public struct Data
            {
                [FieldOffset(0)]
                public uint Buttons;
                [FieldOffset(2)]
                public ushort ButtonData;
                [FieldOffset(0)]
                public RawMouseButtons ButtonFlags;
            };
            public Data m_Data;
            public uint RawButtons;
            public int LastX;
            public int LastY;
            public uint ExtraInformation;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RawInputDevice
        {
            public HIDUsagePage UsagePage;
            public HIDUsage Usage;
            public RawInputDeviceFlags Flags;
            public IntPtr WindowHandle;
        }

        public enum HIDUsage : ushort
        {
            Pointer = 0x01,
            Mouse = 0x02,
            Joystick = 0x04,
            Gamepad = 0x05,
        }

        public enum HIDUsagePage : ushort
        {
            Digitizer = 0x0D
        }

        [Flags]
        public enum RawKeyboardFlags : ushort
        {
            KeyMake = 0,
            KeyBreak = 1,
            KeyE0 = 2,
            KeyE1 = 4,
            TerminalServerSetLED = 8,
            TerminalServerShadow = 0x10,
            TerminalServerVKPACKET = 0x20
        }

        [Flags()]
        public enum RawInputDeviceFlags : int
        {
            None = 0,
            Remove = 0x00000001,
            Exclude = 0x00000010,
            PageOnly = 0x00000020,
            NoLegacy = 0x00000030,
            InputSink = 0x00000100,
            CaptureMouse = 0x00000200,
            NoHotKeys = 0x00000200,
            AppKeys = 0x00000400
        }

        [Flags()]
        public enum RawMouseFlags
            : ushort
        {
            MoveRelative = 0,
            MoveAbsolute = 1,
            VirtualDesktop = 2,
            AttributesChanged = 4
        }

        public enum RawInputType
        {
            Mouse = 0,
            Keyboard = 1,
            HID = 2,
            Other = 3
        }

        public enum RawInputCommand
        {
            Input = 0x10000003,
            Header = 0x10000005
        }

        [Flags()]
        public enum RawMouseButtons
            : ushort
        {
            None = 0,
            LeftDown = 0x0001,
            LeftUp = 0x0002,
            RightDown = 0x0004,
            RightUp = 0x0008,
            MiddleDown = 0x0010,
            MiddleUp = 0x0020,
            Button4Down = 0x0040,
            Button4Up = 0x0080,
            Button5Down = 0x0100,
            Button5Up = 0x0200,
            MouseWheel = 0x0400
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RawKeyboard
        {
            public short MakeCode;
            public RawKeyboardFlags Flags;
            public short Reserved;
            public VirtualKeys VirtualKey;
            public WindowsMessages Message;
            public int ExtraInformation;
        }

        public enum VirtualKeys // Don't think I really need this
            : ushort
        {
            LeftButton = 0x01,
        }

        public enum WindowsMessages : uint
        {
            Input = 0x00FF,
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct DeviceInfo {
            [FieldOffset(0)]
            public int Size;
            [FieldOffset(1)]
            public int Type;
            //[FieldOffset(2)]
            //public int
        };

        [DllImport("user32.dll",SetLastError=true)]
        public static extern int GetRawInputData(IntPtr hRawInput, RawInputCommand uiCommand, out RawInput pData, ref int pcbSize, int cbSizeHeader);
        [DllImport("user32.dll")]
        public static extern uint GetRawInputDeviceInfo(IntPtr hDevice, uint uiCommand, IntPtr pData, ref uint pcbSize);
        public enum RawInputDeviceType : uint
        {
            MOUSE = 0,
            KEYBOARD = 1,
            HID = 2
        }
        [DllImport("user32.dll",SetLastError=true)]
        public static extern bool RegisterRawInputDevices([MarshalAs(UnmanagedType.LPArray, SizeParamIndex=0)] RawInputDevice[] pRawInputDevices, uint uiNumDevices, uint cbSize);

    }
};