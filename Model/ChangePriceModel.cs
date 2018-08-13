using Newtonsoft.Json;
using System;

namespace MarketData.Server.Model
{
    public class ChangePriceModel
    {
        public String Symbol { get; set; }
        public DateTime Date { get; set; }
        public Double? Bid { get; set; }
        public Double? Ask { get; set; }
        public Double? Last { get; set; }
        public Int32? Volume { get; set; }
        
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
