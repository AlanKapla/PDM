using Business.Interfaces.WebModels.Projects;
using MediatR;

namespace CQRS.Projects.ActiveProjectInvitations;

public sealed record ActiveProjectInvitationsQuery : IRequestQuery<IEnumerable<ProjectInvitationWeb>>;
