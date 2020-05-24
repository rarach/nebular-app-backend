using NebularApi.Models.Horizon;
using NebularApi.Models.Nebular;
using System;
using System.Collections.Generic;
using System.Timers;


namespace NebularApi.DataCollectors
{
    /// <summary>
    /// Extract data about top exchanges in past 24 hours based on volume.
    /// </summary>
    /// <remarks>
    /// Exchanges are filtered through blacklist of scam tokens. TODO!
    /// </remarks>
    internal class TopExchangesCollector
    {
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
                _logger.Warning("Cannot collect top exchanges now, another processing is still running");
                return;
            }

            _logger.Info("===================== Starting TopExchanges data collection =====================");
            _inProgress = true;

            List<Trade> trades = _horizon.GetTrades(24);

            try
            {
                Dictionary<string, decimal> volumes = CalculateVolume(trades);
                CollectTopExchanges(volumes, 12);

                _logger.Info($"Going to sleep for {_interval} minutes.");
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message);
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
            while (count-- > 0)
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

                string[] chunks = maxMarketId.Split(new char[] { '-', '/' });
                var exchange = new TopExchange
                {
                    baseAsset = new Asset { code = chunks[0] },
                    counterAsset = new Asset { code = chunks[2] }
                };

                Account baseIssuer = "native" == chunks[1] ? null : new Account { address = chunks[1] };
                if (null != baseIssuer)
                {
                    baseIssuer.domain = _horizon.GetIssuerDomain(exchange.baseAsset.code, baseIssuer.address);
                }
                exchange.baseAsset.issuer = baseIssuer;

                Account counterIssuer = "native" == chunks[3] ? null : new Account { address = chunks[3] };
                if (null != counterIssuer)
                {
                    counterIssuer.domain = _horizon.GetIssuerDomain(exchange.counterAsset.code, counterIssuer.address);
                }
                exchange.counterAsset.issuer = counterIssuer;

                topExchanges.Add(exchange);
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
            int count = 0;

            foreach (Trade trade in trades)
            {
                if (++count >= 1000)
                {
                    count = 0;
                    _logger.Info("Processed volume of another 1000 trades...");
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
                if ("native" != trade.base_asset_type && "native" != trade.counter_asset_type && trade.BasePrice < 0.01m)
                {
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
