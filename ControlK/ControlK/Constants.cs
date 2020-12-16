using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlK
{
    public class Constants
    {
        public const ushort PROTOCOL_ACCESSORY = 0x2D00;
        public const ushort PROTOCOL_ACCESSORY_ADB = 0x2D01;

        public const uint USB_DIR_OUT = 0x00;
        public const uint USB_TYPE_VENDOR = 0x40;
        public const uint USB_DIR_IN = 0x80;

        public const int VENDOR_ID_SAMSUNG = 0x04e8;
        public const int PRODUCT_ID_SAMSUNG = 0x6860;

        public const ushort VENDOR_ID_GOOGLE = 0x18D1;

        public const int ERROR_SEM_TIMEOUT = 121;
    }
}
