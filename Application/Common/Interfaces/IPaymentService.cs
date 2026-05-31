using System.Threading.Tasks;
using FloraCore.Application.Common.Models;

namespace FloraCore.Application.Common.Interfaces;

public interface IPaymentService
{
    string GatewayName { get; }
    Task<CreatePaymentResult> CreatePaymentUrlAsync(OrderPaymentDto order);
    Task<bool> VerifyCallbackAsync(PaymentCallbackDto callbackData);
    string GetCallbackUrl(string baseApiUrl);
}
