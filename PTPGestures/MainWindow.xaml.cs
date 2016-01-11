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
using System.Xml;

namespace PTPGestures
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Touchpad ptp;
		XmlDocument Settings = new XmlDocument();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            ptp = new Touchpad();
            ptp.RegisterDevice(new WindowInteropHelper(this).Handle);
			Settings.Load("settings.xml");
			foreach (XmlNode gesture in Settings.SelectNodes("//gesture/name"))
			{
				GestureList.Items.Add(gesture.InnerText);
			}
            //this.Visibility = Visibility.Hidden;
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
            ptp.UnregisterDevice(new WindowInteropHelper(this).Handle);
            HwndSource.FromHwnd((new WindowInteropHelper(this)).Handle).RemoveHook(new HwndSourceHook(WndProc));
        }

		private void GestureList_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			KeyboardShift.IsChecked = false;
			KeyboardWin.IsChecked = false;
			KeyboardCtrl.IsChecked = false;
			KeyboardAlt.IsChecked = false;
			KeyboardAction.Text = "";
			MouseShift.IsChecked = false;
			MouseWin.IsChecked = false;
			MouseCtrl.IsChecked = false;
			MouseAlt.IsChecked = false;
			MouseLeftRadio.IsChecked = false;
			MouseMiddleRadio.IsChecked = false;
			MouseRightRadio.IsChecked = false;
			ProgramTextbox.Text = "";


			XmlNode gesture = Settings.SelectSingleNode("//gesture[name='" + e.AddedItems[0].ToString() + "']");
			if (gesture != null)
			{
				GestureName.Text = gesture["name"].InnerText;
				GestureTrigger.Text = gesture["trigger"].InnerText;
				XmlNode action = gesture["action"];
				switch (action.Attributes["type"].Value)
				{
					case "keyboard":
						KeyboardRadio.IsChecked = true;
						

						string keys = action.InnerText;
						int keyPos = 0;
						for (keyPos = 0; keyPos < keys.Length; keyPos++)
						{
							bool endOfModifiers = false;
							switch (keys[keyPos])
							{
								case '+':
									KeyboardShift.IsChecked = true;
									break;
								case '^':
									KeyboardCtrl.IsChecked = true;
									break;
								case '#':
									KeyboardWin.IsChecked = true;
									break;
								case '!':
									KeyboardAlt.IsChecked = true;
									break;
								default:
									endOfModifiers = true;
									break;
							}
							if (endOfModifiers)
								break;
						}
						KeyboardAction.Text = keys.Substring(keyPos);
						break;
					case "mouse":
						MouseRadio.IsChecked = true;
						switch (action.InnerText)
						{
							case "LButton":
								MouseLeftRadio.IsChecked = true;
								break;
							case "MButton":
								MouseMiddleRadio.IsChecked = true;
								break;
							case "RButton":
								MouseRightRadio.IsChecked = true;
								break;
						}
						break;
					case "exec":
						ProgramRadio.IsChecked = true;
						ProgramTextbox.Text = action.InnerText;
						break;
					default:
						break;
				}
			}
		}

		private void ActionRadio_Unchecked(object sender, RoutedEventArgs e)
		{
			switch (((RadioButton)e.Source).Content.ToString())
			{
				case "Keyboard":
					KeyboardActionGroup.IsEnabled = false;
					break;
				case "Mouse":
					MouseActionGroup.IsEnabled = false;
					break;
				case "Execute Program":
					ProgramActionGroup.IsEnabled = false;
					break;
			}
		}

		private void ActionRadio_Checked(object sender, RoutedEventArgs e)
		{
			switch (((RadioButton)e.Source).Content.ToString())
			{
				case "Keyboard":
					KeyboardActionGroup.IsEnabled = true;
					break;
				case "Mouse":
					MouseActionGroup.IsEnabled = true;
					break;
				case "Execute Program":
					ProgramActionGroup.IsEnabled = true;
					break;
			}
		}
    }
}
