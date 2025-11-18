using MediatR;

namespace CQRS
{
    public interface IRequestCommand<TResponse> : IRequest<TResponse>
    {
    }
}