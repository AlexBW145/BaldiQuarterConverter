using BepInEx;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.Registers;
using MTM101BaldAPI.SaveSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.TestTools;

namespace BaldiQuarterConverter
{
    public static class PluginInfo
    {
        public const string PLUGIN_GUID = "alexbw145.baldiplus.quarterconverter";
        public const string PLUGIN_NAME = "BaldiQuarterConverter";
        public const string PLUGIN_VERSION = "1.1.1";
    }

    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency("mtm101.rulerp.bbplus.baldidevapi")]
    [BepInProcess("BALDI.exe")]
    public class Plugin : BaseUnityPlugin
    {
        public static List<WeightedSelection<ItemObject>> listsOfQuarters = new List<WeightedSelection<ItemObject>>();
        //public static Structure_EnvironmentObjectPlacer atmBuild { get; private set; }
        private GameObject atm;
        internal static StructureWithParameters param;
        private void Awake()
        {
            Harmony harmony = new Harmony(PluginInfo.PLUGIN_GUID);
            harmony.PatchAllConditionals();

            // Pre-load
            LoadingEvents.RegisterOnAssetsLoaded(Info, PreLoad, false);
            /*// Post-load Bad
            LoadingEvents.RegisterOnAssetsLoaded(Info, () =>
            {
                if (Chainloader.PluginInfos.ContainsKey("alexbw145.baldiplus.bcarnellchars")) // Adding an example, PLEASE MAKE A COMPAT OR ELSE I'LL ADD IT HERE!
                    listsOfQuarters.Add(new() { selection = ItemMetaStorage.Instance.FindByEnum(EnumExtensions.GetFromExtendedName<Items>("ProfitCard")).value , weight = 100 });
                if (Chainloader.PluginInfos.ContainsKey("txv.bbplus.testvariants"))
                    listsOfQuarters.Add(new() { selection = ItemMetaStorage.Instance.FindByEnum(EnumExtensions.GetFromExtendedName<Items>("LuckiestCoin")).value , weight = 10});
            }, true);*/

            ModdedSaveGame.AddSaveHandler(Info); // I hate it when the same ol' mistakes happen!
        }

        void PreLoad()
        {
            foreach (var item in ItemMetaStorage.Instance.FindAllWithTags(true, "currency"))
                listsOfQuarters.Add(new() { selection = item.value, weight = 100 });
            // Before uhmm, yeah I guess we need to add in some assets!
            Material atmMaterial = Instantiate(Resources.FindObjectsOfTypeAll<Material>().ToList().Find(x => x.name == "Bookshelf"));
            atmMaterial.name = "ATM_Material";
            atmMaterial.SetMainTexture(AssetLoader.TextureFromMod(this, "Machine.png"));
            atmMaterial.SetTexture("_LightGuide", AssetLoader.TextureFromMod(this, "Machine_LightGuide.png"));
            Material monitorMaterial = Instantiate(atmMaterial); // We need another??
            monitorMaterial.name = "Monitor_Material";
            monitorMaterial.SetFloat("_Offset", -1f); // Make the item render still visible with this!
                                                      // Uh, lookie here!
            GameObject atmTemp = AssetLoader.ModelFromMod(this, "atm.obj");

            // Now for the prefab itself!
            //Structure_EnvironmentObjectPlacer atmBuilder = new GameObject("ATMHallBuilder", typeof(Structure_EnvironmentObjectPlacer)).GetComponent<Structure_EnvironmentObjectPlacer>();
            /*atmBuilder.obstacle = Obstacle.Null;
            ObjectPlacer wha = new ObjectPlacer()
            {
                eligibleShapes = TileShapeMask.Closed
                | TileShapeMask.Single
                | TileShapeMask.Straight
                | TileShapeMask.Corner
                | TileShapeMask.End,
                coverage = CellCoverage.North | CellCoverage.Down,
            };*/
            GameObject bsodaClone = Instantiate(Resources.FindObjectsOfTypeAll<SodaMachine>().ToList().Find(x => x.name == "SodaMachine")).gameObject;
            Destroy(bsodaClone.GetComponent<SodaMachine>());
            bsodaClone.name = "ATM";
            bsodaClone.GetComponent<MeshFilter>().mesh = atmTemp.GetComponentInChildren<MeshFilter>().mesh; // Why is it not on the parent??
            bsodaClone.GetComponent<MeshRenderer>().materials = [atmMaterial, monitorMaterial];
            // Now to the component!
            ATM yesbro = bsodaClone.AddComponent<ATM>();
            yesbro.render = new GameObject("OutputSprite", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            yesbro.render.transform.SetParent(bsodaClone.transform, false);
            yesbro.render.material = Resources.FindObjectsOfTypeAll<Material>().ToList().Last(x => x.name == "SpriteStandard_NoBillboard");
            yesbro.render.transform.position = new Vector3(0f, 5.2f, 4.25f);
            /*wha.ReflectionSetVariable("prefab", bsodaClone);
            wha.ReflectionSetVariable("max", 3);
            wha.ReflectionSetVariable("useWallDir", true);
            atmBuilder.ReflectionSetVariable("objectPlacer", wha);*/
            yesbro.tileShapeMask = TileShapeMask.Closed
                | TileShapeMask.Single
                | TileShapeMask.Straight
                | TileShapeMask.Corner
                | TileShapeMask.End;
            yesbro.coverage = CellCoverage.North | CellCoverage.Down;
            yesbro.gameObject.ConvertToPrefab(true);
            atm = yesbro.gameObject;
            //atmBuilder.gameObject.ConvertToPrefab(true);
            //atmBuild = atmBuilder;

            // I'm done with this...
            Destroy(atmTemp);

            param = new StructureWithParameters()
            {
                parameters = new StructureParameters()
                {
                    chance = [9f],
                    minMax = [RefreshATM()],
                    prefab = [new()
                        {
                            selection = atm,
                            weight = 99
                        }]
                },
                prefab = Resources.FindObjectsOfTypeAll<Structure_EnvironmentObjectPlacer>().Last(x => x.name == "Structure_EnvironmentObjectBuilder_Individual")
            };
            GeneratorManagement.Register(this, GenerationModType.Addend, (name, num, ld) =>
            {
                ld.levelObject.forcedStructures = ld.levelObject.forcedStructures.AddToArray(param);
            });
        }

        // Refreshes every next floor
        public static IntVector2 RefreshATM()
        {
            if (BaseGameManager.Instance?.levelObject == null)
                return new(1,3);
            /*ObjectPlacer objectPlacer = (ObjectPlacer)atmBuild.ReflectionGetVariable("objectPlacer");
            objectPlacer.ReflectionSetVariable("min", Math.Max(1, Mathf.RoundToInt(Mathf.Min(BaseGameManager.Instance.levelObject.maxSize.x, BaseGameManager.Instance.levelObject.maxSize.z)) / Mathf.Max(BaseGameManager.Instance.levelObject.minSize.x, BaseGameManager.Instance.levelObject.minSize.z) * 4));
            objectPlacer.ReflectionSetVariable("max", Math.Max(3, Mathf.RoundToInt(Mathf.Min(BaseGameManager.Instance.levelObject.maxSize.x, BaseGameManager.Instance.levelObject.maxSize.z)) / Mathf.Max(BaseGameManager.Instance.levelObject.minSize.x, BaseGameManager.Instance.levelObject.minSize.z) * 2));*/
            int maxie = Mathf.Max(3, Mathf.RoundToInt(Mathf.Min(BaseGameManager.Instance.levelObject.maxSize.x, BaseGameManager.Instance.levelObject.maxSize.z)) / Mathf.Max(BaseGameManager.Instance.levelObject.minSize.x, BaseGameManager.Instance.levelObject.minSize.z) * 8);
            return new(
                Mathf.Min(Mathf.Max(1, Mathf.RoundToInt(Mathf.Min(BaseGameManager.Instance.levelObject.maxSize.x, BaseGameManager.Instance.levelObject.maxSize.z)) / Mathf.Max(BaseGameManager.Instance.levelObject.minSize.x, BaseGameManager.Instance.levelObject.minSize.z) * 4), maxie),
                maxie
                );
        }
    }

    [HarmonyPatch(typeof(LevelGenerator), "StartGenerate")]
    class WhenTheATMIsUnbalanced
    {
        static void Prefix()
        {
            Plugin.param.parameters.minMax = [Plugin.RefreshATM()];
        }
    }
}
