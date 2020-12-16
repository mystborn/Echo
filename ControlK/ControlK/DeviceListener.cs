using libusbK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlK
{
    public class DeviceListener : IDisposable
    {
        private static readonly NLog.ILogger _logger = NLog.LogManager.GetCurrentClassLogger();

        private ushort _vid;
        private ushort _pid;
        private Dictionary<IntPtr, UsbDevice> _devices = new Dictionary<IntPtr, UsbDevice>();
        private KHOT_PARAMS _listenParams;
        private HotK _listener;
        private bool _disposed = false;

        public event EventHandler<UsbDevice> Connected;
        public event EventHandler<UsbDevice> Disconnected;

        public DeviceListener(ushort vid, ushort pid)
        {
            _vid = vid;
            _pid = pid;

            _listenParams = new KHOT_PARAMS();
            _listenParams.PatternMatch.DeviceID = string.Format(@"USB\VID_{0:X4}&PID_{1:X4}", vid, pid);
            _listenParams.Flags = KHOT_FLAG.PLUG_ALL_ON_INIT;
            _listenParams.OnHotPlug = OnHotPlug;
        }

        ~DeviceListener()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if(!_disposed)
            {
                if(disposing)
                {
                    if(_listener != null)
                    {
                        Stop();
                    }
                }

                _disposed = true;
            }
        }

        public void Start()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(DeviceListener));

            if(_listener != null)
            {
                _logger.Error($"Tried to start a {nameof(DeviceListener)} for {{VID}}/{{PID}} while it was already running.", _vid, _pid);
                return;
            }

            _logger.Info("Starting a device listener for {VID}/{PID}", _vid, _pid);

            _listener = new HotK(ref _listenParams);
        }

        public void Stop()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(DeviceListener));

            if(_listener is null)
            {
                _logger.Error($"Tried to stop a {nameof(DeviceListener)} for {{VID}}/{{PID}} that was not running.", _vid, _pid);
                Console.WriteLine("Tried to stop listener that isn't running.");
                return;
            }

            _logger.Info("Stopping a device listener for {VID}/{PID}", _vid, _pid);

            foreach (var device in _devices.Values)
            {
                DisconnectDevice(device);
            }

            _devices.Clear();

            _listener.Dispose();
            _listener = null;

            _logger.Info("Stopped device listener for {VID}/{PID}", _vid, _pid);
        }

        private void DisconnectDevice(UsbDevice device)
        {
            _logger.Info("Disconnected device {SerialNumber} ({VID}/{PID})", device.SerialNumber, _vid, _pid);
            Disconnected?.Invoke(this, device);
            device.Dispose();
        }

        private void OnHotPlug(KHOT_HANDLE handle, KLST_DEVINFO_HANDLE deviceInfo, KLST_SYNC_FLAG plugType)
        {
            if(plugType.HasFlag(KLST_SYNC_FLAG.ADDED))
            {
                // The device has been re-added for some reason.
                if (_devices.TryGetValue(handle.Pointer, out var device))
                {
                    _logger.Error("Tried to re-add a connected device with serial {SerialNumber} ({VID}/{PID})", deviceInfo.SerialNumber, _vid, _pid);
                    return;
                }

                _logger.Info("Connected device {SerialNumber} ({VID}/{PID})", deviceInfo.SerialNumber, _vid, _pid);

                device = new UsbDevice(deviceInfo);
                _devices.Add(handle.Pointer, device);
                Connected?.Invoke(this, device);
            }
            else if(plugType.HasFlag(KLST_SYNC_FLAG.REMOVED))
            {
                if (!_devices.TryGetValue(handle.Pointer, out var device))
                {
                    _logger.Error("Tried to remove a device that was never connected with serial {SerialNumber} ({VID}/{PID})", deviceInfo.SerialNumber, _vid, _pid);
                    return;
                }

                DisconnectDevice(device);
                _devices.Remove(handle.Pointer);
            }
        }
    }
}
