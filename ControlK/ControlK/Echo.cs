using libusbK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ControlK
{
    public static class Echo
    {
        private const int IO_TIMEOUT = 3000;

        private static Dictionary<IntPtr, Thread> _echoThreads = new Dictionary<IntPtr, Thread>();
        private static bool _running = true;

        private static List<string> AccessoryValues = new List<string>()
        {
            "ReyRey",
            "Galaxy Scanner",
            "Galaxy Scanner Desc",
            "1.0.0",
            "https://github.com/mystborn",
            "12345"
        };

        private static readonly NLog.ILogger _logger = NLog.LogManager.GetCurrentClassLogger();

        public static void Run()
        {
            var samsungListener = new DeviceListener(Constants.VENDOR_ID_SAMSUNG, Constants.PRODUCT_ID_SAMSUNG);
            var accessoryListener = new DeviceListener(Constants.VENDOR_ID_GOOGLE, Constants.PROTOCOL_ACCESSORY_ADB);

            samsungListener.Connected += OpenDeviceInAccessoryMode;
            accessoryListener.Connected += StartEcho;
            accessoryListener.Disconnected += StopEcho;

            Console.WriteLine("Running Echo in the background");

            accessoryListener.Start();
            samsungListener.Start();

            Console.WriteLine("Press any key to exit...");

            Console.ReadKey();

            _running = false;
            samsungListener.Dispose();
            accessoryListener.Dispose();
        }

        private static bool GetIOPipes(UsbK usb, out byte read, out byte write)
        {
            byte index = 0;
            bool readFound = false;
            bool writeFound = false;

            read = default;
            write = default;

            while (usb.QueryPipe(0, index, out var pipeInfo))
            {
                if (pipeInfo.PipeType != USBD_PIPE_TYPE.UsbdPipeTypeBulk)
                    continue;

                if ((pipeInfo.PipeId & AllKConstants.USB_ENDPOINT_DIRECTION_MASK) == AllKConstants.USB_ENDPOINT_DIRECTION_MASK)
                {
                    // If the pipe has the direction mask, it is a read pipe.
                    if (!readFound)
                    {
                        read = pipeInfo.PipeId;
                        readFound = true;
                    }
                }
                else
                {
                    // If the pipe doesn't have the direction mask, it is a write pipe.
                    if (!writeFound)
                    {
                        write = pipeInfo.PipeId;
                        writeFound = true;
                    }
                }

                if (readFound && writeFound)
                    return true;

                index++;
            }

            return false;
        }

        private static void StartEcho(object sender, UsbDevice usb)
        {
            _logger.Info("Attempting to start echo thread for {SerialNumber} ({VID}/{PID})", usb.SerialNumber, usb.Vid, usb.Pid);

            if (!GetIOPipes(usb, out var read, out var write))
            {
                _logger.Error("Failed to open IO Pipes for {SerialNumber} ({VID}/{PID})", usb.SerialNumber, usb.Vid, usb.Pid);
                return;
            }

            var timeout = new int[] { IO_TIMEOUT };

            usb.SetPipePolicy(read, (int)PipePolicyType.PIPE_TRANSFER_TIMEOUT, Marshal.SizeOf(typeof(int)), timeout);
            usb.SetPipePolicy(write, (int)PipePolicyType.PIPE_TRANSFER_TIMEOUT, Marshal.SizeOf(typeof(int)), timeout);

            var thread = new Thread(() =>
            {
                _logger.Info("Started echo thread for {SerialNumber} ({VID}/{PID})", usb.SerialNumber, usb.Vid, usb.Pid);
                var buffer = new byte[512];
                var error = false;
                while (_running && !error)
                {
                    if (usb.ReadPipe(read, buffer, buffer.Length, out var bytesRead, IntPtr.Zero))
                    {
                        usb.WritePipe(write, buffer, bytesRead, out _, IntPtr.Zero);
                    }
                    else
                    {
                        var errorCode = Marshal.GetLastWin32Error();
                        switch (errorCode)
                        {
                            case Constants.ERROR_SEM_TIMEOUT:
                                break;
                            default:
                                _logger.Error("Unexpected error for {SerialNumber} ({VID}/{PID}). Error code: {ErrorCode}", usb.SerialNumber, usb.Vid, usb.Pid, errorCode);
                                error = true;
                                break;
                        }
                    }
                }

                _logger.Info("Stopped echo thread for {SerialNumber} ({VID}/{PID})", usb.SerialNumber, usb.Vid, usb.Pid);
            });

            _echoThreads.Add(usb.Handle.Pointer, thread);

            thread.Start();
        }

        private static void StopEcho(object sender, UsbDevice usb)
        {
            if (!_echoThreads.TryGetValue(usb.Handle.Pointer, out var thread))
                return;

            thread.Join();
        }

        private static void OpenDeviceInAccessoryMode(object sender, UsbK usb)
        {
            _logger.Info("Attempting to open device in accessory mode.");
            var supportsAccessory = GetProtocol(usb);
            if (supportsAccessory)
            {
                for (var i = 0; i < AccessoryValues.Count; i++)
                    SendString(usb, (ushort)i, AccessoryValues[i]);

                StartAccessory(usb);

                _logger.Info("Started device in accessory mode.");
            }
            else
            {
                _logger.Info("Failed to start device in accessory mode.");
            }
        }

        private static byte[] GetCString(string str)
        {
            var bytes = Encoding.ASCII.GetBytes(str);
            var nullTerminated = new byte[bytes.Length + 1];
            Array.Copy(bytes, 0, nullTerminated, 0, bytes.Length);
            nullTerminated[bytes.Length] = (byte)'\0';

            return nullTerminated;
        }

        private static bool GetProtocol(UsbK usb)
        {
            var bytes = GetProtocolBytes(Constants.PROTOCOL_ACCESSORY);

            WINUSB_SETUP_PACKET packet = new WINUSB_SETUP_PACKET
            {
                RequestType = (byte)(Constants.USB_DIR_IN | Constants.USB_TYPE_VENDOR),
                Request = 51,
                Value = 0,
                Index = 0,
                Length = (ushort)bytes.Length
            };

            return usb.ControlTransfer(packet, bytes, bytes.Length, out _, IntPtr.Zero);
        }

        private static byte[] GetProtocolBytes(ushort protocol)
        {
            var bytes = BitConverter.GetBytes(protocol);
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }

            return bytes;
        }

        private static void SendString(UsbK usb, ushort index, string value)
        {
            var cstr = GetCString(value);

            WINUSB_SETUP_PACKET packet = new WINUSB_SETUP_PACKET
            {
                RequestType = (byte)(Constants.USB_DIR_OUT | Constants.USB_TYPE_VENDOR),
                Request = 52,
                Value = 0,
                Index = index,
                Length = (ushort)cstr.Length
            };

            usb.ControlTransfer(packet, cstr, cstr.Length, out _, IntPtr.Zero);
        }

        private static void StartAccessory(UsbK usb)
        {
            WINUSB_SETUP_PACKET packet = new WINUSB_SETUP_PACKET
            {
                RequestType = (byte)(Constants.USB_DIR_OUT | Constants.USB_TYPE_VENDOR),
                Request = 53,
                Value = 0,
                Index = 0,
                Length = 0
            };

            usb.ControlTransfer(packet, new byte[0], 0, out _, IntPtr.Zero);
        }
    }
}
