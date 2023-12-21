namespace Goose
{
    static class Http
    {
        public static bool Get(string url, out string content)
        {
            content = string.Empty;
            using var client = new HttpClient();
            HttpResponseMessage httpResponseMessage = client.GetAsync(url).Result;
            if (httpResponseMessage.IsSuccessStatusCode)
            {
                content = httpResponseMessage.ToString();
                return true;
            }

            return false;
        }
    }
}