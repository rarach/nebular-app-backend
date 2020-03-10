
namespace NebularApi.Models.Nebular
{
    /// <summary>
    /// Data object that contains top exchanges by some criterion (e.g. num. of trades, volume...)
    /// </summary>
    public class TopExchanges
    {
        public string timestamp { get; set; }
        public TopExchange[] topExchanges { get; set; }
    }

    public class TopExchange
    {
        public Asset baseAsset { get; set; }
        public Asset counterAsset { get; set; }
    }

    public class Asset
    {
        public string code { get; set; }
        public Account issuer { get; set; }
    }

    public class Account
    {
        public string address { get; set; }
        public string domain { get; set; }
    }
}
