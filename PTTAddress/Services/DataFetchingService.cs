using System.Net;
using PTTAddress.Models;
using PTTAddress.Utilities;

namespace PTTAddress.Services
{
    public class DataFetchingService
    {
        private static readonly string BaseUrl = "https://postakodu.ptt.gov.tr";
        private static readonly HttpClientHandler handler = new HttpClientHandler { CookieContainer = new CookieContainer() };
        private static readonly HttpClient client = new HttpClient(handler);

        public async Task<List<Province>> FetchAddressDataAsync()
        {
            List<Province> addressData = new List<Province>();
            string html = await client.GetStringAsync(BaseUrl);
            var provinceOptions = HtmlParser.ExtractOptions(html, "MainContent_DropDownList1");

            int totalProvinces = provinceOptions.Count;
            int currentProvince = 0;

            foreach (var province in provinceOptions)
            {
                Console.WriteLine($"İl işleniyor: {province.Value}");
                string provinceHtml = await HtmlParser.PostFormAsync(client, BaseUrl, province.Key, "__EVENTTARGET", "ctl00$MainContent$DropDownList1", html);
                var districtOptions = HtmlParser.ExtractOptions(provinceHtml, "MainContent_DropDownList2");

                Province provinceData = new Province
                {
                    IlId = int.Parse(province.Key),
                    IlAdi = province.Value,
                    Ilceler = new List<District>()
                };

                foreach (var district in districtOptions)
                {
                    Console.WriteLine($"  İlçe işleniyor: {district.Value}");
                    string districtHtml = await HtmlParser.PostFormAsync(client, BaseUrl, district.Key, "__EVENTTARGET", "ctl00$MainContent$DropDownList2", provinceHtml);
                    var neighborhoodOptions = HtmlParser.ExtractOptions(districtHtml, "MainContent_DropDownList3");

                    District districtData = new District
                    {
                        IlceId = int.Parse(district.Key),
                        IlceAdi = district.Value,
                        Semtler = new List<Subdistrict>()
                    };

                    var subdistrictGruplari = new Dictionary<string, Subdistrict>();
                    foreach (var neighborhood in neighborhoodOptions)
                    {
                        var mahalleBilgisi = HtmlParser.ParseNeighborhoodInfo(neighborhood.Value, neighborhood.Key);

                        if (!subdistrictGruplari.ContainsKey(mahalleBilgisi.SemtAdi))
                        {
                            subdistrictGruplari[mahalleBilgisi.SemtAdi] = new Subdistrict
                            {
                                SemtId = mahalleBilgisi.SemtId,
                                SemtAdi = mahalleBilgisi.SemtAdi,
                                Mahalleler = new List<Neighborhood>()
                            };
                        }

                        subdistrictGruplari[mahalleBilgisi.SemtAdi].Mahalleler.Add(new Neighborhood
                        {
                            MahalleId = mahalleBilgisi.MahalleId,
                            MahalleAdi = mahalleBilgisi.MahalleAdi,
                            PostaKodu = mahalleBilgisi.PostaKodu
                        });
                    }

                    foreach (var subdistrict in subdistrictGruplari.Values)
                    {
                        districtData.Semtler.Add(subdistrict);
                    }

                    provinceData.Ilceler.Add(districtData);
                    await Task.Delay(1000);
                }

                addressData.Add(provinceData);
                currentProvince++;
                Console.WriteLine($"İlerleme: {currentProvince}/{totalProvinces}");
                await Task.Delay(2000);
            }

            return addressData;
        }
    }
}