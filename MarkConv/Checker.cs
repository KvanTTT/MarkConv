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
            _options = options;
            _logger = logger;
        }

        public void Check(ParseResult parseResult)
        {
            var rootDirectory = Path.GetDirectoryName(parseResult.File.Name) ?? "";

            var parallelOptions = new ParallelOptions {MaxDegreeOfParallelism = 8};

            Parallel.ForEach(parseResult.Links.Values, parallelOptions, link =>
            {
                CheckLink(parseResult, link, rootDirectory);
            });

            Parallel.ForEach(parseResult.LinksMap, parallelOptions, linkMap =>
            {
                CheckLink(parseResult, linkMap.Key, rootDirectory);
                CheckLink(parseResult, linkMap.Value, rootDirectory);
            });

            if (parseResult.HeaderImageLink != null)
                CheckLink(parseResult, parseResult.HeaderImageLink, rootDirectory);
        }

        private void CheckLink(ParseResult parseResult, Link link, string rootDirectory)
        {
            switch (link)
            {
                case AbsoluteLink _:
                    if (_options.CheckLinks && !IsUrlAlive(link.Address))
                        _logger.Warn(
                            $"Absolute Link {link.Address} at {link.Node.LineColumnSpan} is probably broken");
                    break;

                case RelativeLink _:
                    string normalizedAddress = HeaderToLinkConverter.ConvertHeaderTitleToLink(link.Address,
                        _options.InputMarkdownType);
                    if (!parseResult.Anchors.ContainsKey(normalizedAddress))
                        _logger.Warn($"Relative link {link.Address} at {link.Node.LineColumnSpan} is broken");
                    break;

                case LocalLink _:
                    var fullPath = Path.Combine(rootDirectory, link.Address);
                    if (!File.Exists(fullPath))
                    {
                        var linkFileName = link.Node.File.Name;
                        string suffix = linkFileName != parseResult.File.Name
                            ? $" at {linkFileName}"
                            : "";
                        _logger.Warn($"Local file {fullPath} at {link.Node.LineColumnSpan}{suffix} does not exist");
                    }
                    break;
            }
        }

        private bool IsUrlAlive(string url)
        {
            try
            {
                if (Uri.TryCreate(url, UriKind.Absolute, out Uri? requestUri))
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