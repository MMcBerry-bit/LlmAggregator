using System.ComponentModel;
using Microsoft.VisualStudio.Shell;
namespace LlmAggregator
{
    public class GeminiOptionsPage : DialogPage
    {
        [Category("Credentials")]
        [DisplayName("API Key")]
        [Description("Global API key or path to service account JSON. Can be overridden per-endpoint.")]
        public string ApiKey { get; set; } = "";

        [Browsable(false)]
        public string EndpointsCsv { get; set; } = "";

        [Browsable(false)]
        public EndpointConfig[] EndpointConfigs { get; set; } = new EndpointConfig[0];
    }
}
