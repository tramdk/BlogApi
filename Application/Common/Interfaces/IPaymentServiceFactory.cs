using FloraCore.Application.Common.Interfaces;

namespace FloraCore.Application.Common.Interfaces;

public interface IPaymentServiceFactory
{
    IPaymentService GetPaymentService(string gateway);
}
