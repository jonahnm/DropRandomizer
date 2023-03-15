using System;
using System.Collections;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UWE;
using SMLHelper.V2.Json.Attributes;
using SMLHelper.V2.Json;
using SMLHelper.V2.Handlers;
using UnityEngine;
using static TechStringCache;
using SMLHelper.V2.Options.Attributes;
using SMLHelper.V2.Options;
using System.Linq;

namespace DropRandomizer
{
    [BepInPlugin("DropRrandomizer", "Drop Randomizer", "0.0.1")]
    public class DropRandomizer : BaseUnityPlugin
    {
        public static readonly Harmony harmony = new Harmony("droprandomizer");
        public static ManualLogSource logger;
        public static Dictionary<TechType, TechType> currentrandompool = new();
        public static List<TechType> unusedTT = new()
        {
            TechType.PrecursorIonCrystalMatrix,
            TechType.PrecursorKey_Red,
            TechType.PrecursorKey_White,
            TechType.CurrentGenerator,
            TechType.KooshZoneEgg,
            TechType.GrandReefsEgg,
            TechType.GrassyPlateausEgg,
            TechType.KelpForestEgg,
            TechType.LavaZoneEgg,
            TechType.MushroomForestEgg,
            TechType.SafeShallowsEgg,
            TechType.TwistyBridgesEgg,
            TechType.ReefbackEgg,
            TechType.JumperEgg,
            TechType.LithiumIonBattery,
            TechType.Nanowires,
            TechType.Thermometer,
            TechType.AminoAcids,
            TechType.BatteryAcidOld,
            TechType.CarbonOld,
            TechType.CompostCreepvine,
            TechType.EmeryOld,
            TechType.EthanolOld,
            TechType.EthyleneOld,
            TechType.FlintOld,
            TechType.HydrogenOld,
            TechType.Lodestone,
            TechType.Magnesium,
            TechType.MembraneOld,
            TechType.MercuryOre,
            TechType.SandLoot,
            TechType.Uranium,
            TechType.HullReinforcementModule,
            TechType.HullReinforcementModule2,
            TechType.HullReinforcementModule3,
            TechType.BasaltChunk,
            TechType.Accumulator,
            TechType.Bioreactor,
            TechType.Centrifuge,
            TechType.FragmentAnalyzer,
            TechType.NuclearReactor,
            TechType.ObservatoryOld,
            TechType.SpecimenAnalyzer,
            TechType.Drill,
            TechType.DiamondBlade,
            TechType.PowerGlide,
            TechType.Terraformer,
            TechType.Transfuser,
            TechType.SeamothReinforcementModule,
            TechType.Signal,
            TechType.Fragment,
            TechType.FragmentAnalyzer,
            TechType.FragmentAnalyzerBlueprintOld,
            TechType.TerraformerFragment,
            TechType.TransfuserFragment,
            TechType.OrangePetalsPlantSeed
        };
        public static List<TechType> vehicleTTs = new()
        {
            TechType.Cyclops,
            TechType.Seamoth,
            TechType.Exosuit,
            TechType.RocketBase
        };
        public static List<TechType> fragmentTTs = new();
        public static Dictionary<TechType, TechType> vehicleRandoms = new();
        public static Dictionary<TechType, TechType> fragmentRandoms = new();
        public static bool isfilled = false;
        public static DropRandomizerData saveData;
        public static bool hasalreadyloadedin = false;
        public static int addedsofar = 0;
        public static List<TechType> notallowedunlockableTT = new()
        {
            TechType.Reginald,
            TechType.Spadefish,
            TechType.Boomerang,
            TechType.Eyeye,
            TechType.Oculus,
            TechType.Hoopfish,
            TechType.Spinefish,
            TechType.CreepvinePiece,
            TechType.CreepvineSeedCluster,
            TechType.StalkerTooth,
            TechType.WhiteMushroom,
            TechType.FiberMesh,
            TechType.Lubricant,
            TechType.LavaBoomerang,
            TechType.LavaEyeye,
            TechType.JellyPlant,
            TechType.HydrochloricAcid,
            TechType.BloodOil,
            TechType.Benzene,
            TechType.AramidFibers,
            TechType.PowerCell,
            TechType.AdvancedWiringKit,
            TechType.PlasteelIngot,
            TechType.HoleFish,
            TechType.Peeper,
            TechType.GarryFish,
            TechType.Hoverfish,
            TechType.Transfuser,
            TechType.Terraformer
        };
        public void Awake()
        {
            harmony.PatchAll();
            logger = Logger;
            saveData = SaveDataHandler.Main.RegisterSaveDataCache<DropRandomizerData>();
            
            OptionsPanelHandler.Main.RegisterModOptions<RandomizerConfig>();
            saveData.OnFinishedLoading += (object sender, JsonFileEventArgs e) =>
            {
                if (!hasalreadyloadedin)
                {
                    hasalreadyloadedin = true;
                    var data = e.Instance as DropRandomizerData;
                    if (data.hassavedinsave)
                    {
                        currentrandompool = data.randompool;
                        vehicleRandoms = data.randomvehicles;
                        fragmentRandoms = data.randomfrags;
                        isfilled = true;
                    }
                    else
                    {
                        CoroutineHost.StartCoroutine(fillrandompool());
                    }
                }
            };
                saveData.OnStartedSaving += (object sender, JsonFileEventArgs e) =>
                {
                    var data = e.Instance as DropRandomizerData;
                    data.randompool = currentrandompool;
                    data.randomvehicles = vehicleRandoms;
                    data.randomfrags = fragmentRandoms;
                    data.hassavedinsave = true;
                };
            }
        internal static System.Random random = new System.Random();
        internal IEnumerator fillrandompool()
        {
            for (var i = 0; i < Enum.GetValues(typeof(TechType)).Length; i++)
            {
                if (currentrandompool.Count == 221)
                    break;
                TechType TT = 0;
                var vals = Enum.GetValues(typeof(TechType));
                lock (random)
                {
                    TT = (TechType)vals.GetValue(random.Next(vals.Length));
                    while (String.IsNullOrEmpty(CraftData.GetClassIdForTechType(TT)) || unusedTT.Contains(TT) || TT.ToString().ToLower().Contains("fragment") || currentrandompool.ContainsValue(TT) || TT.ToString().ToLower().Contains("cured") || TT.ToString().ToLower().Contains("cooked"))
                    {
                        TT = (TechType)vals.GetValue(random.Next(vals.Length));
                    }
                Loop:
                    var TTPrefab = CraftData.GetPrefabForTechTypeAsync(TT);
                    yield return TTPrefab;
                    if (!TTPrefab.GetResult().TryGetComponent<Pickupable>(out _))
                    {
                        TT = (TechType)vals.GetValue(random.Next(vals.Length));
                        while (String.IsNullOrEmpty(CraftData.GetClassIdForTechType(TT)) || unusedTT.Contains(TT) || TT.ToString().ToLower().Contains("fragment") || currentrandompool.ContainsValue(TT) || TT.ToString().ToLower().Contains("cured") || TT.ToString().ToLower().Contains("cooked"))
                        {
                            TT = (TechType)vals.GetValue(random.Next(vals.Length));
                        }
                        goto Loop;
                    }
                }
                if (TT == TechType.None)
                    continue;
                lock (random)
                {
                    var key = (TechType)vals.GetValue(random.Next(vals.Length));
                    while (currentrandompool.ContainsKey(key) || String.IsNullOrEmpty(CraftData.GetClassIdForTechType(key)) || key == TT || unusedTT.Contains(key) || key.ToString().ToLower().Contains("fragment") || key.ToString().ToLower().Contains("cured") || key.ToString().ToLower().Contains("cooked"))
                    {
                        key = (TechType)vals.GetValue(random.Next(vals.Length));
                    }
                Loop2:
                    var keyPrefab = CraftData.GetPrefabForTechTypeAsync(key);
                    yield return keyPrefab;
                    if (!keyPrefab.GetResult().TryGetComponent<Pickupable>(out _))
                    {
                        key = (TechType)vals.GetValue(random.Next(vals.Length));
                        while (currentrandompool.ContainsKey(key) || String.IsNullOrEmpty(CraftData.GetClassIdForTechType(key)) || key == TT || unusedTT.Contains(key) || key.ToString().ToLower().Contains("fragment") || key.ToString().ToLower().Contains("cured") || key.ToString().ToLower().Contains("cooked"))
                        {
                            key = (TechType)vals.GetValue(random.Next(vals.Length));
                        }
                        goto Loop2;
                    }
                    if (!currentrandompool.ContainsKey(key))
                        addedsofar++;
                        currentrandompool.Add(key, TT);
                }
            }
            for (var i = 0; i < vehicleTTs.Count; i++)
            {
                TechType TT = 0;
                lock (random)
                {
                    TT = vehicleTTs[random.Next(vehicleTTs.Count)];
                    while (vehicleRandoms.ContainsValue(TT))
                    {
                        TT = vehicleTTs[random.Next(vehicleTTs.Count)];
                    }
                }
                lock (random)
                {
                    var key = vehicleTTs[random.Next(vehicleTTs.Count)];
                    while (vehicleRandoms.ContainsKey(key) || key == TT)
                    {
                        key = vehicleTTs[random.Next(vehicleTTs.Count)];
                    }
                    vehicleRandoms.Add(key, TT);
                }
            }
            for (var i = 0; i < fragmentTTs.Count; i++)
            {
                TechType TT = 0;
                var TTs = Enum.GetValues(typeof(TechType));
                lock (random)
                {
                    TT = fragmentTTs[random.Next(fragmentTTs.Count)];
                    while (fragmentRandoms.ContainsValue(TT))
                    {
                        TT = fragmentTTs[random.Next(fragmentTTs.Count)];
                    }
                }
                lock (random)
                {
                    var key = fragmentTTs[random.Next(fragmentTTs.Count)];
                    while (fragmentRandoms.ContainsKey(key) || key == TT)
                    {
                        key = fragmentTTs[random.Next(fragmentTTs.Count)];
                    }
                    fragmentRandoms.Add(key, TT);
                }
        }
            isfilled = true;
        }
        public static void WaitCoroutine(IEnumerator func)
        {
            while (func.MoveNext())
            {
                if (func.Current != null)
                {
                    IEnumerator num;
                    try
                    {
                        num = (IEnumerator)func.Current;
                    }
                    catch (InvalidCastException)
                    {
                        if (func.Current.GetType() == typeof(WaitForSeconds))
                            Debug.LogWarning("Skipped call to WaitForSeconds. Use WaitForSecondsRealtime instead.");
                        return;  // Skip WaitForSeconds, WaitForEndOfFrame and WaitForFixedUpdate
                    }
                    WaitCoroutine(num);
                }
            }
        }
    }
        [FileName("droprandomizer")]
        public class DropRandomizerData : SaveDataCache
        {
            public bool hassavedinsave = false;
            public Dictionary<TechType, TechType> randompool;
        public Dictionary<TechType, TechType> randomvehicles;
        public Dictionary<TechType, TechType> randomfrags;
        }
    internal class AlreadyRandom : MonoBehaviour
    {

    }
    internal class OutcropRandom : MonoBehaviour
    {

    }
    [Menu("Drop Randomizer")]
    public class RandomizerConfig : ConfigFile
    {
        public static bool easyMode;
        [Toggle("Easy Mode"),OnChange(nameof(EasyModeChange))]
        public bool easyMode_;
        public void EasyModeChange(ToggleChangedEventArgs e)
        {
            easyMode = e.Value;
        }
    }
}
