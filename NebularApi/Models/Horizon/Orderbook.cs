using System.Collections.Generic;


namespace NebularApi.Models.Horizon
{
    /// <summary>
    /// As seen on {HORIZON}/order_book
    /// </summary>
    public class Orderbook
    {
        public List<object> bids { get; set; }
        public List<object> asks { get; set; }
    }
}
