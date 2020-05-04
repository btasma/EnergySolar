using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace EnergySolarLogger
{
    public class SolarMaxCollector
    {
        private string _host;
        private int _port;

        public SolarMaxCollector(string host, int port)
        {
            _host = host;
            _port = port;
        }

        public Dictionary<string, int> SendMessage(string[] cmds)
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            socket.Connect(_host, _port);
            socket.Send(Encoding.ASCII.GetBytes(GenerateMessage(cmds)));

            var receiveBuffer = new byte[999];
            socket.Receive(receiveBuffer);
            socket.Close();

            return DecodeResponse(Encoding.ASCII.GetString(receiveBuffer));
        }

        public int GenerateChecksum(string msg)
        {
            var msgbytes = Encoding.ASCII.GetBytes(msg);
            int sum = 0;
            foreach (var b in msgbytes)
		    {
			    sum += b;
                sum = (int) (sum % Math.Pow(2, 16));
            }
            return sum;
        }

        public string GenerateMessage(string[] cmds)
        {
            var src = "FB";
            var dst = "00";
            var len = "00";
            var tmpcs = "0000";

            var msg = $"64:{string.Join(';', cmds)}";

            var lenstr = $"{{{src};{dst};{len}|{msg}|{tmpcs}}}";
            var lenhex = $"{lenstr.Length:X2}";


            var checkSum = $"{src};{dst};{lenhex}|{msg}|";
            var cs = GenerateChecksum(checkSum);
            var cshex = $"{cs:X4}";

            return $"{{{src};{dst};{lenhex}|{msg}|{cshex}}}";
        }

        public Dictionary<string, int> DecodeResponse(string response)
        {
            var dic = new Dictionary<string, int>();

            var rx = new Regex(@"\:(.*)\|");
            var es = rx.Match(response).Groups[1].Value;

            foreach(var cmd in es.Split(';'))
            {
                var keyvalue = cmd.Split('=');
                dic.Add(keyvalue[0], Convert.ToInt32(keyvalue[1], 16));
            }

            return dic;
        }
    }
}
