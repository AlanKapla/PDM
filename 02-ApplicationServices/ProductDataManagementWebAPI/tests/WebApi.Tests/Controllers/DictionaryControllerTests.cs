using CQRS.Dictionary.GetCurrencies;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using WebApi.Controllers;

namespace WebApi.Tests.Controllers
{
    public class DictionaryControllerTests : ControllerTestBase
    {
        private readonly DictionaryController sut;

        public DictionaryControllerTests()
        {
            sut = new DictionaryController(MediatorMock.Object);
        }

        [Fact]
        public async Task GetCurrencies_ReturnsOk_AndSendsQuery()
        {
            IActionResult result = await sut.GetCurrencies();

            result.Should().BeOfType<OkObjectResult>();
            VerifyMediatorCalledOnce<GetCurrenciesQuery>();
        }
    }
}
