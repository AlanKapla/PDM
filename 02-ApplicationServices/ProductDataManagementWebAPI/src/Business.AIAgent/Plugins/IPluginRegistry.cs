namespace Business.AIAgent.Plugins;

/// <summary>
/// Registry for managing Semantic Kernel plugins
/// Plugins are collections of KernelFunctions that can be called by AI
/// </summary>
public interface IPluginRegistry
{
    /// <summary>
    /// Registers a plugin type with Semantic Kernel
    /// </summary>
    /// <typeparam name="TPlugin">Plugin class type</typeparam>
    void RegisterPlugin<TPlugin>() where TPlugin : class;

    /// <summary>
    /// Registers a plugin instance with Semantic Kernel
    /// </summary>
    /// <param name="plugin">Plugin instance</param>
    /// <param name="pluginName">Optional custom name for the plugin</param>
    void RegisterPlugin(object plugin, string? pluginName = null);

    /// <summary>
    /// Gets all registered plugin names
    /// </summary>
    /// <returns>List of registered plugin names</returns>
    IReadOnlyList<string> GetRegisteredPlugins();

    /// <summary>
    /// Checks if a plugin is registered
    /// </summary>
    /// <param name="pluginName">Name of the plugin</param>
    /// <returns>True if plugin is registered</returns>
    bool IsPluginRegistered(string pluginName);

    /// <summary>
    /// Removes a plugin from the registry
    /// </summary>
    /// <param name="pluginName">Name of the plugin to remove</param>
    /// <returns>True if plugin was removed</returns>
    bool UnregisterPlugin(string pluginName);
}
