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
        private Device ptp;
        public MainWindow()
        {
            InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            ptp = new GenericDevice();
            ptp.RegisterDevice(new WindowInteropHelper(this).Handle);
            this.Visibility = Visibility.Hidden;
        }

        private void OnSourceInitialized(object sender, EventArgs e)
        {
            // Hook WndProc
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            HwndSource.FromHwnd(hwnd).AddHook(new HwndSourceHook(WndProc));
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            // Handle messages...
            if((RawInputAPI.WindowsMessages)msg == RawInputAPI.WindowsMessages.Input) // Handle WM_INPUT
            {
                ptp.ReadData(lParam);
            }
            return IntPtr.Zero;
        }

        private void OnClosing(object sender, CancelEventArgs e)
        {
            // Remove Hook
            HwndSource.FromHwnd((new WindowInteropHelper(this)).Handle).RemoveHook(new HwndSourceHook(WndProc));
        }
    }
}
