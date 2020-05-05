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

        public SolarMaxResponse SendMessage(SolarMaxMessage message)
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.SendTimeout = 1000;
            socket.ReceiveTimeout = 1000;

            socket.Connect(_host, _port);
            socket.Send(message.ToBytes());

            var receiveBuffer = new byte[999];
            socket.Receive(receiveBuffer);
            socket.Close();

            return new SolarMaxResponse(receiveBuffer);
        }


    }
}
