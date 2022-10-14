using AgricultureEstate;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Agriculture_Estate
{
    public class SubModule : MBSubModuleBase
    {
        private static readonly List<Action> ActionsToExecuteNextTick = new List<Action>();
        public static int PlotBuyPrice;
        public static int PlotSellPrice;
        public static int UndevelopedPlotBuyPrice;
        public static int UndevelopedPlotSellPrice;
        public static int ProjectCost;
        public static float LandRentScale;
        public static float SlaveProductionScale;

        protected override void OnBeforeInitialModuleScreenSetAsRoot() => base.OnBeforeInitialModuleScreenSetAsRoot();

        protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
            if (!(game.GameType is Campaign))
                return;
            ((CampaignGameStarter)gameStarterObject).AddBehavior(new AgricultureEstateBehavior());
        }

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            this.loadSettings();
            new Harmony("AgricultureEstate").PatchAll();
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

        private void loadSettings()
        {
            Settings? settings = new XmlSerializer(typeof(Settings)).Deserialize(File.OpenRead(Path.Combine(BasePath.Name, "Modules/AgricultureEstate/settings.xml"))) as Settings;
            if (settings is not null)
            {
                PlotBuyPrice = settings.PlotBuyPrice;
                PlotSellPrice = settings.PlotSellPrice;
                UndevelopedPlotBuyPrice = settings.UndevelopedPlotBuyPrice;
                UndevelopedPlotSellPrice = settings.UndevelopedPlotSellPrice;
                ProjectCost = settings.ProjectCost;
                LandRentScale = settings.LandRentScale;
                SlaveProductionScale = settings.SlaveProductionScale;
            }
        }
    }
}