using System.Net.Http;
using System.Text;
using System.Windows;
using Newtonsoft.Json;
using Serilog;
using SwimmingTrackSystem.Windows;

namespace SwimmingTrackSystem.Services;

public class PosTerminalService : IDisposable
{
    private readonly HttpClient _httpClient;
    private bool _disposed;
    
    public PosTerminalService(string baseUrl)
    {
        _httpClient = new HttpClient();
        _httpClient.BaseAddress = new Uri(baseUrl);
    }

    public async Task<string> ProcessPaymentAsync(decimal price, string itemName)
    {
        try
        {
            // Prepare the request
            var request = new OpenAndCloseRecRequest
            {
                Goods =
                [
                    new GoodItem
                    {
                        Price = price,
                        ItemName = itemName,
                        Total = $"{price}"
                    }
                ],
                PayItems =
                [
                    new PayItem
                    {
                        Total = $"{price}"
                    }
                ]
            };

            // Serialize the request to JSON
            var json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Send the POST request
            var response = await _httpClient.PostAsync("/fiscal/bills/openAndCloseRec/", content);

            // Check if the request was successful
            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<OpenAndCloseRecResponse>(responseJson);

                // Check the status
                if (result.Status == 0)
                {
                    // Payment successful
                    return string.Empty;
                }
                else
                {
                    // Payment failed, show error message
                    new DialogWindow("Ошибка оплаты", $"Оплата не прошла: {result.ErrorMessage}").ShowDialog();
                    Log.Error($"Оплата не прошла: {result.ErrorMessage}");
                    return $"Оплата не прошла: {result.ErrorMessage}";
                }
            }
            else
            {
                // HTTP request failed
                new DialogWindow("Ошибка соединения", $"Ошибка соединения с терминалом: {response.StatusCode}").ShowDialog();
                Log.Error($"Ошибка соединения с терминалом: {response.StatusCode}");
                return $"Ошибка соединения с терминалом: {response.StatusCode}";
            }
        }
        catch (Exception ex)
        {
            // Handle any exceptions
            new DialogWindow("Ошибка оплаты", $"Ошибка во время оплаты").ShowDialog();
            Log.Error($"Ошибка во время оплаты: {ex.Message}");
            return "Ошибка во время оплаты";
        }
    }
    
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            _httpClient.Dispose();
        }

        _disposed = true;
    }
}

public class OpenAndCloseRecRequest
{
    [JsonProperty("recType")]
    public int RecType { get; set; } = 1;

    [JsonProperty("goods")]
    public List<GoodItem> Goods { get; set; }

    [JsonProperty("payItems")]
    public List<PayItem> PayItems { get; set; }
}

public class GoodItem
{
    [JsonProperty("count")]
    public int Count { get; set; } = 1;

    [JsonProperty("price")]
    public decimal Price { get; set; }

    [JsonProperty("article")] 
    public string Article { get; set; } = "1";

    [JsonProperty("itemName")]
    public string ItemName { get; set; }

    [JsonProperty("total")]
    public string Total { get; set; }
}

public class PayItem
{
    [JsonProperty("payType")]
    public int PayType { get; set; } = 1;

    [JsonProperty("total")]
    public string Total { get; set; }
}

public class OpenAndCloseRecResponse
{
    [JsonProperty("status")]
    public int Status { get; set; }

    [JsonProperty("extCode")]
    public int ExtCode { get; set; }

    [JsonProperty("extCode2")]
    public int ExtCode2 { get; set; }

    [JsonProperty("errorMessage")]
    public string ErrorMessage { get; set; }

    [JsonProperty("qrCode")]
    public string QrCode { get; set; }

    [JsonProperty("tin")]
    public string Tin { get; set; }

    [JsonProperty("registrationNumber")]
    public string RegistrationNumber { get; set; }

    [JsonProperty("fmNumber")]
    public string FmNumber { get; set; }

    [JsonProperty("shiftNumber")]
    public int ShiftNumber { get; set; }

    [JsonProperty("fdNumber")]
    public int FdNumber { get; set; }

    [JsonProperty("dateTime")]
    public string DateTime { get; set; }

    [JsonProperty("fdSign")]
    public string FdSign { get; set; }

    [JsonProperty("transactions")]
    public List<Transaction> Transactions { get; set; }
}

public class Transaction
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("type")]
    public string Type { get; set; }

    [JsonProperty("paymentSystemId")]
    public string PaymentSystemId { get; set; }

    [JsonProperty("title")]
    public string Title { get; set; }

    [JsonProperty("date")]
    public string Date { get; set; }

    [JsonProperty("amount")]
    public decimal Amount { get; set; }

    [JsonProperty("transactionData")]
    public TransactionData TransactionData { get; set; }

    [JsonProperty("source")]
    public string Source { get; set; }

    [JsonProperty("orderId")]
    public string OrderId { get; set; }
}

public class TransactionData
{
    [JsonProperty("uuid")]
    public string Uuid { get; set; }

    [JsonProperty("method")]
    public string Method { get; set; }

    [JsonProperty("transactionDate")]
    public string TransactionDate { get; set; }

    [JsonProperty("amount")]
    public int Amount { get; set; }
}