// TODO: Add scantime in somehow to deal with taps vs. holds
//      - Is a short enough? What if it overshoots?
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Windows;
using WindowsInput;

namespace PTPGestures
{
	class GestureParser
	{
		Dictionary<int, Point> LastPoint = new Dictionary<int, Point>();
		Dictionary<int, String> Movements = new Dictionary<int, String>();
		Dictionary<String, String> Gestures = new Dictionary<String, String>();
		List<Contact> contacts = new List<Contact>();
		InputSimulator iSim = new InputSimulator();
		private const double threshold = 20;
		private bool processed = false;
		//private const int timeThreshold = 0x1388; // Expect ~ 5000 microseconds for a tap?

		public void AddPoint(int ContactID, System.Windows.Point pt)
		{
			if(!contacts.Exists(c => c.id == ContactID))
			{
				processed = false;
				contacts.Add(new Contact(ContactID, (int) pt.X, (int) pt.Y));
			}
			Contact contact = contacts.Find(c => c.id == ContactID);
			if (!Movements.ContainsKey(ContactID))
			{
				Movements.Add(ContactID, "");
			}
			if (LastPoint.ContainsKey(ContactID))
			{
				Vector movement = pt - LastPoint[ContactID];
				if (movement.Length >= threshold)
				{
					//double angle = Vector.AngleBetween(movement, LastPoint[ContactID] - new Point(LastPoint[ContactID].X+10,LastPoint[ContactID].Y));
					double angle = (Math.Atan2(pt.Y - LastPoint[ContactID].Y, pt.X - LastPoint[ContactID].X)*(180.0/Math.PI))+180;
					//Console.WriteLine("[DEBUG] Angle: " + angle);
					if (angle > 60 && angle < 120)
					{
						AddMovement(ContactID, "U");
						contact.addMovement("U");
					}
					else if (angle < 30 || angle > 330)
					{
						AddMovement(ContactID, "L");
						contact.addMovement("L");
					}
					else if (angle > 150 && angle < 210)
					{
						AddMovement(ContactID, "R");
						contact.addMovement("R");
					}
					else if (angle > 240 && angle < 300)
					{
						AddMovement(ContactID, "D");
						contact.addMovement("D");
					}
				}
			}
			else
			{
				LastPoint.Add(ContactID, pt);
			}
			LastPoint[ContactID] = pt;
		}

		private void AddMovement(int id, String direction)
		{
			if (!Movements[id].EndsWith(direction))
			{
				Movements[id] += direction;
			}
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

				Console.Write(gesture);
				if (gesture.Equals("EL"))
				{
					Console.WriteLine("Sending Win+A");
					iSim.Keyboard.ModifiedKeyStroke(WindowsInput.Native.VirtualKeyCode.LWIN, WindowsInput.Native.VirtualKeyCode.VK_A);
				}
				if (gesture.Equals("ER"))
				{
					iSim.Keyboard.ModifiedKeyStroke(WindowsInput.Native.VirtualKeyCode.LWIN, WindowsInput.Native.VirtualKeyCode.TAB);
				}
				if (gesture.Equals("T,T,T"))
				{
					SimulateMiddleClick();
				}
				Console.WriteLine(" : Test");
				processed = true;
			}
		}

		public void ProcessGesture(int ContactID)
		{
			if (Movements.ContainsKey(ContactID))
			{
				ProcessGesture();
				//Console.WriteLine("Contact: " + ContactID + " - Gesture: " + Movements[ContactID]);
				Movements[ContactID] = "";
				LastPoint.Remove(ContactID);
				contacts.Remove(contacts.Find(c => c.id == ContactID));
				//Console.WriteLine(LastPoint.ContainsKey(ContactID));
			}
		}

		
		// Will use to emit mouse events if needed.
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

		// InputSimulator lacks a middle click simulator, so implement our own.
		private void SimulateMiddleClick()
		{
			mouse_event((int)MouseEvent.MIDDLEDOWN | (int)MouseEvent.MIDDLEUP, 0, 0, 0, 0);
		}
	}
}
