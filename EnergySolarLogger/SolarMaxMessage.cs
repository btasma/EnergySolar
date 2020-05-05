using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace EnergySolarLogger
{
    public class SolarMaxMessage
    {
        private readonly IEnumerable<string> _commands;

        public SolarMaxMessage(IEnumerable<string> commands)
        {
            _commands = commands;
        }

        private int GenerateChecksum(string message)
        {
            var msgbytes = Encoding.ASCII.GetBytes(message);

            int checksum = 0;
            foreach (var b in msgbytes)
            {
                checksum += b;
                checksum = (int)(checksum % Math.Pow(2, 16));
            }
            return checksum;
        }

        private string GenerateMessage()
        {
            var src = "FB";
            var dst = "00";
            var len = "00";
            var tmpcs = "0000";

            var msg = $"64:{string.Join(';', _commands)}";

            var lenstr = $"{{{src};{dst};{len}|{msg}|{tmpcs}}}";
            var lenhex = $"{lenstr.Length:X2}";


            var checkSum = $"{src};{dst};{lenhex}|{msg}|";
            var cs = GenerateChecksum(checkSum);
            var cshex = $"{cs:X4}";

            return $"{{{src};{dst};{lenhex}|{msg}|{cshex}}}";
        }

        public override string ToString()
        {
            return GenerateMessage();
        }

        public byte[] ToBytes()
        {
            return Encoding.ASCII.GetBytes(ToString());
        }
    }
}
