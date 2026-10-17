using System;

namespace KayanERP.Api.DTOs
{
    // DTOs الأصول الثابتة والماكينات الرأسمالية فقط
    public record CreateFixedAssetDto(
        string AssetCode,
        string Name,
        string Department,
        string Category,
        string? BrandModel,
        string? SerialNumber,
        string CustodianCode,
        string CustodianName,
        DateTime InstallationDate
    );

    public record UpdateFixedAssetDto(
        string Name,
        string Department,
        string Category,
        string? BrandModel,
        string? SerialNumber,
        string CustodianCode,
        string CustodianName,
        string Status
    );
}