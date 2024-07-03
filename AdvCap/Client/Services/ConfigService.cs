using System;
using System.Collections.Generic;

public class BusinessConfig
{
    public string Name { get; set; }
    public string Image { get; set; }
    public bool AutoUnlocked { get; set; }
    public double InitialCost { get; set; }
    public double Coefficient { get; set; }
    public double InitialTime { get; set; }
    public double InitialRevenue { get; set; }
}

public class ManagerConfig
{
    public bool AutoUnlocked { get; set; }
    public string BusinessID { get; set; }
    public string Name { get; set; }
    public string Image { get; set; }
    public double Cost { get; set; }
}

public class ConfigService
{
    public Dictionary<string, BusinessConfig> Businesses { get; private set; }
    public Dictionary<string, ManagerConfig> Managers { get; private set; }

    public ConfigService()
    {
        InitializeConfigs();
    }

    private void InitializeConfigs()
    {
        Businesses = new Dictionary<string, BusinessConfig>
        {
            { "business-0", new BusinessConfig { Name = "Lemonade Stand", Image = "assets/business/Lemonade_Stand.png", AutoUnlocked = true, InitialCost = 3.738, Coefficient = 1.07, InitialTime = 0.6, InitialRevenue = 1 } },
            { "business-1", new BusinessConfig { Name = "Newspaper Delivery", Image = "assets/business/Newspaper_Delivery.png", InitialCost = 60, Coefficient = 1.15, InitialTime = 3, InitialRevenue = 60 } },
            { "business-2", new BusinessConfig { Name = "Car Wash", Image = "assets/business/Car_Wash.png", InitialCost = 720, Coefficient = 1.14, InitialTime = 6, InitialRevenue = 540 } },
            { "business-3", new BusinessConfig { Name = "Pizza Delivery", Image = "assets/business/Pizza_Delivery.png", InitialCost = 8640, Coefficient = 1.13, InitialTime = 12, InitialRevenue = 4320 } },
            { "business-4", new BusinessConfig { Name = "Donut Shop", Image = "assets/business/Donut_Shop.png", InitialCost = 103680, Coefficient = 1.12, InitialTime = 24, InitialRevenue = 51840 } },
            { "business-5", new BusinessConfig { Name = "Shrimp Boat", Image = "assets/business/Shrimp_Boat.png", InitialCost = 1244160, Coefficient = 1.11, InitialTime = 96, InitialRevenue = 622080 } },
            { "business-6", new BusinessConfig { Name = "Hockey Team", Image = "assets/business/Hockey_Team.png", InitialCost = 14929920, Coefficient = 1.10, InitialTime = 384, InitialRevenue = 7464960 } },
            { "business-7", new BusinessConfig { Name = "Movie Studio", Image = "assets/business/Movie_Studio.png", InitialCost = 179159040, Coefficient = 1.09, InitialTime = 1536, InitialRevenue = 89579520 } },
            { "business-8", new BusinessConfig { Name = "Bank", Image = "assets/business/Bank.png", InitialCost = 2149908480, Coefficient = 1.08, InitialTime = 6144, InitialRevenue = 1074954240 } },
            { "business-9", new BusinessConfig { Name = "Oil Company", Image = "assets/business/Oil_Company.png", InitialCost = 25798901760, Coefficient = 1.07, InitialTime = 36864, InitialRevenue = 29668737024 } }
        };

        Managers = new Dictionary<string, ManagerConfig>
        {
            { "manager-0", new ManagerConfig { AutoUnlocked = false, BusinessID = "business-0", Name = "Cabe Johnson", Image = "assets/manager/Cabejohnson.jpg", Cost = 1000 } },
            { "manager-1", new ManagerConfig { BusinessID = "business-1", Name = "Perry Black", Image = "assets/manager/Perryblack.jpg", Cost = 15000 } },
            { "manager-2", new ManagerConfig { BusinessID = "business-2", Name = "W.W. Heisenbird", Image = "assets/manager/Heisenberg.jpg", Cost = 100000 } },
            { "manager-3", new ManagerConfig { BusinessID = "business-3", Name = "Mama Sean", Image = "assets/manager/Mama.jpg", Cost = 500000 } },
            { "manager-4", new ManagerConfig { BusinessID = "business-4", Name = "Jim Thorton", Image = "assets/manager/Jimthorton.jpg", Cost = 1200000 } },
            { "manager-5", new ManagerConfig { BusinessID = "business-5", Name = "Forest Trump", Image = "assets/manager/Foresttrump.jpg", Cost = 10000000 } },
            { "manager-6", new ManagerConfig { BusinessID = "business-6", Name = "Dawn Cheri", Image = "assets/manager/Dawncherry.jpg", Cost = 111111111 } },
            { "manager-7", new ManagerConfig { BusinessID = "business-7", Name = "Stefani Speilburger", Image = "assets/manager/Sspeilberg.jpg", Cost = 555555555 } },
            { "manager-8", new ManagerConfig { BusinessID = "business-8", Name = "The Dark Lord", Image = "assets/manager/Darklord.jpg", Cost = 10000000000 } },
            { "manager-9", new ManagerConfig { BusinessID = "business-9", Name = "Derrick Plainview", Image = "assets/manager/Derrick.jpg", Cost = 100000000000 } }
        };
    }

    public BusinessConfig GetBusinessConfig(string id)
    {
        return Businesses.ContainsKey(id) ? Businesses[id] : null;
    }

    public ManagerConfig GetManagerConfig(string id)
    {
        return Managers.ContainsKey(id) ? Managers[id] : null;
    }

    public bool IsValidBusiness(string id)
    {
        return Businesses.ContainsKey(id);
    }

    public bool IsValidManager(string id)
    {
        return Managers.ContainsKey(id);
    }

    public List<string> GetInitialBusinessIDs()
    {
        List<string> initialBusinessIDs = new List<string>();
        foreach (var business in Businesses)
        {
            if (business.Value.AutoUnlocked)
            {
                initialBusinessIDs.Add(business.Key);
            }
        }
        return initialBusinessIDs;
    }

    public List<string> GetInitialManagerIDs()
    {
        List<string> initialManagerIDs = new List<string>();
        foreach (var manager in Managers)
        {
            if (manager.Value.AutoUnlocked)
            {
                initialManagerIDs.Add(manager.Key);
            }
        }
        return initialManagerIDs;
    }

    public double GetTimeToProfit(string id, int currentAmount)
    {
        if (!IsValidBusiness(id)) throw new ArgumentException("Invalid business id");
        var cfg = GetBusinessConfig(id);
        double multiplier = 1;
        foreach (var milestone in new[] { 25, 50, 100, 200, 300, 400 })
        {
            if (currentAmount < milestone)
            {
                break;
            }
            multiplier *= 0.5;
        }
        return cfg.InitialTime * multiplier;
    }

    public int GetNextMilestone(string id, int amount)
    {
        foreach (var milestone in new[] { 25, 50, 100, 200, 300, 400 })
        {
            if (amount < milestone)
            {
                return milestone;
            }
        }
        return 400; // maximum milestone
    }

    public string GetManagerIDForBusiness(string businessID)
    {
        foreach (var managerID in Managers.Keys)
        {
            if (Managers[managerID].BusinessID == businessID)
            {
                return managerID;
            }
        }
        return null;
    }
    public List<string> GetBusinessIDs()
    {
        return new List<string>(Businesses.Keys);
    }
}
