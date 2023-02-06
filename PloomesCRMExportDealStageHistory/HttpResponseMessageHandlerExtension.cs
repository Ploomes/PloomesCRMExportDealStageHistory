using System.Net;

namespace PloomesCRMExportDealStageHistory
{
    public static class HttpResponseMessageHandlerExtension
    {
        public static async Task EnsureSuccessStatusCodeAsync(this HttpResponseMessage response, string requestBody)
        {
            if (response.IsSuccessStatusCode)
            {
                return;
            }

            string content = string.Empty;

            if (response.Content != null)
            {
                content += "ResponseContent: " + await response.Content.ReadAsStringAsync();
                response.Content.Dispose();
            }

            if (!string.IsNullOrEmpty(requestBody))
                content += " RequestContent: " + requestBody;

            throw new SimpleHttpResponseException(response.StatusCode, content);
        }
    }

    public class SimpleHttpResponseException : Exception
    {
        public HttpStatusCode StatusCode { get; private set; }

        public SimpleHttpResponseException(HttpStatusCode statusCode, string content) : base(content) => StatusCode = statusCode;
    }
}
