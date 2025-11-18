using MediatR;

namespace CQRS
{
    public interface IRequestQuery<IResponse> : IRequest<IResponse>
    {
    }
}
