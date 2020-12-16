using libusbK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlK
{
    /// <summary>
    /// Represents a USB device.
    /// </summary>
    public class UsbDevice : UsbK
    {
        /// <inheritdoc cref="KLST_DEVINFO_HANDLE.BusNumber"/>
        public int BusNumber { get; }

        /// <inheritdoc cref="KLST_DEVINFO_HANDLE.ClassGUID"/>
        public string ClassGUID { get; }

        /// <inheritdoc cref="KLST_DEV_COMMON_INFO.InstanceID"/>
        public string InstanceId { get; }

        /// <inheritdoc cref="KLST_DEV_COMMON_INFO.MI"/>
        public int MI { get; }

        /// <inheritdoc cref="KLST_DEV_COMMON_INFO.Vid"/>
        public int Vid { get; }

        /// <inheritdoc cref="KLST_DEV_COMMON_INFO.Pid"/>
        public int Pid { get; }

        /// <inheritdoc cref="KLST_DEVINFO_HANDLE.DeviceAddress"/>
        public int DeviceAddress { get; }

        /// <inheritdoc cref="KLST_DEVINFO_HANDLE.DeviceDesc"/>
        public string DeviceDescription { get; }

        /// <inheritdoc cref="KLST_DEVINFO_HANDLE.DeviceID"/>
        public string DeviceId { get; }

        /// <inheritdoc cref="KLST_DEVINFO_HANDLE.DeviceInterfaceGUID"/>
        public string DeviceInterfaceGUID { get; }

        /// <inheritdoc cref="KLST_DEVINFO_HANDLE.DevicePath"/>
        public string DevicePath { get; }

        /// <inheritdoc cref="KLST_DEVINFO_HANDLE.DriverID"/>
        public int DriverId { get; }

        /// <inheritdoc cref="KLST_DEVINFO_HANDLE.Mfg"/>
        public string Manufacturer { get; }

        /// <inheritdoc cref="KLST_DEVINFO_HANDLE.SerialNumber"/>
        public string SerialNumber { get; }

        /// <inheritdoc cref="KLST_DEVINFO_HANDLE.Service"/>
        public string Service { get; }

        /// <inheritdoc cref="KLST_DEVINFO_HANDLE.SymbolicLink"/>
        public string SymbolicLink { get; }

        /// <inheritdoc cref="UsbK.UsbK(KLST_DEVINFO_HANDLE)"/>
        public UsbDevice(KLST_DEVINFO_HANDLE devInfo)
            : base(devInfo)
        {
            BusNumber = devInfo.BusNumber;
            ClassGUID = devInfo.ClassGUID;
            InstanceId = devInfo.Common.InstanceID;
            MI = devInfo.Common.MI;
            Vid = devInfo.Common.Vid;
            Pid = devInfo.Common.Pid;
            DeviceAddress = devInfo.DeviceAddress;
            DeviceDescription = devInfo.DeviceDesc;
            DeviceId = devInfo.DeviceID;
            DeviceInterfaceGUID = devInfo.DeviceInterfaceGUID;
            DevicePath = devInfo.DevicePath;
            DriverId = devInfo.DriverID;
            Manufacturer = devInfo.Mfg;
            SerialNumber = devInfo.SerialNumber;
            Service = devInfo.Service;
            SymbolicLink = devInfo.SymbolicLink;
        }
    }
}
