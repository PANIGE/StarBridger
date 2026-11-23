
using Newtonsoft.Json;
using StarBridger.Contracts.Enums;
using StarBridger.Contracts.IPC.MainBus;
using System.IO.Pipes;
using System.Text;
using System.Threading.Channels;

internal sealed class IpcSession
{
  private readonly Channel<Packet> _outgoing = Channel.CreateBounded<Packet>(
      new BoundedChannelOptions(128)
      {
        SingleReader = true,
        SingleWriter = false,
        FullMode = BoundedChannelFullMode.Wait
      });

  public Guid PipeID { get; } = Guid.NewGuid();

  public Intents Intents { get; set; } = Intents.All;

  public NamedPipeServerStream Pipe { get; }

  public CancellationToken StoppingToken { get; }

  public bool Available =>
    Pipe.IsConnected &&
    !StoppingToken.IsCancellationRequested;

  public IpcSession(NamedPipeServerStream pipe, CancellationToken stoppingToken)
  {
    Pipe = pipe;
    StoppingToken = stoppingToken;
  }

  public bool Send(object? payload, Intents type, bool force = false)
  {
    if (!Available)
    {
      return false;
    }

    if (!force && (type == Intents.None || !Intents.HasFlag(type)))
    {
      return false;
    }

    return _outgoing.Writer.TryWrite(new Packet
    {
      Type = type,
      Payload = payload
    });
  }

  public async Task ProcessAsync()
  {
    using StreamWriter writer = new(Pipe, new UTF8Encoding(false), 1024, leaveOpen: true)
    {
      AutoFlush = true
    };

    await foreach (Packet packet in _outgoing.Reader.ReadAllAsync(StoppingToken))
    {
      string json = JsonConvert.SerializeObject(packet);

      await writer.WriteLineAsync(
        json.AsMemory(),
        StoppingToken);
    }
  }
}
