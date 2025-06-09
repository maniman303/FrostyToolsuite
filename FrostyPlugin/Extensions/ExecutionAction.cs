using FrostySdk.Interfaces;
using System;
using System.Threading;

namespace Frosty.Core
{
    public abstract class ExecutionAction
    {
        public virtual Action<ILogger, PluginManagerType, CancellationToken> PreLaunchAction { get; }
        public virtual Action<ILogger, PluginManagerType, CancellationToken> PostLaunchAction { get; }

        public ExecutionAction()
        {
        }
    }
}
