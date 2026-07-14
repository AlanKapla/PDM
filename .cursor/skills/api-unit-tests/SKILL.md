---
name: api-unit-tests
description: Pisanie testów jednostkowych dla handlerów, walidatorów i kontrolerów (xUnit + Moq). Użyj gdy piszesz testy jednostkowe dla warstwy API.
---

# Skill: API / Testy jednostkowe

## Opis
Pisanie testów jednostkowych dla handlerów, walidatorów i kontrolerów (xUnit + Moq).

## Kiedy używać
Użyj tego skilla gdy piszesz testy jednostkowe dla warstwy API.

---

## Stack

- xUnit + Moq
- AAA (Arrange/Act/Assert z komentarzami)
- Nazewnictwo: `Metoda_Warunek_OczekiwanyWynik`
- Projekty: `CQRS.Tests`, `Business.Tests`, `WebApi.Tests`

## Handler test

```csharp
public sealed class CreateProjectCommandHandlerTests
{
    private readonly Mock<IRepository<Project>> _repoMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly CreateProjectCommandHandler _handler;

    public CreateProjectCommandHandlerTests()
    {
        _repoMock = new Mock<IRepository<Project>>();
        _currentUserMock = new Mock<ICurrentUser>();
        _currentUserMock.Setup(u => u.Id).Returns(Guid.NewGuid());

        _handler = new CreateProjectCommandHandler(
            _repoMock.Object,
            _currentUserMock.Object);
    }

    [Fact]
    public async Task Handle_WhenCommandIsValid_InsertsProjectAndReturnsWeb()
    {
        // Arrange
        CreateProjectCommand command = new(Guid.NewGuid(), "Test Project");

        // Act
        ProjectDetailsWeb result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Test Project");
        _repoMock.Verify(r => r.Insert(It.IsAny<Project>()), Times.Once);
        _repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenProjectNotFound_ThrowsNotFoundApiException()
    {
        // Arrange
        _repoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<Project, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Project?)null);

        GetProjectDetailsQuery query = new(Guid.NewGuid(), Guid.NewGuid());

        // Act
        Func<Task> act = async () => await _handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundApiException>();
    }
}
```

## Validator test

```csharp
public sealed class CreateProjectCommandValidatorTests
{
    private readonly CreateProjectCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenTenantIdIsEmpty_HasValidationError()
    {
        // Arrange
        CreateProjectCommand command = ValidCommand() with { TenantId = Guid.Empty };

        // Act
        TestValidationResult<CreateProjectCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TenantId);
    }

    [Fact]
    public void Validate_WhenCommandIsValid_HasNoValidationErrors()
    {
        // Arrange
        CreateProjectCommand command = ValidCommand();

        // Act
        TestValidationResult<CreateProjectCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    private static CreateProjectCommand ValidCommand() => new(Guid.NewGuid(), "Valid Name");
}
```

## Controller test

```csharp
public sealed class ProjectControllerTests
{
    private readonly Mock<ISender> _senderMock = new();
    private readonly ProjectController _controller;

    public ProjectControllerTests()
    {
        _controller = new ProjectController(_senderMock.Object);
    }

    [Fact]
    public async Task GetProjectDetails_WhenQuerySucceeds_ReturnsOk()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        ProjectDetailsWeb expected = new(projectId, tenantId, "Test", true, DateTime.UtcNow, Guid.NewGuid(), "", "", 0, new HashSet<string>());

        _senderMock
            .Setup(s => s.Send(It.IsAny<GetProjectDetailsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        IActionResult response = await _controller.GetProjectDetails(tenantId, projectId, CancellationToken.None);

        // Assert
        OkObjectResult okResult = response.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expected);
    }
}
```

## Zasady

- Klasa testowa zawsze `sealed`
- Jeden test = jeden przypadek
- `ValidCommand()` / `ValidQuery()` jako `private static` helper
- `ValidCommand() with { Pole = wartość }` dla testów invalid
- Explicit types zawsze — zakaz `var`
- Mock `Expression<Func<T, bool>>` przez `It.IsAny<Expression<Func<T, bool>>>()`
- Weryfikuj wywołania mocków przez `Verify(..., Times.Once)`
