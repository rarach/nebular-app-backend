using NebularApi.Models.Horizon;
using NebularApi.Models.Nebular;
using System;
using System.Collections.Generic;
using System.Timers;


namespace NebularApi.DataCollectors
{
    /// <summary>
    /// Extract data about top exchanges in past N hours based on volume.
    /// </summary>
    /// <remarks>
    /// Exchanges are filtered to exclude dubious markets (e.g. wash trading, hot/cold wallet transfers).
    /// </remarks>
    internal class TopExchangesCollector
    {
        private const int TRADE_HISTORY_IN_HOURS = 8;
        private const int TOP_EXCHANGES_COUNT = 12;
        private const int TRADE_HISTORY_HOURS = 8;
        private const int MIN_TRADE_COUNT = 40;
        private readonly ILog _logger;
        private readonly HorizonService _horizon;
        private readonly TopExchangesStorage _storage;
        private readonly int _interval;
        private readonly Timer _timer;
        private bool _inProgress = false;


        internal TopExchangesCollector(ILog logger, HorizonService horizon, TopExchangesStorage storage, int intervalMinutes)
        {
            _logger = logger;
            _horizon = horizon;
            _storage = storage;
            _interval = intervalMinutes;
            _timer = new Timer(intervalMinutes * 60 * 1000);
        }


        internal void Start()
        {
            _timer.AutoReset = true;
            _timer.Elapsed += Timer_Elapsed;
            _timer.Start();

            new System.Threading.Thread(() => Timer_Elapsed(this, null)).Start();
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (_inProgress)
            {
                _logger.Warning("Cannot collect top exchanges now, another round is still running");
                return;
            }

            _logger.Info("===================== Starting TopExchanges data collection =====================");
            _inProgress = true;

            List<Trade> trades = _horizon.GetTrades(TRADE_HISTORY_IN_HOURS);

            try
            {
                Dictionary<string, decimal> volumes = CalculateVolume(trades);
                CollectTopExchanges(volumes, TOP_EXCHANGES_COUNT);

                _logger.Info($"Going to sleep for {_interval} minutes.");
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message + Environment.NewLine + ex.StackTrace);
            }
            finally
            {
                _inProgress = false;
            }

            _logger.Info("===================== Finished TopExchanges data collection =====================");
        }


        /// <summary>Get given number of top exchanges by volume (in XLM)</summary>
        private void CollectTopExchanges(Dictionary<string, decimal> volumes, int count)
        {
            var topExchanges = new List<TopExchange>();
            while (count > 0)
            {
                decimal maxVolume = -1m;
                string maxMarketId = null;

                foreach (var volume in volumes)
                {
                    if (volume.Value > maxVolume)
                    {
                        maxVolume = volume.Value;
                        maxMarketId = volume.Key;
                    }
                }

                //We didn't have enough data to pick from. Should never happen.
                if (null == maxMarketId)
                {
                    return;
                }

                volumes.Remove(maxMarketId);

                TopExchange exchange = TopExchange.Create(maxMarketId, _horizon);

                if (!_horizon.HasSufficientOrderbook(exchange, 30))
                {
                    _logger.Info($"Market {maxMarketId} disqualified due to tiny orderbook.");
                    continue;
                }

                int recentTradesCount = _horizon.GetTrades(exchange, TRADE_HISTORY_HOURS).Count;
                if (recentTradesCount < MIN_TRADE_COUNT)
                {
                    _logger.Info($"Market {exchange} disqualified due to poor trade history (only {recentTradesCount} trades in last {TRADE_HISTORY_HOURS}h).");
                }

                topExchanges.Add(exchange);
                count--;
            }

            _storage.Data = new TopExchanges
            {
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.FFFZ"),
                topExchanges = topExchanges.ToArray()
            };
        }


        private Dictionary<string, decimal> CalculateVolume(IList<Trade> trades)
        {
            var volumes = new Dictionary<string, decimal>();
            int tradesCount = trades.Count;

            for (int i=1; i <= tradesCount; i++)
            {
                Trade trade = trades[i-1];

                if (i % 2000 == 0)
                {
                    _logger.Info($"Processed volume of {i}/{tradesCount} trades...");
                }

                decimal volumeInNative = -1m;
                string baseAssetId = "XLM-native";
                string counterAssetId = "XLM-native";

                if ("native" != trade.base_asset_type)
                {
                    baseAssetId = trade.base_asset_code + "-" + trade.base_asset_issuer;
                }
                else
                {
                    //Base asset is XLM => we have direct volume
                    volumeInNative = trade.BaseAmount;
                }

                if ("native" != trade.counter_asset_type)
                {
                    counterAssetId = trade.counter_asset_code + "-" + trade.counter_asset_issuer;
                }
                else
                {
                    //Counter asset is XLM => we have direct volume
                    volumeInNative = trade.CounterAmount;
                }

                string marketId = baseAssetId + "/" + counterAssetId;
                string marketIdInverse = counterAssetId + "/" + baseAssetId;

                //We might be already counting in swapped market
                if (volumes.ContainsKey(marketIdInverse))
                {
                    marketId = marketIdInverse;
                }
                //For non-XLM markets, swap the assets order if the counter asset is significantly more valuable than the base asset.
                else if ("native" != trade.base_asset_type && "native" != trade.counter_asset_type && trade.BasePrice < 0.01m)
                {
                    //If we already have some volume for the marketId, rewire it to marketIdInverse
                    decimal volumeSoFar;
                    if (volumes.TryGetValue(marketId, out volumeSoFar))
                    {
                        volumes.Add(marketIdInverse, volumeSoFar);
                        volumes.Remove(marketId);
                    }

                    marketId = marketIdInverse;
                }

                if (!volumes.ContainsKey(marketId))
                {
                    volumes.Add(marketId, 0m);
                }

                if (volumeInNative > 0m)
                {
                    volumes[marketId] += volumeInNative;
                    continue;
                }

                //Try to find price of base asset in XLM
                decimal? price = _horizon.GetAssetPriceInNative(trade.base_asset_code, trade.base_asset_type, trade.base_asset_issuer);
                if (null != price)
                {
                    volumeInNative = price.Value * trade.BaseAmount;
                    volumes[marketId] += volumeInNative;
                    continue;
                }

                //Find price of counter asset
                price = _horizon.GetAssetPriceInNative(trade.counter_asset_code, trade.counter_asset_type, trade.counter_asset_issuer);
                if (null != price)
                {
                    volumeInNative = price.Value * trade.CounterAmount;
                    volumes[marketId] += volumeInNative;
                }
            }

            return volumes;
        }
    }
}
