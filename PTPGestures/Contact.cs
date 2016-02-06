using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace PTPGestures
{
	class Contact
	{
		public int id { get; set; }
		public int startX;
		public int startY;
		public DateTime startTime;
		private const double threshold = 20;
		private const int tapTicks = 7500000;
		private int height = 0x23b;
		private int width = 0x41b;
		private string gesture;
		private Point lastPoint;

		public Contact(int i, int x, int y)
		{
			id = i;
			startX = x;
			startY = y;
			lastPoint = new Point(x, y);

			if (x == 0){
				gesture = "LE";
			}
			else if (y == 0){
				gesture = "TE";
			}
			else if (x == width){
				gesture = "RE";
			}
			else if (y == height) {
				// Indicates gesture starts from edge of touchpad.
				// Techinically this means that you could activate edge swipes from a different edge...
				gesture = "BE";
			}
			else {
				gesture = "";
			}
			startTime = DateTime.Now;
		}

		public void addPoint(Point pt)
		{
			Vector movement = pt - lastPoint;
			if (movement.Length >= threshold)
			{
				double angle = (Math.Atan2(pt.Y - lastPoint.Y, pt.X - lastPoint.X) * (180.0 / Math.PI)) + 180;
				if (angle > 60 && angle < 120)
				{
					addMovement("U");
				}
				else if (angle < 30 || angle > 330)
				{
					addMovement("L");
				}
				else if (angle > 150 && angle < 210)
				{
					addMovement("R");
				}
				else if (angle > 240 && angle < 300)
				{
					addMovement("D");
				}
			}

			lastPoint = pt;
		}

		public void addMovement(string s)
		{
			if(!gesture.EndsWith(s))
				gesture += s;
		}

		public string getGesture()
		{
			if (gesture.Equals("")) // 10million ticks/sec
			{
				if ((DateTime.Now - startTime).Ticks < tapTicks){
					gesture = "T";
				}
				else{
					gesture = "H";
				}
			}
			return gesture;
		}

		public void clearGesture()
		{
			gesture = "";
		}
	}
}
