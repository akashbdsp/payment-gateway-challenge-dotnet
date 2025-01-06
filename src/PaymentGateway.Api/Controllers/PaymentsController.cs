using System.ComponentModel;

using Microsoft.AspNetCore.Mvc;

using PaymentGateway.Api.Models;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Services;
using PaymentGateway.Api.Validators;

namespace PaymentGateway.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PaymentsController : Controller
{
    private readonly PaymentsRepository _paymentsRepository;
    private readonly IAcquiringBankService _acquiringBankService;
    private readonly PaymentRequestValidator _paymentRequestValidator;

    public PaymentsController(PaymentsRepository paymentsRepository, IAcquiringBankService acquiringBankService)
    {
        _paymentsRepository = paymentsRepository;
        _acquiringBankService = acquiringBankService;
        _paymentRequestValidator = new PaymentRequestValidator(); // Initialize the validator
    }

    public class ErrorResponse
    {
        public List<string> Errors { get; set; }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<GetPaymentResponse?>> GetPayment(Guid id)
    {
        var payment = _paymentsRepository.Get(id);

        if (payment == null)
        {
            return NotFound($"Payment with ID {id} not found.");
        }

        var response = new GetPaymentResponse
        {
            Id = payment.Id,
            Status = payment.Status,
            CardNumberLastFour = payment.CardNumberLastFour,
            ExpiryMonth = payment.ExpiryMonth,
            ExpiryYear = payment.ExpiryYear,
            Currency = payment.Currency,
            Amount = payment.Amount
        };

        return Ok(response);
    }

    [HttpPost]
    public async Task<ActionResult<PostPaymentResponse>> ProcessPayment([FromBody] PaymentRequest request)
    {
        if (!_paymentRequestValidator.Validate(request, out var validationErrors))
        {
            return BadRequest(new { Errors = validationErrors });
        }

        var acquiringBankResponse = await _acquiringBankService.ProcessPaymentAsync(request);

        var payment = new PostPaymentResponse
        {
            Id = Guid.NewGuid(),
            Status = acquiringBankResponse.Authorized ? PaymentStatus.Authorized : PaymentStatus.Declined,
            CardNumberLastFour = int.Parse(request.CardNumber.ToString().Substring(request.CardNumber.ToString().Length - 4)),
            ExpiryMonth = request.ExpiryMonth,
            ExpiryYear = request.ExpiryYear,
            Currency = request.Currency,
            Amount = request.Amount,
        };

        _paymentsRepository.Add(payment);

        var response = new PostPaymentResponse
        {
            Id = payment.Id,
            Status = payment.Status,
            CardNumberLastFour = payment.CardNumberLastFour,
            ExpiryMonth = payment.ExpiryMonth,
            ExpiryYear = payment.ExpiryYear,
            Currency = payment.Currency,
            Amount = payment.Amount
        };

        return Ok(response);
    }
}
