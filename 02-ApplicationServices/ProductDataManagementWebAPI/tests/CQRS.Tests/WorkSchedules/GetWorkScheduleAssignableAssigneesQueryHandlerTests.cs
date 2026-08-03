using Business.Interfaces.Services;
using Business.Interfaces.WebModels.WorkSchedules;
using CQRS.WorkSchedules.GetWorkScheduleAssignableAssignees;
using Entities.Models.Tenants;
using Entities.Models.WorkSchedules;
using FluentAssertions;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.WorkSchedules;

public sealed class GetWorkScheduleAssignableAssigneesQueryHandlerTests
{
    private readonly Mock<IUserService> _userServiceMock = new();
    private readonly Mock<IReadRepository<Contractor>> _contractorRepoMock = new();
    private readonly Mock<IRepository<WorkScheduleStageWorkAssignment>> _assignmentRepoMock = new();
    private readonly Mock<IReadRepository<WorkScheduleStageWorkPeriod>> _periodRepoMock = new();
    private readonly GetWorkScheduleAssignableAssigneesQueryHandler _handler;

    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _projectId = Guid.NewGuid();

    public GetWorkScheduleAssignableAssigneesQueryHandlerTests()
    {
        _handler = new GetWorkScheduleAssignableAssigneesQueryHandler(
            _userServiceMock.Object,
            _contractorRepoMock.Object,
            _assignmentRepoMock.Object,
            _periodRepoMock.Object);

        SetupEmptyAssignmentQueries();
    }

    private GetWorkScheduleAssignableAssigneesQuery ValidQuery() =>
        new GetWorkScheduleAssignableAssigneesQuery
        {
            TenantId = _tenantId,
            ProjectId = _projectId
        };

    private void SetupEmptyAssignmentQueries()
    {
        // AssignmentRow / PeriodRow are private — return empty lists via InvocationFunc
        _assignmentRepoMock
            .Setup(r => r.SelectAsync(
                It.IsAny<Expression<Func<WorkScheduleStageWorkAssignment, bool>>>(),
                It.IsAny<Expression<Func<WorkScheduleStageWorkAssignment, It.IsAnyType>>>(),
                It.IsAny<CancellationToken>()))
            .Returns(new InvocationFunc(invocation => CreateEmptyListTask(invocation.Method.GetGenericArguments()[0])));

        _periodRepoMock
            .Setup(r => r.SelectAsync(
                It.IsAny<Expression<Func<WorkScheduleStageWorkPeriod, bool>>>(),
                It.IsAny<Expression<Func<WorkScheduleStageWorkPeriod, It.IsAnyType>>>(),
                It.IsAny<CancellationToken>()))
            .Returns(new InvocationFunc(invocation => CreateEmptyListTask(invocation.Method.GetGenericArguments()[0])));
    }

    private static object CreateEmptyListTask(Type itemType)
    {
        Type listType = typeof(List<>).MakeGenericType(itemType);
        object emptyList = Activator.CreateInstance(listType)!;
        return typeof(Task)
            .GetMethod(nameof(Task.FromResult))!
            .MakeGenericMethod(listType)
            .Invoke(null, new[] { emptyList })!;
    }

    [Fact]
    public async Task Handle_WhenMembersAndContractorsExist_ReturnsMappedAssigneesWithEmptyAssignments()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        Guid contractorId = Guid.NewGuid();

        _userServiceMock
            .Setup(s => s.GetProjectMembersAsync(_tenantId, _projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProjectMemberUserInfo>
            {
                new ProjectMemberUserInfo
                {
                    UserId = userId,
                    Email = "a@test.com",
                    FirstName = "Anna",
                    LastName = "Nowak",
                    CompanyName = "ACME"
                }
            });

        _contractorRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<Expression<Func<Contractor, bool>>>(),
                It.IsAny<Func<IQueryable<Contractor>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<Contractor, object>>[]>()))
            .ReturnsAsync(new List<Contractor>
            {
                new Contractor { Id = contractorId, TenantId = _tenantId, Name = "Firma X" }
            });

        // Act
        WorkScheduleAssignableAssigneesWeb result = await _handler.Handle(ValidQuery(), CancellationToken.None);

        // Assert
        result.Members.Should().HaveCount(1);
        result.Members[0].UserId.Should().Be(userId);
        result.Members[0].Assignments.Should().BeEmpty();
        result.Contractors.Should().HaveCount(1);
        result.Contractors[0].Id.Should().Be(contractorId);
        result.Contractors[0].Assignments.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenNoData_ReturnsEmptyLists()
    {
        // Arrange
        _userServiceMock
            .Setup(s => s.GetProjectMembersAsync(_tenantId, _projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProjectMemberUserInfo>());

        _contractorRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<Expression<Func<Contractor, bool>>>(),
                It.IsAny<Func<IQueryable<Contractor>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<Contractor, object>>[]>()))
            .ReturnsAsync(new List<Contractor>());

        // Act
        WorkScheduleAssignableAssigneesWeb result = await _handler.Handle(ValidQuery(), CancellationToken.None);

        // Assert
        result.Members.Should().BeEmpty();
        result.Contractors.Should().BeEmpty();
    }
}
