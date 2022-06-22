using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Rocket.API;
using Rocket.Core.Utils;
using Rocket.Unturned.Chat;
using SDG.Unturned;

namespace Pustalorc.Libraries.RocketModCommandsExtended.Abstractions;

public abstract class MultiThreadedRocketCommand : IRocketCommand
{
    public abstract AllowedCaller AllowedCaller { get; }
    public abstract string Name { get; }
    public abstract string Help { get; }
    public abstract string Syntax { get; }
    public virtual List<string> Aliases => new();
    public virtual List<string> Permissions => new() { Name };

    protected bool MultiThreadedExecution { get; set; }

    protected MultiThreadedRocketCommand(bool multiThreaded)
    {
        MultiThreadedExecution = multiThreaded;
    }

    public void ReloadMultiThreaded(bool multiThreaded)
    {
        MultiThreadedExecution = multiThreaded;
    }

    [UsedImplicitly]
    protected virtual void SendMessage(string message)
    {
        if (Thread.CurrentThread.IsGameThread())
            UnturnedChat.Say(message);
        else
            TaskDispatcher.QueueOnMainThread(() => UnturnedChat.Say(message));
    }

    [UsedImplicitly]
    protected virtual void SendMessage(IRocketPlayer player, string message)
    {
        if (Thread.CurrentThread.IsGameThread())
            UnturnedChat.Say(player, message);
        else
            TaskDispatcher.QueueOnMainThread(() => UnturnedChat.Say(player, message));
    }

    protected virtual void LogException(IRocketPlayer caller, Exception exception)
    {
        SendMessage(caller,
            $"Error during command execution. Command: {Name}. Error: {exception.Message}. Full exception: {exception}");
    }

    public virtual void Execute(IRocketPlayer caller, string[] command)
    {
        var task = Task.Run(async Task() =>
        {
            try
            {
                await ExecuteAsync(caller, command);
            }
            catch (Exception ex)
            {
                LogException(caller, ex);
                throw;
            }
        });

        if (!MultiThreadedExecution)
            task.Wait();
    }

    [UsedImplicitly]
    public abstract Task ExecuteAsync(IRocketPlayer caller, string[] command);
}