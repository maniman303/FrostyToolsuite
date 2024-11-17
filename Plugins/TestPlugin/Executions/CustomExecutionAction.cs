using Frosty.Core;
using FrostySdk.Interfaces;
using System;
using System.Threading;

namespace TestPlugin.EditorExecutions
{
    public class CustomExecutionAction : ExecutionAction
    {
        public override Action<ILogger, PluginManagerType, bool, CancellationToken> PreLaunchAction => new Action<ILogger, PluginManagerType, bool, CancellationToken>((ILogger logger, PluginManagerType type, bool isInstallOnly, CancellationToken token) =>
        {
            Console.WriteLine($"{type}: PreLaunch Action");
        });

        public override Action<ILogger, PluginManagerType, bool, CancellationToken> PostLaunchAction => new Action<ILogger, PluginManagerType, bool, CancellationToken>((ILogger logger, PluginManagerType type, bool isInstallOnly, CancellationToken token) =>
        {
            Console.WriteLine($"{type}: PostLaunch Action");
        });
    }
}
