# TcpFramework
High-performance and Async TCP networking library for .NET 

Some notable features :
* Built at top of 
[SocketAsyncEventArgs](https://docs.microsoft.com/en-us/dotnet/api/system.net.sockets.socketasynceventargs)
* Lightweight ( no dependencies on libararies like [libuv](https://github.com/libuv/libuv) or [libev](https://github.com/enki/libev) )
* Based on configurable object pooling (No heap fragmentation)
* Use's new .NET features such as ValueTask and ValueTuple to prevent memory allocations as possible
* Compatible with low-level socket APIs such as Socket.Shutdown (half-open sockets), Socket.IsBlocking (non-blocking sockets) and SocketFlags
* Provides fluent API

## Supported Runtimes
- .NET Framework 4.5
- .NET Standard (1.3 and 2.0) 
- .NET Core 2.1 (recommended)

## Documentation (Getting started)
Visit wiki to read [documentation and getting started](https://github.com/moien007/TcpFramework/wiki/Getting-started)

## NuGet
Available on NuGet at https://www.nuget.org/packages/TcpFramework

## TODO
- Add documentation
- Comment code lines
