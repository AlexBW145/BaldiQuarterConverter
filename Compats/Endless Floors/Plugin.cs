using BaldiEndless;
using BepInEx;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.Reflection;
using MTM101BaldAPI.Registers;
using System;
using UnityEngine;

namespace BaldiQuarterConverter_Endless
{
    [BepInPlugin("alexbw145.baldiplus.quarterconverter_endless", "ATM Arcade Mode Compat", "1.0.0")]
    [BepInDependency("alexbw145.baldiplus.quarterconverter")]
    [BepInDependency("mtm101.rulerp.baldiplus.endlessfloors")]
    [BepInProcess("BALDI.exe")]
    public class Plugin : BaseUnityPlugin
    {
        public static GenericHallBuilder atmBuilder_ENDLESS { get;  set; }
        private void Awake()
        {
            Harmony harmony = new Harmony("alexbw145.baldiplus.quarterconverter_endless");
            harmony.PatchAllConditionals();

            LoadingEvents.RegisterOnAssetsLoaded(Info, () =>
            {
                atmBuilder_ENDLESS = Instantiate(ObjectBuilderMetaStorage.Instance.Get(Obstacle.Null, "ATM_Builder").value) as GenericHallBuilder;
                atmBuilder_ENDLESS.name = "ATMHallBuilder_Endless";
                atmBuilder_ENDLESS.gameObject.ConvertToPrefab(true);

                // It's always forced because it's an ATM, get it?
                EndlessFloorsPlugin.AddGeneratorAction(Info, (data) =>
                {
                    data.forcedObjectBuilders.Add(atmBuilder_ENDLESS);
                });
            },true);
        }

        // Refreshes every next floor
        public static void RefreshATM()
        {
            ObjectPlacer objectPlacer = (ObjectPlacer)atmBuilder_ENDLESS.ReflectionGetVariable("objectPlacer");
            objectPlacer.ReflectionSetVariable("min", Math.Max(1, Mathf.RoundToInt(EndlessFloorsPlugin.currentSceneObject.levelNo / 6)));
            objectPlacer.ReflectionSetVariable("max", Math.Max(3, Mathf.RoundToInt(EndlessFloorsPlugin.currentSceneObject.levelNo / 4)));
        }
    }

    [HarmonyPatch(typeof(LevelGenerator), "StartGenerate")]
    class WhenTheATMIsUnbalanced
    {
        static void Prefix()
        {
            Plugin.RefreshATM();
        }
    }
}
