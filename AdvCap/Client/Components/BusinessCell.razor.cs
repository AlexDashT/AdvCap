using Microsoft.AspNetCore.Components;
using System;
using System.Timers;

namespace AdvCap.Client.Components
{
    public partial class BusinessCell : ComponentBase, IDisposable
    {
        [Parameter] public string BusinessID { get; set; }
        [Inject] private StateService StateService { get; set; }
        [Inject] private ConfigService ConfigService { get; set; }

        private BusinessConfig BusinessConfig => ConfigService.GetBusinessConfig(BusinessID);
        private BusinessState BusinessState => StateService.Businesses[BusinessID];
        private bool IsUnlocked => BusinessState.Amount > 0;
        private double UnlockCost => ConfigService.GetBusinessConfig(BusinessID).InitialCost;
        private string UnlockCostString => MoneyUtil.MoneyToString(UnlockCost);
        private string ProfitText => MoneyUtil.MoneyToString(BusinessState.Amount * ConfigService.GetBusinessConfig(BusinessID).InitialRevenue);
        private string UpgradeCostString => MoneyUtil.MoneyToString(CalculateUpgradeCost());
        private bool ShowManagerPopup { get; set; }
        private string ManagerID => ConfigService.GetManagerIDForBusiness(BusinessID);
        private bool IsManagerHired => StateService.Managers.ContainsKey(ManagerID) && StateService.Managers[ManagerID].IsUnlocked;
        private System.Timers.Timer _timer;

        protected override void OnInitialized()
        {
            _timer = new System.Timers.Timer(100); // Update every 100 milliseconds for smoother progress
            _timer.Elapsed += OnTimerElapsed;
            _timer.Start();
        }

        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            InvokeAsync(() =>
            {
                StateService.CheckAndCompleteWork();
                StateHasChanged();
            });
        }

        private void UnlockBusiness()
        {
            if (StateService.Wallet.Money >= UnlockCost)
            {
                StateService.SubtractMoney(UnlockCost);
                StateService.UnlockBusiness(BusinessID);
            }
        }

        private void UpgradeBusiness()
        {
            var cost = CalculateUpgradeCost();
            if (StateService.Wallet.Money >= cost)
            {
                StateService.SubtractMoney(cost);
                StateService.UpgradeBusiness(BusinessID, 1);
            }
        }

        private double CalculateUpgradeCost()
        {
            var amount = BusinessState.Amount;
            var initialCost = ConfigService.GetBusinessConfig(BusinessID).InitialCost;
            var coefficient = ConfigService.GetBusinessConfig(BusinessID).Coefficient;
            return initialCost * Math.Pow(coefficient, amount);
        }

        private void StartWork()
        {
            StateService.StartWork(BusinessID);
        }

        private string RemainingTimeString
        {
            get
            {
                if (BusinessState.IsWorking)
                {
                    var remainingTime = (BusinessState.WorkTimestamp - DateTime.Now.Ticks) / TimeSpan.TicksPerSecond;
                    return TimeUtil.SecondsToString(remainingTime);
                }
                else
                {
                    var timeToProfit = ConfigService.GetTimeToProfit(BusinessID, BusinessState.Amount);
                    return TimeUtil.SecondsToString(timeToProfit);
                }
            }
        }

        private double ProgressPercentage
        {
            get
            {
                if (BusinessState.IsWorking)
                {
                    var totalTime = (BusinessState.WorkTimestamp - BusinessState.StartTime.Ticks) / TimeSpan.TicksPerSecond;
                    if (totalTime == 0) return 0; // Avoid division by zero
                    var elapsedTime = (DateTime.Now.Ticks - BusinessState.StartTime.Ticks) / TimeSpan.TicksPerSecond;
                    return (elapsedTime / totalTime) * 100;
                }
                return 0;
            }
        }

        private double AmountProgressPercentage
        {
            get
            {
                var nextMilestone = ConfigService.GetNextMilestone(BusinessID, BusinessState.Amount);
                return ((double)BusinessState.Amount / nextMilestone) * 100;
            }
        }

        private string AmountProgressString
        {
            get
            {
                var nextMilestone = ConfigService.GetNextMilestone(BusinessID, BusinessState.Amount);
                return $"{BusinessState.Amount}/{nextMilestone}";
            }
        }
        private string GetCardClass()
        {
            return IsUnlocked || StateService.Wallet.Money >= UnlockCost ? "unlocked-business-card" : "locked-business-card";
        }

        private string GetImageClass()
        {
            if (BusinessState.IsWorking)
                return "working-business";
            return "idle-business";
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
