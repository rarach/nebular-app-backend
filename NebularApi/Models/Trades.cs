using System;
using System.Collections.Generic;


namespace NebularApi.Models
{
    /// <summary>
    /// As seen on HORIZON/trades
    /// </summary>
    public class Trades
    {
        public EmbededTradeRecords _embedded;
    }

    public class EmbededTradeRecords
    {
        public List<Trade> records;
    }

    public class Trade
    {
        public string paging_token;
        public string ledger_close_time;
        public decimal/*TODO: verify*/ base_amount;
        /// <summary>
        /// "native" for XLM
        /// </summary>
        public string base_asset_type;
        public string base_asset_code;
        public string base_asset_issuer;
        public decimal counter_amount;
        /// <summary>
        /// "native" for XLM
        /// </summary>
        public string counter_asset_type;
        public string counter_asset_code;
        public string counter_asset_issuer;


        public DateTime LedgerCloseTime
        {
            get
            {
                return DateTime.Parse(ledger_close_time);   //TODO: guess but might work :-|
            }
        }
    }
}
