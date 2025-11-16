using Balkana.Data.Models.Store;

namespace Balkana.Services.Store
{
    public interface IPaymentService
    {
        /// <summary>
        /// Initialize payment for an order
        /// </summary>
        Task<PaymentInitializationResult> InitializePaymentAsync(int orderId, PaymentMethod method);

        /// <summary>
        /// Process payment callback/webhook
        /// </summary>
        Task<bool> ProcessPaymentCallbackAsync(string transactionId, Dictionary<string, string> parameters);

        /// <summary>
        /// Verify payment status
        /// </summary>
        Task<PaymentVerificationResult> VerifyPaymentAsync(string transactionId);

        /// <summary>
        /// Process refund
        /// </summary>
        Task<bool> ProcessRefundAsync(int orderId, decimal amount);
    }

    public class PaymentInitializationResult
    {
        public bool Success { get; set; }
        public string TransactionId { get; set; }
        public string RedirectUrl { get; set; }  // For redirecting to payment gateway
        public string ErrorMessage { get; set; }
    }

    public class PaymentVerificationResult
    {
        public bool Success { get; set; }
        public PaymentStatus Status { get; set; }
        public string TransactionId { get; set; }
        public decimal Amount { get; set; }
        public DateTime? PaidAt { get; set; }
    }
}

