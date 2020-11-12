using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using MarkConv.Nodes;

namespace MarkConv
{
    public class Checker : IDisposable
    {
        private readonly HttpClient _httpClient = new HttpClient {Timeout = TimeSpan.FromMilliseconds(2000)};
        private readonly ILogger _logger;

        public Checker(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Check(Node root, IReadOnlyList<Link> links)
        {
            Parallel.ForEach(links, link =>
            {
                if (link.LinkType == LinkType.Absolute)
                {
                    if (!IsUrlAlive(link.Address))
                    {
                        _logger.Warn($"Link {link.Address} at {link.Node.LineColumnSpan} is probably broken");
                    }
                }
            });
        }

        private bool IsUrlAlive(string url)
        {
            try
            {
                using var response = _httpClient.GetAsync(url).Result;
                return response.IsSuccessStatusCode;
            }
            catch
            {
                // ignored
            }

            return false;
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }
    }
}