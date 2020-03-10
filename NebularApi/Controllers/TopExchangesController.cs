using System.Text.Json;
using Microsoft.AspNetCore.Mvc;


namespace NebularApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TopExchangesController : ControllerBase
    {
        private readonly TopExchangesStorage _storage;


        public TopExchangesController(TopExchangesStorage storage)
        {
            _storage = storage;
        }


        [HttpGet]
        public string Get()
        {
            string json = JsonSerializer.Serialize(_storage.Data);
            return json;
        }
    }
}
