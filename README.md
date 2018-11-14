# TcpFramework
High-performance and Async TCP networking library for .NET 

Some notable features :
* Built at top of 
[SocketAsyncEventArgs](https://docs.microsoft.com/en-us/dotnet/api/system.net.sockets.socketasynceventargs)
* Lightweight ( no dependencies on libararies like [libuv](https://github.com/libuv/libuv) or [libev](https://github.com/enki/libev) )
* Based on configurable object pooling
* Use's new .NET features such as ValueTask and ValueTuple to prevent memory allocations as possible
* Compatible with low-level socket APIs such as Socket.Shutdown (half-open sockets), Socket.IsBlocking (non-blocking sockets) and SocketFlags
* Provides fluent API

## NuGet
Available on NuGet at https://www.nuget.org/packages/TcpFramework

## TODO
This project is at very begining but the core feature which is client send\receive is complete.
