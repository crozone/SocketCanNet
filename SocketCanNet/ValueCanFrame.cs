using System;

namespace SocketCanNet
{
    /// <summary>
    /// Represents a CAN frame backed by a span buffer.
    /// This is a ref struct, and as such can only exist on the stack.
    /// </summary>
    public ref struct ValueCanFrame
    {
        /*
        struct can_frame {
            canid_t can_id;  // 29 bit CAN_ID + EFF/RTR/ERR flags
            __u8 can_dlc; // frame payload length in byte (0 .. 8)
            __u8 __pad;   // padding
            __u8 __res0;  // reserved / padding
            __u8 __res1;  // reserved / padding
            __u8 data[8] __attribute__((aligned(8))); // Start offset = 8
        };
        */

        /*
            struct canfd_frame {
                canid_t can_id;  // 29 bit CAN_ID + EFF/RTR/ERR flags
                __u8 len;     // frame payload length in byte (0 .. 64)
                __u8 flags;   // additional flags for CAN FD
                __u8 __res0;  // reserved / padding
                __u8 __res1;  // reserved / padding
                __u8 data[64] __attribute__((aligned(8))); // Start offset = 8
            };
        */

        public const int StandardFrameSize = Constants.CAN_MTU;
        public const int CanFdFrameSize = Constants.CANFD_MTU;
        public const int MaximumFrameSize = CanFdFrameSize;

        public Span<byte> RawFrame;

        public ValueCanFrame(Span<byte> rawFrame)
        {
            if(rawFrame.Length != StandardFrameSize && rawFrame.Length != CanFdFrameSize)
            {
                throw new ArgumentException($"CAN frame length must be {StandardFrameSize} for CAN or {CanFdFrameSize} for CAN FD", nameof(rawFrame));
            }

            RawFrame = rawFrame;
        }

        /*
         * canid_t: Controller Area Network Identifier structure
         *
         * bit 0-28 | 0x1FFFFFFF: CAN identifier (11/29 bit)
         * bit 29	| 0x20000000: error message frame flag (0 = data frame, 1 = error message) (ERR)
         * bit 30	| 0x40000000: remote transmission request flag (1 = rtr frame) (RTR)
         * bit 31	| 0x80000000: frame format flag (0 = standard 11 bit, 1 = extended 29 bit) (SFF or EFF)
         */

        /// <summary>
        /// Mask for the CAN ID in the can_id field
        /// </summary>
        private const uint CanIdMask = 0x1FFF_FFFF;

        /// <summary>
        /// Mask for the error message frame flag in the can_id field
        /// </summary>
        private const uint CanIdErrMask = 0x2000_0000;

        /// <summary>
        /// Mask for the remote transmission request flag in the can_id field
        /// </summary>
        private const uint CanIdRtrMask = 0x4000_0000;

        /// <summary>
        /// Mask for the frame format flag in the can_id field
        /// </summary>
        private const uint CanIdEffMask = 0x8000_0000;

        internal const int FrameStart = 8;

        public Span<byte> CanIdSpan => RawFrame[..4];
        public Span<byte> DataSpan => RawFrame[FrameStart..];
        public Span<byte> PayloadSpan => DataSpan[..Math.Min(PayloadLength, DataSpan.Length)];

        public bool IsCanFd => RawFrame.Length == CanFdFrameSize;

        /// <summary>
        /// The value of the CAN ID field, with the flags included in the high order bits
        /// </summary>
        public uint RawId {
            get => BitConverter.ToUInt32(CanIdSpan);
            set => BitConverter.TryWriteBytes(CanIdSpan, value);
        }

        /// <summary>
        /// The CAN message ID
        /// </summary>
        public int Id {
            get => (int)(RawId & CanIdMask); // 3 highest order bits are EFF/RTR/ERR flags
            set => RawId = (RawId & ~CanIdMask) | ((uint)value & CanIdMask);
        }

        public int PayloadLength {
            get => RawFrame[4];
            set {
                if (value < 0) throw new ArgumentOutOfRangeException(nameof(value), "CAN frame data length must be greater than zero");
                if (!IsCanFd && value > 8) throw new ArgumentOutOfRangeException(nameof(value), "CAN frame must be between 0-8 bytes");
                if (IsCanFd && value > 64) throw new ArgumentOutOfRangeException(nameof(value), "CAN FD frame must be between 0-64 bytes");

                RawFrame[4] = (byte)value;
                RawFrame[(8 + value)..].Fill(0);
            }
        }

        public bool IsErrorMessage {
            get => (RawId & CanIdErrMask) != 0;
            set => RawId = (RawId & ~CanIdErrMask) | (value ? CanIdErrMask : 0);
        }

        public bool IsRemoteTransmissionRequest {
            get => (RawId & CanIdRtrMask) != 0;
            set {
                RawId = (RawId & ~CanIdRtrMask) | (value ? CanIdRtrMask : 0);
                if (value)
                {
                    PayloadLength = 0;
                }
            }
        }

        public bool IsExtendedFrame {
            get => ((RawId & CanIdEffMask) != 0) || (Id > 0x7FF);
            set => RawId = (RawId & ~CanIdEffMask) | (value ? CanIdEffMask : 0);
        }

        public byte CanFdFlags {
            get => RawFrame[5];
            set => RawFrame[5] = value;
        }
    }
}
