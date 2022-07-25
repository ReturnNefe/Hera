using Flurl.Http;
using System.Net.Sockets;

namespace Hera.Monitor
{
    internal class HTTPMonitor
    {
        static internal async Task<string> GetStatus(string address, int timeout, CancellationToken token)
        {
            try
            {
                var result = await address.WithTimeout(TimeSpan.FromMilliseconds(timeout)).GetAsync(token);
                return $"OK ({result.StatusCode})";
            }
            catch (FlurlHttpException)
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
