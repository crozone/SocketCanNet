# SocketCanNet

Small SocketCAN library for working with Controller Area Network (CAN bus) sockets on Linux with .NET.

## Overview

This library assists with the creation and usage of the [Controller Area Network Protocol Family (aka SocketCAN)](https://www.kernel.org/doc/Documentation/networking/can.txt) sockets available in Linux, on .NET 6 and above.

It allows for easy creation of CAN sockets, binding of CAN sockets by interface name, and sending and receiving of CAN messages using a helper class for serialization and deserialization.

Both CAN and CAN-FD are supported.

## Example

```csharp
// Create the CAN socket
Socket socket = CanSocket.CreateRawCanSocket();

// (Optional) Enable CAN FD
socket.SetCanFdEnabled(true);

// Bind the socket to the CAN interface "can0"
socket.BindToCanInterface("can0");

// Block until a CAN frame is received
CanFrame canFrame = socket.ReceiveCanFrame();

// (An async implementation is also available)
// CanFrame canFrame = socket.ReceiveCanFrameAsync(cancellationToken);

// Print the CAN frame ID and Payload as hex
Console.WriteLine($"[{canFrame.Id}]: ({canFrame.DataLength}) {Convert.ToHexString(canFrame.PayloadSpan)}");

```

## Helper methods

### `CanSocket.CreateRawCanSocket()`

Creates a raw `Controller Area Network` socket.

Equivalent to:

```csharp
new Socket(AddressFamily.ControllerAreaNetwork, SocketType.Raw, ProtocolType.Raw);
```

## Extension methods

### `void Socket.BindToCanInterface(string interfaceName)`

Binds the socket to the SocketCAN interface with the name `interfaceName`

### `void Socket.SendCanFrame(CanFrame canFrame)` and `Task Socket.SendCanFrameAsync(CanFrame canFrame, CancellationToken cancellationToken)`

Sends the serialized `CanFrame` over the socket.

### `CanFrame Socket.ReceiveCanFrame` and `Task<CanFrame> Socket.ReceiveCanFrameAsync(CancellationToken cancellationToken`

Receives a `CanFrame` from the socket.

### `void Socket.SetCanFdEnabled(bool enable)`

Enables or disables CAN FD MTU frames, using the RAW socket option CAN_RAW_FD_FRAMES.

This option is disabled by default.

When enabled, CAN_MTU and CANFD_MTU frames are allowed.
When disabled, only CAN_MTU frames are allowed.

Once CAN_RAW_FD_FRAMES is enabled the application can send both CAN frames and CAN FD frames.

### `void Socket.SetCanLoopback(bool enable)`

Enables or disables local loopback of CAN messages on the socket, using the RAW socket option CAN_RAW_LOOPBACK.

This is enabled by default.

When enabled, all the sent CAN frames are looped back to the open CAN sockets that registered for the CAN frames' CAN-ID on this given interface.

### `void SetCanReceiveOwnMessages(bool enable)`

Enables or disables receiving looped back CAN messages that originated from this socket, using the RAW socket option CAN_RAW_RECV_OWN_MSGS.

This is disabled by default.

### `int Socket.GetUnixInterfaceIndex(string adapterName)`

Gets the index of the network interface with name `adapterName`, using the SIOCGIFINDEX `ioctl()`.

This allows the manual creation of an `EndPoint` for the given interface:

```csharp
int interfaceIndex = socket.GetUnixInterfaceIndex(interfaceName);
CanSocketEndPoint canSocketEndPoint = new CanSocketEndPoint(interfaceIndex);
```

## Helper classes

### `CanSocketEndPoint`

Represents a SocketCAN interface endpoint. This class facilitiates binding a socket to a SocketCAN interface, using its interface index.

Internally, it serializes the interface index into an `sockaddr_can` structure for use by `Bind()`.

### `CanFrame`

Wraps a raw CAN frame stored as a byte array and allows easy serialization and deserialization of the various CAN frame fields.
