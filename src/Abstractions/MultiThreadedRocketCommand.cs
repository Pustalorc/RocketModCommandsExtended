﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Rocket.API;
using Rocket.Core.Utils;
using Rocket.Unturned.Chat;
using SDG.Unturned;

namespace Pustalorc.Libraries.RocketModCommandsExtended.Abstractions;

/// <summary>
/// Abstract class to add support for a multi-threaded and asynchronous command.
/// </summary>
public abstract class MultiThreadedRocketCommand : IRocketCommand
{
    /// <summary>
    /// The allowed caller type. Quite self-explanatory.
    /// </summary>
    /// <remarks>
    /// Console for only console, Player for only players, and both for both.
    /// </remarks>
    public abstract AllowedCaller AllowedCaller { get; }

    /// <summary>
    /// The command name.
    /// </summary>
    /// <remarks>
    /// This will be used in the Permissions list, but is also required as its what users have to type for your command to run.
    /// </remarks>
    public abstract string Name { get; }

    /// <summary>
    /// A basic help message for your command.
    /// </summary>
    public abstract string Help { get; }

    /// <summary>
    /// The syntax for the command. Its recommended to put all parameters your command will need.
    /// </summary>
    /// <remarks>
    /// There is no standard format for the syntax, but here's my recommendation:
    /// [] - Optional
    /// &lt;&gt; - Required
    /// {} - Variable
    /// </remarks>
    public abstract string Syntax { get; }

    /// <summary>
    /// A list of aliases for the commands. Empty by default, but you can override it to add whatever you wish.
    /// </summary>
    public virtual List<string> Aliases => new();

    /// <summary>
    /// A list of permissions required to execute the command.
    /// </summary>
    /// <remarks>
    /// Although the command name is already used to determine the basic permission for the command,
    /// and this permissions list should be used for the extras,
    /// most people end up just not using this, despite the fact that registering your command with
    /// additional permissions here would allow the command to be executed still, even if they dont have the permission
    /// for the command name.
    /// </remarks>
    public virtual List<string> Permissions => new() { Name };

    /// <summary>
    /// The setting determining if this command is multi-threaded or not.
    /// </summary>
    protected bool MultiThreadedExecution { get; set; }

    /// <summary>
    /// The required constructor for this class. Sets if the command will be multi-threaded or not.
    /// </summary>
    /// <param name="multiThreaded">
    /// True if you want the command to always run on a separate thread.
    /// False otherwise.
    /// </param>
    protected MultiThreadedRocketCommand(bool multiThreaded)
    {
        MultiThreadedExecution = multiThreaded;
    }

    /// <summary>
    /// Obsolete method due to naming change.
    /// Simply sets MultiThreadedExecution property to its input.
    /// </summary>
    /// <param name="multiThreaded">
    /// True if you want the command to always run on a separate thread.
    /// False otherwise.
    /// </param>
    [Obsolete("Use SetMultiThreadedExecution instead.")]
    [UsedImplicitly]
    public void ReloadMultiThreaded(bool multiThreaded)
    {
        SetMultiThreadedExecution(multiThreaded);
    }

    /// <summary>
    /// Sets the value of the property MultiThreadedExecution.
    /// </summary>
    /// <param name="multiThreadedExecution">
    /// True if you want the command to always run on a separate thread.
    /// False otherwise.
    /// </param>
    public void SetMultiThreadedExecution(bool multiThreadedExecution)
    {
        MultiThreadedExecution = multiThreadedExecution;
    }

    /// <summary>
    /// Sends a message to the global game chat.
    /// </summary>
    /// <param name="message">The message to be sent.</param>
    /// <remarks>
    /// Since this method uses UnturnedChat.Say by default, one should override it and change the call to
    /// ChatManager.SendMessage if they wish to support rich text right off the bat and permanently.
    /// </remarks>
    [UsedImplicitly]
    protected virtual void SendMessage(string message)
    {
        if (Thread.CurrentThread.IsGameThread())
            UnturnedChat.Say(message);
        else
            TaskDispatcher.QueueOnMainThread(() => UnturnedChat.Say(message));
    }

    /// <summary>
    /// Sends a message to a specific player.
    /// </summary>
    /// <param name="player">The player to send the message to.</param>
    /// <param name="message">The message to be sent.</param>
    /// <remarks>
    /// Since this method uses UnturnedChat.Say by default, one should override it and change the call to
    /// ChatManager.SendMessage if they wish to support rich text right off the bat and permanently.
    ///
    /// Do also note that if player is ConsolePlayer, the message will be logged by UnturnedChat.Say.
    /// If you override this method and do not wish to lose that functionality,
    /// you will have to manually check if its console and log to console.
    /// </remarks>
    [UsedImplicitly]
    protected virtual void SendMessage(IRocketPlayer player, string message)
    {
        if (Thread.CurrentThread.IsGameThread())
            UnturnedChat.Say(player, message);
        else
            TaskDispatcher.QueueOnMainThread(() => UnturnedChat.Say(player, message));
    }

    /// <summary>
    /// Obsolete due to naming change.
    /// Raises an exception during command execution back to the caller.
    /// </summary>
    /// <param name="caller">The player that executed the command.</param>
    /// <param name="exception">The exception that was raised.</param>
    [Obsolete("Use RaisedException instead, as that supports the input array, and has more self-descriptive naming.",
        true)]
    [UsedImplicitly]
    protected virtual void LogException(IRocketPlayer caller, Exception exception)
    {
        SendMessage(caller,
            $"Error during command execution. Command: {Name}. Error: {exception.Message}. Full exception: {exception}");
    }

    /// <summary>
    /// Raises an exception during command execution back to the caller.
    /// </summary>
    /// <param name="caller">The player that executed the command.</param>
    /// <param name="commandInput">The input to the command that the player executed.</param>
    /// <param name="exception">The exception that was raised.</param>
    /// <remarks>
    /// By default this method will send the error message back to the player but not log it.
    /// If you wish to, you could modify the message completely so when an exception is caught,
    /// an explicit console message is sent, and the user that executed it gets a different one.
    /// </remarks>
    [UsedImplicitly]
    protected virtual void RaisedException(IRocketPlayer caller, string[] commandInput, Exception exception)
    {
        SendMessage(caller,
            $"Error during command execution. Command: {Name} {string.Join(" ", commandInput)}. Error: {exception.Message}. Full exception: {exception}");
    }

    /// <summary>
    /// Default implementation of RocketMod's Execute method.
    /// This implementation will run ExecuteAsync in a separate thread, but will wait and run the method synchronously
    /// if MultiThreadedExecution is set to false.
    /// </summary>
    /// <param name="caller">The user that executed the command.</param>
    /// <param name="command">The parameters passed into the command by the user.</param>
    /// <remarks>
    /// Since ExecuteAsync is an asynchronous (Task) method, it has to be forcefully ran with Task.Run.
    /// This requirement is required in order to not cause a deadlock or race condition when running the async method
    /// from the same thread, whilst that thread is locked waiting for the async method to finish.
    /// </remarks>
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
                RaisedException(caller, command, ex);
                throw;
            }
        });

        if (!MultiThreadedExecution)
            task.Wait();
    }

    /// <summary>
    /// An abstract method to override with the final command's implementation.
    /// </summary>
    /// <param name="caller">The user that executed the command.</param>
    /// <param name="command">The parameters passed into the command by the user.</param>
    /// <returns>A Task that describes the current method's execution.</returns>
    /// <remarks>
    /// In order to allow commands to use the async and await keywords, the method returns Task by default.
    /// If you do not wish to use the async keyword, please return Task.CompletedTask.
    /// </remarks>
    [UsedImplicitly]
    public abstract Task ExecuteAsync(IRocketPlayer caller, string[] command);
}