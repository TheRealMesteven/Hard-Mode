using System.Collections.Generic;
using System.IO;
using System.Reflection;
using HarmonyLib;
using System.Reflection.Emit;
using PulsarModLoader.Patches;
using UnityEngine;
using System.Collections;
using CodeStage.AntiCheat.ObscuredTypes;

namespace Hard_Mode
{
    class Custom_Bounty_Hunters
    {
        [HarmonyPatch(typeof(PLEncounterManager), "Start")]
        class HunterAdder //Adds the extra hunters from the HunterCodes.txt for the random list when the game starts 
        {
            static void Postfix(ref List<PLEncounterManager.ShipLayout> ___PossibleHunters_LayoutData)
            {
                string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "HunterCodes.txt");
                if (!File.Exists(path))
                {
                    return;
                }
                using (StreamReader streamReader = new StreamReader(path)) //This reads the file
                {
                    while (!streamReader.EndOfStream)
                    {
                        string bountyhunter = streamReader.ReadLine();
                        if (bountyhunter.Length > 50)
                        {
                            PLEncounterManager.ShipLayout hunter = new PLEncounterManager.ShipLayout(bountyhunter);
                            if (hunter.ShipType == EShipType.E_INTREPID_SC) hunter.ShipType = EShipType.E_INTREPID;
                            if (hunter.ShipType == EShipType.E_ALCHEMIST) hunter.ShipType = EShipType.E_CARRIER;
                            ___PossibleHunters_LayoutData.Add(hunter);
                        }
                    }
                }
            }
        }
        public static bool updated = false;
        public static void UpdateHunterLevel() //This updates the combat level to the system we use
        {
            if (PLServer.GetCurrentSector() != null)
            {
                List<PLEncounterManager.ShipLayout> _PossibleHunters_LayoutData = PLEncounterManager.Instance.GetAllLayouts();
                foreach (PLEncounterManager.ShipLayout ship in _PossibleHunters_LayoutData)
                {
                    /*
                    List<PLShipComponent> allComponents = new List<PLShipComponent>();
                    PLHull hull = null;
                    PLShieldGenerator shield = null;
                    PLReactor reactor = null;
                    List<PLThruster> thrusters = new List<PLThruster>();
                    */
                    BinaryReader binaryReader = new BinaryReader(new MemoryStream(System.Convert.FromBase64String(ship.Data)));
                    binaryReader.ReadInt32();
                    binaryReader.ReadSingle();
                    while (binaryReader.BaseStream.Position < binaryReader.BaseStream.Length)
                    {
                        int inHash = binaryReader.ReadInt32();
                        PLShipComponent plshipComponent = PLWare.CreateFromHash(1, inHash) as PLShipComponent;
                        if (plshipComponent != null && (plshipComponent.ActualSlotType == ESlotType.E_COMP_AUTO_TURRET || plshipComponent.ActualSlotType == ESlotType.E_COMP_MAINTURRET || plshipComponent.ActualSlotType == ESlotType.E_COMP_TURRET))
                        {
                            PLTurret turret = plshipComponent as PLTurret;
                            CombatLevel.GetTurretCombatLevel(turret, ref ship.CL);
                            /*
                            allComponents.Add(plshipComponent);
                            switch (plshipComponent.ActualSlotType)
                            {
                                case ESlotType.E_COMP_REACTOR:
                                    reactor = plshipComponent as PLReactor;
                                    break;
                                case ESlotType.E_COMP_HULL:
                                    hull = plshipComponent as PLHull;
                                    break;
                                case ESlotType.E_COMP_SHLD:
                                    shield = plshipComponent as PLShieldGenerator;
                                    break;
                                case ESlotType.E_COMP_THRUSTER:
                                    thrusters.Add(plshipComponent as PLThruster);
                                    break;
                            }
                            */
                        }
                    }
                    /*
                    ship.CL = 0;
                    foreach (PLShipComponent plshipComponent in allComponents)
                    {
                        ship.CL += Mathf.Pow((float)plshipComponent.GetScaledMarketPrice(true), 0.8f) * 0.001f;
                        if (plshipComponent.ActualSlotType == ESlotType.E_COMP_MAINTURRET || plshipComponent.ActualSlotType == ESlotType.E_COMP_TURRET || plshipComponent.ActualSlotType == ESlotType.E_COMP_AUTO_TURRET)
                        {
                            PLTurret turret = plshipComponent as PLTurret;
                            ship.CL += turret.GetDPS() * 0.1f * (1f - Mathf.Clamp01(turret.HeatGeneratedOnFire * 2.2f / turret.FireDelay));
                            ship.CL += turret.TurretRange * 0.0005f;
                        }
                    }
                    if (hull != null)
                    {
                        ship.CL += hull.Max * hull.LevelMultiplier(0.2f, 1f) * 0.005f;
                        ship.CL += hull.Armor * hull.LevelMultiplier(0.15f, 1f) * 10f * (ship.ShipType == EShipType.E_DESTROYER ? 1.5f : 1);
                    }
                    if (shield != null)
                    {
                    }
                    PLPersistantShipInfo plpersistantShipInfo = new PLPersistantShipInfo(ship.ShipType, -1, PLServer.GetCurrentSector(), 0, false, false, false, -1, -1);
                    PulsarPluginLoader.Utilities.Logger.Info("Ship: ");
                    plpersistantShipInfo.CreateShipInstance(PLEncounterManager.Instance.GetCPEI());
                    plpersistantShipInfo.ShipInstance.MyStats.FormatToDataString(ship.Data);
                    plpersistantShipInfo.ShipInstance.MyStats.CalculateStats();
                    ship.CL = plpersistantShipInfo.ShipInstance.GetCombatLevel();
                    PhotonNetwork.Destroy(plpersistantShipInfo.ShipInstance.photonView);
                    */
                }
                updated = true;
            }
        }
        [HarmonyPatch(typeof(PLPersistantEncounterInstance), "PlayerEnter")]
        public class RelicHunterBalance //Allow to change the Min and Max values for the difference between you and the relic hunter
        {
            static public float MinCombatLevel = 1.2f;
            static public float MaxCombatLevel = 1.5f;
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                List<CodeInstruction> targetSequence = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(PLEncounterManager),"Instance")),
                new CodeInstruction(OpCodes.Ldc_R4,1.2f),
                new CodeInstruction(OpCodes.Ldc_R4,1.5f)
            };
                List<CodeInstruction> patchSequence = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(PLEncounterManager),"Instance")),
                new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(RelicHunterBalance), "MinCombatLevel")),
                new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(RelicHunterBalance), "MaxCombatLevel"))
            };
                return HarmonyHelpers.PatchBySequence(instructions, targetSequence, patchSequence, HarmonyHelpers.PatchMode.REPLACE, HarmonyHelpers.CheckMode.NONNULL, false);
            }
        }
        [HarmonyPatch(typeof(PLPersistantEncounterInstance), "SpawnRelevantRelicHunter")]
        class UpdateRelic //This Updates the combat levels for relic hunter
        {
            static void Prefix()
            {
                if (!updated) UpdateHunterLevel();
            }
        }
        [HarmonyPatch(typeof(PLServer), "SpawnHunter")]
        public class BountyHunterOverhaul // Completely rework the Bounty Hunter spawning mechanics
        {
            static public float MinCombatLevel = 1.2f;
            static public float MaxCombatLevel = 1.5f;
            private static FieldInfo m_ActiveBountyHunter_TypeID = AccessTools.Field(typeof(PLServer), "m_ActiveBountyHunter_TypeID");
            private static FieldInfo m_ActiveBountyHunter_LastCreateHunterUpdateTime = AccessTools.Field(typeof(PLServer), "m_ActiveBountyHunter_LastCreateHunterUpdateTime");
            private static FieldInfo m_ActiveBountyHunter_SectorID = AccessTools.Field(typeof(PLServer), "m_ActiveBountyHunter_SectorID");
            private static FieldInfo m_ActiveBountyHunter_SecondsSinceWarp = AccessTools.Field(typeof(PLServer), "m_ActiveBountyHunter_SecondsSinceWarp");
            private static ObscuredInt NegativeOne = -1;
            private static MethodInfo ControlledSEND_SetupNewHunter = AccessTools.Method(typeof(PLServer), "ControlledSEND_SetupNewHunter");
            static bool Prefix(PLServer __instance)
            {
                if (!updated) UpdateHunterLevel();
                __instance.BountyHuntersSpawned++;
                PLSectorInfo currentSector = PLServer.GetCurrentSector();
                int num = 0;
                foreach (PLPersistantShipInfo plpersistantShipInfo in __instance.AllPSIs)
                {
                    if (plpersistantShipInfo != null && plpersistantShipInfo.MyCurrentSector == currentSector && !plpersistantShipInfo.IsShipDestroyed && plpersistantShipInfo.ShipInstance != null && !plpersistantShipInfo.ShipInstance.IsShipInfoBase)
                    {
                        num++;
                    }
                }
                float lowerBound = MinCombatLevel;
                float higherBound = MaxCombatLevel;
                bool flag;
                if (__instance.BountyHunterInfo == null || __instance.BountyHunterInfo.IsShipDestroyed)
                {
                    flag = ((UnityEngine.Random.value < 0.5f || __instance.ChaosLevel > 5f) && PLEncounterManager.Instance.GetNumberOf_PossibleHunterLayout(lowerBound, higherBound) > 0);
                }
                else
                {
                    flag = (__instance.BountyHunterInfo.ShipInstance == null);
                }
                if (flag && num == 0)
                {
                    PLShipInfoBase shipInstance;
                    if (__instance.BountyHunterInfo == null || __instance.BountyHunterInfo.IsShipDestroyed)
                    {
                        PLEncounterManager.ShipLayout randomPossibleHunterLayout = PLEncounterManager.Instance.GetRandomPossibleHunterLayout(lowerBound, higherBound);
                        __instance.BountyHunterLayout = randomPossibleHunterLayout;
                        __instance.BountyHunterInfo = new PLPersistantShipInfo(__instance.BountyHunterLayout.ShipType, 6, currentSector, 0, false, true, false, -1, -1);
                        __instance.BountyHunterInfo.CreateShipInstance(PLEncounterManager.Instance.GetCPEI());
                        shipInstance = __instance.BountyHunterInfo.ShipInstance;
                    }
                    else
                    {
                        __instance.BountyHunterInfo.MyCurrentSector = currentSector;
                        __instance.BountyHunterInfo.CreateShipInstance(PLEncounterManager.Instance.GetCPEI());
                        shipInstance = __instance.BountyHunterInfo.ShipInstance;
                    }
                    if (shipInstance != null)
                    {
                        if (__instance.BountyHunterLayout != null)
                        {
                            shipInstance.IsBountyHunter = true;
                            shipInstance.CreditsLeftBehind += 2500;
                            shipInstance.MyStats.FormatToDataString(__instance.BountyHunterLayout.Data);
                            shipInstance.MyStats.FormatToCrewString(__instance.BountyHunterLayout.Crew);
                        }
                        shipInstance.OnShipKilled += delegate ()
                        {
                            m_ActiveBountyHunter_TypeID.SetValue(__instance, NegativeOne);
                            m_ActiveBountyHunter_LastCreateHunterUpdateTime.SetValue(__instance, (ObscuredFloat)Time.time);
                            __instance.BountyHunterInfo = null;
                        };
                        shipInstance.OnEnemyShipWarp += delegate (int sectorID)
                        {
                            m_ActiveBountyHunter_SectorID.SetValue(__instance, (ObscuredInt)sectorID);
                            m_ActiveBountyHunter_SecondsSinceWarp.SetValue(__instance, (ObscuredFloat)0f);
                            if (__instance.BountyHunterInfo != null)
                            {
                                __instance.BountyHunterInfo.HullPercent = Mathf.Clamp01(__instance.BountyHunterInfo.HullPercent + 0.33f);
                                __instance.BountyHunterInfo.ShldPercent = 1f;
                                __instance.BountyHunterInfo.MyCurrentSector = PLServer.GetSectorWithID(sectorID);
                            }
                        };
                        if (__instance.BountyHunterInfo != null)
                        {
                            __instance.BountyHunterInfo.ForcedHostileToShipID = PLEncounterManager.Instance.PlayerShip.ShipID;
                            __instance.BountyHunterInfo.IsFlagged = true;
                            shipInstance.IsFlagged = true;
                            return false;
                        }
                    }
                }
                else if (__instance.BountyHunterInfo == null || __instance.BountyHunterInfo.ShipInstance == null)
                {
                    __instance.BountyHunterInfo = null;
                    PLShipInfoBase plshipInfoBase = PLEncounterManager.Instance.GetCPEI().SpawnEnemyShip(EShipType.E_BOUNTY_HUNTER_01, null, default(Vector3), default(Quaternion));
                    if (plshipInfoBase != null)
                    {
                        plshipInfoBase.IsFlagged = true;
                    }
                    SpawnHunterPawn(__instance);
                    m_ActiveBountyHunter_TypeID.SetValue(__instance, NegativeOne);
                }
                return false;
            }
            private static void SpawnHunterPawn(PLServer __instance)
            {
                if (__instance.MyHunterSpawner == null)
                {
                    __instance.MyHunterSpawner = __instance.gameObject.AddMissingComponent<PLSpawner>();
                    __instance.MyHunterSpawner.Spawn = "Bandit";
                    __instance.MyHunterSpawner.ShouldRespawnEnemy = false;
                    __instance.MyHunterSpawner_FactionParameter = new SpawnParameter
                    {
                        Name = "Faction"
                    };
                    __instance.MyHunterSpawner_RaceParameter = new SpawnParameter
                    {
                        Name = "Race"
                    };
                    __instance.MyHunterSpawner_GenderParameter = new SpawnParameter
                    {
                        Name = "Gender"
                    };
                    __instance.MyHunterSpawner.Parameters.Add(__instance.MyHunterSpawner_FactionParameter);
                    __instance.MyHunterSpawner.Parameters.Add(__instance.MyHunterSpawner_RaceParameter);
                    __instance.MyHunterSpawner.Parameters.Add(__instance.MyHunterSpawner_GenderParameter);
                    __instance.MyHunterSpawner.enabled = false;
                }
                if ((ObscuredInt)m_ActiveBountyHunter_TypeID.GetValue(__instance) == 0)
                {
                    __instance.MyHunterSpawner_FactionParameter.Value = "0";
                    __instance.MyHunterSpawner_RaceParameter.Value = UnityEngine.Random.Range(0, 3).ToString();
                }
                else if ((ObscuredInt)m_ActiveBountyHunter_TypeID.GetValue(__instance) == 2)
                {
                    __instance.MyHunterSpawner_FactionParameter.Value = "2";
                    __instance.MyHunterSpawner_RaceParameter.Value = UnityEngine.Random.Range(0, 3).ToString();
                }
                else
                {
                    __instance.MyHunterSpawner_FactionParameter.Value = "1";
                    __instance.MyHunterSpawner_RaceParameter.Value = UnityEngine.Random.Range(0, 3).ToString();
                }
                if (__instance.MyHunterSpawner_RaceParameter.Value != "0")
                {
                    __instance.MyHunterSpawner_GenderParameter.Value = "male";
                }
                else
                {
                    __instance.MyHunterSpawner_GenderParameter.Value = ((UnityEngine.Random.value < 0.5f) ? "male" : "female");
                }
                PLSpawner.DoSpawnStatic(PLEncounterManager.Instance.GetCPEI(), "Bandit", PLEncounterManager.Instance.PlayerShip.MyTLI.AllTTIs[0].transform, __instance.MyHunterSpawner, PLEncounterManager.Instance.PlayerShip.MyTLI, null, null);
                PLPlayer component = PLEncounterManager.Instance.GetCPEI().MyCreatedPlayers[PLEncounterManager.Instance.GetCPEI().MyCreatedPlayers.Count - 1].GetComponent<PLPlayer>();
                component.GetPawn().BlockWarpWhenOnboard = true;
                if (__instance.MyHunterSpawner_RaceParameter.Value == "0")
                {
                    component.GetPawn().SetExosuitIsActive(true);
                }
                string text = PLServer.Instance.NPCNames[UnityEngine.Random.Range(0, PLServer.Instance.NPCNames.Count)];
                if (text.Contains(" "))
                {
                    text = text.Split(new char[]
                    {
                ' '
                    })[0];
                }
                component.SetPlayerName(text + " The Hunter");
                component.Talents[56] = Mathf.RoundToInt(5f + __instance.ChaosLevel * 2f);
                component.Talents[0] = Mathf.RoundToInt(20f + __instance.ChaosLevel * 4f);
                int pawnInvItemIDCounter;
                if ((ObscuredInt)m_ActiveBountyHunter_TypeID.GetValue(__instance) == 0)
                {
                    PLPawnInventoryBase myInventory = component.MyInventory;
                    PLServer instance = PLServer.Instance;
                    pawnInvItemIDCounter = instance.PawnInvItemIDCounter;
                    instance.PawnInvItemIDCounter = pawnInvItemIDCounter + 1;
                    myInventory.UpdateItem(pawnInvItemIDCounter, 7, 0, 4, 6);
                }
                else if ((ObscuredInt)m_ActiveBountyHunter_TypeID.GetValue(__instance) == 2)
                {
                    PLPawnInventoryBase myInventory2 = component.MyInventory;
                    PLServer instance2 = PLServer.Instance;
                    pawnInvItemIDCounter = instance2.PawnInvItemIDCounter;
                    instance2.PawnInvItemIDCounter = pawnInvItemIDCounter + 1;
                    myInventory2.UpdateItem(pawnInvItemIDCounter, 30, 0, 0, 6);
                }
                else
                {
                    PLPawnInventoryBase myInventory3 = component.MyInventory;
                    PLServer instance3 = PLServer.Instance;
                    pawnInvItemIDCounter = instance3.PawnInvItemIDCounter;
                    instance3.PawnInvItemIDCounter = pawnInvItemIDCounter + 1;
                    myInventory3.UpdateItem(pawnInvItemIDCounter, 8, 0, 4, 6);
                }
                PLPawnInventoryBase myInventory4 = component.MyInventory;
                PLServer instance4 = PLServer.Instance;
                pawnInvItemIDCounter = instance4.PawnInvItemIDCounter;
                instance4.PawnInvItemIDCounter = pawnInvItemIDCounter + 1;
                myInventory4.UpdateItem(pawnInvItemIDCounter, 31, 0, 0, -1);
                PLPawnInventoryBase myInventory5 = component.MyInventory;
                PLServer instance5 = PLServer.Instance;
                pawnInvItemIDCounter = instance5.PawnInvItemIDCounter;
                instance5.PawnInvItemIDCounter = pawnInvItemIDCounter + 1;
                myInventory5.UpdateItem(pawnInvItemIDCounter, 31, 0, 0, -1);
                __instance.StartCoroutine((IEnumerator)ControlledSEND_SetupNewHunter.Invoke(__instance, new object[] { component.GetPawn() }));
            }
        }
        [HarmonyPatch(typeof(PLPersistantEncounterInstance), "SpawnEnemyShip")]
        class BountyHunterName //This is so bounty hunters in ships from fluffy company or from no faction to have a name instead of N/A
        {
            static void Postfix(ref PLShipInfoBase __result)
            {
                PLShipInfo ship = __result as PLShipInfo;
                if (ship != null)
                {
                    if (ship.ShipTypeID == EShipType.E_INTREPID_SC || ship.ShipTypeID == EShipType.E_ALCHEMIST || ship.IsDrone || __result.PersistantShipInfo.MyCurrentSector.MissionSpecificID != -1) return;
                    switch (ship.FactionID)
                    {
                        case -1:
                            ship.ShipNameValue = PLServer.Instance.AOGShipNameGenerator.GetName(PLServer.Instance.GalaxySeed + ship.ShipID);
                            break;
                        case 3:
                            string[] biscuitnames = new string[]
                            {
                            "Muffin Mashers",
                            "Crispy Cruiser",
                            "Tasty Toasters",
                            "Delivery Dogs",
                            "Hunger Hinderers",
                            "Bombastic Bites",
                            "Goodstuff Foodstuff",
                            "Merry Meals",
                            "Fasting Foodies",
                            "Sugar Speeders",
                            "Generic Grillers",
                            "Ham Hoarders",
                            "Ration Replacers",
                            "Rapid Responders",
                            "Mad Merchants",
                            "Voracious Vendors",
                            "Palatable Peddlers",
                            "Wandering Wafer",
                            "Biscuit Boys",
                            "Necessary Nutritions",
                            "Hungry Hucksters",
                            "Flavor’s Fury",
                            "Tea Timers",
                            };
                            ship.ShipNameValue = biscuitnames[Random.Range(0, biscuitnames.Length - 1)];
                            break;
                    }
                    if (ship.GetCombatLevel() > 135 && Random.value < 0.1 && ship.PersistantShipInfo.ShipName != "")
                    {
                        ship.ShipNameValue = "The Glass Revenant Mk " + Random.Range(1, 999);
                    }
                }
            }
        }
    }
}