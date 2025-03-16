using Newtonsoft.Json;
using PTTAddress.Models;
using PTTAddress.Services;

class Program
{
    private static readonly string connectionString = "Server=DESKTOP-P5RQ6QE;" + "Database=;PTTAddress" + "Integrated Security=True;" + "TrustServerCertificate=True;" + "Connect Timeout=180;";

    static async Task Main()
    {
        try
        {
            Console.WriteLine("PTT Adres Verisi Çekme İşlemi Başlatılıyor...\n");

            DataFetchingService dataFetchingService = new DataFetchingService();
            List<Province> addressData = await dataFetchingService.FetchAddressDataAsync();

            await SaveDataToFileAsync(addressData);

            DatabaseService databaseService = new DatabaseService(connectionString);
            await databaseService.SaveToDatabaseAsync(addressData);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Program çalışırken bir hata oluştu: {ex.Message}");
        }
    }

    static async Task SaveDataToFileAsync(List<Province> addressData)
    {
        string fileName = $"ptt_address_data_{DateTime.Now:yyyy-MM-dd}.json";
        await File.WriteAllTextAsync(fileName, JsonConvert.SerializeObject(addressData, Formatting.Indented));
        Console.WriteLine($"\nAdres verileri başarıyla kaydedildi: {fileName}");
    }
}