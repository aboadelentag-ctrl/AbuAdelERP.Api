namespace AbuAdelERP.Api.DTOs
{
    // DTOs الأصول الثابتة
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

    // DTOs دورة الأسبات والعهد
    public record DispatchCrateDto(
        string MovementType,
        string ContainerName,
        int DispatchedQuantity,
        string SourceLocation,
        string DestinationLocation,
        string? Notes
    );

    public record ReceiveCrateDto(
        int ActualReceivedQuantity,
        int DamagedCount,
        string? ReceiveNotes
    );

    public record SettleDisputeDto(
        string ArbitrationDecision,
        string ArbitrationText,
        string? FinancialPenalty
    );
}