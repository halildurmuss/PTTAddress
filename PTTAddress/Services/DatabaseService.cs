using System.Data.SqlClient;
using PTTAddress.Models;

namespace PTTAddress.Services
{
    public class DatabaseService
    {
        private readonly string connectionString;

        public DatabaseService(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public async Task SaveToDatabaseAsync(List<Province> addressData)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    await connection.OpenAsync();

                    foreach (var province in addressData)
                    {
                        await InsertProvinceAsync(connection, province);

                        foreach (var district in province.Ilceler)
                        {
                            await InsertDistrictAsync(connection, district, province.IlId);

                            foreach (var subdistrict in district.Semtler)
                            {
                                await InsertSubdistrictAsync(connection, subdistrict, district.IlceId, province.IlId);

                                foreach (var neighborhood in subdistrict.Mahalleler)
                                {
                                    await InsertNeighborhoodAsync(connection, neighborhood, subdistrict.SemtId, district.IlceId, province.IlId);
                                }
                            }
                        }
                    }
                }
                catch (SqlException ex)
                {
                    Console.WriteLine($"Veritabanı hatası: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Beklenmeyen bir hata oluştu: {ex.Message}");
                }
                finally
                {
                    if (connection.State == System.Data.ConnectionState.Open)
                    {
                        await connection.CloseAsync();
                    }
                }
            }

            Console.WriteLine("Veriler başarıyla veritabanına kaydedildi.");
        }

        private async Task InsertProvinceAsync(SqlConnection connection, Province province)
        {
            using (SqlCommand command = new SqlCommand("INSERT INTO Iller (IlId, IlAdi) VALUES (@IlId, @IlAdi)", connection))
            {
                command.Parameters.AddWithValue("@IlId", province.IlId);
                command.Parameters.AddWithValue("@IlAdi", province.IlAdi);
                await command.ExecuteNonQueryAsync();
            }
        }

        private async Task InsertDistrictAsync(SqlConnection connection, District district, int ilId)
        {
            using (SqlCommand command = new SqlCommand("INSERT INTO Ilceler (IlceId, IlceAdi, IlId) VALUES (@IlceId, @IlceAdi, @IlId)", connection))
            {
                command.Parameters.AddWithValue("@IlceId", district.IlceId);
                command.Parameters.AddWithValue("@IlceAdi", district.IlceAdi);
                command.Parameters.AddWithValue("@IlId", ilId);
                await command.ExecuteNonQueryAsync();
            }
        }

        private async Task InsertSubdistrictAsync(SqlConnection connection, Subdistrict subdistrict, int ilceId, int ilId)
        {
            bool semtExists = await CheckIfSemtExistsAsync(connection, subdistrict.SemtId, ilceId, ilId);
            if (semtExists)
            {
                subdistrict.SemtId = await GenerateUniqueSemtIdAsync(connection, ilceId, ilId);
                Console.WriteLine($"Yinelenen SemtId bulundu. Yeni SemtId: {subdistrict.SemtId}");
            }

            using (SqlCommand command = new SqlCommand("INSERT INTO Semtler (SemtId, SemtAdi, IlceId, IlId) VALUES (@SemtId, @SemtAdi, @IlceId, @IlId)", connection))
            {
                command.Parameters.AddWithValue("@SemtId", subdistrict.SemtId);
                command.Parameters.AddWithValue("@SemtAdi", subdistrict.SemtAdi);
                command.Parameters.AddWithValue("@IlceId", ilceId);
                command.Parameters.AddWithValue("@IlId", ilId);
                await command.ExecuteNonQueryAsync();
            }
        }

        private async Task InsertNeighborhoodAsync(SqlConnection connection, Neighborhood neighborhood, int semtId, int ilceId, int ilId)
        {
            bool mahalleExists = await CheckIfMahalleExistsAsync(connection, neighborhood.MahalleId, semtId, ilceId, ilId);
            if (mahalleExists)
            {
                neighborhood.MahalleId = await GenerateUniqueMahalleIdAsync(connection, semtId, ilceId, ilId);
                Console.WriteLine($"Yinelenen MahalleId bulundu. Yeni MahalleId: {neighborhood.MahalleId}");
            }

            using (SqlCommand command = new SqlCommand("INSERT INTO Mahalleler (MahalleId, MahalleAdi, PostaKodu, SemtId, IlceId, IlId) VALUES (@MahalleId, @MahalleAdi, @PostaKodu, @SemtId, @IlceId, @IlId)", connection))
            {
                command.Parameters.AddWithValue("@MahalleId", neighborhood.MahalleId);
                command.Parameters.AddWithValue("@MahalleAdi", neighborhood.MahalleAdi);
                command.Parameters.AddWithValue("@PostaKodu", neighborhood.PostaKodu);
                command.Parameters.AddWithValue("@SemtId", semtId);
                command.Parameters.AddWithValue("@IlceId", ilceId);
                command.Parameters.AddWithValue("@IlId", ilId);
                await command.ExecuteNonQueryAsync();
            }
        }

        private async Task<bool> CheckIfSemtExistsAsync(SqlConnection connection, int semtId, int ilceId, int ilId)
        {
            using (SqlCommand command = new SqlCommand("SELECT COUNT(*) FROM Semtler WHERE SemtId = @SemtId AND IlceId = @IlceId AND IlId = @IlId", connection))
            {
                command.Parameters.AddWithValue("@SemtId", semtId);
                command.Parameters.AddWithValue("@IlceId", ilceId);
                command.Parameters.AddWithValue("@IlId", ilId);
                int count = (int)await command.ExecuteScalarAsync();
                return count > 0;
            }
        }

        private async Task<bool> CheckIfMahalleExistsAsync(SqlConnection connection, int mahalleId, int semtId, int ilceId, int ilId)
        {
            using (SqlCommand command = new SqlCommand("SELECT COUNT(*) FROM Mahalleler WHERE MahalleId = @MahalleId AND SemtId = @SemtId AND IlceId = @IlceId AND IlId = @IlId", connection))
            {
                command.Parameters.AddWithValue("@MahalleId", mahalleId);
                command.Parameters.AddWithValue("@SemtId", semtId);
                command.Parameters.AddWithValue("@IlceId", ilceId);
                command.Parameters.AddWithValue("@IlId", ilId);
                int count = (int)await command.ExecuteScalarAsync();
                return count > 0;
            }
        }

        private async Task<int> GenerateUniqueSemtIdAsync(SqlConnection connection, int ilceId, int ilId)
        {
            using (SqlCommand command = new SqlCommand("SELECT ISNULL(MAX(SemtId), 0) + 1 FROM Semtler WHERE IlceId = @IlceId AND IlId = @IlId", connection))
            {
                command.Parameters.AddWithValue("@IlceId", ilceId);
                command.Parameters.AddWithValue("@IlId", ilId);
                return (int)await command.ExecuteScalarAsync();
            }
        }

        private async Task<int> GenerateUniqueMahalleIdAsync(SqlConnection connection, int semtId, int ilceId, int ilId)
        {
            using (SqlCommand command = new SqlCommand("SELECT ISNULL(MAX(MahalleId), 0) + 1 FROM Mahalleler WHERE SemtId = @SemtId AND IlceId = @IlceId AND IlId = @IlId", connection))
            {
                command.Parameters.AddWithValue("@SemtId", semtId);
                command.Parameters.AddWithValue("@IlceId", ilceId);
                command.Parameters.AddWithValue("@IlId", ilId);
                return (int)await command.ExecuteScalarAsync();
            }
        }
    }
}