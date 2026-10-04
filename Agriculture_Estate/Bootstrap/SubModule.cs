using HarmonyLib;
using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.CampaignSystem.GameComponents;
using System.Linq;

namespace AgricultureEstate
{
    public class SubModule : MBSubModuleBase
    {
        private static readonly List<Action> ActionsToExecuteNextTick = new List<Action>();
        public static int PlotBuyPrice => EstateConfiguration.PlotBuyPrice;
        public static int PlotSellPrice => EstateConfiguration.PlotSellPrice;
        public static int UndevelopedPlotBuyPrice => EstateConfiguration.UndevelopedPlotBuyPrice;
        public static int UndevelopedPlotSellPrice => EstateConfiguration.UndevelopedPlotSellPrice;
        public static int ProjectCost => EstateConfiguration.ProjectCost;
        public static float LandRentScale => EstateConfiguration.LandRentScale;
        public static float SlaveProductionScale => EstateConfiguration.SlaveProductionScale;
        private Harmony harmony = new Harmony("AgricultureEstate");

        protected override void OnBeforeInitialModuleScreenSetAsRoot() => base.OnBeforeInitialModuleScreenSetAsRoot();

        protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
            if (!(game.GameType is Campaign))
                return;
            ((CampaignGameStarter)gameStarterObject).AddBehavior(new AgricultureEstateBehavior());
        }

        public override void OnAfterGameInitializationFinished(Game game, object starterObject)
        {
            harmony.Unpatch(AccessTools.Method(typeof(DefaultClanFinanceModel), "CalculateClanIncomeInternal"),
                    HarmonyPatchType.Postfix, "AgricultureEstate"); // Unpatch as otherwise we add multiple postfixes

            base.OnAfterGameInitializationFinished(game, starterObject);
            harmony.Patch(AccessTools.Method(typeof(DefaultClanFinanceModel), "CalculateClanIncomeInternal"),
                postfix: new HarmonyMethod(typeof(ClanFiancePatch), "Postfix"));
        }

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            harmony.PatchAll();
        }

        public static void ExecuteActionOnNextTick(Action action)
        {
            if (action == null)
                return;
            ActionsToExecuteNextTick.Add(action);
        }

        protected override void OnApplicationTick(float dt)
        {
            base.OnApplicationTick(dt);
            foreach (Action action in ActionsToExecuteNextTick)
                action();
            ActionsToExecuteNextTick.Clear();
        }
    }
}