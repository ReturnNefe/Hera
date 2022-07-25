using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace Hera.Monitor
{
    internal class TCPMonitor
    {
        static internal IPAddress GetIPAddress(string text)
        {
            var regex = new Regex(@"^(((\d{1,2})|(1\d{2})|(2[0-4]\d)|(25[0-5]))\.){3}((\d{1,2})(1\d{2})|(2[0-4]\d)|(25[0-5]))$");

            // text 是 IP
            if (regex.IsMatch(text))
                return IPAddress.Parse(text);
            // text 是域名
            else
                return Dns.GetHostAddresses(text)[0];
        }

        static internal async Task<string> GetStatus(IPAddress ipAddress, int port, int timeout, CancellationToken token)
        {
            try
            {
                var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                await Task.Run(() => socket.Connect(new IPEndPoint(ipAddress, port))).WaitAsync(TimeSpan.FromMilliseconds(timeout), token);
                return "OK";
            }
            catch (TimeoutException)
            {
                return "Timeout";
            }
            catch (SocketException)
            {
                return "Timeout";
            }
            catch (AggregateException)
            {
                return "Timeout";
            }
        }
    }
}
