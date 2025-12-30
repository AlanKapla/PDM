namespace CQRS.Projects.GetProjectsDictionary
{
    public record GetProjectsDictionaryQuery(
        Guid TenantId
    ) : IRequestQuery<Dictionary<Guid, string>>;
}
