using System.Text;
using System.Text.Json;
using Assignment1.ModelsJSON;

class Program
{
    static async Task Main(string[] args)
    {
        using HttpClient client = new HttpClient();
        string date = GetFormattedDate();
        string apiUrl = GetApiUrl(date);

        string csvFilePath = "MTUData.csv";

        try
        {
            HttpResponseMessage response = await client.GetAsync(apiUrl);
            response.EnsureSuccessStatusCode();
            string jsonResponse = await response.Content.ReadAsStringAsync();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            // Deserialize the response into the defined model with case-insensitive options
            Root root = JsonSerializer.Deserialize<Root>(jsonResponse, options);

            if (root?.Mtus == null)
            {
                Console.WriteLine("Deserialization resulted in a null 'Mtus'. Check the JSON structure or class definitions.");
                return;
            }

            // Print table header for Date, Time, and NTC
            Console.WriteLine("{0,-25} {1,25} {2,15}", "MTU Date and Time (UTC)", "GB-NL (MW)", "NL-GB (MW)");
            Console.WriteLine(new string('-', 65));

            StringBuilder csvContent = new StringBuilder();

            // Add CSV header
            csvContent.AppendLine("MTU Date and Time (UTC),GB-NL (MW),NL-GB (MW)");

            foreach (var mtu in root.Mtus)
            {
                int gbNlNtc = 0;
                int nlGbNtc = 0;

                if (mtu.Values != null)
                {
                    foreach (var val in mtu.Values)
                    {
                        if (val.Direction == "GB_NL")
                        {
                            gbNlNtc = val.Ntc / 1000;
                        }
                        else if (val.Direction == "NL_GB")
                        {
                            nlGbNtc = val.Ntc / 1000;
                        }
                    }

                    var startTime = mtu.Mtu;
                    var endTime = startTime.AddHours(1);

                    // Adjust the width of the columns to make them aligned under the headers
                    Console.WriteLine("{0,-25} {1,15:N0} {2,15:N0}",
                                      $"{startTime.ToString("ddd, MMM dd yyyy HH:mm")} - {endTime.ToString("HH:mm")}",
                                      gbNlNtc.ToString(), nlGbNtc.ToString());

                    csvContent.AppendLine($"{startTime.ToString("f")},{gbNlNtc},{nlGbNtc}");
                }
            }

            await File.WriteAllTextAsync(csvFilePath, csvContent.ToString());

            string fullPath = Path.GetFullPath(csvFilePath);
            Console.WriteLine($"Data successfully written to {fullPath}");
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine($"Request error: {e.Message}");
        }
        catch (JsonException e)
        {
            Console.WriteLine($"JSON parsing error: {e.Message}");
        }
        catch (NullReferenceException e)
        {
            Console.WriteLine($"Null reference error: {e.Message}");
        }
        catch (Exception e)
        {
            Console.WriteLine($"An unexpected error occurred: {e.Message}");
        }
    }

    public static string GetFormattedDate()
    {
        DateOnly dateOnly = DateOnly.FromDateTime(DateTime.Now);
        return dateOnly.ToString("yyyy-MM-dd");
    }

    public static string GetApiUrl(string date)
    {
        return $"https://api.empire.britned.com/v1/public/nominations/aggregated-overview?deliveryDay={date}&timescales=LONG_TERM&timescales=DAY_AHEAD&timescales=INTRA_DAY";
    }
}