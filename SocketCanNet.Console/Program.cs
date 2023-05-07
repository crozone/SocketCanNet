using System;
using System.Net.Sockets;

namespace SocketCanNet.CLI
{
    internal class Program
    {
        // Supports SocketCAN with CAN 2.0B.
        // See https://en.wikipedia.org/wiki/SocketCAN
        // See https://elinux.org/Bringing_CAN_interface_up#Virtual_Interfaces
        // See Linux can-utils for Linux utilities: https://github.com/linux-can/can-utils

        // Set bitrate (eg 125000 or 250000): ip link set can0 type can bitrate 125000
        // Bring up interface: ip link set can0 up
        // Verify: ip link show can0
        // eg 2: can0: <NOARP,UP,LOWER_UP,ECHO> mtu 16 qdisc pfifo_fast state UP mode DEFAULT group default qlen 1000
        //    link/can

        static int Main(string[] args)
        {
            Console.WriteLine("Hello, World!");

            string? canInterface = args.Length > 0 ? args[0] : null;

            if (canInterface != null)
            {
                Console.WriteLine($"Using CAN interface {canInterface}");
            }
            else
            {
                Console.WriteLine("CAN interface name must be given as first argument");
                return 1;
            }

            Console.WriteLine("Creating RAW CAN socket ...");
            Socket socket = CanSocket.CreateRawCanSocket();

            Console.WriteLine("Enabling CAN FD ...");
            socket.SetCanFdEnabled(true);

            Console.WriteLine($"Binding to CAN interface {canInterface} ...");

            try
            {
                socket.BindToCanInterface(canInterface);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not bind to interface: {ex.Message}");
                return 2;
            }

            Console.WriteLine($"Ready to receive CAN frames");

            while (true)
            {
                CanFrame canFrame = socket.ReceiveCanFrame();
                Console.WriteLine($"[{canFrame.Id}]: ({canFrame.PayloadLength}) {Convert.ToHexString(canFrame.PayloadSpan)}");
            }
        }
    }
}