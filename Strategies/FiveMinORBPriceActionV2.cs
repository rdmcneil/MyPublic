#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

//This namespace holds Strategies in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Strategies
{
    public class FiveMinORBPriceActionV2 : Strategy
    {
        private TimeSpan tradingStartTime = new TimeSpan(7, 30, 0); // 9:30 AM
        private TimeSpan tradingEndTime = new TimeSpan(15, 59, 0);


        //private TimeSpan tradingStartTime = new TimeSpan(7, 30, 0); // 9:30 AM
        private TimeSpan tradingStartEndTime = new TimeSpan(8, 30, 0);
        private TimeSpan endTradingTime = new TimeSpan(14, 0, 0); // 9:30 AM


        private TimeSpan orbStartTime = new TimeSpan(7, 30, 0); // 9:30 AM
        private TimeSpan orbEndTime = new TimeSpan(7, 45, 0);

        private double orbRangeHigh;
        private double orbRangeLow;
        private double orbRangeMid;

        private double openingRangeHigh = double.MinValue;
        private double openingRangeLow = double.MaxValue;

        private bool ordersPlaced = false;
        private bool lastBarWasSignal = false;

        private bool hasTraded = false;

        private double longOrderPrice = 0;
        private double shortOrderPrice = 0;


        private double signalBarHigh = 0;
        private double signalBarLow = 0;

        private string longOrder = "LongBreakout";
        private string shortOrder = "ShortBreakdown";


        private bool orderFilled = false;

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = @"Enter the description for your new custom Strategy here.";
                Name = "FiveMinORBPriceActionV2";
                Calculate = Calculate.OnEachTick;
                //Calculate = Calculate.OnBarClose;
                EntriesPerDirection = 1;
                EntryHandling = EntryHandling.AllEntries;
                IsExitOnSessionCloseStrategy = true;
                ExitOnSessionCloseSeconds = 30;
                IsFillLimitOnTouch = false;
                MaximumBarsLookBack = MaximumBarsLookBack.TwoHundredFiftySix;
                OrderFillResolution = OrderFillResolution.Standard;
                Slippage = 0;
                StartBehavior = StartBehavior.WaitUntilFlat;
                TimeInForce = TimeInForce.Gtc;
                TraceOrders = false;
                RealtimeErrorHandling = RealtimeErrorHandling.StopCancelClose;
                StopTargetHandling = StopTargetHandling.PerEntryExecution;
                BarsRequiredToTrade = 20;
                // Disable this property for performance gains in Strategy Analyzer optimizations
                // See the Help Guide for additional information
                IsInstantiatedOnEachOptimizationIteration = true;
            }
            else if (State == State.Configure)
            {
                //AddDataSeries(Data.BarsPeriodType.Tick, 1);
            }
        }

        protected override void OnBarUpdate()
        {
            if (BarsInProgress != 0)
                return;
            if (true)
            {
                // Test for close outside ORB
                if (Position.MarketPosition == MarketPosition.Flat)
                {
                    if (GetCurrentAsk() > signalBarHigh && lastBarWasSignal)
                    {
                        Draw.Diamond(this, "EnterLong" + CurrentBar + Time, false, 0, Low[0] - TickSize * 10, Brushes.Green);
                        SetUpLongOrders(1, 1, signalBarHigh, signalBarLow);
                    }
                    if (GetCurrentAsk() < signalBarLow && lastBarWasSignal)
                    {
                        Draw.Diamond(this, "EnterShort" + CurrentBar + Time, false, 0, High[0] + TickSize * 10, Brushes.Red);
                        SetUpShortOrders(1, 1, signalBarHigh, signalBarLow);
                    }
                }
            }


            if (CurrentBar < 20 )
            {
                return;
            }


            // Get ORB H/Ls
            if ( Time[0].TimeOfDay >= orbStartTime && Time[0].TimeOfDay <= orbEndTime)
            {
                if (Time[0].TimeOfDay == orbStartTime)
                {
                    orbRangeHigh = High[0];
                    orbRangeLow = Low[0];
                    //fiveHasClosedAbove = false;
                    //fiveHasClosedBelow = false;
                    ordersPlaced = false;
                    hasTraded = false;
                    CancelAllOrders();
                }

                if (High[0] > orbRangeHigh)
                {
                    orbRangeHigh = High[0];
                }
                if (Low[0] < orbRangeLow)
                {
                    orbRangeLow = Low[0];
                }
                // Draw.ArrowUp(this, "Low" + CurrentBar + Time, false, 0, orbRangeLow - TickSize * 2, Brushes.Red);
                // Draw.ArrowDown(this, "High" + CurrentBar + Time, false, 0, orbRangeHigh + TickSize * 2, Brushes.Red);
            }
            // Print lines For ORB
            if (IsFirstTickOfBar && Time[0].TimeOfDay >= orbStartTime)
            {
                orbRangeMid = ((orbRangeHigh - orbRangeLow) / 2) + orbRangeLow;

                Draw.Line(this, "ORBHigh" + CurrentBar, false, 1, orbRangeHigh, 0, orbRangeHigh, Brushes.Green, DashStyleHelper.Solid, 2);
                Draw.Line(this, "ORBLow" + CurrentBar, false, 1, orbRangeLow, 0, orbRangeLow, Brushes.Red, DashStyleHelper.Solid, 2); Draw.Line(this, "ORBLow" + CurrentBar, false, 1, orbRangeLow, 0, orbRangeLow, Brushes.Red, DashStyleHelper.Solid, 2);
                Draw.Line(this, "ORBMid" + CurrentBar, false, 1, orbRangeMid, 0, orbRangeMid, Brushes.Yellow, DashStyleHelper.Solid, 2);

                //Add your custom strategy logic here.
            }
            // Look for close outside ORB
            if (Time[0].TimeOfDay >= orbStartTime)
            {
                // Break High of range
                if (Close[0] > orbRangeHigh && Close[1] < orbRangeHigh)
                {
                    if (true)
                    {
                        Draw.Line(this, "5 Orb High High" + CurrentBar, false, 1, High[0], 0, High[0], Brushes.Green, DashStyleHelper.Solid, 4);
                        Draw.Line(this, "5 Orb High Low" + CurrentBar, false, 1, Low[0], 0, Low[0], Brushes.Red, DashStyleHelper.Solid, 4);
                        Draw.Line(this, "5 Orb High Closes" + CurrentBar, false, 1, Close[0], 0, Close[0], Brushes.Yellow, DashStyleHelper.Solid, 4);
                        Draw.Line(this, "5 Orb High Open" + CurrentBar, false, 1, Open[0], 0, Open[0], Brushes.Yellow, DashStyleHelper.Solid, 4);
                        Draw.Diamond(this, "5 Close above OR " + CurrentBar + Time, false, 0, High[0] + TickSize * 10, Brushes.Red);
                        Draw.ArrowUp(this, "UpperBreak " + CurrentBar + Time, false, 0, Low[0] -  (TickSize * 10), Brushes.Green);
                    }
                    lastBarWasSignal = true;
                    signalBarHigh = High[0];
                    signalBarLow = Low[0];
                    //SetUpShortLimitOrders(1, 1, High[0], Low[0]);
                    //Draw.Diamond(this, "5 Close above OR " + CurrentBar + Time, false, 0, Lows[1][0] - TickSize * 10, Brushes.Green);
                }
                if (Close[0] < orbRangeLow && Close[1] > orbRangeLow)
                {
                    if (true)
                    {
                        Draw.Line(this, "5 Orb Low High" + CurrentBar, false, 1, High[0], 0, High[0], Brushes.Green, DashStyleHelper.Solid, 4);
                        Draw.Line(this, "5 Orb Low Low" + CurrentBar, false, 1, Low[0], 0, Low[0], Brushes.Red, DashStyleHelper.Solid, 4);
                        Draw.Line(this, "5 Orb Low Closes" + CurrentBar, false, 1, Close[0], 0, Close[0], Brushes.Yellow, DashStyleHelper.Solid, 4);
                        Draw.Line(this, "5 Orb Low Open" + CurrentBar, false, 1, Open[0], 0, Open[0], Brushes.Yellow, DashStyleHelper.Solid, 4);
                        Draw.ArrowDown (this, "LowerBreak " + CurrentBar + Time, false, 0, High[0] + (TickSize * 10), Brushes.Red);
                    }
                    lastBarWasSignal = true;
                    signalBarHigh = High[0];
                    signalBarLow = Low[0];
                    //SetUpLongLimitOrders(1, 1, High[0], Low[0]);
                    //Draw.Diamond(this, "5 Close above OR " + CurrentBar + Time, false, 0, Highs[1][0] + TickSize * 10, Brushes.Red);
                }

                
            }
        }

        private void SetUpLimitOrders(int orderMultiplier, int orderTypes, double highOrderPrice, double lowOrderPrice)
        {
            shortOrderPrice = lowOrderPrice;
            longOrderPrice = highOrderPrice;
            EnterLongLimit(0, true, 1, longOrderPrice, longOrder);
            EnterShortLimit(0, true, 1, shortOrderPrice, shortOrder);
            ordersPlaced = true;
            if (true)
            {
                Draw.Line(this, "shortOrderPrice" + CurrentBar, false, 1, shortOrderPrice, 0, shortOrderPrice, Brushes.Yellow, DashStyleHelper.Solid, 4);
                Draw.Line(this, "longOrderPrice" + CurrentBar, false, 1, longOrderPrice, 0, longOrderPrice, Brushes.Yellow, DashStyleHelper.Solid, 4);
                //Draw.Line(this, "longTarget" + CurrentBar, false, 1, longTarget, 0, longTarget, Brushes.Green, DashStyleHelper.Solid, 4);
                //Draw.Line(this, "shortTarget" + CurrentBar, false, 1, shortTarget, 0, shortTarget, Brushes.Red, DashStyleHelper.Solid, 4);
            }
        }
        private void SetUpLongOrders(int orderMultiplier, int orderTypes, double highOrderPrice, double lowOrderPrice)
        {
            shortOrderPrice = lowOrderPrice;
            longOrderPrice = highOrderPrice;
            double longSL = lowOrderPrice;
            double slRange = highOrderPrice - lowOrderPrice;
            double targetMultiplier = 1.2;
            double longTarget = (highOrderPrice + (slRange * targetMultiplier));
            EnterLong(1, 1, longOrder);
            SetStopLoss(longOrder, CalculationMode.Price, shortOrderPrice, false);
            SetProfitTarget(longOrder, CalculationMode.Price, longTarget);
            //EnterShortLimit(0, true, 1, shortOrderPrice, shortOrder);
            ordersPlaced = true;
            lastBarWasSignal = false;
            if (true)
            {
                Draw.Line(this, "shortOrderPrice" + CurrentBar, false, 1, shortOrderPrice, 0, shortOrderPrice, Brushes.Yellow, DashStyleHelper.Solid, 4);
                Draw.Line(this, "longOrderPrice" + CurrentBar, false, 1, longOrderPrice, 0, longOrderPrice, Brushes.Yellow, DashStyleHelper.Solid, 4);
                //Draw.Line(this, "longTarget" + CurrentBar, false, 1, longTarget, 0, longTarget, Brushes.Green, DashStyleHelper.Solid, 4);
                //Draw.Line(this, "shortTarget" + CurrentBar, false, 1, shortTarget, 0, shortTarget, Brushes.Red, DashStyleHelper.Solid, 4);
            }
        }
        private void SetUpShortOrders(int orderMultiplier, int orderTypes, double highOrderPrice, double lowOrderPrice)
        {
            shortOrderPrice = lowOrderPrice;
            longOrderPrice = highOrderPrice;
            double slRange = highOrderPrice - lowOrderPrice;
            double targetMultiplier = 1.2;
            double shortTarget = (lowOrderPrice - (slRange * targetMultiplier));
            //EnterLongLimit(0, true, 1, longOrderPrice, longOrder);
            EnterShort(1, 1, shortOrder);
            SetStopLoss(shortOrder, CalculationMode.Price, longOrderPrice, false);
            SetProfitTarget(shortOrder, CalculationMode.Price, shortTarget);
            ordersPlaced = true;
            lastBarWasSignal = false;
            if (true)
            {
                Draw.Line(this, "shortOrderPrice" + CurrentBar, false, 1, shortOrderPrice, 0, shortOrderPrice, Brushes.Yellow, DashStyleHelper.Solid, 4);
                Draw.Line(this, "longOrderPrice" + CurrentBar, false, 1, longOrderPrice, 0, longOrderPrice, Brushes.Yellow, DashStyleHelper.Solid, 4);
                //Draw.Line(this, "longTarget" + CurrentBar, false, 1, longTarget, 0, longTarget, Brushes.Green, DashStyleHelper.Solid, 4);
                //Draw.Line(this, "shortTarget" + CurrentBar, false, 1, shortTarget, 0, shortTarget, Brushes.Red, DashStyleHelper.Solid, 4);
            }
        }
        private void CancelAllOrders()
        {
            foreach (Order o in Orders)
            {
                if (o != null && o.OrderState == OrderState.Working)
                {
                    CancelOrder(o);
                }
            }
        }
        private void CancelOrderByName(string name)
        {
            foreach (Order o in Orders)
            {
                if (o != null && o.Name == name && o.OrderState == OrderState.Working)
                {
                    CancelOrder(o);
                }
            }
        }
    }
}
