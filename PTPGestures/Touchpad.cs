using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

/**
 * Class for a generic (hopefully?) PTP device.
 * Assumes Single-Finger Hybrid reporting mode, max.3 contacts, and 10-byte RAWINPUT data.
 * Will try to make this work more generically later.
 **/

namespace PTPGestures
{
	class Touchpad
	{
		private struct TouchData
		{
			public TouchData(byte[] raw)
			{
				RawData = raw;
				InputMode = raw[0];
				ContactID = (byte)((raw[1] & 0x0c) >> 2); // 0x0c == 1100b
				Tip = ((raw[1] & 0x02) >> 1) > 0; // 0x02 == 0010b
				Confidence = (raw[1] & 1) > 0;
				X = (ushort)((raw[3] << 8) | raw[2]);
				Y = (ushort)((raw[5] << 8) | raw[4]);
				ScanTime = (ushort)((raw[7] << 8) | raw[6]);
				ContactCount = raw[8];
				ButtonPressed = raw[9] > 0;
			}

			public override string ToString()
			{
				string output = String.Format("ContactID: {0}; Tip: {1}; Confidence: {2}; X: {3:X4}; Y: {4:X4}; ScanTime: {5:X4}; ContactCount: {6}; ButtonPressed: {7}",
					ContactID, Tip, Confidence, X, Y, ScanTime, ContactCount, ButtonPressed);
				return output;
			}

			public byte InputMode; // Not sure about this?? Always 0x03?
			public byte ContactID; // 2 bits?
			public bool Tip;
			public bool Confidence;
			public byte ContactCount;
			public ushort ScanTime; // In microseconds. (10E-4) MAX: 0xffff
			public ushort X;
			public ushort Y;
			public byte[] RawData;
			public bool ButtonPressed;
		}

		private int skipAmt = 25;
		private int skip = 0;
		private GestureParser gp = new GestureParser();
		//private RawInputAPI.DeviceInfo deviceInfo; // If needed, this is the decl

		public void ReadData(IntPtr lParam)
		{
			int size = Marshal.SizeOf(typeof(RawInputAPI.RawInput));
			
			RawInputAPI.RawInput inputData = new RawInputAPI.RawInput();
			int insize = RawInputAPI.GetRawInputData(lParam, RawInputAPI.RawInputCommand.Input, out inputData, ref size, Marshal.SizeOf(typeof(RawInputAPI.RawInputHeader)));
			if (insize == -1)
			{
				// This block seems to execute often: data area size too small. Why? (Seems to work fine anyway)
				// Happens more often with 2+ contacts, but not always.
				// Rmk: Matching the size with what's in the header leads to all sorts of exceptions.
				//		In particular, AccessViolationException --- so memory is getting corrupted whenever we get the whole thing.
				Console.Write("[Touchpad] Error getting initial RawInput: ");
				Console.WriteLine((new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error())).Message);
			}
			else
			{
				List<TouchData> dataList = new List<TouchData>();
				dataList.Add(new TouchData(RawInputAPI.GetRawHIDDataFromInput(inputData)));
				for(int ctr = 1; ctr < dataList[0].ContactCount; ctr++){
					insize = RawInputAPI.GetRawInputData(lParam, RawInputAPI.RawInputCommand.Input, out inputData, ref size, Marshal.SizeOf(typeof(RawInputAPI.RawInputHeader)));
					if (insize == -1)
					{
						Console.Write("[Touchpad] Error retrieving RawInput");
						Console.WriteLine((new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error())).Message);
					}
					TouchData data = new TouchData(RawInputAPI.GetRawHIDDataFromInput(inputData));
					if (dataList.Count(d => d.ContactID == data.ContactID) == 0)
					{
						dataList.Add(data);
					}
				}
				if ((!dataList[0].Tip) || (skip <= 0))
				{
					skip = skipAmt;
					foreach (TouchData d in dataList)
					{
						//Console.WriteLine(d.ToString());
						gp.AddPoint(d.ContactID, new System.Windows.Point(d.X, d.Y));
						if (!d.Tip)
						{
							gp.ProcessGesture(d.ContactID);
							skip = 1;
						}
					}
				}

				skip--;
			}
		}

		public void RegisterDevice(IntPtr hwnd)
		{
			RawInputAPI.RawInputDevice[] rid = new RawInputAPI.RawInputDevice[1];
			rid[0].UsagePage = RawInputAPI.HIDUsagePage.Digitizer;
			rid[0].Usage = RawInputAPI.HIDUsage.Gamepad;
			rid[0].Flags = RawInputAPI.RawInputDeviceFlags.InputSink;
			rid[0].WindowHandle = hwnd;
			if (RawInputAPI.RegisterRawInputDevices(rid, (uint)rid.Length, (uint)Marshal.SizeOf(rid[0])) == false)
			{
				Console.Write("ERROR: Unable to register digitizer.\n");
				Console.WriteLine((new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error())).Message);
			}
		}

		public void UnregisterDevice(IntPtr hwnd)
		{
			RawInputAPI.RawInputDevice[] rid = new RawInputAPI.RawInputDevice[1];
			rid[0].UsagePage = RawInputAPI.HIDUsagePage.Digitizer;
			rid[0].Usage = RawInputAPI.HIDUsage.Gamepad;
			rid[0].Flags = RawInputAPI.RawInputDeviceFlags.InputSink | RawInputAPI.RawInputDeviceFlags.Remove;
			rid[0].WindowHandle = hwnd;
			if (RawInputAPI.RegisterRawInputDevices(rid, (uint)rid.Length, (uint)Marshal.SizeOf(rid[0])) == false)
			{
				Console.Write("ERROR: Unable to register digitizer.\n");
				Console.WriteLine((new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error())).Message);
			}
		}

		public void SetSkip(int amt)
		{
			skipAmt = amt;
		}
	}
}
