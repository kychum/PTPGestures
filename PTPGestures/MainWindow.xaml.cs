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
		private bool settingsChanged = false;
		private bool programmaticChanges = false;
		private string version = "0.3";
		private XmlDocument Settings = new XmlDocument();
		private string initialSettings = "";

        public MainWindow()
        {
            InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            ptp = new Touchpad();
            ptp.RegisterDevice(new WindowInteropHelper(this).Handle);
			Settings.Load("settings.xml");
			initialSettings = Settings.InnerXml;
			foreach (XmlNode gesture in Settings.SelectNodes("//gesture/name"))
			{
				GestureList.Items.Add(gesture.InnerText);
			}
			AboutText.Text = "PTPGestures version " + version;
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
			programmaticChanges = true;
			SaveGestureButton.IsEnabled = false;
			GestureName.Text = "";
			GestureTrigger.Text = "";
			KeyboardRadio.IsChecked = false;
			MouseRadio.IsChecked = false;
			ProgramRadio.IsChecked = false;
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

			if (e.AddedItems.Count > 0)
			{
				GestureSettingsBox.IsEnabled = true;
				DeleteGestureButton.IsEnabled = true;
				XmlNode gesture = Settings.SelectSingleNode("//gesture[name='" + e.AddedItems[0].ToString() + "']");
				if (gesture != null)
				{
					GestureName.Text = e.AddedItems[0].ToString();
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
							int mouseActionPos = 0; // Weird shenainegans with declarations in cases
							string mouseAction = gesture["action"].InnerText;
							for (int actionPos = 0; actionPos < mouseAction.Length; actionPos++)
							{
								bool done = false;
								switch (mouseAction[actionPos])
								{
									case '+':
										MouseShift.IsChecked = true;
										break;
									case '^':
										MouseCtrl.IsChecked = true;
										break;
									case '!':
										MouseAlt.IsChecked = true;
										break;
									case '#':
										MouseWin.IsChecked = true;
										break;
									default:
										mouseActionPos = actionPos;
										done = true;
										break;
								}
								if (done) break;
							}
							switch (mouseAction.Substring(mouseActionPos))
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
			else
			{
				GestureSettingsBox.IsEnabled = false;
				DeleteGestureButton.IsEnabled = false;
			}
			programmaticChanges = false;
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

			if(!programmaticChanges)
			CheckChangedGesture();
		}

		private void CreateNewGesture(object sender, RoutedEventArgs e)
		{
			string name = "New Gesture";
			int numberOfConflicts = 0;
			while(Settings.SelectSingleNode("//gesture[name='" + name + (numberOfConflicts==0 ? "" : " ("+numberOfConflicts+")") + "']") != null){
				numberOfConflicts++;
			}
			name = name + (numberOfConflicts == 0 ? "" : " ("+ numberOfConflicts + ")");
			GestureList.Items.Add(name);

			XmlElement newNode = Settings.CreateElement("gesture");
			newNode.InnerXml = "<name>" + name + "</name><trigger></trigger><action type=\"\"></action>";
			Settings["settings"]["gesturelist"].AppendChild(newNode);

			GestureList.SelectedItem = GestureList.Items.GetItemAt(GestureList.Items.Count - 1);

			WriteSettings();
		}

		private void DeleteGesture(object sender, RoutedEventArgs e)
		{
			XmlNode toDelete = Settings.SelectSingleNode("//gesture[name='" + GestureList.SelectedItem + "']");
			if (toDelete != null)
			{
				Settings["settings"]["gesturelist"].RemoveChild(toDelete);
				GestureList.Items.Remove(GestureList.SelectedItem);
				WriteSettings();
			}
		}

		private void SaveGesture(object sender, RoutedEventArgs e)
		{
			string oldName = GestureList.SelectedItem as string;
			XmlNode modifiedNode = Settings.SelectSingleNode("//gesture[name='" + oldName + "']");
			if (modifiedNode == null)
			{
				modifiedNode = Settings.CreateElement("gesture");
				Settings.SelectSingleNode("//gesturelist").AppendChild(modifiedNode);
			}
			if (modifiedNode != null)
			{
				string gestureXml = "";
				// Individually setting the child nodes' InnerText/Value doesn't work.
				gestureXml = "";
				gestureXml += "<name>" + GestureName.Text + "</name>";
				Console.WriteLine(gestureXml + GestureName.Text);
				gestureXml += "<trigger>" + GestureTrigger.Text + "</trigger>";
				string actionXml = "<action type=\"";
				//var nodeAction = modifiedNode["action"];
				if (KeyboardRadio.IsChecked == true)
				{
					//nodeAction.SetAttribute("type","keyboard");
					string actionText = KeyboardAction.Text;
					if (KeyboardShift.IsChecked == true)
						actionText = "+" + actionText;
					if (KeyboardCtrl.IsChecked == true)
						actionText = "^" + actionText;
					if (KeyboardAlt.IsChecked == true)
						actionText = "!" + actionText;
					if (KeyboardWin.IsChecked == true)
						actionText = "#" + actionText;
					//nodeAction.InnerText = actionText;
					actionXml += "keyboard\">" + actionText + "</action>";
				}
				else if (MouseRadio.IsChecked == true)
				{
					//nodeAction.SetAttribute("type","mouse");
					string actionText = "";
					if (MouseShift.IsChecked == true)
						actionText = "+" + actionText;
					if (MouseCtrl.IsChecked == true)
						actionText = "^" + actionText;
					if (MouseAlt.IsChecked == true)
						actionText = "!" + actionText;
					if (MouseWin.IsChecked == true)
						actionText = "#" + actionText;

					if (MouseLeftRadio.IsChecked == true)
						actionText += "LButton";
					if (MouseRightRadio.IsChecked == true)
						actionText += "RButton";
					if (MouseMiddleRadio.IsChecked == true)
						actionText += "MButton";

					//nodeAction.InnerText = actionText;
					actionXml += "mouse\">" + actionText + "</action>";
				}
				else if (ProgramRadio.IsChecked == true)
				{
					//nodeAction.SetAttribute("type","exec");
					//nodeAction.InnerText = ProgramTextbox.Text;
					actionXml += "exec\">" + ProgramTextbox.Text + "</action>";
				}
				else
				{
					actionXml += "\"></action>";
				}

				gestureXml += actionXml;
				Console.WriteLine(gestureXml);
				modifiedNode.InnerXml = gestureXml;
				int index = GestureList.SelectedIndex;
				if (index == -1)
				{
					GestureList.Items.Add(GestureName.Text);
					GestureList.SelectedIndex = GestureList.Items.Count-1;
				}
				else
				{
					GestureList.Items[index] = GestureName.Text;
					GestureList.SelectedIndex = index;
				}

				WriteSettings();
				
				//Settings["settings"]["gesturelist"].ReplaceChild(Settings.SelectSingleNode("//gesture[name='" + oldName + "']"), modifiedNode);
			}
		}

		private void WriteSettings()
		{
			if (!initialSettings.Equals(Settings.InnerXml))
			{
				Settings.Save("settings.xml");
				initialSettings = Settings.InnerXml;
			}
			GestureParser.UpdateSettings();
		}

		private void GestureChanged(object sender, TextChangedEventArgs e)
		{
			if(!programmaticChanges)
				CheckChangedGesture();
		}

		private void GestureChanged(object sender, RoutedEventArgs e)
		{
			if(!programmaticChanges)
				CheckChangedGesture();
		}

		private void CheckChangedGesture()
		{
			XmlNode gesture = Settings.SelectSingleNode("//gesture[name='" + GestureList.SelectedItem + "']");
			SaveGestureButton.IsEnabled = false;
			if (gesture != null)
			{
				if (!gesture["name"].InnerText.Equals(GestureName.Text) ||
					!gesture["trigger"].InnerText.Equals(GestureTrigger.Text))
				{
					SaveGestureButton.IsEnabled = true;
					Console.WriteLine("name or trigger differs");
					return;
				}
				switch (gesture["action"].Attributes["type"].Value)
				{
					case "keyboard":
						if (KeyboardRadio.IsChecked != true)
							SaveGestureButton.IsEnabled = true;
						else
						{
							string action = gesture["action"].InnerText;
							if ((KeyboardAlt.IsChecked == true && !action.Contains("!")) ||
								(KeyboardAlt.IsChecked == false && action.Contains("!")) ||
								(KeyboardCtrl.IsChecked == true && !action.Contains("^")) ||
								(KeyboardCtrl.IsChecked == false && action.Contains("^")) ||
								(KeyboardShift.IsChecked == true && !action.Contains("+")) ||
								(KeyboardShift.IsChecked == false && action.Contains("+")) ||
								(KeyboardWin.IsChecked == true && !action.Contains("#")) ||
								(KeyboardWin.IsChecked == false && action.Contains("#")))
								SaveGestureButton.IsEnabled = true;
							else if (!action.Substring(action.LastIndexOfAny(new[] { '^', '!', '#', '+' }) + 1).Equals(KeyboardAction.Text))
								SaveGestureButton.IsEnabled = true;
							Console.WriteLine("Expect: " + KeyboardAction.Text + "\nActual: " + action.Substring(action.LastIndexOfAny(new[] { '^', '!', '#', '+' }) + 1));
						}
						break;
					case "mouse":
						if (MouseRadio.IsChecked != true)
							SaveGestureButton.IsEnabled = true;
						else
						{
							string action = gesture["action"].InnerText;
							if ((MouseAlt.IsChecked == true && !action.Contains("!")) ||
								(MouseAlt.IsChecked == false && action.Contains("!")) ||
								(MouseCtrl.IsChecked == true && !action.Contains("^")) ||
								(MouseCtrl.IsChecked == false && action.Contains("^")) ||
								(MouseShift.IsChecked == true && !action.Contains("+")) ||
								(MouseShift.IsChecked == false && action.Contains("+")) ||
								(MouseWin.IsChecked == true && !action.Contains("#")) ||
								(MouseWin.IsChecked == false && action.Contains("#")))
								SaveGestureButton.IsEnabled = true;
							else switch (action.Substring(action.IndexOfAny(new[] { 'L', 'M', 'R' })))
							{
								case "LButton":
									if (MouseLeftRadio.IsChecked == false)
										SaveGestureButton.IsEnabled = true;
									break;
								case "RButton":
									if (MouseRightRadio.IsChecked == false)
										SaveGestureButton.IsEnabled = true;
									break;
								case "MButton":
									if (MouseMiddleRadio.IsChecked == false)
										SaveGestureButton.IsEnabled = true;
									break;
								default: break;
							}
						}
						break;
					case "exec":
						if (ProgramRadio.IsChecked != true)
							SaveGestureButton.IsEnabled = true;
						else if (!gesture["action"].InnerText.Equals(ProgramTextbox.Text))
							SaveGestureButton.IsEnabled = true;
						break;
				}
			}
		}
    }
}
