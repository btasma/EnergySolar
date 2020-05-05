using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace EnergySolarLogger
{
    public class SolarMaxResponse : Dictionary<string, int>
    {
        public SolarMaxResponse(byte[] response)
        {
            ResponseToDictionary(Encoding.ASCII.GetString(response), this);
        }

        private void ResponseToDictionary(string response, Dictionary<string, int> dictionary)
        {
            var rx = new Regex(@"\:(.*)\|");
            var commandResponses = rx.Match(response).Groups[1].Value;

            foreach (var command in commandResponses.Split(';'))
            {
                var keyvalue = command.Split('=');
                dictionary.Add(keyvalue[0], Convert.ToInt32(keyvalue[1], 16));
            }
        }
    }
}
