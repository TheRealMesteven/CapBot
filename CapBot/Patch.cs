﻿using PulsarModLoader;
using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;

namespace CapBot
{
    [HarmonyPatch(typeof(PLPlayer), "UpdateAIPriorities")]
    class Patch
    {
        static float LastAction = 0;
        static float LastMapUpdate = Time.time;
        static float LastBlindJump = 0;
        static void Postfix(PLPlayer __instance)
        {
            if (__instance.GetPawn() == null || !__instance.IsBot || __instance.GetClassID() != 0 || __instance.TeamID != 0) return;
            if (__instance.StartingShip != null && __instance.StartingShip.ShipTypeID == EShipType.E_POLYTECH_SHIP && __instance.RaceID != 2)
            {
                __instance.RaceID = 2;
            }
            if (__instance.StartingShip != null && __instance.StartingShip.InWarp && PLServer.Instance.AllPlayersLoaded()) //Skip warp
                PLInGameUI.Instance.WarpSkipButtonClicked();
            if (__instance.MyBot.AI_TargetPos != __instance.StartingShip.CaptainsChairPivot.position && __instance.StartingShip.CaptainsChairPlayerID == __instance.GetPlayerID())// leave chair
            {
                __instance.StartingShip.AttemptToSitInCaptainsChair(-1);
            }
            if (PLServer.GetCurrentSector() != null && PLServer.GetCurrentSector().VisualIndication == ESectorVisualIndication.TOPSEC)//Inside the colony 
            {
                AtColony(__instance);
                return;
            }
            if (PLServer.GetCurrentSector() != null && PLServer.GetCurrentSector().VisualIndication == ESectorVisualIndication.DESERT_HUB && !PLServer.Instance.IsFragmentCollected(1))//In the burrow
            {
                if (PLServer.Instance.CurrentCrewCredits >= 100000)
                {
                    __instance.MyBot.AI_TargetPos = new Vector3(212, 64, -38);
                    __instance.MyBot.AI_TargetPos_Raw = __instance.MyBot.AI_TargetPos;
                    foreach (PLTeleportationLocationInstance teleport in Object.FindObjectsOfType(typeof(PLTeleportationLocationInstance)))
                    {
                        if (teleport.name == "PLGame")
                        {
                            __instance.MyBot.AI_TargetTLI = teleport;
                            break;
                        }
                    }
                    if ((__instance.MyBot.AI_TargetPos - __instance.GetPawn().transform.position).sqrMagnitude > 4)
                    {
                        __instance.MyBot.EnablePathing = true;
                    }
                    else
                    {
                        PLServer.Instance.photonView.RPC("AttemptForceEndMissionOfTypeID", PhotonTargets.All, new object[]
                        {
                        100786
                        });
                        PLServer.Instance.CollectFragment(1);
                        PLServer.Instance.CurrentCrewCredits -= 100000;
                    }
                }
                else if (PLServer.Instance.CurrentCrewCredits >= 50000 && PLServer.Instance.CurrentCrewLevel >= 5)
                {
                    __instance.MyBot.AI_TargetPos = new Vector3(62, 18, -56);
                    __instance.MyBot.AI_TargetPos_Raw = __instance.MyBot.AI_TargetPos;
                    PLBurrowArena arena = Object.FindObjectOfType(typeof(PLBurrowArena)) as PLBurrowArena;
                    if (arena != null)
                    {
                        if (__instance.GetPawn().SpawnedInArena)
                        {
                            __instance.MyBot.AI_TargetPos = new Vector3(103, 4, -115);
                            __instance.MyBot.AI_TargetPos_Raw = __instance.MyBot.AI_TargetPos;
                            __instance.MyBot.EnablePathing = true;
                        }
                        foreach (PLTeleportationLocationInstance teleport in Object.FindObjectsOfType(typeof(PLTeleportationLocationInstance)))
                        {
                            if (teleport.name == "PLGame")
                            {
                                __instance.MyBot.AI_TargetTLI = teleport;
                                break;
                            }
                        }
                        if ((__instance.MyBot.AI_TargetPos - __instance.GetPawn().transform.position).sqrMagnitude > 4 && !__instance.GetPawn().SpawnedInArena)
                        {
                            __instance.MyBot.EnablePathing = true;
                        }
                        else if (!arena.ArenaIsActive)
                        {
                            arena.StartArena(0);
                            __instance.GetPawn().transform.position = new Vector3(103, 4, -115);
                        }
                    }
                }
                LastAction = Time.time;
                return;
            }
            if (PLServer.Instance.m_ShipCourseGoals.Count == 0 || Time.time - LastMapUpdate > 15)
            {
                //Updates the map destines
                PLServer.Instance.photonView.RPC("ClearCourseGoals", PhotonTargets.All, new object[0]);
                SetNextDestiny();
                if (PLServer.Instance.m_ShipCourseGoals.Count > 0 && PLServer.Instance.m_ShipCourseGoals[0] == PLServer.GetCurrentSector().ID)
                {
                    PLServer.Instance.photonView.RPC("RemoveCourseGoal", PhotonTargets.All, new object[]
                    {
                    PLServer.Instance.m_ShipCourseGoals[0]
                    });
                }
            }
            if (__instance.StartingShip != null && __instance.StartingShip.MyStats.GetShipComponent<PLCaptainsChair>(ESlotType.E_COMP_CAPTAINS_CHAIR, false) != null && Time.time - LastAction > 20f) //Sit in chair
            {
                __instance.MyBot.AI_TargetPos = __instance.StartingShip.CaptainsChairPivot.position;
                __instance.MyBot.AI_TargetPos_Raw = __instance.MyBot.AI_TargetPos;
                __instance.MyBot.AI_TargetTLI = __instance.StartingShip.MyTLI;
                if ((__instance.StartingShip.CaptainsChairPivot.position - __instance.GetPawn().transform.position).sqrMagnitude > 4)
                {
                    __instance.MyBot.EnablePathing = true;
                }
                else
                {
                    if (__instance.StartingShip.CaptainsChairPlayerID != __instance.GetPlayerID())
                    {
                        __instance.StartingShip.AttemptToSitInCaptainsChair(__instance.GetPlayerID());
                    }
                }

            }
            if ((__instance.StartingShip.HostileShips.Count > 1 || (__instance.StartingShip.TargetShip != null && __instance.StartingShip.TargetShip.GetCombatLevel() > __instance.StartingShip.GetCombatLevel())) && __instance.StartingShip.MyStats.HullCurrent / __instance.StartingShip.MyStats.HullMax < 0.2f && !__instance.StartingShip.InWarp && Time.time - LastBlindJump > 60)
            {
                //Blind jump in emergency
                __instance.MyBot.AI_TargetPos = (__instance.StartingShip.Spawners[4] as GameObject).transform.position;
                __instance.MyBot.AI_TargetPos_Raw = __instance.MyBot.AI_TargetPos;
                __instance.MyBot.AI_TargetTLI = __instance.StartingShip.MyTLI;
                if ((__instance.MyBot.AI_TargetPos - __instance.GetPawn().transform.position).sqrMagnitude > 4)
                {
                    __instance.MyBot.EnablePathing = true;
                }
                else
                {
                    __instance.StartingShip.BlindJumpUnlocked = true;
                    PLServer.Instance.photonView.RpcSecure("AttemptBlindJump", PhotonTargets.MasterClient, true, new object[]
                    {
                        __instance.StartingShip.ShipID,
                        __instance.GetPlayerID()
                    });
                    LastBlindJump = Time.time;
                }
                LastAction = Time.time;
            }



        }

        static float LastDestiny = Time.time;
        static void AtColony(PLPlayer CapBot)
        {
            PLBot AI = CapBot.MyBot;
            PLPawn pawn = CapBot.GetPawn();
            PLTeleportationLocationInstance planet = null;
            foreach (PLTeleportationLocationInstance teleport in Object.FindObjectsOfType(typeof(PLTeleportationLocationInstance)))
            {
                if (teleport.name == "PLGamePlanet")
                {
                    planet = teleport;
                    break;
                }
            }
            if (planet == null || pawn == null) return;
            AI.AI_TargetTLI = planet;
            List<Vector3> possibleTargets;
            PLContainmentSystem colonyDoor = Object.FindObjectOfType(typeof(PLContainmentSystem)) as PLContainmentSystem;
            if (!PLServer.AnyPlayerHasItemOfName("Facility Keycard")) //Step 1: Find facility key
            {
                if (Time.time - LastDestiny > 10f)
                {
                    possibleTargets = new List<Vector3>()
                    {
                        new Vector3(1025,-516,476),
                        new Vector3(1054,-516,483),
                        new Vector3(1042,-515,447),
                        new Vector3(1014,-515,444),
                        new Vector3(972,-517,461),
                        new Vector3(1062,-512,508),
                        new Vector3(1019,-515,523),
                        new Vector3(1001,-515,443),
                        new Vector3(988,-517,494),
                    };
                    AI.AI_TargetPos = possibleTargets[Random.Range(0, possibleTargets.Count - 1)];
                    AI.AI_TargetPos_Raw = AI.AI_TargetPos;
                    LastDestiny = Time.time;
                }
            }
            else if (!PLServer.AnyPlayerHasItemOfName("Lower Facilities Keycard")) //Step 2: Find lower facility key
            {
                if (Time.time - LastDestiny > 25f)
                {
                    possibleTargets = new List<Vector3>()
                    {
                        new Vector3(941,-497,468),
                        new Vector3(946,-517,503),
                        new Vector3(964,-517,528),
                        new Vector3(984,-517,505),
                        new Vector3(975,-517,484),
                        new Vector3(946,-511,506),
                        new Vector3(921,-514,519),
                        new Vector3(943,-498,499),
                        new Vector3(962,-499,500),
                        new Vector3(961,-499,521),
                        new Vector3(920,-503,503),
                        new Vector3(954,-481,515),
                        new Vector3(952,-499,495),
                        new Vector3(966,-511,526),
                        new Vector3(963,-505,562),
                    };
                    AI.AI_TargetPos = possibleTargets[Random.Range(0, possibleTargets.Count - 1)];
                    AI.AI_TargetPos_Raw = AI.AI_TargetPos;
                    LastDestiny = Time.time;
                }
            }
            else if (colonyDoor != null && !colonyDoor.GetHasBeenCompleted()) //Step 3: Fix errors at locked door
            {
                AI.AI_TargetPos = new Vector3(954, -534, 511);
                AI.AI_TargetPos_Raw = AI.AI_TargetPos;
                if ((pawn.transform.position - AI.AI_TargetPos).sqrMagnitude < 4 && !colonyDoor.HasStarted)
                {
                    colonyDoor.SetHasStarted();
                }
                if (Time.time - LastDestiny > 30 && colonyDoor.HasStarted)
                {
                    PulsarModLoader.Utilities.Messaging.ChatMessage(PhotonTargets.All, "Need fixing:", CapBot.GetPlayerID());
                    foreach (ContainmentSystemParameter parameter in colonyDoor.Parameters)
                    {
                        if (!parameter.GoalAndTargetMatch())
                        {
                            PulsarModLoader.Utilities.Messaging.ChatMessage(PhotonTargets.All, parameter.GetStringCategoryName() + ": " + parameter.GetStringDisplayName() + ": " + parameter.GetString_GoalValue(), CapBot.GetPlayerID());
                        }
                    }
                    LastDestiny = Time.time;
                }
            }
            else if (!colonyDoor.ContainmentDoor.GetIsOpen()) //Step 4: Open the door
            {
                colonyDoor.OpenContainmentDoorNow();
                LastDestiny = Time.time;
            }
            else if (PLLCChair.Instance != null && !PLLCChair.Instance.Triggered && PLLCChair.Instance.GetNumErrors(true) > 0) //Step 5: Fix screen erros at final door
            {
                AI.Tick_HelpWithChairSyncMiniGame(true);
                LastDestiny = Time.time;
            }
            else if (PLLCChair.Instance != null && PLLCChair.Instance.GetNumErrors(true) <= 0 && PLLCChair.Instance.PlayerIDInChair != CapBot.GetPlayerID() && !PLLCChair.Instance.Triggered_LevelThree) //Step 6: Sit in the chair
            {
                AI.AI_TargetPos = PLLCChair.Instance.gameObject.transform.position;
                AI.AI_TargetPos_Raw = AI.AI_TargetPos;
                if ((pawn.transform.position - AI.AI_TargetPos).sqrMagnitude < 8)
                {
                    PLLCChair.Instance.photonView.RPC("Trigger", PhotonTargets.All, new object[0]);
                    if (Time.time - LastDestiny > 60)
                    {
                        PLLCChair.Instance.photonView.RPC("SetPlayerIDInChair", PhotonTargets.All, new object[]
                        {
                            CapBot.GetPlayerID()
                        });
                        PLLCChair.Instance.photonView.RPC("Trigger_LevelTwo", PhotonTargets.All, new object[0]);
                    }
                }
            }
            else if (PLLCChair.Instance != null && PLLCChair.Instance.Triggered_LevelTwo && PLLCChair.Instance.PlayerIDInChair == CapBot.GetPlayerID() && !PLLCChair.Instance.Triggered_LevelThree) //Step 7: Do the minigame
            {
                if (Time.time - LastDestiny > 30 && PLLCChairUI.Instance != null)
                {
                    if (Random.Range(0, 9) != 0)
                    {
                        PLLCChair.Instance.photonView.RPC("SetUICurrentLayer", PhotonTargets.All, new object[]
                        {
                        PLLCChairUI.Instance.CurrentLayer+1
                        });
                    }
                    else
                    {
                        PLLCChair.Instance.photonView.RPC("SetUICurrentLayer", PhotonTargets.All, new object[]
                            {
                        PLLCChairUI.Instance.CurrentLayer > 0 ? PLLCChairUI.Instance.CurrentLayer-1 : 0
                            });
                    }
                    LastDestiny = Time.time;
                }
            }
            else if (PLLCChair.Instance != null && PLLCChair.Instance.Triggered_LevelThree && PLLCChair.Instance.PlayerIDInChair == CapBot.GetPlayerID()) //Step 8: You keep control over the infected for yourselfs
            {
                PLLCChair.Instance.photonView.RPC("StartKeepItEnding", PhotonTargets.All, new object[0]);
                PLLCChair.Instance.SetPlayerIDInChair(-1);
            }
            foreach (PLPickupObject item in Object.FindObjectsOfType(typeof(PLPickupObject)))
            {
                if ((item.transform.position - pawn.transform.position).sqrMagnitude < 16 && item.GetItemName(true).Contains("Keycard") && !item.PickedUp)
                {
                    AI.AI_TargetPos = item.transform.position;
                    AI.AI_TargetPos_Raw = AI.AI_TargetPos;
                    PLMusic.PostEvent("play_sx_player_item_pickup", pawn.gameObject);
                    pawn.photonView.RPC("Anim_Pickup", PhotonTargets.Others, new object[0]);
                    CapBot.photonView.RPC("AttemptToPickupObjectAtID", PhotonTargets.MasterClient, new object[]
                    {
                            item.PickupID
                    });
                }
            }
        }

        static void SetNextDestiny()
        {
            if (PLEncounterManager.Instance.PlayerShip == null) return;
            List<PLSectorInfo> destines = new List<PLSectorInfo>();
            PLSectorInfo GWG = PLGlobal.Instance.Galaxy.GetSectorOfVisualIndication(ESectorVisualIndication.GWG);
            float nearestWarpGatedist = 500;
            PLSectorInfo nearestWarpGate = null;
            PLSectorInfo nearestWarpGatetoDest = null;
            PLSectorInfo nearestDestiny = null;
            if (PLEncounterManager.Instance.PlayerShip.GetCombatLevel() > 80 && PLServer.Instance.GetNumFragmentsCollected() >= 4 && PLServer.Instance.CurrentCrewLevel >= 10)
            {
                destines.Add(GWG);
            }
            else
            {
                foreach (PLMissionBase mission in PLServer.Instance.AllMissions) //Add mission sectors not visited and not completed
                {
                    if (!mission.Ended && !mission.Abandoned)
                    {
                        foreach (PLSectorInfo plsectorInfo in PLGlobal.Instance.Galaxy.AllSectorInfos.Values)
                        {
                            if (plsectorInfo.MissionSpecificID == mission.MissionTypeID && plsectorInfo != GWG && !plsectorInfo.Visited)
                            {
                                destines.Add(plsectorInfo);
                                break;
                            }
                        }
                    }
                }
                if (PLEncounterManager.Instance.PlayerShip.MyStats.ThrustOutputMax > 25 && !PLServer.Instance.IsFragmentCollected(10) && PLServer.Instance.CurrentCrewLevel >= 4) //Add races to possible destinations
                {
                    if ((PLServer.Instance.RacesWonBitfield & 1) == 0)
                    {
                        destines.Add(PLGlobal.Instance.Galaxy.GetSectorOfVisualIndication(ESectorVisualIndication.RACING_SECTOR));
                    }
                    if ((PLServer.Instance.RacesWonBitfield & 2) == 0)
                    {
                        destines.Add(PLGlobal.Instance.Galaxy.GetSectorOfVisualIndication(ESectorVisualIndication.RACING_SECTOR_2));
                    }
                    if ((PLServer.Instance.RacesWonBitfield & 1) != 0 && (PLServer.Instance.RacesWonBitfield & 2) != 0)
                    {
                        destines.Add(PLGlobal.Instance.Galaxy.GetSectorOfVisualIndication(ESectorVisualIndication.RACING_SECTOR_3));
                    }
                }
                if (!PLServer.Instance.IsFragmentCollected(1) && (PLServer.Instance.CurrentCrewCredits >= 100000 || (PLServer.Instance.CurrentCrewCredits >= 50000 && PLServer.Instance.CurrentCrewLevel >= 5)))
                {
                    destines.Add(PLGlobal.Instance.Galaxy.GetSectorOfVisualIndication(ESectorVisualIndication.DESERT_HUB));
                }
            }
            destines.RemoveAll((PLSectorInfo sector) => sector == PLServer.GetCurrentSector());
            foreach (PLSectorInfo sector in destines) //finds nearest destiny
            {
                if ((sector.Position - PLServer.GetCurrentSector().Position).magnitude < nearestWarpGatedist)
                {
                    nearestWarpGatedist = (sector.Position - PLServer.GetCurrentSector().Position).magnitude;
                    nearestDestiny = sector;
                }
            }
            nearestWarpGatedist = 500;
            if (PLEncounterManager.Instance.PlayerShip.MyStats.HullCurrent / PLEncounterManager.Instance.PlayerShip.MyStats.HullMax < 0.6)
            {
                foreach (PLSectorInfo sector in PLGlobal.Instance.Galaxy.AllSectorInfos.Values) //finds nearest repair depot
                {
                    if ((sector.Position - PLServer.GetCurrentSector().Position).magnitude < nearestWarpGatedist && (sector.VisualIndication == ESectorVisualIndication.GENERAL_STORE || sector.VisualIndication == ESectorVisualIndication.EXOTIC1 || sector.VisualIndication == ESectorVisualIndication.EXOTIC2 || sector.VisualIndication == ESectorVisualIndication.EXOTIC3 || sector.VisualIndication == ESectorVisualIndication.EXOTIC4
                        || sector.VisualIndication == ESectorVisualIndication.EXOTIC5 || sector.VisualIndication == ESectorVisualIndication.EXOTIC6 || sector.VisualIndication == ESectorVisualIndication.EXOTIC7 || sector.VisualIndication == ESectorVisualIndication.AOG_HUB || sector.VisualIndication == ESectorVisualIndication.GENTLEMEN_START || sector.VisualIndication == ESectorVisualIndication.CORNELIA_HUB
                        || sector.VisualIndication == ESectorVisualIndication.COLONIAL_HUB || sector.VisualIndication == ESectorVisualIndication.WD_START || sector.VisualIndication == ESectorVisualIndication.SPACE_SCRAPYARD || (sector.VisualIndication == ESectorVisualIndication.FLUFFY_FACTORY_01 && PLServer.Instance.CrewFactionID == 3) || (sector.VisualIndication == ESectorVisualIndication.FLUFFY_FACTORY_02 && PLServer.Instance.CrewFactionID == 3) || (sector.VisualIndication == ESectorVisualIndication.FLUFFY_FACTORY_03 && PLServer.Instance.CrewFactionID == 3)))
                    {
                        nearestWarpGatedist = (sector.Position - PLServer.GetCurrentSector().Position).magnitude;
                        nearestDestiny = sector;
                    }
                }

            }
            if (nearestDestiny == null && PLServer.Instance.CurrentCrewLevel >= 4)
            {
                List<PLSectorInfo> random = new List<PLSectorInfo>();
                foreach (PLSectorInfo plsectorInfo in PLGlobal.Instance.Galaxy.AllSectorInfos.Values) //finds near random sectors
                {
                    if ((plsectorInfo.Position - PLServer.GetCurrentSector().Position).magnitude <= PLEncounterManager.Instance.PlayerShip.MyStats.WarpRange && !plsectorInfo.Visited && plsectorInfo.MySPI.Faction != 4)
                    {
                        random.Add(plsectorInfo);
                    }
                }
                if (random.Count == 0) return;
                nearestDestiny = random[Random.Range(0, random.Count - 1)];
            }
            else if (nearestDestiny == null) return;
            nearestWarpGatedist = 500;
            foreach (PLSectorInfo plsectorInfo in PLGlobal.Instance.Galaxy.AllSectorInfos.Values) //finds nearest warpgate
            {
                if (plsectorInfo.IsPartOfLongRangeWarpNetwork)
                {
                    if ((plsectorInfo.Position - PLServer.GetCurrentSector().Position).magnitude < nearestWarpGatedist)
                    {
                        nearestWarpGatedist = (plsectorInfo.Position - PLServer.GetCurrentSector().Position).magnitude;
                        nearestWarpGate = plsectorInfo;
                    }
                }
            }
            nearestWarpGatedist = 500;
            foreach (PLSectorInfo plsectorInfo in PLGlobal.Instance.Galaxy.AllSectorInfos.Values) //finds nearest warpgate to destiny
            {
                if (plsectorInfo.IsPartOfLongRangeWarpNetwork)
                {
                    if ((plsectorInfo.Position - nearestDestiny.Position).magnitude < nearestWarpGatedist)
                    {
                        nearestWarpGatedist = (plsectorInfo.Position - nearestDestiny.Position).magnitude;
                        nearestWarpGatetoDest = plsectorInfo;
                    }
                }
            }
            if (PLGlobal.Instance.Galaxy.GetPathToSector(PLServer.GetCurrentSector(), nearestWarpGate).Count + 1 + PLGlobal.Instance.Galaxy.GetPathToSector(nearestDestiny, nearestWarpGatetoDest).Count < PLGlobal.Instance.Galaxy.GetPathToSector(PLServer.GetCurrentSector(), nearestDestiny).Count && nearestWarpGate != PLServer.GetCurrentSector() && nearestWarpGate.ID != PLEncounterManager.Instance.PlayerShip.WarpTargetID)
            {
                PLServer.Instance.photonView.RPC("AddCourseGoal", PhotonTargets.All, new object[]
                {
                    nearestWarpGate.ID
                });
                PLServer.Instance.photonView.RPC("AddCourseGoal", PhotonTargets.All, new object[]
                {
                    nearestDestiny.ID
                });
            }
            else if (nearestDestiny != PLServer.GetCurrentSector() && nearestDestiny.ID != PLEncounterManager.Instance.PlayerShip.WarpTargetID)
            {
                PLServer.Instance.photonView.RPC("AddCourseGoal", PhotonTargets.All, new object[]
                {
                    nearestDestiny.ID
                });
            }
        }
    }

    [HarmonyPatch(typeof(PLController), "Update")]
    class SitInChair
    {
        static void Postfix(PLController __instance)
        {
            if (__instance.MyPawn == null || __instance.MyPawn.MyPlayer == null || !__instance.MyPawn.MyPlayer.IsBot || __instance.MyPawn.MyPlayer.GetClassID() != 0 || __instance.MyPawn.MyPlayer.StartingShip == null) return;
            if (__instance.MyPawn.MyPlayer.StartingShip.CaptainsChairPlayerID == __instance.MyPawn.MyPlayer.GetPlayerID())
            {
                PLCaptainsChair shipComponent = __instance.MyPawn.MyPlayer.StartingShip.MyStats.GetShipComponent<PLCaptainsChair>(ESlotType.E_COMP_CAPTAINS_CHAIR, false);
                if (shipComponent != null && shipComponent.MyInstance != null && shipComponent.MyInstance.MalePawnPivot != null)
                {
                    __instance.MyPawn.MyPlayer.GetPawn().transform.position = shipComponent.MyInstance.MalePawnPivot.position;
                    shipComponent.MyInstance.Rot.transform.rotation = __instance.MyPawn.MyPlayer.GetPawn().HorizontalMouseLook.transform.rotation;
                }
            }
            else if (PLLCChair.Instance != null && PLLCChair.Instance.PlayerIDInChair == __instance.MyPawn.GetPlayer().GetPlayerID())
            {
                __instance.MyPawn.transform.position = PLLCChair.Instance.transform.TransformPoint(PLLCChair.Instance.Offset_RootAnimPos);
                __instance.MyPawn.VerticalMouseLook.RotationY = 35f;
                __instance.MyPawn.HorizontalMouseLook.RotationX = PLLCChair.Instance.transform.rotation.eulerAngles.y;
            }
        }
    }

    [HarmonyPatch(typeof(PLUIClassSelectionMenu), "Update")]
    class SpawnBot
    {
        public static float delay = 0f;
        static void Postfix()
        {
            if (PLEncounterManager.Instance.PlayerShip != null && PLServer.Instance.GetCachedFriendlyPlayerOfClass(0, PLEncounterManager.Instance.PlayerShip) == null && delay > 3f)
            {
                PLServer.Instance.ServerAddCrewBotPlayer(0);
                PLServer.Instance.GameHasStarted = true;
                PLServer.Instance.CrewPurchaseLimitsEnabled = false;
            }
            else if (PLEncounterManager.Instance.PlayerShip != null && PLServer.Instance.GetCachedFriendlyPlayerOfClass(0, PLEncounterManager.Instance.PlayerShip) == null) delay += Time.deltaTime;
        }
    }

    [HarmonyPatch(typeof(PLGlobal), "EnterNewGame")]
    class OnJoin
    {
        static void Postfix()
        {
            SpawnBot.delay = 0f;
        }
    }
}
