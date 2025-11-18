using CQRS;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    public class BaseApiController(IMediator mediator) : ControllerBase
    {
        protected readonly IMediator mediator = mediator;

        protected async Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            return await mediator.Send(request);
        }
    }
}
