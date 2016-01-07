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
		XmlDocument gestures = new XmlDocument();
		private bool processed = false;
		//private const int timeThreshold = 0x1388; // Expect ~ 5000 microseconds for a tap?

		public GestureParser()
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
