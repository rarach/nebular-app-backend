﻿using System;
using System.Collections.Generic;


namespace NebularApi.Models.Horizon
{
    /// <summary>
    /// As seen on {HORIZON}/trades
    /// </summary>
    public class Trades
    {
        public EmbeddedTradeRecords _embedded { get; set; }
    }

    public class EmbeddedTradeRecords
    {
        public List<Trade> records { get; set; }
    }

    public class Trade
    {
        public string paging_token { get; set; }
        public string ledger_close_time { get; set; }
        public string base_amount { get; set; }
        /// <summary>
        /// "native" for XLM
        /// </summary>
        public string base_asset_type { get; set; }
        public string base_asset_code { get; set; }
        public string base_asset_issuer { get; set; }
        public string counter_amount { get; set; }
        /// <summary>
        /// "native" for XLM
        /// </summary>
        public string counter_asset_type { get; set; }
        public string counter_asset_code { get; set; }
        public string counter_asset_issuer { get; set; }

        public Price price { get; set; }


        internal decimal BaseAmount
        {
            get { return Decimal.Parse(base_amount); }
        }

        internal decimal CounterAmount
        {
            get { return Decimal.Parse(counter_amount); }
        }

        internal DateTime LedgerCloseTime
        {
            get { return DateTime.Parse(ledger_close_time); }
        }

        /// <summary>Price of base asset</summary>
        internal decimal BasePrice
        {
            get { return price.n / price.d; }
        }
    }


    public class Price
    {
        public decimal n { get; set; }
        public decimal d { get; set; }
    }
}
