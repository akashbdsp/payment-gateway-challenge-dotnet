using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;

using PaymentGateway.Api.Controllers;
using PaymentGateway.Api.Models;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Services;

namespace PaymentGateway.Api.Tests;

public class PaymentsControllerTests
{
    private readonly Random _random = new();
    
    [Fact]
    public async Task RetrievesAPaymentSuccessfully()
    {
        // Arrange
        var payment = new PostPaymentResponse
        {
            Id = Guid.NewGuid(),
            ExpiryYear = _random.Next(2023, 2030),
            ExpiryMonth = _random.Next(1, 12),
            Amount = _random.Next(1, 10000),
            CardNumberLastFour = _random.Next(1111, 9999),
            Currency = "GBP"
        };

        var paymentsRepository = new PaymentsRepository();
        paymentsRepository.Add(payment);

        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        var client = webApplicationFactory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services => ((ServiceCollection)services)
                .AddSingleton(paymentsRepository)))
            .CreateClient();

        // Act
        var response = await client.GetAsync($"/api/Payments/{payment.Id}");
        var paymentResponse = await response.Content.ReadFromJsonAsync<PostPaymentResponse>();
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(paymentResponse);
    }

    [Fact]
    public async Task ProcessesPaymentSuccessfully()
    {
        // Arrange
        var acquiringBankService = new Mock<IAcquiringBankService>();
        acquiringBankService
            .Setup(service => service.ProcessPaymentAsync(It.IsAny<PaymentRequest>()))
            .ReturnsAsync(new BankSimulatorResponse { Authorized = true });

        var paymentsRepository = new PaymentsRepository();
        
        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        var client = webApplicationFactory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                ((ServiceCollection)services)
                    .AddSingleton(paymentsRepository)
                    .AddSingleton(acquiringBankService.Object);
            })).CreateClient();

        var paymentRequest = new PaymentRequest
        {
            CardNumber = "4111111111111111",
            ExpiryMonth = _random.Next(1, 12),
            ExpiryYear = DateTime.UtcNow.Year + 1,
            Cvv = 123,
            Currency = "USD",
            Amount = _random.Next(1, 10000)
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/Payments", paymentRequest);
        var paymentResponse = await response.Content.ReadFromJsonAsync<PostPaymentResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(paymentResponse);
        Assert.Equal(PaymentStatus.Authorized, paymentResponse.Status);
        Assert.Equal(paymentRequest.Currency, paymentResponse.Currency);
        Assert.Equal(paymentRequest.Amount, paymentResponse.Amount);
        Assert.Equal(paymentRequest.ExpiryMonth, paymentResponse.ExpiryMonth);
        Assert.Equal(paymentRequest.ExpiryYear, paymentResponse.ExpiryYear);
    }

    [Fact]
    public async Task ReturnsBadRequestForInvalidPaymentRequest()
    {
        // Arrange
        var acquiringBankServiceMock = new Mock<IAcquiringBankService>();
        var paymentsRepository = new PaymentsRepository();

        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        var client = webApplicationFactory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                ((ServiceCollection)services).AddSingleton(paymentsRepository);
                ((ServiceCollection)services).AddSingleton(acquiringBankServiceMock.Object);
            })).CreateClient();

        var invalidPaymentRequest = new PaymentRequest
        {
            CardNumber = "123",
            ExpiryMonth = 1,
            ExpiryYear = 2022,
            Cvv = 12,
            Amount = -1,
            Currency = "USD"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/Payments", invalidPaymentRequest);
        var responseContent = await response.Content.ReadFromJsonAsync<Dictionary<string, List<string>>>();

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(responseContent);
        Assert.Contains("Card number must be between 14 and 16 numeric characters.", responseContent["errors"]);
        Assert.Contains("The expiry date must be in the future.", responseContent["errors"]);
        Assert.Contains("CVV must be 3 or 4 digits long.", responseContent["errors"]);
        Assert.Contains("Amount must be a positive integer.", responseContent["errors"]);
    }

    [Fact]
    public async Task Returns404IfPaymentNotFound()
    {
        // Arrange
        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        var client = webApplicationFactory.CreateClient();
        
        // Act
        var response = await client.GetAsync($"/api/Payments/{Guid.NewGuid()}");
        
        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
