using Business.Interfaces.Services;

namespace Business.Implementation.Services.Files
{
    public static class ProjectMemberNameResolver
    {
        public static string ResolveUserName(IReadOnlyDictionary<Guid, ProjectMemberUserInfo> userDict, Guid userId)
        {
            return userDict.TryGetValue(userId, out ProjectMemberUserInfo? user) ? user.FullName : string.Empty;
        }
    }
}
