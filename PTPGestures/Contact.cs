using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PTPGestures
{
	class Contact
	{
		public int id { get; set; }
		public int startX;
		public int startY;
		public DateTime startTime;
		private string gesture;

		public Contact(int i, int x, int y)
		{
			id = i;
			startX = x;
			startY = y;
			if (x == 0 || y == 0 || x==0x41b || y==0x23b) {
				gesture = "E";
			}
			else {
				gesture = "";
			}
			startTime = DateTime.Now;
		}

		public void addMovement(string s)
		{
			if(!gesture.EndsWith(s))
				gesture += s;
		}

		public string getGesture()
		{
			if (gesture.Equals("") && (DateTime.Now - startTime).Ticks < 7500000) // 10million ticks/sec
			{
				gesture = "T";
			}
			return gesture;
		}

		public void clearGesture()
		{
			gesture = "";
		}
	}
}
