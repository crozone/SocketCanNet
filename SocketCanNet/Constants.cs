using System.Net.Sockets;

namespace SocketCanNet
{
    public static class Constants
    {
        public enum CanSocketOptionLevel
        {
            SOL_CAN_RAW = 101
        }

        public enum CanRawSocketOptionName
        {
            CAN_RAW_FILTER = 1,
            CAN_RAW_ERR_FILTER = 2,
            CAN_RAW_LOOPBACK = 3,
            CAN_RAW_RECV_OWN_MSGS = 4,
            CAN_RAW_FD_FRAMES = 5,
        }

        /// <summary>
        /// Legacy CAN frame maximum transmission unit
        /// </summary>
        public const int CAN_MTU = 16;

        /// <summary>
        /// CAN FD frame maximum transmission unit
        /// </summary>
        public const int CANFD_MTU = 72;

        /// <summary>
        /// SIOCGIFINDEX name -> if_index mapping ioctl request for socket configuration control.
        /// 
        /// See <see href="https://github.com/torvalds/linux/blob/1a5304fecee523060f26e2778d9d8e33c0562df3/include/uapi/linux/sockios.h#L85">ioctls.h</see>
        /// </summary>
        public const int SIOCGIFINDEX = 0x8933;

        /// <summary>
        /// No such device errno value.
        /// 
        /// See <see href="https://github.com/torvalds/linux/blob/78b421b6a7c6dbb6a213877c742af52330f5026d/include/uapi/asm-generic/errno-base.h#L23">errno-base.h</see>
        /// </summary>
        public const int ENODEV = 19;

        /// <summary>
        /// The size of the sockaddr_can structure
        /// 
        /// See <see href="https://github.com/torvalds/linux/blob/1a5304fecee523060f26e2778d9d8e33c0562df3/include/uapi/linux/can.h#L241-L267">can.h</see>
        /// </summary>
        public const int SOCKADDR_CAN_SIZE = 24;

        // https://github.com/torvalds/linux/blob/1a5304fecee523060f26e2778d9d8e33c0562df3/include/uapi/linux/if.h#L33
        public const int IFNAMSIZ = 16;

        /// <summary>
        /// AF_FAMILY value for Controller Area Network.
        /// 
        /// See <see href="https://github.com/torvalds/linux/blob/1a5304fecee523060f26e2778d9d8e33c0562df3/include/linux/socket.h#L219">socket.h</see>
        /// </summary>
        public const ushort AF_CAN = 29;

        /// <summary>
        /// Protocol family value for Controller Area Network
        /// 
        /// See <see href="https://github.com/torvalds/linux/blob/1a5304fecee523060f26e2778d9d8e33c0562df3/include/linux/socket.h#L276">socket.h</see>
        /// </summary>
        public const ushort PF_CAN = AF_CAN;

        // https://github.com/torvalds/linux/blob/1a5304fecee523060f26e2778d9d8e33c0562df3/arch/mips/include/asm/socket.h#LL32C11-L32C11
        public const int SOCK_RAW = 3;

        // https://github.com/torvalds/linux/blob/1a5304fecee523060f26e2778d9d8e33c0562df3/include/uapi/linux/can.h#L224
        public const int CAN_RAW = 1;

        // AddressFamily.ControllerAreaNetwork has a value of 65537, however this is converted by the runtime into AF_CAN (29)
        // by an internal function called TryConvertAddressFamilyPalToPlatform:
        // https://github.com/dotnet/runtime/blob/2e17200fc6782beac0b63c290628dbf79ff13650/src/native/libs/System.Native/pal_networking.c#L209C13-L24
        //
        // TryConvertAddressFamilyPalToPlatform traps known values of AddressFamily that have a different under Unix compare to Windows.
        // The special cased AddressFamily constants are defined here:
        // https://github.com/dotnet/runtime/blob/2e17200fc6782beac0b63c290628dbf79ff13650/src/native/libs/System.Native/pal_networking.h#L53-L70
        //
        // Since AddressFamily.ControllerAreaNetwork is special cased, we don't have to pass in the value of AF_CAN explicitly.
        // This is lucky, since if the value isn't caught by TryConvertAddressFamilyPalToPlatform, it returns false and an
        // exception is thrown when trying to create the socket.
        //
        public const AddressFamily CanAddressFamily = AddressFamily.ControllerAreaNetwork; // -> AF_CAN
        public const SocketType CanRawSocketType = SocketType.Raw; // -> SOCK_RAW
        public const ProtocolType CanRawProtocol = ProtocolType.Raw; // -> CAN_RAW;
    }
}
