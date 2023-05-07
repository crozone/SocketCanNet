using System;
using System.Net;
using System.Net.Sockets;

using static SocketCanNet.Constants;

namespace SocketCanNet
{
    public class CanSocketEndPoint : EndPoint
    {

        /*
            struct sockaddr_can {
	            __kernel_sa_family_t can_family;
	            int         can_ifindex;
	            union {
		            // transport protocol class address information (e.g. ISOTP)
		            struct { canid_t rx_id, tx_id; } tp;

		            // J1939 address information
		            struct {
			            // 8 byte name when using dynamic addressing
			            __u64 name;

			                //* pgn:
			                //* 8 bit: PS in PDU2 case, else 0
			                //* 8 bit: PF
			                //* 1 bit: DP
			                //* 1 bit: reserved
			                //*
			            __u32 pgn;

			            // 1 byte address
			            __u8 addr;
		            } j1939;

		            // reserved for future CAN protocols address information
	            } can_addr;
            };
        */
        private int interfaceId;

        public CanSocketEndPoint(int interfaceId)
        {
            this.interfaceId = interfaceId;
        }

        internal CanSocketEndPoint(SocketAddress socketAddress)
        {
            ArgumentNullException.ThrowIfNull(socketAddress);

            if (socketAddress.Family != CanAddressFamily ||
                socketAddress.Size != SOCKADDR_CAN_SIZE)
            {
                throw new ArgumentOutOfRangeException(nameof(socketAddress));
            }

            Span<byte> interfaceIdBytes = stackalloc byte[4];

            for(int i = 0; i < interfaceIdBytes.Length; i++) {
                interfaceIdBytes[i] = socketAddress[4 + i];
            }

            interfaceId = BitConverter.ToInt32(interfaceIdBytes);
        }

        public override AddressFamily AddressFamily => CanAddressFamily;

        public override EndPoint Create(SocketAddress socketAddress) => new CanSocketEndPoint(socketAddress);

        public override SocketAddress Serialize()
        {
            // The first two bytes of the socket address buffer are set from the AddressFamily by
            // TryConvertAddressFamilyPalToPlatform
            // https://github.com/dotnet/runtime/blob/2e17200fc6782beac0b63c290628dbf79ff13650/src/native/libs/System.Native/pal_networking.c#L695-L710
            var result = new SocketAddress(CanAddressFamily, SOCKADDR_CAN_SIZE);

            Span<byte> addressBuffer = stackalloc byte[SOCKADDR_CAN_SIZE];
            GetCanInterfaceAddress(interfaceId, addressBuffer);

            // The SocketAddress source states that for the indexer:
            //
            // "Access to unmanaged serialized data. This doesn't
            // allow access to the first 2 bytes of unmanaged memory
            // that are supposed to contain the address family which
            // is readonly."
            //
            // However, there doesn't actually appear to be any restriction. This may be a left over comment or a bug.
            // In any case, we'll just skip over the first 2 bytes anyway.

            for (int index = 2; index < addressBuffer.Length; index++)
            {
                result[index] = addressBuffer[index];
            }

            return result;
        }

        public static void GetCanInterfaceAddress(int interfaceId, Span<byte> destination)
        {
            if (destination.Length < SOCKADDR_CAN_SIZE) throw new ArgumentException($"{nameof(destination)} must be at least {SOCKADDR_CAN_SIZE} bytes in length");

            Span<byte> address = destination[0..SOCKADDR_CAN_SIZE];

            // Pre-clear the buffer
            address.Fill(0);

            BitConverter.TryWriteBytes(address[..2], AF_CAN);
            // There are 2 bytes of padding for struct alignment in sockaddr_can
            BitConverter.TryWriteBytes(address[4..8], interfaceId);
            // The final 16 bytes are can_addr, which are left as zero.
        }
    }
}
