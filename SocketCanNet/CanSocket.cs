using System.Net.Sockets;

using static SocketCanNet.Constants;

namespace SocketCanNet
{
    public static class CanSocket
    {
        /// <summary>
        /// Creates a Controller Area Network socket with a raw socket type and CAN_RAW protocol type.
        /// </summary>
        /// <returns></returns>
        public static Socket CreateRawCanSocket() => new Socket(CanAddressFamily, CanRawSocketType, CanRawProtocol);
    }
}
