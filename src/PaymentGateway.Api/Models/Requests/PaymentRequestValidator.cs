using PaymentGateway.Api.Models.Requests;

namespace PaymentGateway.Api.Validators
{
    public class PaymentRequestValidator
    {
        public bool Validate(PaymentRequest request, out List<string> errors)
        {
            errors = new List<string>();

            // Currency Validation: checks length and if it is any of USD, EUR or GBP
            if (string.IsNullOrWhiteSpace(request.Currency) || request.Currency.Length != 3 || !new[] { "USD", "EUR", "GBP" }.Contains(request.Currency))
            {
                errors.Add("Currency must be a 3-character ISO code.");
            }

            if (request.Amount <= 0)
            {
                errors.Add("Amount must be a positive integer.");
            }

            // We are checking against the APIs local machine time. Ideally this would depend on DB
            if (request.ExpiryYear < DateTime.UtcNow.Year || 
               (request.ExpiryYear == DateTime.UtcNow.Year && request.ExpiryMonth < DateTime.UtcNow.Month))
            {
                errors.Add("The expiry date must be in the future.");
            }

            if (request.Cvv.ToString().Length < 3 || request.Cvv.ToString().Length > 4)
            {
                errors.Add("CVV must be 3 or 4 digits long.");
            }

            if (request.CardNumber.Length < 14 || request.CardNumber.Length > 19 || !request.CardNumber.All(char.IsDigit))
            {
                errors.Add("Card number must be between 14 and 16 numeric characters.");
            }

            return errors.Count == 0;
        }
    }
}
