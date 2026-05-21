using Business.Interfaces.Model;
using Business.Interfaces.Services;
using CQRS.CostEstimateTemplates.CreateCostEstimateTemplateFromDefault;
using FluentAssertions;
using Moq;

namespace CQRS.Tests.CostEstimateTemplates;

public sealed class CreateCostEstimateTemplateFromDefaultCommandHandlerTests
{
    private readonly Mock<ICostEstimateTemplateService> _templateServiceMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly CreateCostEstimateTemplateFromDefaultCommandHandler _handler;

    public CreateCostEstimateTemplateFromDefaultCommandHandlerTests()
    {
        _currentUserMock.Setup(u => u.Id).Returns(Guid.NewGuid());

        _handler = new CreateCostEstimateTemplateFromDefaultCommandHandler(
            _templateServiceMock.Object,
            _currentUserMock.Object);
    }

    [Fact]
    public async Task Handle_WhenCalled_DelegatesToTemplateService()
    {
        // Arrange
        Guid expectedId = Guid.NewGuid();
        CreateCostEstimateTemplateFromDefaultCommand command = new("My Template", "Description")
        {
            Slug = "default-template"
        };

        _templateServiceMock
            .Setup(s => s.CreateTemplateFromDefaultAsync(
                It.IsAny<Guid>(),
                It.IsAny<string?>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedId);

        // Act
        Guid result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(expectedId);
        _templateServiceMock.Verify(
            s => s.CreateTemplateFromDefaultAsync(
                _currentUserMock.Object.Id,
                command.Slug,
                command.Name,
                command.Description,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenSlugIsNull_DelegatesToTemplateService()
    {
        // Arrange
        Guid expectedId = Guid.NewGuid();
        CreateCostEstimateTemplateFromDefaultCommand command = new("My Template", null)
        {
            Slug = null
        };

        _templateServiceMock
            .Setup(s => s.CreateTemplateFromDefaultAsync(
                It.IsAny<Guid>(),
                It.IsAny<string?>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedId);

        // Act
        Guid result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(expectedId);
    }
}
