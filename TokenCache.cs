using System;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;

namespace LlmAggregator
{
    // Simple token cache for service account tokens. Not thread-safe; acceptable for sample.
    internal static class TokenCache
    {
        private static string? cachedToken;
        private static DateTimeOffset expiry = DateTimeOffset.MinValue;
        private static string? cachedKeyOrPath;

        public static async Task<string?> GetTokenAsync(string serviceAccountJsonOrFilePath)
        {
            // If same input and token not expired, return cached
            if (!string.IsNullOrWhiteSpace(cachedToken) && !string.IsNullOrWhiteSpace(cachedKeyOrPath) && string.Equals(cachedKeyOrPath, serviceAccountJsonOrFilePath, StringComparison.Ordinal) && DateTimeOffset.UtcNow < expiry)
            {
                return cachedToken;
            }

            GoogleCredential credential;
            if (serviceAccountJsonOrFilePath.Trim().StartsWith("{"))
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(serviceAccountJsonOrFilePath);
                using (var ms = new System.IO.MemoryStream(bytes))
                {
                    credential = GoogleCredential.FromStream(ms);
                }
            }
            else
            {
                credential = GoogleCredential.FromFile(serviceAccountJsonOrFilePath);
            }

            var scoped = credential.CreateScoped(new[] { "https://www.googleapis.com/auth/cloud-platform" });
            // UnderlyingCredential may implement GetAccessTokenForRequestAsync
            var token = await scoped.UnderlyingCredential.GetAccessTokenForRequestAsync();
            // Token lifetime isn't directly exposed; refresh after 55 minutes to be safe
            expiry = DateTimeOffset.UtcNow.AddMinutes(55);
            cachedToken = token;
            cachedKeyOrPath = serviceAccountJsonOrFilePath;
            return token;
        }
    }
}
