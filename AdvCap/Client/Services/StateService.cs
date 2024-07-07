using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using System.Timers;

public class BusinessState
{
    public int Amount { get; set; }
    public double WorkTimestamp { get; set; }
    public bool IsWorking { get; set; }
    public DateTime StartTime { get; set; }
}

public class ManagerState
{
    public bool IsUnlocked { get; set; }
}

public class WalletState
{
    public double Money { get; set; }
    public string MoneyString => MoneyUtil.MoneyToString(Money);
}

public class StateService : IDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ConfigService _configService;
    public event Action OnChange;
    private static StateService _instance;
    private System.Timers.Timer _timer;

    public Dictionary<string, BusinessState> Businesses { get; private set; }
    public Dictionary<string, ManagerState> Managers { get; private set; }
    public WalletState Wallet { get; private set; }
    public double LastTimestamp { get; private set; }
    public double OfflineEarnings { get; private set; }

    public StateService(ConfigService configService, IJSRuntime jsRuntime)
    {
        _configService = configService;
        _jsRuntime = jsRuntime;
        Businesses = new Dictionary<string, BusinessState>();
        Managers = new Dictionary<string, ManagerState>();
        Wallet = new WalletState { Money = 0 };
        _instance = this;

        _timer = new System.Timers.Timer(1000);
        _timer.Elapsed += (sender, args) => CheckAndCompleteWork();
        _timer.Start();
    }

    public async Task InitializeAsync()
    {
        InitializeBusinesses();
        InitializeManagers();
        UnlockInitialBusinesses();
        UnlockInitialManagers();
        await LoadStateAsync();
    }

    private void InitializeBusinesses()
    {
        foreach (var businessID in _configService.Businesses.Keys)
        {
            Businesses[businessID] = new BusinessState { Amount = 0, WorkTimestamp = -1, IsWorking = false, StartTime = DateTime.MinValue };
        }
    }

    private void InitializeManagers()
    {
        foreach (var managerID in _configService.Managers.Keys)
        {
            Managers[managerID] = new ManagerState { IsUnlocked = false };
        }
    }

    private void UnlockInitialBusinesses()
    {
        foreach (var initialBusinessID in _configService.GetInitialBusinessIDs())
        {
            UnlockBusiness(initialBusinessID);
        }
    }

    private void UnlockInitialManagers()
    {
        foreach (var initialManagerID in _configService.GetInitialManagerIDs())
        {
            UnlockManager(initialManagerID);
        }
    }

    public async Task SaveStateAsync()
    {
        var state = new
        {
            Businesses,
            Managers,
            Wallet,
            LastTimestamp = DateTime.Now.Ticks
        };

        var json = JsonSerializer.Serialize(state);
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "gameState", json);
    }

    [JSInvokable]
    public static async Task SaveStateOnExit()
    {
        if (_instance != null)
        {
            await _instance.SaveStateAsync();
        }
    }

    public async Task LoadStateAsync()
    {
        var json = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "gameState");
        if (!string.IsNullOrEmpty(json))
        {
            var state = JsonSerializer.Deserialize<State>(json);
            Businesses = state.Businesses;
            Managers = state.Managers;
            Wallet = state.Wallet;
            LastTimestamp = state.LastTimestamp;
        }
        else
        {
            // Store the initial state if no state is found
            LastTimestamp = DateTime.Now.Ticks;
            await SaveStateAsync();
        }
    }

    private class State
    {
        public Dictionary<string, BusinessState> Businesses { get; set; }
        public Dictionary<string, ManagerState> Managers { get; set; }
        public WalletState Wallet { get; set; }
        public double LastTimestamp { get; set; }
    }

    public void UnlockBusiness(string businessID)
    {
        if (Businesses.ContainsKey(businessID))
        {
            Businesses[businessID].Amount = 1;
            NotifyStateChanged();
        }
    }

    public void UnlockManager(string managerID)
    {
        if (Managers.ContainsKey(managerID))
        {
            Managers[managerID].IsUnlocked = true;
            NotifyStateChanged();
        }
    }

    public void UpgradeBusiness(string businessID, int amount)
    {
        if (Businesses.ContainsKey(businessID))
        {
            Businesses[businessID].Amount += amount;
            NotifyStateChanged();
        }
    }

    public void AddMoney(double amount)
    {
        Wallet.Money += amount;
        NotifyStateChanged();
    }

    public void SubtractMoney(double amount)
    {
        Wallet.Money -= amount;
        NotifyStateChanged();
    }

    public double CalculateProfit(string businessID)
    {
        if (!Businesses.ContainsKey(businessID))
            return 0;

        var business = Businesses[businessID];
        var config = _configService.GetBusinessConfig(businessID);

        return business.Amount * config.InitialRevenue;
    }

    public double GetTimeToProfit(string businessID)
    {
        var business = Businesses[businessID];
        return _configService.GetTimeToProfit(businessID, business.Amount);
    }

    public void StartWork(string businessID)
    {
        if (!Businesses.ContainsKey(businessID))
            return;

        var business = Businesses[businessID];
        business.StartTime = DateTime.Now;
        business.IsWorking = true;
        business.WorkTimestamp = DateTime.Now.AddSeconds(GetTimeToProfit(businessID)).Ticks;
        NotifyStateChanged();
    }

    public void CheckAndCompleteWork()
    {
        foreach (var businessID in Businesses.Keys)
        {
            var business = Businesses[businessID];
            if (business.IsWorking && DateTime.Now.Ticks >= business.WorkTimestamp)
            {
                double profit = CalculateProfit(businessID);
                AddMoney(profit);
                business.IsWorking = false;
                NotifyStateChanged();
            }

            var managerID = GetManagerIDForBusiness(businessID);
            if (managerID != null && Managers[managerID].IsUnlocked && !business.IsWorking)
            {
                StartWork(businessID);
            }
        }
    }

    public bool HasHireableManager()
    {
        foreach (var managerID in _configService.GetManagerIDs())
        {
            if (CanHireManager(managerID))
            {
                return true;
            }
        }
        return false;
    }

    public bool CanHireManager(string managerID)
    {
        if (!Managers.ContainsKey(managerID))
            return false;

        var managerConfig = _configService.GetManagerConfig(managerID);
        return Wallet.Money >= managerConfig.Cost && !Managers[managerID].IsUnlocked;
    }
    public void HireManager(string managerID)
    {
        if (!Managers.ContainsKey(managerID))
            return;

        var managerConfig = _configService.GetManagerConfig(managerID);
        if (Wallet.Money >= managerConfig.Cost && !Managers[managerID].IsUnlocked)
        {
            SubtractMoney(managerConfig.Cost);
            Managers[managerID].IsUnlocked = true;

            var businessID = managerConfig.BusinessID;
            if (!Businesses[businessID].IsWorking)
            {
                StartWork(businessID);
            }

            NotifyStateChanged();
        }
    }

    public string GetManagerIDForBusiness(string businessID)
    {
        foreach (var managerID in _configService.Managers.Keys)
        {
            if (_configService.Managers[managerID].BusinessID == businessID)
            {
                return managerID;
            }
        }
        return null;
    }

    private double CalculateOfflineEarnings()
    {
        double totalEarnings = 0;
        double now = DateTime.Now.Ticks;
        double elapsedSeconds = (now - LastTimestamp) / TimeSpan.TicksPerSecond;

        foreach (var businessID in Businesses.Keys)
        {
            var business = Businesses[businessID];
            if (Managers.ContainsKey(businessID) && Managers[businessID].IsUnlocked)
            {
                var timeToProfit = GetTimeToProfit(businessID);
                double cycles = elapsedSeconds / timeToProfit;
                totalEarnings += CalculateProfit(businessID) * cycles;
            }
            else if (business.IsWorking && now >= business.WorkTimestamp)
            {
                totalEarnings += CalculateProfit(businessID);
            }
        }

        return totalEarnings;
    }

    public async Task HandleAppStartAsync()
    {
        await LoadStateAsync();
        OfflineEarnings = CalculateOfflineEarnings();
        if (OfflineEarnings > 0)
        {
            NotifyStateChanged();
        }
    }

    public async Task CollectOfflineEarningsAsync()
    {
        AddMoney(OfflineEarnings);
        OfflineEarnings = 0;
        await SaveStateAsync();
    }

    private void NotifyStateChanged() => OnChange?.Invoke();

    public void Dispose()
    {
        _timer?.Dispose();
    }
}
