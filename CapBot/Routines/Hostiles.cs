using UnityEngine;

namespace CapBot.Routines
{
    internal class Hostiles
    {
        internal static void CheckForHostiles(PLPlayer __instance, out bool HasIntruders)
        {
            HasIntruders = false;
            if (__instance.StartingShip != null) //Check for intruders and set hostile ships
            {
                foreach (PLPlayer player in PLServer.Instance.AllPlayers) // Find if there is intruders in the ship
                {
                    if (player.TeamID != 0 && player.MyCurrentTLI == __instance.StartingShip.MyTLI)
                    {
                        HasIntruders = true;
                        break;
                    }
                }
                foreach (PLShipInfoBase ship in PLEncounterManager.Instance.AllShips.Values) //Attack everyone that hates us, and warp disable beacons
                {
                    if (ship.HostileShips.Contains(__instance.StartingShip.ShipID) || (ship.ShipTypeID == EShipType.E_BEACON && (ship as PLBeaconInfo).BeaconType == EBeaconType.E_WARP_DISABLE))
                    {
                        __instance.StartingShip.HostileShips.Add(ship.ShipID);
                    }
                }
            }
        }
        internal static void BoardShip(PLPlayer CapBot, ref float LastOrder)
        {
            if (CapBot.StartingShip.TargetShip != null && CapBot.StartingShip.TargetShip != CapBot.StartingShip && CapBot.StartingShip.TargetShip is PLShipInfo && CapBot.StartingShip.TargetShip.TeamID > 0 && (!CapBot.StartingShip.TargetShip.IsQuantumShieldActive || CapBot.MyCurrentTLI == CapBot.StartingShip.TargetShip.MyTLI))
            {
                //Board enemy to remove claim
                PLShipInfo targetEnemy = CapBot.StartingShip.TargetShip as PLShipInfo;
                int screensCaptured = 0;
                int num2 = 0;
                bool CaptainScreenCaptured = false;
                if (PLServer.Instance.CaptainsOrdersID != 6 && Time.time - LastOrder > 1f)
                {
                    LastOrder = Time.time;
                    PLServer.Instance.CaptainSetOrderID(6);
                }
                CapBot.StartingShip.AlertLevel = 2;
                CapBot.MyBot.AI_TargetTLI = targetEnemy.MyTLI;
                foreach (PLUIScreen pluiscreen in targetEnemy.MyScreenBase.AllScreens)
                {
                    if (pluiscreen != null && !pluiscreen.IsClonedScreen)
                    {
                        if (pluiscreen.PlayerControlAlpha >= 0.9f)
                        {
                            screensCaptured++;
                            if ((pluiscreen as PLCaptainScreen) != null)
                            {
                                CaptainScreenCaptured = true;
                            }
                        }
                        num2++;
                    }
                }
                if (screensCaptured >= num2 / 2 && CaptainScreenCaptured)
                {
                    foreach (PLUIScreen pluiscreen in targetEnemy.MyScreenBase.AllScreens)
                    {
                        if ((pluiscreen as PLCaptainScreen) != null)
                        {
                            CapBot.MyBot.AI_TargetPos = pluiscreen.transform.position;
                            CapBot.MyBot.AI_TargetPos_Raw = CapBot.MyBot.AI_TargetPos;
                            break;
                        }
                    }
                    if ((CapBot.MyBot.AI_TargetPos - CapBot.GetPawn().transform.position).sqrMagnitude > 4)
                    {
                        CapBot.MyBot.EnablePathing = true;
                    }
                    else
                    {
                        PLServer.Instance.photonView.RPC("ClaimShip", PhotonTargets.MasterClient, new object[]
                        {
                            targetEnemy.ShipID
                        });
                    }

                    return;
                }
            }
        }
        internal static void ClaimShip(PLPlayer CapBot)
        {
            if (CapBot.StartingShip == null && CapBot.MyCurrentTLI.MyShipInfo != null) //Claim current ship if player ship was destroyed/captured
            {
                PLShipInfo targetEnemy = CapBot.MyCurrentTLI.MyShipInfo;
                int screensCaptured = 0;
                int num2 = 0;
                bool CaptainScreenCaptured = false;
                foreach (PLUIScreen pluiscreen in targetEnemy.MyScreenBase.AllScreens)//Capture enough screens
                {
                    if (pluiscreen != null && !pluiscreen.IsClonedScreen)
                    {
                        if (pluiscreen.PlayerControlAlpha >= 0.9f)
                        {
                            screensCaptured++;
                            if ((pluiscreen as PLCaptainScreen) != null)
                            {
                                CaptainScreenCaptured = true;
                            }
                        }
                        num2++;
                    }
                }
                if (screensCaptured >= num2 / 2 && CaptainScreenCaptured)//Claim the ship
                {
                    foreach (PLUIScreen pluiscreen in targetEnemy.MyScreenBase.AllScreens)
                    {
                        if ((pluiscreen as PLCaptainScreen) != null)
                        {
                            CapBot.MyBot.AI_TargetPos = pluiscreen.transform.position;
                            CapBot.MyBot.AI_TargetPos_Raw = CapBot.MyBot.AI_TargetPos;
                            break;
                        }
                    }
                    CapBot.MyBot.AI_TargetTLI = targetEnemy.MyTLI;
                    if ((CapBot.MyBot.AI_TargetPos - CapBot.GetPawn().transform.position).sqrMagnitude > 4)
                    {
                        CapBot.MyBot.EnablePathing = true;
                    }
                    else
                    {
                        PLServer.Instance.photonView.RPC("ClaimShip", PhotonTargets.MasterClient, new object[]
                        {
                            targetEnemy.ShipID
                        });
                    }
                    return;
                }
            }
        }
    }
}
