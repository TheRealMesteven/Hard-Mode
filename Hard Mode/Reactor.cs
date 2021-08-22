﻿using HarmonyLib;
using UnityEngine;

namespace Hard_Mode
{
    [HarmonyPatch(typeof(PLReactorInstance), "Update")]
    class Reactor_Radiation
    {

        static void Postfix(PLRadiationPoint ___RadPoint, PLReactorInstance __instance) //Increases radiation from reactor deppending on max temp and current stability
        {
            PLTempRadius tempRadius = null;
            if (__instance.MyShipInfo.MyReactor != null && !PLGlobal.WithinTimeLimit(__instance.MyShipInfo.ReactorLastCoreEjectServerTime, PLServer.Instance.GetEstimatedServerMs(), 5000) && Options.DangerousReactor) // Check to not cause a lot of exceptions with the reactor ejecting
            {
                PLReactor reactor = __instance.MyShipInfo.MyStats.GetShipComponent<PLReactor>(ESlotType.E_COMP_REACTOR, false);
                ___RadPoint.RaditationRange = reactor.TempMax / 150f;
                ___RadPoint.RaditationRange *= 1f + (__instance.MyShipInfo.CoreInstability * 4f);
                if (___RadPoint.RaditationRange < 25f) ___RadPoint.RaditationRange = 25; // This is so sylvassi reactor doesn't become radiation proof

                if (__instance.gameObject.GetComponent<PLTempRadius>() != null) //This part adds temperature range to reactor, so it changes the ship interior temperature
                {
                    tempRadius = __instance.gameObject.GetComponent<PLTempRadius>();
                }
                else
                {
                    tempRadius = __instance.gameObject.AddComponent<PLTempRadius>();
                }
                tempRadius.IsOnShip = true;
                tempRadius.MinRange = 0f;
                tempRadius.MaxRange = 20f;
                tempRadius.MaxRange += __instance.MyShipInfo.MyStats.ReactorTempCurrent / (__instance.MyShipInfo.MyStats.ReactorTempMax * 0.5f);
                tempRadius.Temperature = __instance.MyShipInfo.MyStats.ReactorTempCurrent / (__instance.MyShipInfo.MyStats.ReactorTempMax * 0.3f);
                if (tempRadius.Temperature < 1) tempRadius.Temperature = 1; //This is so reactor doesn't decide to make the nearby area colder
                else if (tempRadius.Temperature > 20) tempRadius.Temperature = 10; //This is more for the OP hunters that have reactor that would just kill them with the temp
                if (__instance.MyShipInfo.MyStats.ReactorTempCurrent >= __instance.MyShipInfo.MyStats.ReactorTempMax * 0.90 && Random.Range(0, 200) == 10) //this spawns fire when too hot
                {
                    PLMainSystem system = __instance.MyShipInfo.GetSystemFromID(Random.Range(0, 3));
                    int looplimit = 0;
                    while ((system == null || system.IsOnFire()) && looplimit < 20)
                    {
                        system = __instance.MyShipInfo.GetSystemFromID(Random.Range(0, 3));
                        looplimit++;
                    }
                    PLServer.Instance.CreateFireAtSystem(system, false);
                }
            }
            else if (!Options.DangerousReactor && __instance.gameObject.GetComponent<PLTempRadius>() != null) //This will bring things to normal when dangerous reactor is disabled
            {
                UnityEngine.Object.Destroy(__instance.gameObject.GetComponent<PLTempRadius>());
                ___RadPoint.RaditationRange = 1f;
            }
        }
    }
    /*
    [HarmonyPatch(typeof(PLReactor), "ShipUpdate")]
    class ShieldUsage
    {
        static void Postfix(PLShipInfoBase inShipInfo)
        {
            PLShieldGenerator shield = inShipInfo.MyStats.GetShipComponent<PLShieldGenerator>(ESlotType.E_COMP_SHLD, false);
            shield.IsPowerActive = true;
            shield.RequestPowerUsage_Limit = shield.CalculatedMaxPowerUsage_Watts * 0.25f;
            shield.InputPower_Watts = shield.CalculatedMaxPowerUsage_Watts * 0.25f;
            shield.RequestPowerUsage_Percent = 1f;

        }
    }
    */
    [HarmonyPatch(typeof(PLEnergySphere), "Detonate")]
    class Explosion
    {
        static void Prefix(PLEnergySphere __instance, bool ___HasExploded, PLShipInfoBase ___MyOwner, float ___MaxRange) // This should make the damage from the reactor more powerfull deppending on the price
        {
            if (!PhotonNetwork.isMasterClient)
            {
                return;
            }
            if (!___HasExploded)
            {
                ___HasExploded = true;
                if (___MyOwner != null)
                {
                    PLEnergySphereExplosion component = UnityEngine.Object.Instantiate<GameObject>(PLGlobal.Instance.EnergySphereExplosionPrefab, __instance.transform.position, __instance.transform.rotation).GetComponent<PLEnergySphereExplosion>();
                    if (component != null)
                    {
                        component.Init(__instance.DamageMultiplier - 8f, ___MaxRange);
                    }
                    PLNetworkManager.Instance.WaitForNetworkDestroy(__instance.gameObject, 2f);
                    foreach (PLShipInfoBase plshipInfoBase in PLEncounterManager.Instance.AllShips.Values)
                    {
                        if (plshipInfoBase != null)
                        {
                            float magnitude = (plshipInfoBase.Exterior.transform.position - __instance.transform.position).magnitude;
                            if (magnitude < ___MaxRange)
                            {
                                float num = Mathf.Clamp01(magnitude / ___MaxRange);
                                num += 0.001f;
                                float dmg = Mathf.Clamp01(1f - num) * 1800f;
                                plshipInfoBase.TakeDamage(dmg, false, EDamageType.E_ENERGY, Random.Range(0f, 1f), -1, ___MyOwner, -1);
                            }
                        }
                    }

                    __instance.VisibleMesh.SetActive(false);
                }
            }
            foreach (PLTempRadius temp in PLGameStatic.Instance.m_TempRadius) //This makes the reactor exploding without ejecting also cool down the ship
            {
                if (temp.IsOnShip == true) temp.Temperature = 1;
            }
        }

    }

    [HarmonyPatch(typeof(PLServer), "ServerEjectReactorCore")]
    class ResetTemperature //This makes that when reactor is ejected the temperature go back to normal levels
    {
        static void Postfix()
        {
            foreach (PLTempRadius temp in PLGameStatic.Instance.m_TempRadius)
            {
                if (temp.IsOnShip == true) temp.Temperature = 1;
            }
        }
    }
}
