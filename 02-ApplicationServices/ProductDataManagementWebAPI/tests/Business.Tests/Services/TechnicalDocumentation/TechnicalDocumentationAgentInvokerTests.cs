using Business.AIAgent.Core;
using Business.AIAgent.Services;
using Business.Implementation.Services.AI.TechnicalDocumentation;
using Business.Interfaces.Configurations;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;

namespace Business.Tests.Services.TechnicalDocumentation;

public sealed class TechnicalDocumentationAgentInvokerTests
{
    [Fact]
    public async Task CompleteWithImagesAsync_whenImageCountExceedsMax_throwsInvalidOperationException()
    {
        // Arrange
        Mock<IAICompletionService> completionMock = new();
        IOptions<TechnicalDocumentationOptions> options = Options.Create(new TechnicalDocumentationOptions
        {
            MaxImagesPerGroup = 2,
        });
        List<(byte[] ImageBytes, string MediaType)> images =
        [
            ([1], "image/jpeg"),
            ([2], "image/jpeg"),
            ([3], "image/jpeg"),
        ];

        // Act
        Func<Task> act = async () => await TechnicalDocumentationAgentInvoker.CompleteWithImagesAsync(
            completionMock.Object,
            new AgentDefinitionLoader(),
            "universal-extraction-agent",
            null,
            images,
            CancellationToken.None,
            options);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*exceeds MaxImagesPerGroup*");
        completionMock.Verify(
            service => service.CompleteWithImagesAsync(
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<IReadOnlyList<(byte[] ImageBytes, string MediaType)>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<int>(),
                It.IsAny<float?>(),
                It.IsAny<bool>()),
            Times.Never);
    }

    [Fact]
    public async Task CompleteWithImagesAsync_delegatesToCompletionService_withMultipleImages()
    {
        // Arrange
        Mock<IAICompletionService> completionMock = new();
        completionMock
            .Setup(service => service.CompleteWithImagesAsync(
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<IReadOnlyList<(byte[] ImageBytes, string MediaType)>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<int>(),
                It.IsAny<float?>(),
                true))
            .ReturnsAsync("{}");

        byte[] imageA = [10];
        byte[] imageB = [20];
        List<(byte[] ImageBytes, string MediaType)> images =
        [
            (imageA, "image/jpeg"),
            (imageB, "image/png"),
        ];

        // Act
        string result = await TechnicalDocumentationAgentInvoker.CompleteWithImagesAsync(
            completionMock.Object,
            new AgentDefinitionLoader(),
            "universal-extraction-agent",
            "analyze these drawings",
            images,
            CancellationToken.None);

        // Assert
        result.Should().Be("{}");
        completionMock.Verify(
            service => service.CompleteWithImagesAsync(
                It.IsAny<string>(),
                "analyze these drawings",
                It.Is<IReadOnlyList<(byte[] ImageBytes, string MediaType)>>(list => list.Count == 2),
                It.IsAny<CancellationToken>(),
                It.IsAny<int>(),
                It.IsAny<float?>(),
                true),
            Times.Once);
    }
}
