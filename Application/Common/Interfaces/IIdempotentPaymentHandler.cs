using System.Threading.Tasks;
using FloraCore.Application.Common.Models;

namespace FloraCore.Application.Common.Interfaces;

public interface IIdempotentPaymentHandler
{
    Task<bool> ProcessPaymentCallbackAsync(string gateway, string transactionId, PaymentCallbackDto callbackData);
}
