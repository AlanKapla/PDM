using Business.Interfaces.WebModels.Dictionary;
using CQRS.Dictionary.GetCurrencies;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [Route("api/dictionary")]
    [ApiController]
    [Authorize]
    public class DictionaryController(IMediator mediator) : BaseApiController(mediator)
    {
        [HttpGet("currencies")]
        [ProducesResponseType(typeof(IReadOnlyList<CurrencyDictionaryItemWeb>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCurrencies()
        {
            IReadOnlyList<CurrencyDictionaryItemWeb> result = await Send(new GetCurrenciesQuery());
            return Ok(result);
        }
    }
}
