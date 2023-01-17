using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;



namespace InprocessBenchmark.Actors;

public class PongActor : Proto.IActor
{
    public Task ReceiveAsync(Proto.IContext context)
    {
        switch (context.Message)
        {
            case Msg msg:
                context.Send(msg.PingActor, msg);
                break;
        }

        return Task.CompletedTask;
    }
}
