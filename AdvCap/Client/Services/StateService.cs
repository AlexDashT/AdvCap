using System;
using System.Collections.Generic;

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

public class StateService
{
    private ConfigService _configService;
    public event Action OnChange;

    public Dictionary<string, BusinessState> Businesses { get; private set; }
    public Dictionary<string, ManagerState> Managers { get; private set; }
    public WalletState Wallet { get; private set; }

    public StateService(ConfigService configService)
    {
        _configService = configService;
        Businesses = new Dictionary<string, BusinessState>();
        Managers = new Dictionary<string, ManagerState>();
        Wallet = new WalletState { Money = 1000000000 };

        InitializeBusinesses();
        InitializeManagers();
        UnlockInitialBusinesses();
        UnlockInitialManagers();
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
        if (!Businesses.ContainsKey(businessID)) return 0;

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
        if (!Businesses.ContainsKey(businessID)) return;

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
        }
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
