using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Windows;

namespace PTPGestures
{
    class GestureParser
    {
        Dictionary<int, Point> LastPoint = new Dictionary<int, Point>();
        Dictionary<int, String> Movements = new Dictionary<int, String>();
        private const double threshold = 50;

        public void AddPoint(int ContactID, System.Windows.Point pt)
        {
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
                    }
                    else if (angle < 30 || angle > 330)
                    {
                        AddMovement(ContactID, "L");
                    }
                    else if (angle > 150 && angle < 210)
                    {
                        AddMovement(ContactID, "R");
                    }
                    else if (angle > 240 && angle < 300)
                    {
                        AddMovement(ContactID, "D");
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

        public void ProcessGesture(int ContactID)
        {
            if (Movements.ContainsKey(ContactID))
            {
                Console.WriteLine("Contact: " + ContactID + " - Gesture: " + Movements[ContactID]);
                Movements[ContactID] = "";
                LastPoint.Remove(ContactID);
            }
        }

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
    }
}
