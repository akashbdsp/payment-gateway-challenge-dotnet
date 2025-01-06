using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Validators;

namespace PaymentGateway.Api.Tests.Validators
{
    public class PaymentRequestValidatorTests
    {
        private readonly PaymentRequestValidator _validator;

        public PaymentRequestValidatorTests()
        {
            _validator = new PaymentRequestValidator();
        }

        [Fact]
        public void Validate_WithValidRequest_ReturnsTrue()
        {
            var request = new PaymentRequest
            {
                Currency = "USD",
                Amount = 100,
                ExpiryMonth = 12,
                ExpiryYear = DateTime.UtcNow.Year + 1,
                Cvv = 123,
                CardNumber = "4111111111111111"
            };

            var isValid = _validator.Validate(request, out var errors);

            Assert.True(isValid);
            Assert.Empty(errors);
        }

        [Fact]
        public void Validate_WithInvalidCurrency_ReturnsFalse()
        {
            var request = new PaymentRequest
            {
                Currency = "ABC",
                Amount = 100,
                ExpiryMonth = 12,
                ExpiryYear = DateTime.UtcNow.Year + 1,
                Cvv = 123,
                CardNumber = "4111111111111111"
            };

            var isValid = _validator.Validate(request, out var errors);

            Assert.False(isValid);
            Assert.Contains("Currency must be a 3-character ISO code.", errors);
        }

        [Fact]
        public void Validate_WithNegativeAmount_ReturnsFalse()
        {
            var request = new PaymentRequest
            {
                Currency = "USD",
                Amount = -100,
                ExpiryMonth = 12,
                ExpiryYear = DateTime.UtcNow.Year + 1,
                Cvv = 123,
                CardNumber = "4111111111111111"
            };

            var isValid = _validator.Validate(request, out var errors);

            Assert.False(isValid);
            Assert.Contains("Amount must be a positive integer.", errors);
        }

        [Fact]
        public void Validate_WithExpiredCard_ReturnsFalse()
        {
            var request = new PaymentRequest
            {
                Currency = "USD",
                Amount = 100,
                ExpiryMonth = DateTime.UtcNow.Month - 1,
                ExpiryYear = DateTime.UtcNow.Year, // Expired card
                Cvv = 123,
                CardNumber = "4111111111111111"
            };

            var isValid = _validator.Validate(request, out var errors);

            Assert.False(isValid);
            Assert.Contains("The expiry date must be in the future.", errors);
        }

        [Fact]
        public void Validate_WithInvalidCvv_ReturnsFalse()
        {
            var request = new PaymentRequest
            {
                Currency = "USD",
                Amount = 100,
                ExpiryMonth = 12,
                ExpiryYear = DateTime.UtcNow.Year + 1,
                Cvv = 12,
                CardNumber = "4111111111111111"
            };

            var isValid = _validator.Validate(request, out var errors);

            Assert.False(isValid);
            Assert.Contains("CVV must be 3 or 4 digits long.", errors);
        }

        [Fact]
        public void Validate_WithInvalidCardNumber_ReturnsFalse()
        {
            var request = new PaymentRequest
            {
                Currency = "USD",
                Amount = 100,
                ExpiryMonth = 12,
                ExpiryYear = DateTime.UtcNow.Year + 1,
                Cvv = 123,
                CardNumber = "12345ABC678"
            };

            var isValid = _validator.Validate(request, out var errors);

            Assert.False(isValid);
            Assert.Contains("Card number must be between 14 and 16 numeric characters.", errors);
        }

        [Fact]
        public void Validate_WithShortCardNumber_ReturnsFalse()
        {
            var request = new PaymentRequest
            {
                Currency = "USD",
                Amount = 100,
                ExpiryMonth = 12,
                ExpiryYear = DateTime.UtcNow.Year + 1,
                Cvv = 123,
                CardNumber = "123456789"
            };

            var isValid = _validator.Validate(request, out var errors);

            Assert.False(isValid);
            Assert.Contains("Card number must be between 14 and 16 numeric characters.", errors);
        }
    }
}
