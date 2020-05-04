using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace EnergySolarLogger.Tests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMessage()
        {
            var output = new SolarMaxCollector("192.168.1.5", 12345).GenerateMessage(new string[] { "PAC", "KDY" });
            Assert.AreEqual(output, "{FB;00;1A|64:PAC;KDY|0563}");
        }

        [TestMethod]
        public void TestChecksum()
        {
            var output = new SolarMaxCollector("192.168.1.5", 12345).GenerateChecksum("FB;00;1A|64:PAC;KDY|");
            Assert.AreEqual(output, 1379);
        }


        [TestMethod]
        public void TestDecodeResponse()
        {
            var output = new SolarMaxCollector("192.168.1.5", 12345).DecodeResponse("{37;FB;21|64:PAC=47C;KDY=A9|0700}");
            Assert.AreEqual(output["PAC"], 1148);
            Assert.AreEqual(output["KDY"], 169);
            Assert.AreEqual(output.Count, 2);
        }
    }
}
