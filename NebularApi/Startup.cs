using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NebularApi.DataCollectors;


namespace NebularApi
{
    public class Startup
    {
        private readonly ILog _logger = new ConsoleAndMemoryLogger();
        private readonly TopExchangesStorage _database = new TopExchangesStorage();
        public IConfiguration Configuration { get; }


        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;

            var appConfig = configuration.GetSection("AppConfig");
            string horizonUrl = appConfig.GetValue<string>("HorizonApiUrl");
            int dataInterval = appConfig.GetValue<int>("DataCollectionInterval");

            var exchCollector = new TopExchangesCollector(_logger, new HorizonService(_logger, horizonUrl, dataInterval), _database, dataInterval);
            exchCollector.Start();
        }


        #region Methods called by the runtime

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(typeof(ILog), _logger);
            services.AddSingleton(typeof(TopExchangesStorage), _database);
            services.AddControllers();
        }

        public void Configure(IApplicationBuilder app)
        {
            //We can do this even in PROD for this kind of system
            app.UseDeveloperExceptionPage();

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
        #endregion
    }
}
