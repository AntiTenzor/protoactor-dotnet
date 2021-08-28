﻿using System;
using System.Threading.Tasks;
using Messages;
using Proto;
using Proto.Channels.Experimental;
using static Proto.Remote.GrpcNet.GrpcNetRemoteConfig;
using static System.Threading.Channels.Channel;

var system = await ActorSystem.StartNew(BindToLocalhost(8000));
var channel = CreateUnbounded<MyMessage>();
var publisher = system.Root.SpawnPublisherActor<MyMessage>("publisher");
_ = channel.PublishToPid(system.Root, publisher);

//produce messages
for (var i = 0; i < 30; i++)
{
    Console.WriteLine("Sending message " + i);
    await channel.Writer.WriteAsync(new MyMessage(i));
    await Task.Delay(1000);
}