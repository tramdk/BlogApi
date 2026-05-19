using System.ComponentModel.DataAnnotations;

namespace FloraCore.Application.Common.Models;

/// <summary>
/// Đại diện cho các cấu hình của hệ thống Telemetry và OpenTelemetry.
/// </summary>
public class TelemetryOptions
{
    /// <summary>
    /// Vị trí chính xác của section cấu hình Telemetry trong appsettings.json.
    /// </summary>
    public const string SectionName = "Telemetry";

    /// <summary>
    /// Địa chỉ Endpoint OTLP của bộ thu thập dữ liệu (ví dụ: http://localhost:4317).
    /// </summary>
    [Required(ErrorMessage = "OtlpEndpoint là bắt buộc.")]
    [Url(ErrorMessage = "OtlpEndpoint phải là một địa chỉ URL hợp lệ.")]
    public string OtlpEndpoint { get; set; } = "http://localhost:4317";

    /// <summary>
    /// Xác định xem có xuất thông tin telemetry ra Console hay không.
    /// </summary>
    public bool ExportToConsole { get; set; }
}
