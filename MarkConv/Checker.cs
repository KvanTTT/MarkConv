using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MarkConv.Links;

namespace MarkConv
{
    public class Checker : IDisposable
    {
        private readonly HttpClient _httpClient = new HttpClient {Timeout = TimeSpan.FromMilliseconds(3000)};
        private readonly ProcessorOptions _options;
        private readonly ILogger _logger;

        public Checker(ProcessorOptions options, ILogger logger)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Check(ParseResult parseResult)
        {
            var rootDirectory = Path.GetDirectoryName(parseResult.File.Name) ?? "";

            if (_options.CheckLinks)
            {
                Parallel.ForEach(parseResult.Links.Values, link =>
                {
                    switch (link)
                    {
                        case AbsoluteLink _:
                            if (!IsUrlAlive(link.Address))
                                _logger.Warn(
                                    $"Absolute Link {link.Address} at {link.Node.LineColumnSpan} is probably broken");
                            break;

                        case RelativeLink relativeLink:
                            string normalizedAddress = HeaderToLinkConverter.ConvertHeaderTitleToLink(relativeLink.Address,
                                _options.InputMarkdownType);
                            if (!parseResult.Anchors.ContainsKey(normalizedAddress))
                                _logger.Warn($"Relative link {link.Address} at {link.Node.LineColumnSpan} is broken");
                            break;

                        case LocalLink localLink:
                            var fullPath = Path.Combine(rootDirectory, localLink.Address);
                            if (!File.Exists(fullPath))
                                _logger.Warn($"Local file {fullPath} at {link.Node.LineColumnSpan} does not exist");
                            break;
                    }
                });
            }
        }

        private bool IsUrlAlive(string url)
        {
            try
            {
                if (Uri.TryCreate(url, UriKind.Absolute, out Uri requestUri))
                {
                    var result = _httpClient.GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead, CancellationToken.None).Result;
                    return result.IsSuccessStatusCode;
                }
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