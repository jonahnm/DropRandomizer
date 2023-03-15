using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using rail;
using SMLHelper.V2.Assets;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UWE;

namespace DropRandomizer
{
    internal class Patches
    {
        [HarmonyPatch(typeof(SpawnOnKill))]
        public static class SpawnOnKill_Patch
        {
            [HarmonyPatch(nameof(SpawnOnKill.OnKill))]
            [HarmonyTranspiler]
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var list = new List<CodeInstruction>(instructions);
                for (var i = 0; i < list.Count; i++)
                {
                    if (list[i].opcode == OpCodes.Ldfld && list[i].operand == typeof(SpawnOnKill).GetField(nameof(SpawnOnKill.prefabToSpawn)))
                    {
                        list[i] = new CodeInstruction(OpCodes.Call, typeof(SpawnOnKill_Patch).GetMethod(nameof(SpawnOnKill_Patch.prefabtospawnpatch)));
                    }
                }
                return list.AsEnumerable();
            }
            public static GameObject prefabtospawnpatch(SpawnOnKill instance)
            {
                var TT = CraftData.GetTechType(instance.prefabToSpawn);
                var go = CraftData.GetPrefabForTechTypeAsync(DropRandomizer.currentrandompool[TT]);
                DropRandomizer.WaitCoroutine(go);
                var returngo = go.GetResult();
                if (returngo.TryGetComponent<Pickupable>(out _))
                {
                    returngo.EnsureComponent<AlreadyRandom>();
                    returngo.EnsureComponent<OutcropRandom>();
                    return returngo;
                }
                else
                    return instance.prefabToSpawn;
            }
        }
        [HarmonyPatch(typeof(BreakableResource))]
        public static class BreakableResource_Patch
        {
            [HarmonyPatch(nameof(BreakableResource.SpawnResourceFromPrefab),new Type[] {typeof(AssetReferenceGameObject),typeof(Vector3),typeof(Vector3)})]
            [HarmonyPrefix]
            public static bool Prefix(AssetReferenceGameObject breakPrefab,Vector3 position,Vector3 up)
            {
                CoroutineHost.StartCoroutine(dostuff(breakPrefab, position, up));
                return false;
            }
            public static IEnumerator dostuff(AssetReferenceGameObject breakPrefab, Vector3 position, Vector3 up)
            {
                var prefab_ = Addressables.LoadAssetAsync<GameObject>(breakPrefab.RuntimeKey);
                yield return prefab_;
                var prefab = prefab_.Result;
                var TT = CraftData.GetTechType(prefab);
                var newTT = DropRandomizer.currentrandompool[TT];
                DropRandomizer.logger.LogInfo(newTT);
                var newprefab_ = CraftData.GetPrefabForTechTypeAsync(newTT);
                yield return newprefab_;
                var newprefab = newprefab_.GetResult();
                var thing = GameObject.Instantiate(newprefab, position, default);
                thing.EnsureComponent<AlreadyRandom>();
                thing.EnsureComponent<OutcropRandom>();
            }
        }
        [HarmonyPatch(typeof(CraftData))]
        public static class CraftData_Patch
        {
            [HarmonyPatch(nameof(CraftData.GetHarvestOutputData))]
            [HarmonyPostfix]
            public static void Postfix(ref TechType __result)
            {
                __result = DropRandomizer.currentrandompool[__result];
            }
        }
        [HarmonyPatch(typeof(Inventory))]
        public static class Inventory_Patch
        {
            [HarmonyPatch(nameof(Inventory.Pickup))]
            [HarmonyPrefix]
           public static bool Prefix(Pickupable pickupable, ref bool __result)
            {
                var trace = new StackTrace();
                if (!pickupable.gameObject.GetComponent<OutcropRandom>() && RandomizerConfig.easyMode && !trace.GetFrame(2).GetMethod().Name.ToLower().Contains("movenext") && !pickupable.gameObject.GetComponent<CreatureEgg>() && DropRandomizer.currentrandompool.ContainsKey(pickupable.GetTechType()))
                {
                    var go = new TaskResult<GameObject>();
                    var asyncdata = CraftData.AddToInventoryAsync(DropRandomizer.currentrandompool[pickupable.GetTechType()],result: go);
                    DropRandomizer.WaitCoroutine(asyncdata);
                    if (go.value)
                    {
                        go.value.EnsureComponent<AlreadyRandom>();
                    }
                    GameObject.Destroy(pickupable.gameObject);
                    __result = true;
                    return false;
                }else if(!pickupable.gameObject.GetComponent<OutcropRandom>() && RandomizerConfig.easyMode && !trace.GetFrame(2).GetMethod().Name.ToLower().Contains("movenext") && pickupable.gameObject.GetComponent<CreatureEgg>())
                {
                    var go = new TaskResult<GameObject>();
                    var asyncdata = CraftData.AddToInventoryAsync(DropRandomizer.currentrandompool[CraftData.GetTechType(pickupable.gameObject)],result: go);
                    DropRandomizer.WaitCoroutine(asyncdata);
                    if (go.value)
                    {
                        go.value.EnsureComponent<AlreadyRandom>();
                    }
                    GameObject.Destroy(pickupable.gameObject);
                    __result = true;
                    return false;
                }else if(!RandomizerConfig.easyMode && !trace.GetFrame(2).GetMethod().Name.ToLower().Contains("movenext") && !pickupable.gameObject.GetComponent<CreatureEgg>() && DropRandomizer.currentrandompool.ContainsKey(pickupable.GetTechType()) && !pickupable.gameObject.GetComponent<AlreadyRandom>())
                {
                    var go = new TaskResult<GameObject>();
                    var asyncdata = CraftData.AddToInventoryAsync(DropRandomizer.currentrandompool[pickupable.GetTechType()], result: go);
                    DropRandomizer.WaitCoroutine(asyncdata);
                    if (go.value)
                    {
                        go.value.EnsureComponent<AlreadyRandom>();
                    }
                    GameObject.Destroy(pickupable.gameObject);
                    __result = true;
                    return false;
                }
                else if (!RandomizerConfig.easyMode && !trace.GetFrame(2).GetMethod().Name.ToLower().Contains("movenext") && pickupable.gameObject.GetComponent<CreatureEgg>() && !pickupable.gameObject.GetComponent<AlreadyRandom>())
                {
                    var go = new TaskResult<GameObject>();
                    var asyncdata = CraftData.AddToInventoryAsync(DropRandomizer.currentrandompool[CraftData.GetTechType(pickupable.gameObject)], result: go);
                    DropRandomizer.WaitCoroutine(asyncdata);
                    if (go.value)
                    {
                        go.value.EnsureComponent<AlreadyRandom>();
                    }
                    GameObject.Destroy(pickupable.gameObject);
                    __result = true;
                    return false;
                }
                else if (pickupable.gameObject.GetComponent<OutcropRandom>())
                {
                    GameObject.Destroy(pickupable.gameObject.GetComponent<OutcropRandom>());
                }
                    return true;
            }
        }
        [HarmonyPatch(typeof(PAXTerrainController))]
        public static class PAXTerrainControllerPatch {
            [HarmonyPatch(nameof(PAXTerrainController.LoadAsync))]
            [HarmonyPostfix]
            public static IEnumerator Postfix(IEnumerator result)
            {
                var n = 0;
                while(result.MoveNext())
                {
                    var element = result.Current;
                    if(n == 7)
                    {
                        DropRandomizer.fragmentTTs = KnownTech.GetAllUnlockables(true).ToList();
                        DropRandomizer.fragmentTTs.RemoveAll(TT => DropRandomizer.notallowedunlockableTT.Contains(TT) || TT == TechType.None);
                        Console.WriteLine("Waiting to fill.");
                        DropRandomizer.saveData.Load();
                        string prevmsg = null;
                        while(!DropRandomizer.isfilled)
                        {
                            if(!String.IsNullOrEmpty(prevmsg))
                            {
                                var prev = ErrorMessage.main.GetExistingMessage(prevmsg);
                                ErrorMessage.main.offsetY += prev.entry.preferredHeight;
                                ErrorMessage.main.messages.Remove(prev);
                                ErrorMessage.main.ReleaseEntry(prev.entry);
                            }
                            var percent = ((double)DropRandomizer.addedsofar / (double)221) * (double)100;
                            prevmsg = $"{System.Math.Round(percent)}% done filling the pool!";
                            ErrorMessage.AddMessage(prevmsg);
                            yield return null;
                        }
                    }
                    yield return element;
                    n++;
                }
            }
        }
        [HarmonyPatch(typeof(ConstructorInput))]
       public static class ConstructorInputPatch
        {
            [HarmonyPatch(nameof(ConstructorInput.Craft))]
            [HarmonyPrefix]
            public static void Prefix(ref TechType techType)
            {
                techType = DropRandomizer.vehicleRandoms[techType];
            }
        }
        [HarmonyPatch(typeof(LootSpawner))]
        public static class LootSpawnerPatch
        {
            [HarmonyPatch(nameof(LootSpawner.GetEscapePodStorageTechTypes))]
            [HarmonyPostfix]
            public static void Postfix(ref TechType[] __result)
            {
                var list = __result.Append(TechType.Knife);
                __result = list.ToArray();
            }
        }
        [HarmonyPatch(typeof(PickPrefab))]
        public static class PickPrefabPatch
        {
            [HarmonyPatch(nameof(PickPrefab.Start))]
            [HarmonyPostfix]
            public static void Postfix(PickPrefab __instance)
            {
                __instance.pickTech = DropRandomizer.currentrandompool[__instance.pickTech];
            }
        }
        [HarmonyPatch(typeof(KnownTech))]
        public static class KnownTechPatch
        {
            [HarmonyPatch(nameof(KnownTech.Add))]
            [HarmonyPrefix]
            public static void Prefix(ref TechType techType)
            {
                DropRandomizer.logger.LogInfo(techType);
                if(DropRandomizer.fragmentTTs.Contains(techType))
                {
                    techType = DropRandomizer.fragmentRandoms[techType];
                }
            }
        }
    }
    
        }
