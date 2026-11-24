using StarBridger.Contracts.Core.Shell;
using StarBridger.Contracts.IPC.MainBus;
using StarBridger.Contracts.IPC.Shell;
using System;
using System.Collections.Generic;
using System.Text;

namespace StarBridger.SDK.Clients;

internal interface IStarBridgerConnection : IAsyncDisposable
{
  bool IsConnected { get; }

  Guid? SessionId { get; set; }

  event Func<Packet, CancellationToken, Task>? MainBusReceived;

  Task ConnectAsync(CancellationToken cancellationToken = default);

  Task DisconnectAsync();

  IAsyncEnumerable<ShellResponse> ExecuteShellAsync(Command command, CancellationToken cancellationToken = default);
  
}
