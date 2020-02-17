using Microsoft.AspNetCore.Mvc;
using System;


namespace NebularApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LogsController : ControllerBase
    {
        [HttpGet]
        public string Get()
        {
            try
            {
                string exeDir = AppDomain.CurrentDomain.BaseDirectory;

                //Get the data from "database"
                string data = System.IO.File.ReadAllText(exeDir + @".\data\logs.txt");
                return data;
            }
            catch (Exception ex)
            {
                return @"{ ""error"": ""Failed to read top_exchanges.json. Exception=" + ex.Message + @""" }";
            }
        }
    }
}
