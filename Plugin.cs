using BepInEx;
using BepInEx.Bootstrap;
using Dummiesman;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.Reflection;
using MTM101BaldAPI.Registers;
using MTM101BaldAPI.SaveSystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace BaldiQuarterConverter
{
    public static class PluginInfo
    {
        public const string PLUGIN_GUID = "alexbw145.baldiplus.quarterconverter";
        public const string PLUGIN_NAME = "BaldiQuarterConverter";
        public const string PLUGIN_VERSION = "1.0.0";
    }

    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency("mtm101.rulerp.bbplus.baldidevapi")]
    [BepInProcess("BALDI.exe")]
    public class Plugin : BaseUnityPlugin
    {
        public static List<ItemObject> listsOfQuarters = new List<ItemObject>();
        private void Awake()
        {
            Harmony harmony = new Harmony(PluginInfo.PLUGIN_GUID);
            harmony.PatchAllConditionals();

            // Pre-load
            LoadingEvents.RegisterOnAssetsLoaded(() =>
            {
                listsOfQuarters.Add(ItemMetaStorage.Instance.FindByEnum(Items.Quarter).value);
                // Before uhmm, yeah I guess we need to add in some assets!
                Material atmMaterial = Instantiate(Resources.FindObjectsOfTypeAll<Material>().ToList().Find(x => x.name == "Bookshelf"));
                atmMaterial.name = "ATM_Material";
                atmMaterial.SetMainTexture(AssetLoader.TextureFromMod(this, "Machine.png"));
                atmMaterial.SetTexture("_LightGuide", AssetLoader.TextureFromMod(this, "Machine_LightGuide.png"));
                Material monitorMaterial = Instantiate(atmMaterial); // We need another??
                monitorMaterial.name = "Monitor_Material";
                monitorMaterial.SetFloat("_Offset", -1f); // Make the item render still visible with this!
                // Uh, lookie here!
                OBJLoader objImport = new OBJLoader(); // This ain't a MonoScript but we're okay!
                GameObject atmTemp = objImport.Load(Path.Combine(AssetLoader.GetModPath(this), "atm.obj"), Path.Combine(AssetLoader.GetModPath(this), "atm.mtl")); // .mtl file is required bcus of how materials were assigned to...

                // Now for the prefab itself!
                GenericHallBuilder atmBuilder = new GameObject("ATMHallBuilder", typeof(GenericHallBuilder)).GetComponent<GenericHallBuilder>();
                atmBuilder.obstacle = Obstacle.Null;
                ObjectPlacer wha = new ObjectPlacer()
                {
                    eligibleShapes = [
                        TileShape.Closed,
                        TileShape.Single,
                        TileShape.Straight,
                        TileShape.Corner,
                        TileShape.End,
                        ],
                    coverage = CellCoverage.North | CellCoverage.Down,
                };
                GameObject bsodaClone = Instantiate(Resources.FindObjectsOfTypeAll<SodaMachine>().ToList().Find(x => x.name == "SodaMachine")).gameObject;
                Destroy(bsodaClone.GetComponent<SodaMachine>());
                bsodaClone.name = "ATM";
                bsodaClone.GetComponent<MeshFilter>().mesh = atmTemp.GetComponentInChildren<MeshFilter>().mesh; // Why is it not on the parent??
                bsodaClone.GetComponent<MeshRenderer>().materials = [atmMaterial, monitorMaterial];
                // Now to the component!
                ATM yesbro = bsodaClone.AddComponent<ATM>();
                yesbro.render = new GameObject("OutputSprite", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
                yesbro.render.transform.SetParent(bsodaClone.transform, false);
                yesbro.render.material = Resources.FindObjectsOfTypeAll<Material>().ToList().Find(x => x.name == "Lit_SpriteStandard_NoBillboard");
                yesbro.render.transform.position = new Vector3(0f, 5.2f, 4.25f);
                wha.ReflectionSetVariable("prefab", bsodaClone);
                wha.ReflectionSetVariable("max", 3);
                wha.ReflectionSetVariable("useWallDir", true);
                atmBuilder.ReflectionSetVariable("objectPlacer", wha);

                DontDestroyOnLoad(bsodaClone);
                DontDestroyOnLoad(atmBuilder.gameObject);
                yesbro.transform.position = Vector3.up * float.MaxValue; // Most likely do this if you have a shitton of prefabs.
                ObjectBuilderMetaStorage.Instance.Add(new ObjectBuilderMeta(Info, atmBuilder), "ATM_Builder");

                // I'm done with this...
                Destroy(atmTemp);
            }, false);
            // Post-load
            LoadingEvents.RegisterOnAssetsLoaded(() =>
            {
                if (Chainloader.PluginInfos.ContainsKey("alexbw145.baldiplus.bcarnellchars")) // Adding an example, PLEASE MAKE A COMPAT OR ELSE I'LL ADD IT HERE!
                    listsOfQuarters.Add(ItemMetaStorage.Instance.FindByEnum(EnumExtensions.GetFromExtendedName<Items>("ProfitCard")).value);
            }, true);

            GeneratorManagement.Register(this, GenerationModType.Addend, (name, num, ld) =>
            {
                ld.forcedSpecialHallBuilders = ld.forcedSpecialHallBuilders.AddToArray(ObjectBuilderMetaStorage.Instance.Get(Obstacle.Null, "ATM_Builder").value);
            });

            ModdedSaveGame.AddSaveHandler(Info); // I hate it when the same ol' mistakes happen!
        }
    }
}
