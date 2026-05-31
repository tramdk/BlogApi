using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FloraCore.Application.Common.Interfaces;
using FloraCore.Application.Common.Models;
using FloraCore.Domain.Constants;
using Microsoft.Extensions.Configuration;

namespace FloraCore.Infrastructure.Services.Payments;

/// <summary>
/// MoMo payment gateway integration.
/// </summary>
public class MoMoService(IConfiguration configuration, IHttpClientFactory httpClientFactory) : IPaymentService
{
    private readonly IConfiguration _configuration =
        configuration ?? throw new ArgumentNullException(nameof(configuration));
    private readonly IHttpClientFactory _httpClientFactory =
        httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));

    public string GatewayName => PaymentMethod.MOMO;

    public string GetCallbackUrl(string baseApiUrl) => $"{baseApiUrl.TrimEnd('/')}/api/v1/payments/momo-ipn";

    public async Task<CreatePaymentResult> CreatePaymentUrlAsync(OrderPaymentDto order)
    {
        try
        {
            var endpoint = _configuration["PaymentGateways:MoMo:Url"];
            var partnerCode = _configuration["PaymentGateways:MoMo:PartnerCode"];
            var accessKey = _configuration["PaymentGateways:MoMo:AccessKey"];
            var secretKey = _configuration["PaymentGateways:MoMo:SecretKey"];
            var ipnUrl = _configuration["PaymentGateways:MoMo:IpnUrl"];

            if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(partnerCode) || 
                string.IsNullOrEmpty(accessKey) || string.IsNullOrEmpty(secretKey))
            {
                var msg = "One or more MoMo configuration values are missing.";
                throw new InvalidOperationException(msg);
            }

            if (string.IsNullOrEmpty(ipnUrl))
            {
                var apiUrl = _configuration["PaymentGateways:ApiUrl"];
                if (string.IsNullOrEmpty(apiUrl))
                {
                    var msg = "Both MoMo:IpnUrl and PaymentGateways:ApiUrl are missing.";
                    throw new InvalidOperationException(msg);
                }
                ipnUrl = $"{apiUrl.TrimEnd('/')}/api/v1/payments/momo-ipn";
            }

            var requestId = Guid.NewGuid().ToString();
            var orderId = order.OrderId.ToString();
            var orderInfo = order.Description;
            var amount = ((long)order.Amount).ToString();
            var redirectUrl = order.ReturnUrl;
            var extraData = "";
            var requestType = "captureWallet";

            var rawHash = $"accessKey={accessKey}&amount={amount}&extraData={extraData}&ipnUrl={ipnUrl}&orderId={orderId}&orderInfo={orderInfo}&partnerCode={partnerCode}&redirectUrl={redirectUrl}&requestId={requestId}&requestType={requestType}";
            var signature = HmacSha256(secretKey, rawHash);

            var requestBody = new
            {
                partnerCode,
                partnerName = "Test Store",
                storeId = "MomoTestStore",
                requestId,
                amount = (long)order.Amount,
                orderId,
                orderInfo,
                redirectUrl,
                ipnUrl,
                lang = "vi",
                extraData,
                requestType,
                signature
            };

            using var httpClient = _httpClientFactory.CreateClient();
            var response = await httpClient.PostAsJsonAsync(endpoint, requestBody);
            
            if (!response.IsSuccessStatusCode)
            {
                return new CreatePaymentResult
                {
                    Success = false,
                    Message = $"MoMo API error: {response.ReasonPhrase}"
                };
            }

            var responseJson = await response.Content.ReadFromJsonAsync<JsonElement>();
            if (responseJson.TryGetProperty("resultCode", out var codeProperty) && codeProperty.GetInt32() == 0)
            {
                var payUrl = responseJson.GetProperty("payUrl").GetString() ?? string.Empty;
                return new CreatePaymentResult
                {
                    Success = true,
                    PaymentUrl = payUrl,
                    TransactionId = orderId,
                    Message = "MoMo URL generated successfully."
                };
            }

            var errorMessage = responseJson.TryGetProperty("message", out var msgProp) ? msgProp.GetString() : "Unknown error";
            return new CreatePaymentResult
            {
                Success = false,
                Message = $"MoMo Payment generation failed: {errorMessage}"
            };
        }
        catch (Exception ex)
        {
            return new CreatePaymentResult
            {
                Success = false,
                Message = $"Error calling MoMo Gateway: {ex.Message}"
            };
        }
    }

    public Task<bool> VerifyCallbackAsync(PaymentCallbackDto callbackData)
    {
        try
        {
            var secretKey = _configuration["PaymentGateways:MoMo:SecretKey"];
            if (string.IsNullOrEmpty(secretKey))
            {
                var msg = "PaymentGateways:MoMo:SecretKey configuration is missing.";
                throw new InvalidOperationException(msg);
            }

            // Verify MoMo Webhook/IPN
            var partnerCode = callbackData.QueryParameters.GetValueOrDefault("partnerCode") ?? string.Empty;
            var orderId = callbackData.QueryParameters.GetValueOrDefault("orderId") ?? string.Empty;
            var requestId = callbackData.QueryParameters.GetValueOrDefault("requestId") ?? string.Empty;
            var amount = callbackData.QueryParameters.GetValueOrDefault("amount") ?? string.Empty;
            var orderInfo = callbackData.QueryParameters.GetValueOrDefault("orderInfo") ?? string.Empty;
            var orderType = callbackData.QueryParameters.GetValueOrDefault("orderType") ?? string.Empty;
            var transId = callbackData.QueryParameters.GetValueOrDefault("transId") ?? string.Empty;
            var resultCode = callbackData.QueryParameters.GetValueOrDefault("resultCode") ?? string.Empty;
            var message = callbackData.QueryParameters.GetValueOrDefault("message") ?? string.Empty;
            var payType = callbackData.QueryParameters.GetValueOrDefault("payType") ?? string.Empty;
            var responseTime = callbackData.QueryParameters.GetValueOrDefault("responseTime") ?? string.Empty;
            var extraData = callbackData.QueryParameters.GetValueOrDefault("extraData") ?? string.Empty;
            var signature = callbackData.QueryParameters.GetValueOrDefault("signature") ?? string.Empty;

            var rawHash = $"amount={amount}&contactInfo=&extraData={extraData}&message={message}&orderId={orderId}&orderInfo={orderInfo}&orderType={orderType}&partnerCode={partnerCode}&payType={payType}&requestId={requestId}&responseTime={responseTime}&resultCode={resultCode}&transId={transId}";
            // Note: Depending on MoMo version/doc, the contactInfo field might or might not be there. Let's make sure our signature check matches MoMo spec.
            // If contactInfo is not present in rawHash, MoMo signature verification might omit it.
            // Let's use the most common signature string:
            var rawHashStandard = $"amount={amount}&extraData={extraData}&message={message}&orderId={orderId}&orderInfo={orderInfo}&orderType={orderType}&partnerCode={partnerCode}&payType={payType}&requestId={requestId}&responseTime={responseTime}&resultCode={resultCode}&transId={transId}";

            var checkHash = HmacSha256(secretKey, rawHashStandard);
            var isValid = string.Equals(signature, checkHash, StringComparison.OrdinalIgnoreCase);

            return Task.FromResult(isValid && resultCode == "0");
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    private static string HmacSha256(string key, string inputData)
    {
        var hash = new StringBuilder();
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var inputBytes = Encoding.UTF8.GetBytes(inputData);
        using (var hmac = new HMACSHA256(keyBytes))
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
