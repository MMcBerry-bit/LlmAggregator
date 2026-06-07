using System;

namespace LlmAggregator
{
    public class EndpointConfig
    {
        public string Name { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty; // optional per-endpoint key or service-account path
    }
}
