using Microsoft.Extensions.Logging;
using Business.AIAgent.Core;

namespace Business.AIAgent.Plugins;

/// <summary>
/// Thread-safe registry for managing Semantic Kernel plugins
/// </summary>
public sealed class PluginRegistry : IPluginRegistry
{
    private readonly IKernelOrchestrator _orchestrator;
    private readonly ILogger<PluginRegistry> _logger;
    private readonly HashSet<string> _registeredPlugins = new();
    private readonly object _lock = new();

    public PluginRegistry(
        IKernelOrchestrator orchestrator,
        ILogger<PluginRegistry> logger)
    {
        _orchestrator = orchestrator;
        _logger = logger;
    }

    public void RegisterPlugin<TPlugin>() where TPlugin : class
    {
        var pluginName = typeof(TPlugin).Name;

        lock (_lock)
        {
            if (_registeredPlugins.Contains(pluginName))
            {
                _logger.LogWarning("Plugin {PluginName} is already registered", pluginName);
                return;
            }

            _orchestrator.RegisterPlugin<TPlugin>();
            _registeredPlugins.Add(pluginName);

            _logger.LogInformation("Plugin {PluginName} registered successfully", pluginName);
        }
    }

    public void RegisterPlugin(object plugin, string? pluginName = null)
    {
        var name = pluginName ?? plugin.GetType().Name;

        lock (_lock)
        {
            if (_registeredPlugins.Contains(name))
            {
                _logger.LogWarning("Plugin {PluginName} is already registered", name);
                return;
            }

            _orchestrator.RegisterPlugin(plugin, name);
            _registeredPlugins.Add(name);

            _logger.LogInformation("Plugin instance {PluginName} registered successfully", name);
        }
    }

    public IReadOnlyList<string> GetRegisteredPlugins()
    {
        lock (_lock)
        {
            return _registeredPlugins.ToList();
        }
    }

    public bool IsPluginRegistered(string pluginName)
    {
        lock (_lock)
        {
            return _registeredPlugins.Contains(pluginName);
        }
    }

    public bool UnregisterPlugin(string pluginName)
    {
        lock (_lock)
        {
            var removed = _registeredPlugins.Remove(pluginName);

            if (removed)
            {
                _logger.LogInformation("Plugin {PluginName} unregistered successfully", pluginName);
            }
            else
            {
                _logger.LogWarning("Plugin {PluginName} was not registered", pluginName);
            }

            return removed;
        }
    }
}
