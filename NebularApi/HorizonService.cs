using NebularApi.Models.Horizon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;


namespace NebularApi
{
    /// <summary>
    /// Requestor for the Horizon API
    /// </summary>
    internal class HorizonService
    {
        private readonly ILog _logger;
        private readonly string _horizonUrl;
        private readonly TimeSpan _cacheTimeout;
        private readonly WebClient _webClient = new WebClient();
        private readonly Dictionary<string, Tuple<DateTime, decimal>> _priceCache = new Dictionary<string, Tuple<DateTime, decimal>>();
        private readonly Dictionary<string, string> _domainCache = new Dictionary<string, string>();


        /// <summary>Constructor</summary>
        /// <param name="cacheTimeout">
        /// Cache timeout in minutes. Some data won't be re-requested within the slot.
        /// </param>
        internal HorizonService(ILog logger, string horizonApiUrl, int cacheTimeout)
        {
            _logger = logger;
            _horizonUrl = horizonApiUrl.TrimEnd('/');
            _cacheTimeout = new TimeSpan(0, cacheTimeout, 0);
        }

        internal List<Trade> GetTrades(int hours)
        {
            var trades = new List<Trade>();
            string apiUrl = _horizonUrl + "/trades?order=desc&limit=200";

            try
            {
                string json = _webClient.DownloadString(apiUrl);

                Trades tradesData = JsonSerializer.Deserialize<Trades>(json);
                _logger.Info($"Parsed {tradesData._embedded.records.Count} last trades");

                trades.AddRange(tradesData._embedded.records);

                DateTime dataEnd = tradesData._embedded.records[0].LedgerCloseTime;
                DateTime dataStart = dataEnd.Subtract(new TimeSpan(hours, 0, 0));

                Trade lastRecord = tradesData._embedded.records.Last();
                while (lastRecord.LedgerCloseTime > dataStart)
                {
                    string cursor = tradesData._embedded.records.Last().paging_token;
                    string url = $"{apiUrl}&cursor={cursor}";
                    json = _webClient.DownloadString(url);
                    tradesData = JsonSerializer.Deserialize<Trades>(json);
                    trades.AddRange(tradesData._embedded.records);

                    _logger.Info($"Parsed {tradesData._embedded.records.Count} more trades (last from {lastRecord.ledger_close_time})");
                    lastRecord = tradesData._embedded.records.Last();
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message + Environment.NewLine + ex.StackTrace);
            }

            return trades;
        }

        internal decimal? GetAssetPriceInNative(string assetCode, string assetType, string assetIssuer)
        {
            string id = assetCode + "-" + assetIssuer;
            if (_priceCache.ContainsKey(id))
            {
                Tuple<DateTime, decimal> oldPrice = _priceCache[id];
                if (oldPrice.Item1 < DateTime.UtcNow.Subtract(_cacheTimeout))
                {
                    _priceCache.Remove(id);
                }
                else
                {
                    return oldPrice.Item2;
                }
            }

            string url = $"{_horizonUrl}/trades?base_asset_code={assetCode}&base_asset_type={assetType}&base_asset_issuer={assetIssuer}&counter_asset_code=XLM&counter_asset_type=native&order=desc&limit=1";

            string json = _webClient.DownloadString(url);
            var trades = JsonSerializer.Deserialize<Trades>(json);

            if (null == trades._embedded || null == trades._embedded.records || trades._embedded.records.Count <= 0)
            {
                return null;
            }

            Trade trade = trades._embedded.records[0];
            if (trade.LedgerCloseTime < DateTime.UtcNow.Subtract(new TimeSpan(3, 0, 0, 0)))
            {
                //No trades vs XLM in past 3 days
                return null;
            }

            _priceCache.Add(id, new Tuple<DateTime, decimal>(DateTime.UtcNow, trade.BasePrice));
            _logger.Info($"Cached price of {assetCode}-{assetIssuer} ({trade.BasePrice:0.00000000} XLM)");

            return trade.BasePrice;
        }

        internal string GetIssuerDomain(string assetCode, string issuerAddress)
        {
            string id = assetCode + "-" + issuerAddress;
            if (_domainCache.ContainsKey(id))
            {
                //Asset's domain should barely change, normally never
                return _domainCache[id];
            }

            string url = $"{_horizonUrl}/assets?asset_code={assetCode}&asset_issuer={issuerAddress}";
            Assets assets = null;

            try
            {
                string json = _webClient.DownloadString(url);
                assets = JsonSerializer.Deserialize<Assets>(json);
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to fetch issuer's domain. Message: " + ex.Message);
            }

            if (null == assets._embedded || null == assets._embedded.records || assets._embedded.records.Count <= 0)
            {
                return null;
            }

            string domain = assets._embedded.records[0]?._links?.toml?.href;
            if (!String.IsNullOrWhiteSpace(domain))
            {
                Uri tempUri;
                if (Uri.TryCreate(domain, UriKind.RelativeOrAbsolute, out tempUri))
                {
                    domain = tempUri.Host;
                }
            }
            _domainCache.Add(id, domain);
            _logger.Info($"Asset {assetCode}-{issuerAddress} issued by {domain}");

            return domain;
        }

        /// <summary>
        /// Evaluates if market is rich enough based on its orderbook. i.e. with small number of asks/bids
        /// </summary>
        /// <param name="marketId">Exchange to be evaluated</param>
        /// <param name="minOrderbookItems">Min. number of asks+bids to consider a market rich enough</param>
        internal bool HasSufficientOrderbook(string marketId, ushort minOrderbookItems)
        {
            string url = GetOrderbookUrl(marketId, minOrderbookItems);
            try
            {
                string json = _webClient.DownloadString(url);
                Orderbook orderbook = JsonSerializer.Deserialize<Orderbook>(json);
                if (orderbook.bids.Count + orderbook.asks.Count >= minOrderbookItems)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to get order-book for {marketId}. Message: {ex.Message}");
                return true;
            }
        }

        private string GetOrderbookUrl(string marketId, ushort maxItems)
        {
            string[] chunks = marketId.Split(new char[] { '-', '/' });
            string baseAssetCode = chunks[0];
            string baseIssuer = "native" == chunks[1] ? null : chunks[1];
            string counterAssetCode = chunks[2];
            string counterIssuer = "native" == chunks[3] ? null : chunks[3];


            string url = $"{_horizonUrl}/order_book?selling_asset_type=";
            if (null == baseIssuer)
            {
                url += "native&selling_asset_code=XLM";
            }
            else
            {
                if (baseAssetCode.Length <= 4)
                {
                    url += "credit_alphanum4";
                }
                else
                {
                    url += "credit_alphanum12";
                }

                url += $"&selling_asset_code={baseAssetCode}&selling_asset_issuer={baseIssuer}";
            }

            if (null == counterIssuer)
            {
                url += "&buying_asset_type=native&buying_asset_code=XLM";
            }
            else
            {
                if (counterAssetCode.Length <= 4)
                {
                    url += "&buying_asset_type=credit_alphanum4";
                }
                else
                {
                    url += "&buying_asset_type=credit_alphanum12";
                }

                url += $"&buying_asset_code={counterAssetCode}&buying_asset_issuer={counterIssuer}";
            }

            url += $"&limit={maxItems / 2 + 1}";

            return url;
        }
    }
}
