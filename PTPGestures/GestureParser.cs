// TODO: Add scantime in somehow to deal with taps vs. holds
//      - Is a short enough? What if it overshoots?
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Windows;
using System.Xml;
using WindowsInput;

namespace PTPGestures
{
	class GestureParser
	{
		List<Contact> contacts = new List<Contact>();
		InputSimulator iSim = new InputSimulator();
		static XmlDocument gestures = new XmlDocument();
		private bool processed = false;
		//private const int timeThreshold = 0x1388; // Expect ~ 5000 microseconds for a tap?

		public GestureParser()
		{
			UpdateSettings();
		}

		public static void UpdateSettings()
		{
			gestures.Load("settings.xml");
		}

		public void AddPoint(int ContactID, Point pt)
		{
			if(!contacts.Exists(c => c.id == ContactID))
			{
				processed = false;
				contacts.Add(new Contact(ContactID, (int) pt.X, (int) pt.Y));
			}
			Contact contact = contacts.Find(c => c.id == ContactID);
			contact.addPoint(pt);
		}

		public void ProcessGesture()
		{
			if (!processed) {
				Console.WriteLine("ProcessGesture: " + contacts.Count + "contacts");
				contacts = contacts.OrderBy(c => c.startX).ToList();
				string gesture = "";
				foreach (Contact c in contacts)
				{
					gesture += c.getGesture() + ",";
					c.clearGesture();
				}
				gesture = gesture.TrimEnd(',');

				Console.WriteLine(gesture);

				XmlNode action = gestures.SelectSingleNode("//gesture[trigger='" + gesture + "']/action");
				if (action != null)
				{
					Console.WriteLine(action.Attributes["type"].Value);
					Console.WriteLine(action.InnerText);
					switch (action.Attributes["type"].Value)
					{
						case "keyboard":
							sendKeyboardAction(action.InnerText);
							break;
						case "mouse":
							sendMouseAction(action.InnerText);
							break;
						case "exec":
							sendExecAction(action.InnerText);
							break;
						default:
							break;
					}
				}
				processed = true;
			}
		}

		// Seems inefficient. This way makes it so that contacts are tracked until they leave the touchpad
		public void ProcessGesture(int ContactID)
		{
			if (contacts.Exists(c => c.id == ContactID))
			{
				ProcessGesture();
				contacts.Remove(contacts.Find(c => c.id == ContactID));
			}
		}

		public void sendKeyboardAction(string action)
		{
			var keys = new Stack<WindowsInput.Native.VirtualKeyCode>();
			for (int keyPos = 0; keyPos < action.Length; keyPos++)
			{
				switch (action[keyPos])
				{
					case '+':
						iSim.Keyboard.KeyDown(WindowsInput.Native.VirtualKeyCode.LSHIFT);
						keys.Push(WindowsInput.Native.VirtualKeyCode.LSHIFT);
						break;
					case '^':
						iSim.Keyboard.KeyDown(WindowsInput.Native.VirtualKeyCode.LCONTROL);
						keys.Push(WindowsInput.Native.VirtualKeyCode.LCONTROL);
						break;
					case '#':
						iSim.Keyboard.KeyDown(WindowsInput.Native.VirtualKeyCode.LWIN);
						keys.Push(WindowsInput.Native.VirtualKeyCode.LWIN);
						break;
					case '!':
						// InputSimulator lacks ALT?
						iSim.Keyboard.KeyDown(WindowsInput.Native.VirtualKeyCode.MENU);
						keys.Push(WindowsInput.Native.VirtualKeyCode.MENU);
						break;
					case '{':
						int endpoint = action.Substring(keyPos).IndexOf('}')+keyPos-1;
						string keyName = action.Substring(keyPos + 1, (endpoint - keyPos));
						keyPos = endpoint + 1;
						WindowsInput.Native.VirtualKeyCode key;
						Console.WriteLine("action:" + action);
						Console.WriteLine("keyPos: " + keyPos + ", endpoint: " + endpoint + ", length:" + action.Length);
						Console.WriteLine("Specific keyname: " + keyName);
						if (System.Enum.TryParse<WindowsInput.Native.VirtualKeyCode>(keyName, true, out key))
						{
							Console.WriteLine("Found " + keyName);
							iSim.Keyboard.KeyDown(key);
							keys.Push(key);
						}
						break;
					default:
						char upperKey = System.Char.ToUpper(action[keyPos]);
						if (System.Char.IsLetterOrDigit(upperKey))
						{
							iSim.Keyboard.KeyDown((WindowsInput.Native.VirtualKeyCode)((int)upperKey));
							keys.Push((WindowsInput.Native.VirtualKeyCode)((int)upperKey));
						}
						break;
				}
			}

			foreach(var key in keys){
				iSim.Keyboard.KeyUp(key);
				Console.WriteLine(System.Enum.GetName(typeof(WindowsInput.Native.VirtualKeyCode),key));
			}
		}

		public void sendMouseAction(string action)
		{
			//Search for modifier keys, then look for mouse buttons
			int mouseStart = 0;
			var modifiers = new Stack<WindowsInput.Native.VirtualKeyCode>();
			for (int keyPos = 0; keyPos < 3; keyPos++)
			{
				bool done = false;
				switch (action[keyPos])
				{
					case '+':
						iSim.Keyboard.KeyDown(WindowsInput.Native.VirtualKeyCode.LSHIFT);
						modifiers.Push(WindowsInput.Native.VirtualKeyCode.LSHIFT);
						break;
					case '^':
						iSim.Keyboard.KeyDown(WindowsInput.Native.VirtualKeyCode.LCONTROL);
						modifiers.Push(WindowsInput.Native.VirtualKeyCode.LCONTROL);
						break;
					case '#':
						iSim.Keyboard.KeyDown(WindowsInput.Native.VirtualKeyCode.LWIN);
						modifiers.Push(WindowsInput.Native.VirtualKeyCode.LWIN);
						break;
					default:
						mouseStart = keyPos;
						done = true;
						break;
				}
				if (done)
				{
					break;
				}
			}
			switch (action.Substring(mouseStart))
			{
				case "LButton":
					iSim.Mouse.LeftButtonClick();
					break;
				case "RButton":
					iSim.Mouse.RightButtonClick();
					break;
				case "MButton":
					SimulateMiddleClick();
					break;
				default:
					break;
			}

			foreach(var key in modifiers){
				iSim.Keyboard.KeyUp(key);
			}
		}

		public void sendExecAction(string action)
		{

		}


		// Use to simulate mouse events not provided in InputSimulator.
		[DllImport("user32.dll")]
		public static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);

		public enum MouseEvent
		{
			LEFTDOWN = 0x02,
			LEFTUP = 0x04,
			RIGHTDOWN = 0x08,
			RIGHTUP = 0x10,
			MIDDLEDOWN = 0x0020,
			MIDDLEUP = 0x0040
		}

		// InputSimulator lacks a middle click, so implement our own.
		private void SimulateMiddleClick()
		{
			mouse_event((int)MouseEvent.MIDDLEDOWN | (int)MouseEvent.MIDDLEUP, 0, 0, 0, 0);
		}
	}
}
