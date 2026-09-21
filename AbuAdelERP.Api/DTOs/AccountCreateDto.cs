namespace AbuAdelERP.Api.DTOs;

public class AccountCreateDto
{
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty; // أمثلة: ASSET (أصل)، EXPENSE (مصروف)
    public long? ParentAccountId { get; set; }
}