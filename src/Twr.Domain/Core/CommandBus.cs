using Twr.Domain.Contracts;
namespace Twr.Domain.Core;
public sealed class CommandBus { private readonly Queue<IGameCommand> _queue=new(); public void Enqueue(IGameCommand c)=>_queue.Enqueue(c); public bool TryDequeue(out IGameCommand? c)=>_queue.TryDequeue(out c); }
