using System;
using System.Collections.Generic;
using System.Linq;
using FloraCore.Application.Common.Interfaces;

namespace FloraCore.Infrastructure.Services.Payments;

/// <summary>
/// Factory resolves the correct <see cref="IPaymentService"/> implementation by gateway name.
/// </summary>
public class PaymentServiceFactory(IEnumerable<IPaymentService> services) : IPaymentServiceFactory
{
    private readonly IEnumerable<IPaymentService> _services =
        services ?? throw new ArgumentNullException(nameof(services));

    /// <inheritdoc />
    public IPaymentService GetPaymentService(string gateway)
    {
        ArgumentNullException.ThrowIfNull(gateway);

        var service = _services.FirstOrDefault(s =>
            string.Equals(s.GatewayName, gateway, StringComparison.OrdinalIgnoreCase));

        return service ?? throw new ArgumentException(
            $"Payment gateway '{gateway}' is not supported.", nameof(gateway));
    }
}
