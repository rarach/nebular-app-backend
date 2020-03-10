using Microsoft.AspNetCore.Mvc;
using System;


namespace NebularApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LogsController : ControllerBase
    {
        private readonly ILog _logger;


        public LogsController(ILog logger)
        {
            _logger = logger;
        }


        [HttpGet]
        public string Get()
        {
            var logLines = _logger.Dumb();
            string allLogs = String.Join(Environment.NewLine, logLines);
            return allLogs;
        }
    }
}
