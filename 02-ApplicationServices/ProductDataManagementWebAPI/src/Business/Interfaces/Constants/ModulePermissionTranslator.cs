using Entities.Enums;

namespace Business.Interfaces.Constants;

public static class ModulePermissionTranslator
{
    public static HashSet<string> Translate(ProjectModule module)
    {
        return module switch
        {
            ProjectModule.Settings => new HashSet<string> { PermissionCodes.ProjectSettings },
            ProjectModule.Files => new HashSet<string> { PermissionCodes.ProjectFiles },
            ProjectModule.Estimates => new HashSet<string> { PermissionCodes.ProjectEstimates },
            ProjectModule.Costs => new HashSet<string> { PermissionCodes.ProjectCosts },
            ProjectModule.Schedule => new HashSet<string> { PermissionCodes.ProjectSchedule },
            ProjectModule.DashboardTracker => new HashSet<string> { PermissionCodes.ProjectDashboardTracker },
            ProjectModule.TechnicalDocumentation => new HashSet<string> { PermissionCodes.ProjectTechnicalDocumentation },
            _ => new HashSet<string>()
        };
    }

    /// <summary>Returns all module permission codes (all 9 modules).</summary>
    public static HashSet<string> GetAllModulePermissions()
    {
        HashSet<string> result = new();
        foreach (ProjectModule module in Enum.GetValues<ProjectModule>())
        {
            foreach (string code in Translate(module))
                result.Add(code);
        }
        return result;
    }
}
