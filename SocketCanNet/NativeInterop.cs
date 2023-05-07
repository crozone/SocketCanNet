using System;
using System.Runtime.InteropServices;

namespace SocketCanNet
{
    internal static class NativeInterop
    {
        /// <summary>
        /// The ioctl() system call manipulates the underlying device parameters of special files.
        /// In particular, many operating characteristics of character special files (e.g.terminals)
        /// may be controlled with ioctl() requests.
        /// </summary>
        /// <param name="fd">The IOCTL file descriptor. Must be an open file descriptor.
        /// This is always a 32 bit integer, regardless of platform.</param>
        /// <param name="cmd">Device-dependent request code. This is an "unsigned long", aka unsigned 32 bit int.</param>
        /// <param name="data">Pointer to memory containing request data.</param>
        /// <returns>-1 indicates error. The errno will be set.</returns>
        [DllImport("libc", EntryPoint = "ioctl", ExactSpelling = true, SetLastError = true)]
        public static extern unsafe int Ioctl(int fd, uint request, byte* data);

        /// <summary>
        /// The ioctl() system call manipulates the underlying device parameters of special files.
        /// In particular, many operating characteristics of character special files (e.g.terminals)
        /// may be controlled with ioctl() requests.
        /// </summary>
        /// <param name="fd">The IOCTL file descriptor. Must be an open file descriptor.
        /// This is always a 32 bit integer, regardless of platform.</param>
        /// <param name="cmd">Device-dependent request code. This is an "unsigned long", aka unsigned 32 bit int.</param>
        /// <param name="data">Span containing request data.</param>
        /// <returns>-1 indicates error. The errno will be set.</returns>
        public static unsafe int Ioctl(int fd, uint request, Span<byte> data)
        {
            fixed (byte* dataPtr = data)
            {
                return Ioctl(fd, request, dataPtr);
            }
        }
        // Use LibraryImport once we can upgrade > .NET 6
        //[LibraryImport("libc", EntryPoint = "ioctl", SetLastError = true)]
        //public static partial int Ioctl(int fd, uint request, Span<byte> data);
    }
}
