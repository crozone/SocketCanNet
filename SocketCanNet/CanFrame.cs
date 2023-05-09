using System;

namespace SocketCanNet
{
    /// <summary>
    /// Represents a CAN frame backed by a memory buffer.
    /// </summary>
    public class CanFrame
    {
        public const int StandardFrameSize = Constants.CAN_MTU;
        public const int CanFdFrameSize = Constants.CANFD_MTU;
        public const int MaximumFrameSize = CanFdFrameSize;

        public Memory<byte> RawFrameMemory;

        public CanFrame(bool canFd) : this(new byte[canFd ? CanFdFrameSize : StandardFrameSize].AsMemory()) { }

        public CanFrame(Memory<byte> rawFrame)
        {
            if (rawFrame.Length != StandardFrameSize && rawFrame.Length != CanFdFrameSize)
            {
                throw new ArgumentException($"CAN frame length must be {StandardFrameSize} for CAN or {CanFdFrameSize} for CAN FD", nameof(rawFrame));
            }

            RawFrameMemory = rawFrame;
        }

        public Memory<byte> CanIdMemory => RawFrameMemory[..4];
        public Memory<byte> DataMemory => RawFrameMemory[ValueCanFrame.FrameStart..];
        public Memory<byte> PayloadMemory => DataMemory[..Math.Min(PayloadLength, DataMemory.Length)];

        public ValueCanFrame ValueCanFrame => new ValueCanFrame(RawFrameMemory.Span);

        public bool IsCanFd => ValueCanFrame.IsCanFd;

        /// <summary>
        /// The value of the CAN ID field, with the flags included in the high order bits
        /// </summary>
        public uint RawId {
            get => ValueCanFrame.RawId;
            set {
                ValueCanFrame valueCanFrame = this.ValueCanFrame;
                valueCanFrame.RawId = value;
            }
        }

        /// <summary>
        /// The CAN message ID
        /// </summary>
        public int Id {
            get => ValueCanFrame.Id;
            set {
                ValueCanFrame valueCanFrame = this.ValueCanFrame;
                valueCanFrame.Id = value;
            }
        }

        public int PayloadLength {
            get => ValueCanFrame.PayloadLength;
            set {
                ValueCanFrame valueCanFrame = this.ValueCanFrame;
                valueCanFrame.PayloadLength = value;
            }
        }

        public bool IsErrorMessage {
            get => ValueCanFrame.IsErrorMessage;
            set {
                ValueCanFrame valueCanFrame = this.ValueCanFrame;
                valueCanFrame.IsErrorMessage = value;
            }
        }

        public bool IsRemoteTransmissionRequest {
            get => ValueCanFrame.IsRemoteTransmissionRequest;
            set {
                ValueCanFrame valueCanFrame = this.ValueCanFrame;
                valueCanFrame.IsRemoteTransmissionRequest = value;
            }
        }

        public bool IsExtendedFrame {
            get => ValueCanFrame.IsExtendedFrame;
            set {
                ValueCanFrame valueCanFrame = this.ValueCanFrame;
                valueCanFrame.IsExtendedFrame = value;
            }
        }

        public byte CanFdFlags {
            get => ValueCanFrame.CanFdFlags;
            set {
                ValueCanFrame valueCanFrame = this.ValueCanFrame;
                valueCanFrame.CanFdFlags = value;
            }
        }
    }
}
