using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using FloraCore.Application.Common.Interfaces;
using FloraCore.Application.Common.Models;
using FloraCore.Domain.Constants;
using Microsoft.Extensions.Configuration;

namespace FloraCore.Infrastructure.Services.Payments;

/// <summary>
/// VNPay payment gateway integration.
/// </summary>
public class VnPayService(IConfiguration configuration) : IPaymentService
{
    private readonly IConfiguration _configuration =
        configuration ?? throw new ArgumentNullException(nameof(configuration));

    public string GatewayName => PaymentMethod.VNPAY;

    public string GetCallbackUrl(string baseApiUrl) => $"{baseApiUrl.TrimEnd('/')}/api/v1/payments/vnpay-callback";

    public Task<CreatePaymentResult> CreatePaymentUrlAsync(OrderPaymentDto order)
    {
        try
        {
            var vnpUrl = _configuration["PaymentGateways:VnPay:Url"];
            var vnpTmnCode = _configuration["PaymentGateways:VnPay:TmnCode"];
            var vnpHashSecret = _configuration["PaymentGateways:VnPay:HashSecret"];

            if (string.IsNullOrEmpty(vnpUrl) || string.IsNullOrEmpty(vnpTmnCode) || string.IsNullOrEmpty(vnpHashSecret))
            {
                var msg = "One or more VNPay configuration values are missing.";
                throw new InvalidOperationException(msg);
            }

            var timeZoneById = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            var timeNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZoneById);
            var tick = DateTime.UtcNow.Ticks.ToString();

            var requestData = new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                { "vnp_Version", "2.1.0" },
                { "vnp_Command", "pay" },
                { "vnp_TmnCode", vnpTmnCode },
                { "vnp_Amount", ((long)(order.Amount * 100)).ToString() },
                { "vnp_CreateDate", timeNow.ToString("yyyyMMddHHmmss") },
                { "vnp_CurrCode", "VND" },
                { "vnp_IpAddr", "127.0.0.1" },
                { "vnp_Locale", "vn" },
                { "vnp_OrderInfo", order.Description },
                { "vnp_OrderType", "other" },
                { "vnp_ReturnUrl", order.ReturnUrl },
                { "vnp_TxnRef", order.OrderId.ToString() }
            };

            var rawData = string.Join("&", requestData.Select(kv => $"{kv.Key}={kv.Value}"));
            var signData = string.Join("&", requestData.Select(kv => $"{kv.Key}={WebUtility.UrlEncode(kv.Value)}"));
            var secureHash = HmacSha512(vnpHashSecret, rawData);
            
            var paymentUrl = $"{vnpUrl}?{signData}&vnp_SecureHash={secureHash}";

            return Task.FromResult(new CreatePaymentResult
            {
                Success = true,
                PaymentUrl = paymentUrl,
                TransactionId = order.OrderId.ToString(),
                Message = "VNPay URL generated successfully."
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new CreatePaymentResult
            {
                Success = false,
                Message = $"Error generating VNPay URL: {ex.Message}"
            });
        }
    }

    public Task<bool> VerifyCallbackAsync(PaymentCallbackDto callbackData)
    {
        try
        {
            var vnpHashSecret = _configuration["PaymentGateways:VnPay:HashSecret"];
            if (string.IsNullOrEmpty(vnpHashSecret))
            {
                var msg = "PaymentGateways:VnPay:HashSecret configuration is missing.";
                throw new InvalidOperationException(msg);
            }
            var secureHash = callbackData.QueryParameters.GetValueOrDefault("vnp_SecureHash") ?? string.Empty;

            var sortedParams = new SortedDictionary<string, string>(StringComparer.Ordinal);
            foreach (var (key, value) in callbackData.QueryParameters)
            {
                if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_") && key != "vnp_SecureHash" && key != "vnp_SecureHashType")
                {
                    sortedParams.Add(key, value);
                }
            }

            var rawData = string.Join("&", sortedParams.Select(kv => $"{kv.Key}={kv.Value}"));
            var checkHash = HmacSha512(vnpHashSecret, rawData);

            var isValid = string.Equals(secureHash, checkHash, StringComparison.OrdinalIgnoreCase);
            var responseCode = callbackData.QueryParameters.GetValueOrDefault("vnp_ResponseCode");

            return Task.FromResult(isValid && responseCode == "00");
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    private static string HmacSha512(string key, string inputData)
    {
        var hash = new StringBuilder();
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var inputBytes = Encoding.UTF8.GetBytes(inputData);
        using (var hmac = new HMACSHA512(keyBytes))
        {
            var hashValue = hmac.ComputeHash(inputBytes);
            foreach (var theByte in hashValue)
            {
                hash.Append(theByte.ToString("x2"));
            }
        }
        return hash.ToString();
    }
}
