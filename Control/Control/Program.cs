using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LibUsbDotNet;
using LibUsbDotNet.LibUsb;
using LibUsbDotNet.Main;

namespace Control
{
    class Program
    {
        private const ushort PROTOCOL_ACCESSORY = 0x2D00;
        private const ushort PROTOCOL_ACCESSORY_ADB = 0x2D01;

        private const uint USB_DIR_OUT = 0x00;
        private const uint USB_TYPE_VENDOR = 0x40;
        private const uint USB_DIR_IN = 0x80;

        private const ushort VENDOR_ID_SAMSUNG = 0x04e8;
        private const ushort VENDOR_ID_GOOGLE = 0x18D1;


        private static List<string> AccessoryValues = new List<string>()
        {
            "ReyRey",
            "Galaxy Scanner",
            "Galaxy Scanner Desc",
            "1.0.0",
            "https://github.com/mystborn",
            "12345"
        };

        static void Main(string[] args)
        {
            OpenDeviceInAccessoryMode();
            OpenDeviceIO();
        }

        private static void ListDevices(UsbContext context)
        {
            using (var list = context.List())
            {
                foreach (var device in list)
                {
                    if(!device.IsOpen)
                    {
                        if(device.TryOpen())
                        {
                            device.Info 
                        }
                    }
                    else
                    {
                        device.Close();
                    }
                }
            }
        }

        private static void OpenDeviceIO()
        {
            using (var context = new UsbContext())
            {
                context.SetDebugLevel(LogLevel.Debug);

                foreach(var device in context.List().Where(d => d.VendorId == VENDOR_ID_GOOGLE))
                {
                    if (!device.TryOpen())
                        continue;

                    var interfaceNumber = ClaimInterface(device);

                    UsbEndpointReader reader = null;
                    UsbEndpointWriter writer = null;
                    switch(interfaceNumber)
                    {
                        case 0:
                            reader = device.OpenEndpointReader(ReadEndpointID.Ep01);
                            writer = device.OpenEndpointWriter(WriteEndpointID.Ep01);
                            break;
                        case 1:

                            reader = device.OpenEndpointReader(ReadEndpointID.Ep02);
                            writer = device.OpenEndpointWriter(WriteEndpointID.Ep02);
                            break;
                    }

                    var finished = false;

                    Task.Run(() =>
                    {
                        byte[] readBuffer = new byte[1024];

                        while (!finished)
                        {
                            int bytesRead;
                            var error = reader.Read(readBuffer, 2000, out bytesRead);
                            switch(error)
                            {
                                case Error.Timeout:
                                    continue;
                                case Error.Success:
                                    break;
                                default:
                                    finished = true;
                                    break;
                            }

                            writer.Write(readBuffer, 0, bytesRead, 2000, out _);
                        }
                    });

                    Console.WriteLine("Press any key to finish...");
                    Console.ReadKey();

                    finished = true;

                    device.ReleaseInterface(interfaceNumber);

                    device.Close();
                }
            }
        }

        private static int ClaimInterface(IUsbDevice device)
        {
            foreach(var config in device.Configs)
            {
                foreach(var number in config.Interfaces.Select(i => i.Number))
                {
                    try
                    {
                        device.ClaimInterface(number);
                        return number;
                    }
                    catch(Exception e)
                    {
                        continue;
                    }
                }
            }

            return -1;
        }

        private static void OpenDeviceInAccessoryMode()
        {
            using (var context = new UsbContext())
            {
                context.SetDebugLevel(LogLevel.Debug);

                var devices = context.List();
                foreach (var device in devices.Where(d => d.VendorId == VENDOR_ID_SAMSUNG))
                {
                    if (!device.TryOpen())
                        continue;

                    device.Open();
                    var info = device.Info;

                    var supportsAccessory = GetProtocol(device);
                    if (supportsAccessory)
                    {
                        for (int i = 0; i < AccessoryValues.Count; i++)
                        {
                            SendString(device, i, AccessoryValues[i]);
                        }

                        StartAccessory(device);
                    }

                    device.ResetDevice();
                    device.Close();

                    return;
                }
            }
        }

        private static byte[] GetProtocolBytes(ushort protocol)
        {
            var bytes = BitConverter.GetBytes(protocol);
            if(!BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }

            return bytes;
        }

        private static byte[] GetCString(string str)
        {
            var bytes = Encoding.ASCII.GetBytes(str);
            var nullTerminated = new byte[bytes.Length + 1];
            Array.Copy(bytes, 0, nullTerminated, 0, bytes.Length);
            nullTerminated[bytes.Length] = (byte)'\0';

            return nullTerminated;
        }

        private static bool GetProtocol(IUsbDevice device)
        {
            var protocol = GetProtocolBytes(PROTOCOL_ACCESSORY);

            var packet = new UsbSetupPacket(
                (byte)(USB_DIR_IN | USB_TYPE_VENDOR),
                51,
                0,
                0,
                protocol.Length);

            var result = device.ControlTransfer(packet, protocol, 0, protocol.Length);
            return result != 0;
        }

        private static void SendString(IUsbDevice device, int index, string value)
        {
            var cstr = GetCString(value);

            var packet = new UsbSetupPacket(
                (byte)(USB_DIR_OUT | USB_TYPE_VENDOR),
                52,
                0,
                index,
                cstr.Length);

            device.ControlTransfer(packet, cstr, 0, cstr.Length);
        }

        private static void StartAccessory(IUsbDevice device)
        {
            var packet = new UsbSetupPacket(
                (byte)(USB_DIR_OUT | USB_TYPE_VENDOR),
                53,
                0,
                0,
                0);

            device.ControlTransfer(packet);
        }
    }
}
