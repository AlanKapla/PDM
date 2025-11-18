using Business.Implementation.Model;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Entities.Enums;
using Entities.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Filters.Project
{
    public sealed class ProjectAccessHandler
    : AuthorizationHandler<ProjectAccessRequirement>
    {
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly ICurrentUser currentUser;

        public ProjectAccessHandler(
            IHttpContextAccessor httpContextAccessor,
            ICurrentUser currentUser)
        {
            this.httpContextAccessor = httpContextAccessor;
            this.currentUser = currentUser;
        }

        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            ProjectAccessRequirement requirement)
        {
            if (currentUser == null)
            {
                return Task.CompletedTask;
            }

            if (currentUser.SystemRole == SystemRole.SuperAdmin)
            {   
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            if (currentUser.ActiveTenantId is null || currentUser.ActiveTenantRole is null)
            {
                return Task.CompletedTask;
            }

            var routeValues = httpContextAccessor.HttpContext?.Request.RouteValues;

            if (routeValues == null)
            {
                return Task.CompletedTask;
            }

            if (!routeValues.TryGetValue("tenantId", out var rawTenantId) || rawTenantId is null)
            {
                return Task.CompletedTask;
            }

            if (!routeValues.TryGetValue("projectId", out var rawId) || rawId is null)
            {
                return Task.CompletedTask;
            }

            if (!Guid.TryParse(rawTenantId.ToString(), out var tenantIdFromUrl))
            {
                return Task.CompletedTask;
            }

            if (!Guid.TryParse(rawId.ToString(), out var projectId))
            {
                return Task.CompletedTask;
            }

            if (tenantIdFromUrl != currentUser.ActiveTenantId)
            {
                return Task.CompletedTask;
            }

            if (currentUser.ActiveTenantRole == TenantRole.Admin)
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            var isProjectMember = currentUser.Projects?.Any(p => p.ProjectId == projectId) == true;

            if (isProjectMember)
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            return Task.CompletedTask;
        }
    }
}
