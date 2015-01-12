using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using System.ComponentModel;

namespace PTPGestures
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private IntPtr hwnd; 
        public MainWindow()
        {
            InitializeComponent();
            //RawInput input = new RawInput(this);
        }

        public struct TouchData
        {
            public UInt16 ContactAmount;
            public UInt16 Time; // ???
            public UInt16 Y;
            public UInt16 X;
            public UInt16 Status;
        }

        public enum StatusType : ushort
        {
            CONTACT1_DOWN = 0x0303,
            CONTACT1_UP = 0x0103,
            CONTACT2_DOWN = 0x0703,
            CONTACT2_UP = 0x0503,
            CONTACT3_DOWN = 0x0b03,
            CONTACT3_UP = 0x0903
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            RawInputAPI.RawInputDevice[] rid = new RawInputAPI.RawInputDevice[1];
            rid[0].UsagePage = RawInputAPI.HIDUsagePage.Digitizer;
            rid[0].Usage = RawInputAPI.HIDUsage.Gamepad;
            rid[0].Flags = RawInputAPI.RawInputDeviceFlags.InputSink;
            rid[0].WindowHandle = (new WindowInteropHelper(this)).Handle;
            //txt.Text += "Handle: " + rid[0].WindowHandle.ToInt32() + "\n";
            if (RawInputAPI.RegisterRawInputDevices(rid, (uint)rid.Length, (uint)Marshal.SizeOf(rid[0])) == false)
            {
                //txt.Text += "ERROR: Unable to register digitizer.\n";
                //txt.Text += (new Win32Exception(Marshal.GetLastWin32Error())).Message + "\n";
            }
            else
            {
                //txt.Text += "Registered digitizer!\n";
            }
        }

        private void OnSourceInitialized(object sender, EventArgs e)
        {
            // Hook WndProc
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            HwndSource.FromHwnd(hwnd).AddHook(new HwndSourceHook(WndProc));
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        //protected override void WndProc(ref Message m)
        {
            // Handle messages...
            switch (msg)
            {
                case (int)(RawInputAPI.WindowsMessages.Input):
                    int size = Marshal.SizeOf(typeof(RawInputAPI.RawInput));
                    RawInputAPI.RawInput inputData = new RawInputAPI.RawInput();
                    int insize = RawInputAPI.GetRawInputData(lParam, RawInputAPI.RawInputCommand.Input, out inputData, ref size, Marshal.SizeOf(typeof(RawInputAPI.RawInputHeader)));
                    if (insize == -1)
                    {
                        //txt.Text += (new Win32Exception(Marshal.GetLastWin32Error())).Message + "\n";
                    }
                    else
                    {
                        string s = "";
                        //TouchData theData;
                        //theData = (TouchData)Marshal.PtrToStructure(inputData.HID.Data, typeof(TouchData));
                        //txt.Text += "Size:" + insize + ":" + size + "; Data:" + inputData.HID.Data.ToString("X") + "\n";
                        if (false || inputData.Header.Type == RawInputAPI.RawInputType.HID)
                        {
                            byte[] wholedata = new byte[inputData.HID.Size * inputData.HID.Count];
                            //Marshal.Copy(inputData.HID.Data, wholedata, 0, inputData.HID.Size * inputData.HID.Count);
                            //txt.Text += "Size:" + insize + ":" + size + "; Data: ";
                            //foreach (Byte b in wholedata) txt.Text += b.ToString("X");
                            //txt.Text += "\n";
                        }
                        s = "Data:" + inputData.HID.Data3.ToString("X4") + inputData.HID.Data2.ToString("X8") + inputData.HID.Data.ToString("X8");
                        //s = "Data:" + inputData.HID.Data.ToString("X10") + inputData.Mouse.LastX.ToString("X8") + inputData.Mouse.LastY.ToString("X4") + ", " + inputData.Header.Size;
                        TouchData data = new TouchData();
                        data.ContactAmount = (UInt16)(inputData.HID.Data3.ToInt32() & 0xffff);
                        data.Time = (UInt16)(inputData.HID.Data2.ToInt32() >> 16);
                        data.Y = (UInt16)(inputData.HID.Data2.ToInt32() & 0xffff);
                        data.X = (UInt16)(inputData.HID.Data.ToInt32() >> 16);
                        data.Status = (UInt16)(inputData.HID.Data.ToInt32() & 0xffff);
                        s = "Contacts: " + data.ContactAmount.ToString("X") + "; Time: " + data.Time.ToString("X4") + "; Status: " + data.Status.ToString("X4") + "; X: " + data.X.ToString("X4") + "; Y: " + data.Y.ToString("X4");
                        Console.WriteLine(s);

                        switch ((StatusType)data.Status)
                        {
                            case StatusType.CONTACT1_DOWN:
                                if(!Contact1.Points.Any(p => p.X == data.X && p.Y == data.Y))
                                    Contact1.Points.Add(new Point(data.X, data.Y));
                                break;
                            case StatusType.CONTACT1_UP:
                                Contact1.Points.Clear();
                                break;
                            case StatusType.CONTACT2_DOWN:
                                if(!Contact2.Points.Any(p => p.X == data.X && p.Y == data.Y))
                                    Contact2.Points.Add(new Point(data.X, data.Y));
                                break;
                            case StatusType.CONTACT2_UP:
                                Contact2.Points.Clear();
                                break;
                            case StatusType.CONTACT3_DOWN:
                                if (!Contact3.Points.Any(p => p.X == data.X && p.Y == data.Y))
                                    Contact3.Points.Add(new Point(data.X, data.Y));
                                break;
                            case StatusType.CONTACT3_UP:
                                Contact3.Points.Clear();
                                break;
                            default:
                                break;
                        }
                    }
                    break;
                default:
                    break;
            }
            //scrollarea.ScrollToBottom();
            return IntPtr.Zero;
            //return base.WndProc(hwnd, msg, wParam, lParam, ref handled);
        }

        private void OnClosing(object sender, CancelEventArgs e)
        {
            // Remove Hook
            HwndSource.FromHwnd((new WindowInteropHelper(this)).Handle).RemoveHook(new HwndSourceHook(WndProc));
        }
    }
}
