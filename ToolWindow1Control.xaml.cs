using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.Shell;
using System.Net.Http;
using System.Threading.Tasks;
using System;
using System.Linq;
using Google.Apis.Auth.OAuth2;
using System.Threading;
using Newtonsoft.Json.Linq;

namespace LlmAggregator
{
    /// <summary>
    /// Interaction logic for ToolWindow1Control.
    /// </summary>
    public partial class ToolWindow1Control : UserControl
    {
        private readonly AsyncPackage package;
        private static readonly HttpClient httpClient = new HttpClient();

        /// <summary>
        /// Initializes a new instance of the <see cref="ToolWindow1Control"/> class.
        /// </summary>
        public ToolWindow1Control()
        {
            this.InitializeComponent();
        }

/// <summary>
/// Initializes a new instance that has access to the package (to read options).
/// </summary>
/// <param name="package">The owning AsyncPackage.</param>
public ToolWindow1Control(AsyncPackage package) : this()
{
    this.package = package;
}

[SuppressMessage("Microsoft.Globalization", "CA1300:SpecifyMessageBoxOptions", Justification = "Sample code")]
        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:ElementMustBeginWithUpperCaseLetter", Justification = "Default event handler naming pattern")]
        private void button1_Click(object sender, RoutedEventArgs e)
        {
            if (this.package != null)
            {
                var page = (GeminiOptionsPage)this.package.GetDialogPage(typeof(GeminiOptionsPage));
                MessageBox.Show($"API Key: {page.ApiKey}\nEndpoints: {page.EndpointsCsv}", "ToolWindow1");
            }
            else
            {
                MessageBox.Show("Package not available", "ToolWindow1");
            }
        }

        private async void aggregateButton_Click(object sender, RoutedEventArgs e)
        {
            var prompt = PromptTextBox.Text;
            if (string.IsNullOrWhiteSpace(prompt))
            {
                MessageBox.Show("Enter a prompt first.", "Aggregate");
                return;
            }
            if (package == null)
            {
                MessageBox.Show("Package not available", "Aggregate");
                return;
            }
            var page = (GeminiOptionsPage)this.package.GetDialogPage(typeof(GeminiOptionsPage));
            var endpointConfigs = page.EndpointConfigs ?? new EndpointConfig[0];
            if (endpointConfigs.Length == 0)
            {
                MessageBox.Show("No endpoints configured. Use 'Configure Endpoints' to add endpoints.", "Aggregate");
                return;
            }
            ResultTextBox.Text = "Running...";
            try
            {
                var tasks = endpointConfigs.Select(cfg => CallVertexAiEndpointAsync(cfg.Url, string.IsNullOrWhiteSpace(cfg.ApiKey) ? page.ApiKey : cfg.ApiKey, prompt));
                var results = await Task.WhenAll(tasks);
                var aggregated = string.Join("\n\n---\n\n", results);
                ResultTextBox.Text = aggregated;
            }
            catch(Exception ex)
            {
                ResultTextBox.Text = "Error: " + ex.Message;
            }
        }

        private static string EscapeJsonString(string s)
        {
            if (s == null) return string.Empty;
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r\n", "\\n").Replace("\n", "\\n").Replace("\r", "\\n").Replace("\t", "\\t");
        }

        private void configureEndpoints_Click(object sender, RoutedEventArgs e)
        {
            if (package == null)
            {
                MessageBox.Show("Package not available", "Configure Endpoints");
                return;
            }
            var page = (GeminiOptionsPage)this.package.GetDialogPage(typeof(GeminiOptionsPage));
            var dialog = new EndpointConfigDialog();
            dialog.Configs = page.EndpointConfigs ?? new EndpointConfig[0];
            // show as WinForms dialog
            var result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                page.EndpointConfigs = dialog.Configs;
                // Persist immediately
                try
                {
                    page.SaveSettingsToStorage();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to save settings: {ex.Message}", "Configure Endpoints");
                }
                MessageBox.Show("Endpoints updated and saved.", "Configure Endpoints");
            }
        }

        private async Task<string> CallVertexAiEndpointAsync(string endpoint, string serviceAccountJsonOrApiKey, string prompt)
        {
            try
            {
                // If the field contains a JSON path or JSON content, exchange for access token using Google.Apis.Auth
                string bearerToken = serviceAccountJsonOrApiKey;
                if (!string.IsNullOrWhiteSpace(serviceAccountJsonOrApiKey) && (serviceAccountJsonOrApiKey.Trim().EndsWith(".json", StringComparison.OrdinalIgnoreCase) || serviceAccountJsonOrApiKey.Trim().StartsWith("{")))
                {
                    bearerToken = await TokenCache.GetTokenAsync(serviceAccountJsonOrApiKey);
                }

                var req = new HttpRequestMessage(HttpMethod.Post, endpoint);
                // Vertex AI generative models accept a specific JSON shape. Use a simple common form here.
                var requestJson = "{\"instances\": [{\"content\": \"" + EscapeJsonString(prompt) + "\"}]}";
                req.Content = new StringContent(requestJson, System.Text.Encoding.UTF8, "application/json");
                if (!string.IsNullOrEmpty(bearerToken))
                {
                    req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", bearerToken);
                }
                var resp = await httpClient.SendAsync(req);
                var body = await resp.Content.ReadAsStringAsync();
                try
                {
                    // Try to parse Vertex AI response and extract generated text.
                    var json = JObject.Parse(body);
                    // Vertex may return 'predictions' or 'outputs' or 'candidates'. Try common locations.
                    var text = "";
                    if (json["predictions"] != null)
                    {
                        text = json["predictions"].First?.ToString() ?? json["predictions"].ToString();
                    }
                    else if (json["outputs"] != null)
                    {
                        text = json["outputs"].First?.ToString() ?? json["outputs"].ToString();
                    }
                    else if (json["candidates"] != null)
                    {
                        text = json["candidates"].First?["content"]?.ToString() ?? json["candidates"].First?.ToString();
                    }
                    else if (json["instances"] != null)
                    {
                        // Newer Vertex generative response shapes
                        var inst = json["instances"].First;
                        text = inst?["content"]?.ToString() ?? inst?.ToString();
                    }
                    if (string.IsNullOrWhiteSpace(text)) text = body;
                    return $"[{endpoint}] {text}";
                }
                catch
                {
                    return $"[{endpoint}] {body}";
                }
            }
            catch(Exception ex)
            {
                return $"[{endpoint}] Error: {ex.Message}";
            }
        }
    }
}
