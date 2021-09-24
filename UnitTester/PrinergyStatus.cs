using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace ChateauSiteFlowApp
{
    public class PrinergyStatus
    {
        public static bool CheckPrinegyStatus()
        {
            Ping pingSender = new Ping();
            PingOptions options = new PingOptions();

            // Use the default Ttl value which is 128,
            // but change the fragmentation behavior.
            options.DontFragment = true;

            // Create a buffer of 32 bytes of data to be transmitted.
            string data = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
            byte[] buffer = Encoding.ASCII.GetBytes(data);
            int timeout = 120;
            PingReply reply = pingSender.Send("192.168.16.231", timeout, buffer, options);

            if (reply.Status == IPStatus.Success)
            {
                if (Directory.Exists(@"\\192.168.16.231\AraxiVolume_HW33546-46_J\Jobs\Auto_Impose\SmartHotFolders"))
                {
                    return true;
                }
            }


            return false;
        }
    }
}
