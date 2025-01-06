using System.Net.Http.Json;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;

namespace PaymentGateway.Api.Services;

public class AcquiringBankService : IAcquiringBankService
{
    private readonly HttpClient _httpClient;
    private readonly string _bankApiUrl;

    public AcquiringBankService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _bankApiUrl = configuration["BankApi:Url"] ?? throw new ArgumentNullException("BankApi:Url is not configured.");
    }

    public async Task<BankSimulatorResponse> ProcessPaymentAsync(PaymentRequest request)
    {
        var bankRequest = new
        {
            card_number = request.CardNumber.ToString(),
            expiry_date = $"{request.ExpiryMonth:D2}/{request.ExpiryYear}",
            currency = request.Currency,
            amount = request.Amount,
            cvv = request.Cvv.ToString()
        };

        var response = await _httpClient.PostAsJsonAsync(_bankApiUrl, bankRequest);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Bank simulator returned error: {response.StatusCode}");
        }

        return await response.Content.ReadFromJsonAsync<BankSimulatorResponse>();
    }

    public Task<BankSimulatorResponse> ProcessPaymentAsync(PostPaymentRequest request)
    {
        throw new NotImplementedException();
    }

}

public interface IAcquiringBankService
{
    Task<BankSimulatorResponse> ProcessPaymentAsync(PaymentRequest request);
}

public class BankSimulatorResponse
{
    public bool Authorized { get; set; }
    public string AuthorizationCode { get; set; }
}
