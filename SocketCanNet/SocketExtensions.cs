using System;
using System.Buffers;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using static SocketCanNet.Constants;

namespace SocketCanNet
{
    // Reference: https://www.kernel.org/doc/Documentation/networking/can.txt

    public static class SocketExtensions
    {
        /// <summary>
        /// Send a CAN frame.
        /// </summary>
        /// <param name="canFrame"></param>
        public static void SendCanFrame(this Socket socket, ValueCanFrame canFrame)
        {
            socket.Send(canFrame.RawFrame, SocketFlags.None);
        }

        /// <summary>
        /// Send a CAN frame.
        /// </summary>
        /// <param name="canFrame"></param>
        public static void SendCanFrame(this Socket socket, CanFrame canFrame)
        {
            socket.Send(canFrame.ValueCanFrame.RawFrame, SocketFlags.None);
        }


        /// <summary>
        /// Send a CAN frame.
        /// </summary>
        /// <param name="canFrame"></param>
        public static async ValueTask SendCanFrameAsync(this Socket socket, CanFrame canFrame, CancellationToken cancellationToken = default)
        {
            await socket.SendAsync(canFrame.RawFrameMemory, SocketFlags.None, cancellationToken);
        }

        /// <summary>
        /// Receive a CAN frame.
        /// This method internally allocates an array of size <see cref="CanFrame.MaximumFrameSize"/> to use as the backing buffer.
        /// </summary>
        /// <returns>The received CAN frame</returns>
        public static CanFrame ReceiveCanFrame(this Socket socket)
        {
            byte[] receiveBuffer = new byte[CanFrame.MaximumFrameSize];
            int bytesRead = socket.Receive(receiveBuffer, SocketFlags.None);
            return new CanFrame(receiveBuffer[..bytesRead]);
        }

        /// <summary>
        /// Receive a CAN frame.
        /// This method returns a stack-only ref struct and does not allocate. It is intended for high performance scenarios.
        /// </summary>
        /// <param name="receiveBuffer">Buffer that will receive CAN frame data.
        /// If CAN FD frames are disabled, the buffer must be at least <see cref="ValueCanFrame.StandardFrameSize"/> in length.
        /// If CAN FD frames are enabled, the buffer must be at least <see cref="ValueCanFrame.CanFdFrameSize"/> in length.
        /// </param>
        /// <returns>The received CAN frame as a ValueCanFrame ref struct</returns>
        public static ValueCanFrame ReceiveCanFrame(this Socket socket, Span<byte> receiveBuffer)
        {
            int bytesRead = socket.Receive(receiveBuffer, SocketFlags.None);
            return new ValueCanFrame(receiveBuffer[..bytesRead]);
        }

        /// <summary>
        /// Receive a CAN frame.
        /// This method internally allocates an array of size <see cref="CanFrame.MaximumFrameSize"/> to use as the backing buffer.
        /// </summary>
        /// <returns>The received CAN frame</returns>
        public static async ValueTask<CanFrame> ReceiveCanFrameAsync(this Socket socket, CancellationToken cancellationToken = default)
        {
            byte[] receiveBuffer = new byte[CanFrame.MaximumFrameSize];
            return await socket.ReceiveCanFrameAsync(receiveBuffer, cancellationToken);
        }

        /// <summary>
        /// Receive a CAN frame.
        /// </summary>
        /// <param name="receiveBuffer">Memory that will receive CAN frame data.
        /// If CAN FD frames are disabled, the buffer must be at least <see cref="CanFrame.StandardFrameSize"/> in length.
        /// If CAN FD frames are enabled, the buffer must be at least <see cref="CanFrame.CanFdFrameSize"/> in length.
        /// </param>
        /// <returns>The received CAN frame</returns>
        public static async ValueTask<CanFrame> ReceiveCanFrameAsync(this Socket socket, Memory<byte> receiveBuffer, CancellationToken cancellationToken = default)
        {
            int bytesRead = await socket.ReceiveAsync(receiveBuffer, SocketFlags.None, cancellationToken);
            return new CanFrame(receiveBuffer[..bytesRead]);
        }

        /// <summary>
        /// Bind to a CAN interface by its adapter name.
        /// </summary>
        /// <param name="interfaceName">The interface name. For example, "eth0".</param>
        public static void BindToCanInterface(this Socket socket, string interfaceName)
        {
            int interfaceIndex = socket.GetUnixInterfaceIndex(interfaceName);
            CanSocketEndPoint canSocketEndPoint = new CanSocketEndPoint(interfaceIndex);
            socket.Bind(canSocketEndPoint);
        }

        /// <summary>
        /// Gets the if_index of a network interface from the adapter name, using ioctl SIOCGIFINDEX.
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="adapterName"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public static int GetUnixInterfaceIndex(this Socket socket, string adapterName)
        {
            // The maximum adapter name size is IFNAMSIZ - 1, to allow room for the null terminator character
            const int MaxNameSize = IFNAMSIZ - 1;
            int deviceNameByteLength = Encoding.UTF8.GetByteCount(adapterName);
            if (deviceNameByteLength > MaxNameSize) throw new ArgumentException($"{nameof(adapterName)} to long should be max {MaxNameSize} chars, canAdapter", adapterName);

            // The adapter name should fit in the first 16 bytes
            Span<byte> data = stackalloc byte[32];

            /*
                struct ifreq {
                    char ifr_name[IFNAMSIZ]; // In Linux, IFNAMSIZ = 16 bytes. Must include a null terminator.
                    union { // Union can be any one of the types below. Largest value is 16 bytes, so the union is 16 bytes.
                        struct sockaddr ifr_addr;
                        struct sockaddr ifr_dstaddr;
                        struct sockaddr ifr_broadaddr;
                        struct sockaddr ifr_netmask;
                        struct sockaddr ifr_hwaddr;
                        short           ifr_flags;
                        int             ifr_ifindex;
                        int             ifr_metric;
                        int             ifr_mtu;
                        struct ifmap    ifr_map;
                        char            ifr_slave[IFNAMSIZ];
                        char            ifr_newname[IFNAMSIZ];
                        char           *ifr_data;
                    };
                };
            */

            // First, write in ifr_name in the first 16 bytes of ifreq

            // ifr_name:
            // * The name must not be empty
            // * The name must be less than 16 (IFNAMSIZ) characters, including the null terminator
            // * Can apparently be any byte sequence that doesn't contain \0, so can contain UTF8, but tools may not handle it.

            // Only write bytes 0 through 14 to allow space for a null terminator at index 15
            deviceNameByteLength = Encoding.UTF8.GetBytes(adapterName, data[..MaxNameSize]);
            data[deviceNameByteLength..].Fill(0); // Null terminate the rest of the string, and zero out the rest of the struct

            // Call ioctl to execute the SIOCGIFINDEX request to get name -> if_index mapping
            // See SIOCGIFINDEX in https://man7.org/linux/man-pages/man7/netdevice.7.html
            int result = NativeInterop.Ioctl(socket.Handle.ToInt32(), SIOCGIFINDEX, data);
            // ioctl() returns -1 on error, with errno set
            if (result == -1)
            {
                int error = Marshal.GetLastPInvokeError();
                // TODO: There are probably more common SIOCGIFINDEX errors we should catch and throw specific exceptions for
                if (error == ENODEV)
                {
                    throw new KeyNotFoundException($"No such device {adapterName}");
                }
                else
                {
                    // Throw a more generic error for all the other errors
                    throw new InvalidOperationException($"ioctl SIOCGIFINDEX failed: {error}");
                }
            }

            // Read ifr_ifindex. It's a 32 bit integer at index 16.
            return BitConverter.ToInt32(data.Slice(IFNAMSIZ, 4));
        }

        /// <summary>
        /// Enables or disables receiving looped back CAN messages that originated from this socket,
        /// using the RAW socket option CAN_RAW_RECV_OWN_MSGS.
        /// 
        /// This is disabled by default.
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="enabled"></param>
        public static void SetCanReceiveOwnMessages(this Socket socket, bool enable)
        {
            Span<byte> optVal = stackalloc byte[4];
            BitConverter.TryWriteBytes(optVal, enable ? 1 : 0);
            socket.SetRawSocketOption((int)CanSocketOptionLevel.SOL_CAN_RAW, (int)CanRawSocketOptionName.CAN_RAW_RECV_OWN_MSGS, optVal);
        }

        /// <summary>
        /// Enables or disables local loopback of CAN messages on the socket,
        /// using the RAW socket option CAN_RAW_LOOPBACK.
        /// 
        /// This is enabled by default.
        /// 
        /// When enabled, all the sent CAN frames are looped back to the open CAN sockets
        /// that registered for the CAN frames' CAN-ID on this given interface.
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="enable"></param>
        public static void SetCanLoopback(this Socket socket, bool enable)
        {
            Span<byte> optVal = stackalloc byte[4];
            BitConverter.TryWriteBytes(optVal, enable ? 1 : 0);
            socket.SetRawSocketOption((int)CanSocketOptionLevel.SOL_CAN_RAW, (int)CanRawSocketOptionName.CAN_RAW_LOOPBACK, optVal);
        }

        /// <summary>
        /// Enables or disables CAN FD MTU frames, using the RAW socket option CAN_RAW_FD_FRAMES.
        /// 
        /// This option is disabled by default.
        /// 
        /// When enabled, CAN_MTU and CANFD_MTU frames are allowed.
        /// When disabled, only CAN_MTU frames are allowed.
        /// 
        /// When the socket option is not supported by the CAN_RAW socket (e.g.on older kernels),
        /// switching the CAN_RAW_FD_FRAMES option returns the error -ENOPROTOOPT.
        /// 
        /// Once CAN_RAW_FD_FRAMES is enabled the application can send both CAN frames
        /// and CAN FD frames. Both CAN and CAN FD frames must be handled when reading
        /// from the socket.
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="enable"></param>
        public static void SetCanFdEnabled(this Socket socket, bool enable)
        {
            Span<byte> optVal = stackalloc byte[4];
            BitConverter.TryWriteBytes(optVal, enable ? 1 : 0);
            socket.SetRawSocketOption((int)CanSocketOptionLevel.SOL_CAN_RAW, (int)CanRawSocketOptionName.CAN_RAW_FD_FRAMES, optVal);
        }

        // TODO:    4.1.1 RAW socket option CAN_RAW_FILTER
        // TODO:    4.1.2 RAW socket option CAN_RAW_ERR_FILTER
    }
}
