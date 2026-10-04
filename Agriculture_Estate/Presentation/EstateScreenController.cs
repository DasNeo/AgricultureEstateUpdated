using System;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI.Data;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;

namespace AgricultureEstate
{
    internal static class EstateScreenController
    {
        private static GauntletLayer? layer;
        private static IGauntletMovie? gauntletMovie;
        private static GauntletMovieIdentifier? gauntletMovieIdentifier;

        private static LandManagementVM? landManagementVM;
        private static GauntletLayer? layer2;
        private static IGauntletMovie? gauntletMovie2;

        private static GauntletMovieIdentifier? gauntletMovie2Identifier;

        public static EstateListVM? estateListVM;
        public static void CreateVMLayer(VillageLand? village = null)
        {
            if (village is null)
                village = AgricultureEstateBehavior.GetVillageLand(Settlement.CurrentSettlement);
            try
            {
                if (layer != null)
                    return;
                layer = new GauntletLayer("GauntletLayer", 1000, false);
                if (landManagementVM == null)
                    landManagementVM = new LandManagementVM(village);
                landManagementVM.RefreshValues();
                gauntletMovieIdentifier = layer.LoadMovie("LandManagement", landManagementVM);
                gauntletMovie = gauntletMovieIdentifier.Movie;
                layer.InputRestrictions.SetInputRestrictions(true, (InputUsageMask)7);
                ScreenManager.TopScreen.AddLayer(layer);
                layer.IsFocusLayer = true;
                ScreenManager.TrySetFocus(layer);
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage(ex.ToString()));
                Console.WriteLine(ex);
            }
        }

        public static void DeleteVMLayer()
        {
            ScreenBase topScreen = ScreenManager.TopScreen;
            if (layer != null)
            {
                layer.InputRestrictions.ResetInputRestrictions();
                layer.IsFocusLayer = false;
                if (gauntletMovie != null)
                    layer.ReleaseMovie(gauntletMovieIdentifier);
                topScreen.RemoveLayer(layer);
            }
            layer = null;
            gauntletMovie = null;
            landManagementVM = null;
        }

        public static void CreateVMLayer2()
        {
            try
            {
                if (layer2 != null)
                    return;
                layer2 = new GauntletLayer("GauntletLayer", 1200, false);
                if (estateListVM == null)
                    estateListVM = new EstateListVM();
                estateListVM.RefreshValues();
                gauntletMovie2Identifier = layer2.LoadMovie("EstateList", estateListVM);
                gauntletMovie2 = gauntletMovie2Identifier.Movie;

                layer2.InputRestrictions.SetInputRestrictions(true, (InputUsageMask)7);
                ScreenManager.TopScreen.AddLayer(layer2);
                layer2.IsFocusLayer = true;
                ScreenManager.TrySetFocus(layer2);
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage(ex.ToString()));
                Console.WriteLine(ex);
            }
        }

        public static void DeleteVMLayer2()
        {
            ScreenBase topScreen = ScreenManager.TopScreen;
            if (layer2 != null)
            {
                layer2.InputRestrictions.ResetInputRestrictions();
                layer2.IsFocusLayer = false;
                if (gauntletMovie2 != null)
                    layer2.ReleaseMovie(gauntletMovie2Identifier);
                topScreen.RemoveLayer(layer2);
            }
            layer2 = null;
            gauntletMovie2 = null;
            estateListVM = null;
        }

    }
}
