using Business.Interfaces.Model;
using Business.Interfaces.Services;
using CQRS.CostEstimateTemplates.CreateCostEstimateTemplate;
using FluentAssertions;
using Moq;

namespace CQRS.Tests.CostEstimateTemplates;

public sealed class CreateCostEstimateTemplateCommandHandlerTests
{
    private readonly Mock<ICostEstimateTemplateService> _templateServiceMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly CreateCostEstimateTemplateCommandHandler _handler;

    public CreateCostEstimateTemplateCommandHandlerTests()
    {
        _currentUserMock.Setup(u => u.Id).Returns(Guid.NewGuid());

        _handler = new CreateCostEstimateTemplateCommandHandler(
            _templateServiceMock.Object,
            _currentUserMock.Object);
    }

    [Fact]
    public async Task Handle_WhenCalled_DelegatesToTemplateService()
    {
        // Arrange
        Guid expectedId = Guid.NewGuid();
        CreateCostEstimateTemplateCommand command = new("My Template", "Description");

        _templateServiceMock
            .Setup(s => s.CreateTemplateAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedId);

        // Act
        Guid result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(expectedId);
        _templateServiceMock.Verify(
            s => s.CreateTemplateAsync(
                _currentUserMock.Object.Id,
                command.Name,
                command.Description,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCalledWithNullDescription_DelegatesToTemplateService()
    {
        // Arrange
        Guid expectedId = Guid.NewGuid();
        CreateCostEstimateTemplateCommand command = new("My Template", null);

        _templateServiceMock
            .Setup(s => s.CreateTemplateAsync(
                It.IsAny<Guid>(),
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
