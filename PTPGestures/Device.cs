using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PTPGestures
{
	abstract class Device
	{
		abstract public void ReadData(IntPtr lParam);
		abstract public void RegisterDevice(IntPtr hwnd);
		abstract public void UnregisterDevice(IntPtr hwnd);

		private GestureParser gestureParser;
		private RawInputAPI.DeviceInfo deviceInfo;
	}
}
