namespace PloomesCRMExportDealStageHistory
{
    public class RequestService
    {
        private readonly HttpClient _httpClient;

        public RequestService(HttpClient httpClient) => _httpClient = httpClient;

        public async Task<string> Request(string uri, HttpMethod method, Dictionary<string, string> headers = null, string requestBody = null)
        {
            using HttpRequestMessage requestMessage = new(method, uri);

            if (headers != null)
            {
                foreach (var header in headers)
                    requestMessage.Headers.Add(header.Key, header.Value);
            }

            if (!string.IsNullOrEmpty(requestBody))
                requestMessage.Content = new StringContent(requestBody);

            using HttpResponseMessage response = await _httpClient.SendAsync(requestMessage);
            await response.EnsureSuccessStatusCodeAsync(requestBody);

            return await response.Content.ReadAsStringAsync();
        }
    }
}
