using System.Net;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace PTTAddress.Utilities
{
    public static class HtmlParser
    {
        public static Dictionary<string, string> ExtractOptions(string html, string dropdownId)
        {
            Dictionary<string, string> options = new Dictionary<string, string>();

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var selectNode = doc.DocumentNode.SelectSingleNode($"//select[@id='{dropdownId}']");
            if (selectNode == null) return options;

            foreach (var option in selectNode.SelectNodes("option"))
            {
                string value = option.GetAttributeValue("value", "");
                string text = WebUtility.HtmlDecode(option.InnerText.Trim());
                if (!string.IsNullOrEmpty(value) && value != "-1")
                    options[value] = text;
            }
            return options;
        }

        public static async Task<string> PostFormAsync(HttpClient client, string baseUrl, string id, string eventTarget, string dropdownName, string previousHtml)
        {
            var viewState = ExtractInputValue(previousHtml, "__VIEWSTATE");
            var eventValidation = ExtractInputValue(previousHtml, "__EVENTVALIDATION");

            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "__EVENTTARGET", dropdownName },
                { "__EVENTARGUMENT", "" },
                { "__VIEWSTATE", viewState },
                { "__EVENTVALIDATION", eventValidation },
                { dropdownName, id }
            });

            HttpResponseMessage response = await client.PostAsync(baseUrl, content);
            return await response.Content.ReadAsStringAsync();
        }

        public static string ExtractInputValue(string html, string inputId)
        {
            Match match = Regex.Match(html, $@"id=""{inputId}""\s+value=""([^""]+)""");
            return match.Success ? match.Groups[1].Value : "";
        }

        public static (string MahalleAdi, string SemtAdi, string PostaKodu, int SemtId, int MahalleId) ParseNeighborhoodInfo(string neighborhoodInfo, string mahalleId)
        {
            var parts = neighborhoodInfo.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            string mahalleAdi = parts.Length > 0 ? parts[0].Trim() : string.Empty;
            string semtAdi = parts.Length > 1 ? parts[1].Trim() : string.Empty;
            string postaKodu = parts.Length > 2 ? parts[2].Trim() : string.Empty;

            var mahalleIdParts = mahalleId.Split('/');
            int semtId = mahalleIdParts.Length > 1 ? int.Parse(mahalleIdParts[1]) : 0;
            int mahalleIdParsed = mahalleIdParts.Length > 0 ? int.Parse(mahalleIdParts[0]) : 0;

            return (mahalleAdi, semtAdi, postaKodu, semtId, mahalleIdParsed);
        }
    }
}