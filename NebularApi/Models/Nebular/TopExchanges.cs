
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
        public Asset baseAsset { get; }
        public Asset counterAsset { get; }

        private TopExchange(Asset baseAsset, Asset counterAsset)
        {
            this.baseAsset = baseAsset;
            this.counterAsset = counterAsset;
        }

        internal static TopExchange Create(string marketId, HorizonService horizon)
        {
            string[] chunks = marketId.Split(new char[] { '-', '/' });
            var exchange = new TopExchange(new Asset { code = chunks[0] }, new Asset { code = chunks[2] });

            exchange.baseAsset.type = exchange.baseAsset.code.Length <= 4 ? "credit_alphanum4" : "credit_alphanum12";
            exchange.counterAsset.type = exchange.counterAsset.code.Length <= 4 ? "credit_alphanum4" : "credit_alphanum12";

            Account baseIssuer = "native" == chunks[1] ? null : new Account { address = chunks[1] };
            exchange.baseAsset.issuer = baseIssuer;
            if (null != exchange.baseAsset.issuer)
            {
                exchange.baseAsset.issuer.domain = horizon.GetIssuerDomain(exchange.baseAsset.code, baseIssuer.address);
            }
            else
            {
                exchange.baseAsset.type = "native";
            }

            Account counterIssuer = "native" == chunks[3] ? null : new Account { address = chunks[3] };
            exchange.counterAsset.issuer = counterIssuer;
            if (null != counterIssuer)
            {
                counterIssuer.domain = horizon.GetIssuerDomain(exchange.counterAsset.code, counterIssuer.address);
            }
            else
            {
                exchange.counterAsset.type = "native";
            }

            return exchange;
        }

        public override string ToString()
        {
            string text = baseAsset.code;
            if (null != baseAsset.issuer)
            {
                text += "-" + baseAsset.issuer.address;
            }

            text += "/" + counterAsset.code;
            if (null != counterAsset.issuer)
            {
                text += "-" + counterAsset.issuer.address;
            }

            return text;
        }
    }

    public class Asset
    {
        public string code { get; set; }
        public string type { get; set; }
        public Account issuer { get; set; }
    }

    public class Account
    {
        public string address { get; set; }
        public string domain { get; set; }
    }
}
