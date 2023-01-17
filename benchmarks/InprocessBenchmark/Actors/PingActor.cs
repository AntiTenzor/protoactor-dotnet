using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;

namespace InprocessBenchmark.Actors;

public class PingActor : Proto.IActor
{
    private readonly int _batchSize;
    private readonly TaskCompletionSource<bool> _wgStop;

    private long lastMessageId = -1;

    private int _messageCount;
    private Proto.PID _targetPid;

    public PingActor(TaskCompletionSource<bool> wgStop, int messageCount, int batchSize)
    {
        _wgStop = wgStop;
        _messageCount = messageCount;
        _batchSize = batchSize;
    }

    public Task ReceiveAsync(Proto.IContext context)
    {
        switch (context.Message)
        {
            case Start s:
                _targetPid = s.Sender;
                SendBatch(context);
                break;

            case Msg m:
                _messageCount--;

                if (_messageCount <= 0)
                    _wgStop.SetResult(true);
                else
                {
                    // !WARNING! Do I just return the same object to the PongActor???
                    // But why did I sent original messages in a 'batch'???
                    context.Send(_targetPid, m);
                }
                break;
        }

        return Task.CompletedTask;
    }

    private void SendBatch(Proto.IContext context)
    {
        long msgId = Interlocked.Increment(ref this.lastMessageId);
        Msg m = new Msg(context.Self, msgId);

        for (var i = 0; i < _batchSize; i++)
        {
            context.Send(_targetPid, m);
        }

        // !WARNING! This is strange to decrease _messageCount just after context.Send!
        // I think this is a mistake which increases reported speed x2, isn't it?
        //_messageCount -= _batchSize;
    }
}
