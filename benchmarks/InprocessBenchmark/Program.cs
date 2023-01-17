// -----------------------------------------------------------------------
// <copyright file="Program.cs" company="Asynkron AB">
//      Copyright (C) 2015-2022 Asynkron AB All rights reserved
// </copyright>
// -----------------------------------------------------------------------
using System;
using System.Linq;
using System.Runtime;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;

using Proto;
using Proto.Mailbox;

public class Program
{
    public const int batchSize = 100;
    public const int messageCount = 10_000_000;

    private static async Task Main()
    {
        var context = new RootContext(new ActorSystem());
        Console.WriteLine($"Is Server GC : {GCSettings.IsServerGC}");
        Console.WriteLine($"Batch size   : {batchSize}");
        Console.WriteLine($"Message count: {messageCount}");

        Console.WriteLine("ClientCount\t\tDispatcher\t\tElapsed\t\tMsg/sec");
        var tps = new[] {50, 100, 200, 400, 800};
        int[] clientCounts = {1, 2, 4, 8, 16, 32, 64};

        foreach (var throughput in tps)
        {
            var d = new Proto.Mailbox.ThreadPoolDispatcher { Throughput = throughput };

            foreach (var clientCount in clientCounts)
            {
                var pingActor = new PID[clientCount];
                var pongActor = new PID[clientCount];
                var completions = new TaskCompletionSource<bool>[clientCount];

                Proto.Props pongProps = Props.FromProducer(() => new InprocessBenchmark.Actors.PongActor()).WithDispatcher(d);

                for (var i = 0; i < clientCount; i++)
                {
                    var tsc = new TaskCompletionSource<bool>();
                    completions[i] = tsc;
                    var pingProps = Props.FromProducer(() => new InprocessBenchmark.Actors.PingActor(tsc, messageCount, batchSize))
                        .WithDispatcher(d);

                    pingActor[i] = context.Spawn(pingProps);
                    pongActor[i] = context.Spawn(pongProps);
                }

                var tasks = completions.Select(tsc => tsc.Task).ToArray();
                var sw = Stopwatch.StartNew();

                for (var i = 0; i < clientCount; i++)
                {
                    PID pingActorId = pingActor[i];
                    var echo = pongActor[i];

                    context.Send(pingActorId, new Start(echo));
                }

                await Task.WhenAll(tasks);

                sw.Stop();
                long totalMessages = messageCount * 2L * clientCount;

                long speedMsgPerSecond = (long)Math.Floor(totalMessages / (double)sw.ElapsedMilliseconds * 1000.0d);
                string speedStr = speedMsgPerSecond.ToString("# ### ### ##0", CultureInfo.InvariantCulture);

                Console.WriteLine($"{clientCount}\t\t\t{throughput}\t\t\t{sw.ElapsedMilliseconds} ms\t\t{speedStr}");
                await Task.Delay(5000);
            } // End foreach (var clientCount in clientCounts)
        } // End foreach (var throughput in tps)
    }
}

public class Msg
{
    public Msg(PID pingActor, long msgId)
    {
        MsgID = msgId;
        PingActor = pingActor;
    }

    public long MsgID { get; }

    public PID PingActor { get; }
}

public class Start
{
    public Start(PID sender)
    {
        Sender = sender;
    }

    public PID Sender { get; }
}
