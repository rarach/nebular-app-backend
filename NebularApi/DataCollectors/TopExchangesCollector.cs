using NebularApi.Models.Horizon;
using NebularApi.Models.Nebular;
using System;
using System.Linq;
using System.Text.Json;
using System.Timers;


namespace NebularApi.DataCollectors
{
    /// <summary>
    /// Extract data about top exchanges in past 24 hours based on volume.
    /// </summary>
    /// <remarks>
    /// Exchanges are filtered through blacklist of scam tokens.
    /// </remarks>
    internal class TopExchangesCollector
    {
        private readonly ILog _logger;
        private readonly TopExchangesStorage _storage;
        private readonly string _horizonUrl;
        private readonly int _interval;
        private readonly Timer _timer;


        internal TopExchangesCollector(ILog logger, TopExchangesStorage storage, string horizonApiUrl, int intervalMinutes)
        {
            _logger = logger;
            _storage = storage;
            _horizonUrl = horizonApiUrl.TrimEnd('/') + "/trades?order=desc&limit=200";
            _interval = intervalMinutes;
            _timer = new Timer(intervalMinutes * 60 * 1000);
        }


        internal void Start()
        {
            _logger.Info("===================== Starting TopExchanges data collection =====================");
            _timer.AutoReset = true;
            _timer.Elapsed += Timer_Elapsed;
            _timer.Start();
            Timer_Elapsed(this, null);
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            var webClient = new System.Net.WebClient();
            try
            {
                string json = webClient.DownloadString(_horizonUrl);

                Trades trades = JsonSerializer.Deserialize<Trades>(json);
                _logger.Info($"Parsed {trades._embedded.records.Count} last trades");

                DateTime dataEnd = trades._embedded.records[0].LedgerCloseTime;
                DateTime dataStart = dataEnd.Subtract(new TimeSpan(24, 0, 0));

                Trade lastRecord = trades._embedded.records.Last();
                while (lastRecord.LedgerCloseTime > dataStart)
                {
                    string cursor = trades._embedded.records.Last().paging_token;
                    string url = $"{_horizonUrl}&cursor={cursor}";
                    json = webClient.DownloadString(url);
                    trades = JsonSerializer.Deserialize<Trades>(json);
                    _logger.Info($"Parsed {trades._embedded.records.Count} trades (last from {lastRecord.ledger_close_time})");
                    lastRecord = trades._embedded.records.Last();
                }



                //TODO!
                _storage.Data = new TopExchanges
                {
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.FFFZ"),
                    topExchanges = new[]
                    {
                        new TopExchange
                        {
                            baseAsset = new Asset
                            {
                                code = "ASDF",
                                issuer = new Account
                                {
                                    address = "GASDFFFFFFF",
                                    domain = "asdf.com"
                                }
                            },
                            counterAsset = new Asset
                            {
                                code = "xyz",
                                issuer = new Account
                                {
                                    address = "GDDDSCAMCOIN",
                                    domain = "example.org"
                                }
                            }
                        }
                    }
                };



                _logger.Info($"Going to sleep for {_interval} minutes.");
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message);
            }
        }
    }
}
