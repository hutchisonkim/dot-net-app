using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
#nullable enable
using System.Collections.Generic;

namespace DotNetApp.Client.Tests
{
    // Simple HttpMessageHandler that returns the provided response body and status code.
    public class TestHttpMessageHandler : HttpMessageHandler
    {
        private readonly string _responseBody;
        private readonly HttpStatusCode _statusCode;
        private readonly int _delayMs;

        public TestHttpMessageHandler(string responseBody, HttpStatusCode statusCode, int delayMs = 0)
        {
            _responseBody = responseBody;
            _statusCode = statusCode;
            _delayMs = delayMs;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_delayMs > 0)
            {
                await Task.Delay(_delayMs, cancellationToken);
            }
            
            var msg = new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_responseBody)
            };
            msg.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            return msg;
        }
    }

    // Lightweight IConfiguration wrapper around an in-memory dictionary for tests.
    public class SimpleConfiguration : IConfiguration
    {
        private readonly IConfigurationRoot _root;

        public SimpleConfiguration(IDictionary<string, string?> items)
        {
            _root = new ConfigurationBuilder().AddInMemoryCollection(items).Build();
        }

        public string? this[string key]
        {
            get => _root[key];
            set => _root[key] = value;
        }

        public IEnumerable<IConfigurationSection> GetChildren() => _root.GetChildren();

        public IChangeToken GetReloadToken() => _root.GetReloadToken();

        public IConfigurationSection GetSection(string key) => _root.GetSection(key);

    }

}
