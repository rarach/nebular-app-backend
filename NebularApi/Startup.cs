using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NebularApi.DataCollectors;


namespace NebularApi
{
    public class Startup
    {
        private readonly ILog _logger = new MemoryLogger();
        private readonly TopExchangesStorage _database = new TopExchangesStorage();
        public IConfiguration Configuration { get; }


        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;

            var appConfig = configuration.GetSection("AppConfig");
            string horizonUrl = appConfig.GetValue<string>("HorizonApiUrl");
            int dataInterval = appConfig.GetValue<int>("DataCollectionInterval");

//            ILog logger = new ConsoleAndFileLogger(System.AppDomain.CurrentDomain.BaseDirectory + "data\\logs.txt");
             var exchCollector = new TopExchangesCollector(_logger, _database, horizonUrl, dataInterval);
            exchCollector.Start();
        }


        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(typeof(ILog), _logger);
            services.AddSingleton(typeof(TopExchangesStorage), _database);
            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
