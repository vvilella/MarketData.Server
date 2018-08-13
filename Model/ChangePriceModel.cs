using Newtonsoft.Json;
using System;

namespace MarketData.Server.Model
{
    public class ChangePriceModel
    {
        public String Symbol { get; set; }
        public DateTime Date { get; set; }
        public Double Price { get; set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
