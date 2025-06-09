using Frosty.Core;
using FrostySdk.Interfaces;
using System;
using System.Threading;

namespace TestPlugin.EditorExecutions
{
    public class CustomExecutionAction : ExecutionAction
    {
        public override Action<ILogger, PluginManagerType, CancellationToken> PreLaunchAction => new Action<ILogger, PluginManagerType, CancellationToken>((ILogger logger, PluginManagerType type, CancellationToken token) =>
        {
            Console.WriteLine($"{type}: PreLaunch Action");
        });

        public override Action<ILogger, PluginManagerType, CancellationToken> PostLaunchAction => new Action<ILogger, PluginManagerType, CancellationToken>((ILogger logger, PluginManagerType type, CancellationToken token) =>
        {
            Console.WriteLine($"{type}: PostLaunch Action");
        });
    }
}
