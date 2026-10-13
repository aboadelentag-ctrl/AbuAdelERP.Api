using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace KayanERP.UI.Services;

public sealed record ActiveBranchDto(
    long Id,
    string Code,
    string NameAr,
    string BranchType,
    int ActiveWarehousesCount,
    decimal TodaySalesVolume,
    bool IsActive
);

public interface IBranchContextService
{
    event Action? OnBranchChanged;
    ActiveBranchDto? CurrentBranch { get; }
    bool HasActiveBranch { get; }
    Task SetActiveBranchAsync(ActiveBranchDto branch);
    Task<ActiveBranchDto?> LoadSavedBranchAsync();
    Task ClearBranchContextAsync();
}

public sealed class BranchContextService : IBranchContextService
{
    private const string StorageKey = "kayan_active_branch_context";
    private readonly IJSRuntime _jsRuntime;

    public event Action? OnBranchChanged;
    public ActiveBranchDto? CurrentBranch { get; private set; }
    public bool HasActiveBranch => CurrentBranch is not null;

    public BranchContextService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task SetActiveBranchAsync(ActiveBranchDto branch)
    {
        CurrentBranch = branch ?? throw new ArgumentNullException(nameof(branch));
        var json = JsonSerializer.Serialize(branch);
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", StorageKey, json);
        OnBranchChanged?.Invoke();
    }

    public async Task<ActiveBranchDto?> LoadSavedBranchAsync()
    {
        try
        {
            var json = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", StorageKey);
            if (!string.IsNullOrWhiteSpace(json))
            {
                CurrentBranch = JsonSerializer.Deserialize<ActiveBranchDto>(json);
                OnBranchChanged?.Invoke();
            }
        }
        catch
        {
            CurrentBranch = null;
        }

        return CurrentBranch;
    }

    public async Task ClearBranchContextAsync()
    {
        CurrentBranch = null;
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", StorageKey);
        OnBranchChanged?.Invoke();
    }
}