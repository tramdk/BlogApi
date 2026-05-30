using System;
using System.Collections.Generic;
using System.Linq;
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
/// PayOS payment gateway integration.
/// </summary>
public class PayOsService(IConfiguration configuration, IHttpClientFactory httpClientFactory) : IPaymentService
{
    private readonly IConfiguration _configuration =
        configuration ?? throw new ArgumentNullException(nameof(configuration));
    private readonly IHttpClientFactory _httpClientFactory =
        httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));

    public string GatewayName => PaymentMethod.PAYOS;

    public async Task<CreatePaymentResult> CreatePaymentUrlAsync(OrderPaymentDto order)
    {
        try
        {
            var endpoint = _configuration["PaymentGateways:PayOS:Url"];
            var clientId = _configuration["PaymentGateways:PayOS:ClientId"];
            var apiKey = _configuration["PaymentGateways:PayOS:ApiKey"];
            var checksumKey = _configuration["PaymentGateways:PayOS:ChecksumKey"];

            if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(clientId) || 
                string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(checksumKey))
            {
                throw new InvalidOperationException("One or more PayOS configuration values are missing.");
            }

            // PayOS requires orderCode as long (64-bit integer)
            long orderCode = Math.Abs(BitConverter.ToInt64(order.OrderId.ToByteArray(), 0) % 9999999999);

            var cancelUrl = order.ReturnUrl;
            var returnUrl = order.ReturnUrl;
            var description = order.Description.Length > 25 ? order.Description[..25] : order.Description; // Max length for PayOS is 25 chars
            var amount = (long)order.Amount;

            var rawHash = $"amount={amount}&cancelUrl={cancelUrl}&description={description}&orderCode={orderCode}&returnUrl={returnUrl}";
            var signature = HmacSha256(checksumKey, rawHash);

            var requestBody = new
            {
                orderCode,
                amount,
                description,
                cancelUrl,
                returnUrl,
                signature
            };

            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Add("x-client-id", clientId);
            httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);

            var response = await httpClient.PostAsJsonAsync(endpoint, requestBody);
            
            if (!response.IsSuccessStatusCode)
            {
                return new CreatePaymentResult
                {
                    Success = false,
                    Message = $"PayOS API error: {response.ReasonPhrase}"
                };
            }

            var responseJson = await response.Content.ReadFromJsonAsync<JsonElement>();
            if (responseJson.TryGetProperty("code", out var codeProperty) && codeProperty.GetString() == "00")
            {
                var data = responseJson.GetProperty("data");
                var checkoutUrl = data.GetProperty("checkoutUrl").GetString() ?? string.Empty;
                return new CreatePaymentResult
                {
                    Success = true,
                    PaymentUrl = checkoutUrl,
                    TransactionId = orderCode.ToString(),
                    Message = "PayOS Link generated successfully."
                };
            }

            var errorMessage = responseJson.TryGetProperty("desc", out var descProp) ? descProp.GetString() : "Unknown error";
            return new CreatePaymentResult
            {
                Success = false,
                Message = $"PayOS generation failed: {errorMessage}"
            };
        }
        catch (Exception ex)
        {
            return new CreatePaymentResult
            {
                Success = false,
                Message = $"Error calling PayOS Gateway: {ex.Message}"
            };
        }
    }

    public Task<bool> VerifyCallbackAsync(PaymentCallbackDto callbackData)
    {
        try
        {
            var checksumKey = _configuration["PaymentGateways:PayOS:ChecksumKey"];
            if (string.IsNullOrEmpty(checksumKey))
            {
                throw new InvalidOperationException("PaymentGateways:PayOS:ChecksumKey configuration is missing.");
            }

            // In PayOS webhook, data signature is verification
            // Payload is a JSON string received inside the webhook body
            if (string.IsNullOrEmpty(callbackData.RawBody))
            {
                return Task.FromResult(false);
            }

            using var jsonDoc = JsonDocument.Parse(callbackData.RawBody);
            var root = jsonDoc.RootElement;
            var code = root.GetProperty("code").GetString();
            if (code != "00")
            {
                return Task.FromResult(false);
            }

            var dataElement = root.GetProperty("data");
            var signature = root.GetProperty("signature").GetString() ?? string.Empty;

            // PayOS signature verification: order data params sorted alphabetically
            var amount = dataElement.GetProperty("amount").GetInt64();
            var description = dataElement.GetProperty("description").GetString() ?? string.Empty;
            var orderCode = dataElement.GetProperty("orderCode").GetInt64();
            var reference = dataElement.GetProperty("reference").GetString() ?? string.Empty;

            var rawHash = $"amount={amount}&description={description}&orderCode={orderCode}&reference={reference}";
            var checkHash = HmacSha256(checksumKey, rawHash);

            var isValid = string.Equals(signature, checkHash, StringComparison.OrdinalIgnoreCase);
            return Task.FromResult(isValid);
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
