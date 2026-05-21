using FluentAssertions;
using MediatR;
using Moq;

namespace WebApi.Tests
{
    /// <summary>
    /// Base for controller unit tests. Provides a Mock&lt;IMediator&gt; that returns
    /// the supplied response for any IRequest&lt;TResponse&gt; passed to Send.
    /// </summary>
    public abstract class ControllerTestBase
    {
        protected readonly Mock<IMediator> MediatorMock = new(MockBehavior.Loose);

        protected void SetupMediatorReturns<TRequest, TResponse>(TResponse response)
            where TRequest : IRequest<TResponse>
        {
            MediatorMock
                .As<ISender>()
                .Setup(m => m.Send(It.IsAny<TRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);
        }

        /// <summary>
        /// Assert that exactly one Send invocation was recorded for the given request type,
        /// matching the given predicate. Works regardless of which ISender/IMediator overload
        /// was used, by inspecting Moq's invocation log.
        /// </summary>
        protected void VerifyMediatorCalledOnce<TRequest>(Func<TRequest, bool> predicate)
            where TRequest : notnull
        {
            IEnumerable<Moq.IInvocation> matching = MediatorMock.Invocations
                .Where(inv => inv.Arguments.Count > 0 &&
                              inv.Arguments[0] is TRequest r &&
                              predicate(r));

            matching.Should().HaveCount(1, $"Expected exactly one Send({typeof(TRequest).Name}, ...) matching the predicate.");
        }

        protected void VerifyMediatorCalledOnce<TRequest>()
            where TRequest : notnull
        {
            IEnumerable<Moq.IInvocation> matching = MediatorMock.Invocations
                .Where(inv => inv.Arguments.Count > 0 && inv.Arguments[0] is TRequest);

            matching.Should().HaveCount(1, $"Expected exactly one Send({typeof(TRequest).Name}, ...).");
        }
    }
}
