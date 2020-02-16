using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;


namespace NebularApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TopExchangesController : ControllerBase
    {
        private readonly ILogger<TopExchangesController> _logger;

        public TopExchangesController(ILogger<TopExchangesController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public string Get()
        {
            try
            {
                string exeDir = AppDomain.CurrentDomain.BaseDirectory;

                //Get the data from "database"
                string data = System.IO.File.ReadAllText(exeDir + @".\data\top_exchanges.json");
                return data;
            }
            catch (Exception ex)
            {
                return @"{ ""error"": ""Failed to read top_exchanges.json. Exception=" + ex.Message + @""" }";
            }
        }
    }
}
