using System.Collections.Generic;
using UnityEngine;

namespace CapBot.Routines
{
    internal class Captain
    {
        internal static void CaptainOrders(PLPlayer __instance, ref float LastOrder, ref float LastAction, bool HasIntruders)
        {
            //Set captain orders and special actions
            if (__instance.StartingShip == null || __instance.StartingShip.MyFlightAI == null || __instance.StartingShip.MyStats == null) return;
            if (__instance.StartingShip.MyFlightAI.cachedRepairDepotList.Count > 0 && __instance.StartingShip.MyStats.HullCurrent / __instance.StartingShip.MyStats.HullMax < 0.99f)//Repair procedures on repair station
            {
                if (PLServer.Instance.CaptainsOrdersID != 9 && Time.time - LastOrder > 1f)
                {
                    LastOrder = Time.time;
                    PLServer.Instance.CaptainSetOrderID(9);
                }
                __instance.StartingShip.AlertLevel = 0;
                PLRepairDepot repair = __instance.StartingShip.MyFlightAI.cachedRepairDepotList[0];
                if (repair.TargetShip == __instance.StartingShip && !__instance.StartingShip.ShieldIsActive && Time.time - LastAction > 1f)//Uses repair station if possible
                {
                    int ammount = 0;
                    int price = 0;
                    PLRepairDepot.GetAutoPurchaseInfo(__instance.StartingShip, out ammount, out price, 2);
                    PLServer.Instance.ServerRepairHull(__instance.StartingShip.ShipID, ammount, price);
                    repair.photonView.RPC("OnRepairTargetShip", PhotonTargets.All, new object[]
                    {
                        __instance.StartingShip.ShipID
                    });
                    LastAction = Time.time;
                }
            }
            else if (__instance.StartingShip.MyFlightAI.cachedWarpStationList.Count > 0 && __instance.StartingShip.MyFlightAI.cachedWarpStationList[0].IsAligned)//Asks to use the warp gate
            {
                if (PLServer.Instance.CaptainsOrdersID != 8 && Time.time - LastOrder > 1f)
                {
                    LastOrder = Time.time;
                    PLServer.Instance.CaptainSetOrderID(8);
                }
                __instance.StartingShip.AlertLevel = 0;
            }
            else if (__instance.StartingShip != null && HasIntruders) //Repel any intruders
            {
                if (PLServer.Instance.CaptainsOrdersID != 6 && Time.time - LastOrder > 1f)
                {
                    LastOrder = Time.time;
                    PLServer.Instance.CaptainSetOrderID(6);
                }
                __instance.StartingShip.AlertLevel = 2;
            }
            else if ((__instance.StartingShip.TargetShip != null && __instance.StartingShip.TargetShip != __instance.StartingShip) || __instance.StartingShip.TargetSpaceTarget != null)//Kill enemies
            {
                if (PLServer.Instance.CaptainsOrdersID != 4 && Time.time - LastOrder > 1f)
                {
                    LastOrder = Time.time;
                    PLServer.Instance.CaptainSetOrderID(4);
                }
                __instance.StartingShip.AlertLevel = 2;
            }
            else if (PLServer.GetCurrentSector().MySPI.HasPlanet && __instance.StartingShip != null)//Explore planet
            {
                if (PLServer.Instance.CaptainsOrdersID != 12 && Time.time - LastOrder > 1f)
                {
                    LastOrder = Time.time;
                    PLServer.Instance.CaptainSetOrderID(12);
                }
                if (SpawnBot.crewisbot || (PLServer.Instance.GetCachedFriendlyPlayerOfClass(2) != null && PLServer.Instance.GetCachedFriendlyPlayerOfClass(2).IsBot))
                {
                    List<PLPawnBase> targets = new List<PLPawnBase>();
                    List<PLPickupObject> pickupTargets = new List<PLPickupObject>();
                    List<PLPickupComponent> componentsTargets = new List<PLPickupComponent>();
                    foreach (PLMissionBase mission in PLServer.Instance.AllMissions)
                    {
                        if (!mission.Ended && !mission.Abandoned)
                        {
                            foreach (PLMissionObjective objective in mission.Objectives)
                            {
                                if (!objective.IsCompleted)
                                {
                                    if (objective is PLMissionObjective_KillEnemyOfName)
                                    {
                                        foreach (PLPawnBase target in PLGameStatic.Instance.AllPawnBases)
                                        {
                                            if (target.GetPlayer() != null && target.GetPlayer().GetPlayerName(false) == (objective as PLMissionObjective_KillEnemyOfName).EnemyName)
                                            {
                                                targets.Add(target);
                                            }
                                        }
                                    }
                                    else if (objective is PLMissionObjective_KillEnemyOfType)
                                    {
                                        foreach (PLPawnBase target in PLGameStatic.Instance.AllPawnBases)
                                        {
                                            if (target.PawnType == (objective as PLMissionObjective_KillEnemyOfType).EnemyType)
                                            {
                                                targets.Add(target);
                                            }
                                        }
                                    }
                                    else if (objective is PLMissionObjective_ReachSectorOfType && (objective as PLMissionObjective_ReachSectorOfType).MustKillAllEnemies)
                                    {
                                        foreach (PLPawnBase target in PLGameStatic.Instance.AllPawnBases)
                                        {
                                            if (target.GetPlayer() != null && target.GetPlayer().name == "PreviewPlayer" || target.IsDead || target.GetIsFriendly()) continue;
                                            if (target.CurrentShip != null && target.CurrentShip != __instance.StartingShip && (((target is PLPawn) && (target as PLPawn).TeamID != 0) || target.GetPlayer() == null || target.GetPlayer().TeamID != 0))
                                            {
                                                __instance.StartingShip.AddHostileShip(target.CurrentShip);
                                            }
                                            else if ((((target is PLPawn) && (target as PLPawn).TeamID != 0) || target.GetPlayer() == null || target.GetPlayer().TeamID != 0))
                                            {
                                                targets.Add(target);
                                            }
                                        }
                                    }
                                    else if (objective is PLMissionObjective_PickupItem && (SpawnBot.crewisbot || (PLServer.Instance.GetCachedFriendlyPlayerOfClass(2) != null && PLServer.Instance.GetCachedFriendlyPlayerOfClass(2).IsBot && PLServer.Instance.GetCachedFriendlyPlayerOfClass(2).Talents[34] == 1)))
                                    {
                                        foreach (PLPickupObject inObj in PLGameStatic.Instance.m_AllPickupObjects)
                                        {
                                            if (inObj.ItemType == (objective as PLMissionObjective_PickupItem).ItemTypeToPickup && inObj.SubItemType == (objective as PLMissionObjective_PickupItem).SubItemType && !inObj.PickedUp)
                                            {
                                                pickupTargets.Add(inObj);
                                            }
                                        }
                                    }
                                    else if (objective is PLMissionObjective_PickupComponent && (SpawnBot.crewisbot || (PLServer.Instance.GetCachedFriendlyPlayerOfClass(2) != null && PLServer.Instance.GetCachedFriendlyPlayerOfClass(2).IsBot && PLServer.Instance.GetCachedFriendlyPlayerOfClass(2).Talents[34] == 1)))
                                    {
                                        foreach (PLPickupComponent component in Object.FindObjectsOfType(typeof(PLPickupComponent)))
                                        {
                                            if (!component.PickedUp && component.ItemType == (objective as PLMissionObjective_PickupComponent).CompType && component.SubItemType == (objective as PLMissionObjective_PickupComponent).SubType)
                                            {
                                                componentsTargets.Add(component);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if (targets.Count > 0)
                    {
                        float distance = (targets[0].transform.position - __instance.GetPawn().transform.position).magnitude;
                        PLPawnBase target = targets[0];
                        foreach (PLPawnBase pawn in targets)
                        {
                            if ((pawn.transform.position - __instance.GetPawn().transform.position).magnitude < distance)
                            {
                                distance = (pawn.transform.position - __instance.GetPawn().transform.position).magnitude;
                                target = pawn;
                            }
                        }
                        __instance.MyBot.AI_TargetPos = target.transform.position;
                        __instance.MyBot.AI_TargetPos_Raw = __instance.MyBot.AI_TargetPos;
                        __instance.MyBot.AI_TargetTLI = target.MyCurrentTLI;
                        __instance.MyBot.AI_TargetInterior = target.MyInterior;
                        __instance.MyBot.EnablePathing = true;
                        PLServer.Instance.photonView.RPC("ClearCourseGoals", PhotonTargets.All, new object[0]);
                        return;
                    }
                    else if (pickupTargets.Count > 0)
                    {
                        float distance = (pickupTargets[0].transform.position - __instance.GetPawn().transform.position).magnitude;
                        PLPickupObject target = pickupTargets[0];
                        foreach (PLPickupObject item in pickupTargets)
                        {
                            if ((item.transform.position - __instance.GetPawn().transform.position).magnitude < distance)
                            {
                                distance = (item.transform.position - __instance.GetPawn().transform.position).magnitude;
                                target = item;
                            }
                        }
                        __instance.MyBot.AI_TargetPos = target.transform.position;
                        __instance.MyBot.AI_TargetPos_Raw = __instance.MyBot.AI_TargetPos;
                        foreach (PLTeleportationLocationInstance teleport in Object.FindObjectsOfType(typeof(PLTeleportationLocationInstance)))
                        {
                            if (teleport.name == "PLGamePlanet" || teleport.name == "PL_GamePlanet" || teleport.name == "PLGame")
                            {
                                __instance.MyBot.AI_TargetTLI = teleport;
                                break;
                            }
                        }
                        __instance.MyBot.AI_TargetInterior = target.MyInterior;
                        if (distance < 8f)
                        {
                            __instance.photonView.RPC("AttemptToPickupObjectAtID", PhotonTargets.MasterClient, new object[]
                            {
                            target.PickupID
                            });
                            __instance.GetPawn().photonView.RPC("Anim_Pickup", PhotonTargets.Others, new object[0]);
                            PLMusic.PostEvent("play_sx_player_item_pickup", __instance.GetPawn().gameObject);
                        }
                        else __instance.MyBot.EnablePathing = true;
                        PLServer.Instance.photonView.RPC("ClearCourseGoals", PhotonTargets.All, new object[0]);
                        return;
                    }
                    else if (componentsTargets.Count > 0)
                    {
                        float distance = (componentsTargets[0].transform.position - __instance.GetPawn().transform.position).magnitude;
                        PLPickupComponent target = componentsTargets[0];
                        foreach (PLPickupComponent component in componentsTargets)
                        {
                            if ((component.transform.position - __instance.GetPawn().transform.position).magnitude < distance)
                            {
                                distance = (component.transform.position - __instance.GetPawn().transform.position).magnitude;
                                target = component;
                            }
                        }
                        __instance.MyBot.AI_TargetPos = target.transform.position;
                        __instance.MyBot.AI_TargetPos_Raw = __instance.MyBot.AI_TargetPos;
                        foreach (PLTeleportationLocationInstance teleport in Object.FindObjectsOfType(typeof(PLTeleportationLocationInstance)))
                        {
                            if (teleport.name == "PLGamePlanet" || teleport.name == "PL_GamePlanet" || teleport.name == "PLGame")
                            {
                                __instance.MyBot.AI_TargetTLI = teleport;
                                break;
                            }
                        }
                        __instance.MyBot.AI_TargetInterior = target.MyInterior;
                        if (distance < 8f)
                        {
                            __instance.photonView.RPC("AttemptToPickupComponentAtID", PhotonTargets.MasterClient, new object[]
                                    {
                                    target.PickupID
                                    });
                            __instance.GetPawn().photonView.RPC("Anim_Pickup", PhotonTargets.Others, new object[0]);
                            PLMusic.PostEvent("play_sx_player_item_pickup_large", __instance.GetPawn().gameObject);
                        }
                        else __instance.MyBot.EnablePathing = true;
                        PLServer.Instance.photonView.RPC("ClearCourseGoals", PhotonTargets.All, new object[0]);
                        return;
                    }
                }
            }
            else if (PLStarmap.Instance.CurrentShipPath.Count > 0 && (__instance.StartingShip.MyFlightAI.cachedWarpStationList.Count == 0 || (!__instance.StartingShip.MyFlightAI.cachedWarpStationList[0].IsAligned && __instance.StartingShip.MyFlightAI.cachedWarpStationList[0].TargetedWarpSectorID == -1)))//Align the ship
            {
                if (PLServer.Instance.CaptainsOrdersID != 10 && Time.time - LastOrder > 1f)
                {
                    LastOrder = Time.time;
                    PLServer.Instance.CaptainSetOrderID(10);
                }
                __instance.StartingShip.AlertLevel = 0;
            }
            else//Just be at atention
            {
                if (PLServer.Instance.CaptainsOrdersID != 1 && Time.time - LastOrder > 1f)
                {
                    LastOrder = Time.time;
                    PLServer.Instance.CaptainSetOrderID(1);
                }
                __instance.StartingShip.AlertLevel = 0;
            }
        }
        internal static void AnswerComms(PLPlayer __instance, ref float LastAction)
        {
            if (__instance.StartingShip.CurrentHailTargetSelection != null)//Handle ship comms
            {
                if (__instance.StartingShip.CurrentHailTargetSelection is PLHailTarget_StartPickupMission)//Accepts any missions from long range comms
                {
                    PLHailTarget_StartPickupMission mission = __instance.StartingShip.CurrentHailTargetSelection as PLHailTarget_StartPickupMission;
                    if (mission.PickupMissionID != -1 && !PLServer.Instance.HasActiveMissionWithID(mission.PickupMissionID))
                    {
                        PLServer.Instance.photonView.RPC("AttemptStartMissionOfTypeID", PhotonTargets.MasterClient, new object[]
                        {
                        mission.PickupMissionID,
                        true
                        });
                        __instance.StartingShip.TargetHailTargetID = -1;
                    }
                }
                if (__instance.StartingShip.CurrentHailTargetSelection is PLHailTarget_Ship && Time.time - LastAction > 3f)//Does dialogue with ships
                {
                    PLHailTarget_Ship ship = __instance.StartingShip.CurrentHailTargetSelection as PLHailTarget_Ship;
                    if (ship.Hostile())
                    {
                        __instance.StartingShip.OnHailChoiceSelected(0, true, false);
                    }
                    else if (PLServer.GetCurrentSector().MissionSpecificID == 20572 && PLServer.Instance.HasActiveMissionWithID(20572) && !PLServer.Instance.GetMissionWithID(20572).Ended && PLEncounterManager.Instance.PlayerShip != null && PLEncounterManager.Instance.PlayerShip.NumberOfFuelCapsules > 1)
                    {
                        __instance.StartingShip.OnHailChoiceSelected(0, true, false);
                    }
                    LastAction = Time.time;
                }
            }
        }
        internal static bool GetSectorMissions(PLPlayer CapBot, ref float LastOrder, ref float LastDestiny)
        {
            List<PLDialogueActorInstance> allNPC = new List<PLDialogueActorInstance>();
            foreach (PLDialogueActorInstance pLDialogueActorInstance in Object.FindObjectsOfType<PLDialogueActorInstance>()) //Finds all NPCs that have mission (with exception of Explorer's appeal)
            {
                if (pLDialogueActorInstance.AllAvailableChoices().Count <= 0 && pLDialogueActorInstance.CurrentLine == null && pLDialogueActorInstance.ActorTypeData != null)
                {
                    if (pLDialogueActorInstance.ActorTypeData.OpeningLines.Count > 0)
                    {
                        foreach (LineData lineData in pLDialogueActorInstance.ActorTypeData.OpeningLines)
                        {
                            if (lineData != null && lineData.PassesRequirements(pLDialogueActorInstance, null, null))
                            {
                                pLDialogueActorInstance.CurrentLine = lineData;
                                break;
                            }
                        }
                    }
                }
                if ((pLDialogueActorInstance.HasMissionStartAvailable && pLDialogueActorInstance.DisplayName == "Eldon Gatra") || (pLDialogueActorInstance.DisplayName == "Commander Darine Hatham" && (!PLServer.Instance.HasActiveMissionWithID(48632))) || pLDialogueActorInstance.DisplayName.ToLower().Contains("baris")
                    || pLDialogueActorInstance.DisplayName.ToLower().Contains("zeng") || (pLDialogueActorInstance.HasMissionStartAvailable && pLDialogueActorInstance.DisplayName.ToLower().Contains("zesho")) || pLDialogueActorInstance.DisplayName.ToLower().Contains("eikeni")) continue;
                if (!pLDialogueActorInstance.ShipDialogue && (pLDialogueActorInstance.HasMissionStartAvailable && pLDialogueActorInstance.AllAvailableChoices().Count > 0) || pLDialogueActorInstance.HasMissionEndAvailable)
                {
                    allNPC.Add(pLDialogueActorInstance);
                }
            }
            if (allNPC.Count > 0) //if there is at least 1 mission to gather or deliver
            {
                if (PLServer.Instance.CaptainsOrdersID != 11 && Time.time - LastOrder > 1f)
                {
                    LastOrder = Time.time;
                    PLServer.Instance.CaptainSetOrderID(11);
                }
                float NearestNPCDistance = (allNPC[0].gameObject.transform.position - CapBot.GetPawn().transform.position).magnitude;
                CapBot.MyBot.AI_TargetPos = allNPC[0].gameObject.transform.position;
                CapBot.MyBot.AI_TargetPos_Raw = CapBot.MyBot.AI_TargetPos;
                PLDialogueActorInstance targetNPC = allNPC[0];
                foreach (PLTeleportationLocationInstance teleport in Object.FindObjectsOfType<PLTeleportationLocationInstance>())
                {
                    if (teleport.name == "PLGamePlanet" || teleport.name == "PL_GamePlanet" || teleport.name == "PLGame")
                    {
                        CapBot.MyBot.AI_TargetTLI = teleport;
                        break;
                    }
                }
                foreach (PLDialogueActorInstance pLDialogueActorInstance in allNPC)
                {
                    if (pLDialogueActorInstance.DisplayName.ToLower().Contains("yiria") && pLDialogueActorInstance.HasMissionStartAvailable && (pLDialogueActorInstance.AllAvailableChoices().Count < 2 || (pLDialogueActorInstance.AllAvailableChoices()[0].ChildLines.Count <= 1 && pLDialogueActorInstance.AllAvailableChoices()[1].ChildLines.Count <= 1))) continue;
                    if ((pLDialogueActorInstance.gameObject.transform.position - CapBot.GetPawn().transform.position).magnitude < NearestNPCDistance)
                    {
                        NearestNPCDistance = (pLDialogueActorInstance.gameObject.transform.position - CapBot.GetPawn().transform.position).magnitude;
                        CapBot.MyBot.AI_TargetPos = pLDialogueActorInstance.gameObject.transform.position;
                        CapBot.MyBot.AI_TargetPos_Raw = CapBot.MyBot.AI_TargetPos;
                        targetNPC = pLDialogueActorInstance;
                    }
                }
                if ((CapBot.MyBot.AI_TargetPos - CapBot.GetPawn().transform.position).sqrMagnitude > 8)
                {
                    CapBot.MyBot.EnablePathing = true;
                }
                else if (targetNPC.HasMissionStartAvailable)
                {
                    LineData currentDiologue = targetNPC.AllAvailableChoices()[0];
                    if (targetNPC.DisplayName.ToLower().Contains("yiria"))
                    {
                        if (targetNPC.AllAvailableChoices()[0].ChildLines.Count > 1)
                        {
                            while ((currentDiologue.TextOptions.Count <= 0 || currentDiologue.TextOptions[0].ToLower() != "accept") && currentDiologue.ChildLines.Count > 0)
                            {
                                currentDiologue = currentDiologue.ChildLines[0];
                            }
                        }
                        else
                        {
                            currentDiologue = targetNPC.AllAvailableChoices()[1];
                            while ((currentDiologue.TextOptions.Count <= 0 || currentDiologue.TextOptions[0].ToLower() != "accept") && currentDiologue.ChildLines.Count > 0)
                            {
                                currentDiologue = currentDiologue.ChildLines[0];
                            }
                        }
                        targetNPC.SelectChoice(currentDiologue, true, true);
                    }
                    else if (targetNPC.DisplayName.ToLower().Contains("bomy"))
                    {
                        targetNPC.SelectChoice(targetNPC.AllAvailableChoices()[0], true, true);
                    }
                    else if (targetNPC.DisplayName.ToLower().Contains("oskal"))
                    {
                        while (currentDiologue.ChildLines.Count > 0)
                        {
                            currentDiologue = currentDiologue.ChildLines[0];
                        }
                        targetNPC.SelectChoice(currentDiologue, true, true);
                    }
                    else
                    {
                        while ((currentDiologue.TextOptions.Count <= 0 || currentDiologue.TextOptions[0].ToLower() != "accept") && currentDiologue.ChildLines.Count > 0)
                        {
                            currentDiologue = currentDiologue.ChildLines[0];
                        }
                        if (currentDiologue.TextOptions[0].ToLower() == "accept")
                        {
                            targetNPC.SelectChoice(currentDiologue, true, true);
                        }
                    }


                }
                else if (targetNPC.HasMissionEndAvailable)
                {
                    if (targetNPC.AllAvailableChoices().Count > 0)
                    {
                        targetNPC.SelectChoice(targetNPC.AllAvailableChoices()[0], true, true);
                    }
                    try
                    {
                        targetNPC.BeginDialogue();
                    }
                    catch { }
                }
                return true;
            }
            return false;
        }
        internal static void IdleInChair(PLPlayer CapBot, ref float LastAction)
        {
            if (CapBot.StartingShip != null && CapBot.StartingShip.MyStats.GetShipComponent<PLCaptainsChair>(ESlotType.E_COMP_CAPTAINS_CHAIR, false) != null && Time.time - LastAction > 20f) //Sit in chair
            {
                //If there is nothing to do captain will sit in the chair
                CapBot.MyBot.AI_TargetPos = CapBot.StartingShip.CaptainsChairPivot.position;
                CapBot.MyBot.AI_TargetPos_Raw = CapBot.MyBot.AI_TargetPos;
                CapBot.MyBot.AI_TargetTLI = CapBot.StartingShip.MyTLI;
                if ((CapBot.StartingShip.CaptainsChairPivot.position - CapBot.GetPawn().transform.position).sqrMagnitude > 4)
                {
                    CapBot.MyBot.EnablePathing = true;
                }
                else
                {
                    if (CapBot.StartingShip.CaptainsChairPlayerID != CapBot.GetPlayerID())
                    {
                        CapBot.StartingShip.AttemptToSitInCaptainsChair(CapBot.GetPlayerID());
                    }
                }

            }
        }
        static bool IsRandomDestiny = false;
        static float LastWarpGateUse = Time.time;
        static float LastMapUpdate = Time.time;
        internal static void UpdateWarpPath(PLPlayer CapBot)
        {
            if ((PLServer.Instance.m_ShipCourseGoals.Count == 0 || Time.time - LastMapUpdate > 15) && (!IsRandomDestiny || (PLServer.Instance.m_ShipCourseGoals.Count > 0 && (PLServer.Instance.m_ShipCourseGoals[0] == PLServer.GetCurrentSector().ID || (PLGlobal.Instance.Galaxy.AllSectorInfos[PLServer.Instance.m_ShipCourseGoals[0]].Position - PLServer.GetCurrentSector().Position).magnitude > CapBot.StartingShip.MyStats.WarpRange) && (PLServer.GetCurrentSector() != null && PLServer.GetCurrentSector().VisualIndication != ESectorVisualIndication.STOP_ASTEROID_ENCOUNTER))))
            {
                //Updates the map destines
                if (PLServer.Instance.m_ShipCourseGoals.Count == 0) IsRandomDestiny = false;
                PLServer.Instance.photonView.RPC("ClearCourseGoals", PhotonTargets.All, new object[0]);
                PlotWarpPath();
                if (PLServer.Instance.m_ShipCourseGoals.Count > 0 && PLServer.Instance.m_ShipCourseGoals[0] == PLServer.GetCurrentSector().ID)
                {
                    PLServer.Instance.photonView.RPC("RemoveCourseGoal", PhotonTargets.All, new object[]
                    {
                    PLServer.Instance.m_ShipCourseGoals[0]
                    });
                }
            }
        }
        static void PlotWarpPath()
        {
            if (PLEncounterManager.Instance.PlayerShip == null) return;
            List<PLSectorInfo> destines = new List<PLSectorInfo>();
            List<PLSectorInfo> priorityDestines = new List<PLSectorInfo>();
            PLSectorInfo GWG = PLGlobal.Instance.Galaxy.GetSectorOfVisualIndication(ESectorVisualIndication.GWG);
            float nearestWarpGatedist = 500;
            PLSectorInfo nearestWarpGate = null;
            PLSectorInfo nearestWarpGatetoDest = null;
            PLSectorInfo nearestDestiny = null;
            if (PLEncounterManager.Instance.PlayerShip.GetCombatLevel() > 80 && PLServer.Instance.GetNumFragmentsCollected() >= 4 && PLServer.Instance.CurrentCrewLevel >= 10)
            {
                destines.Add(GWG);
            }
            else if (PLEncounterManager.Instance.PlayerShip.ShipTypeID == EShipType.E_POLYTECH_SHIP && PLServer.Instance.PTCountdownArmed && (PLServer.Instance.PTCountdownTime <= 600 || PLEncounterManager.Instance.PlayerShip.GetCombatLevel() >= 80))
            {
                destines.Add(PLGlobal.Instance.Galaxy.GetSectorOfVisualIndication(ESectorVisualIndication.PT_WARP_GATE));
            }
            else
            {
                foreach (PLMissionBase mission in PLServer.Instance.AllMissions) //Add mission sectors not visited and not completed
                {
                    if (!mission.Ended && !mission.Abandoned)
                    {
                        bool isPriority = false;
                        bool foundSector = false;
                        List<int> sectors = new List<int>();
                        foreach (PLMissionObjective objective in mission.Objectives)
                        {
                            if (objective is PLMissionObjective_CompleteWithinJumpCount)
                            {
                                isPriority = true;
                            }
                            else if (objective is PLMissionObjective_ReachSector && !objective.IsCompleted)
                            {
                                sectors.Add((objective as PLMissionObjective_ReachSector).SectorToReach);
                            }
                        }
                        foreach (PLSectorInfo plsectorInfo in PLGlobal.Instance.Galaxy.AllSectorInfos.Values)
                        {
                            if (plsectorInfo.MissionSpecificID == mission.MissionTypeID && plsectorInfo != GWG && !plsectorInfo.Visited && PLStarmap.ShouldShowSector(plsectorInfo))
                            {
                                if (isPriority) priorityDestines.Add(plsectorInfo);
                                else destines.Add(plsectorInfo);
                                foundSector = true;
                                break;
                            }
                        }
                        if (!foundSector && sectors.Count > 0)
                        {
                            if (isPriority) priorityDestines.Add(PLGlobal.Instance.Galaxy.AllSectorInfos[sectors[0]]);
                            else destines.Add(PLGlobal.Instance.Galaxy.AllSectorInfos[sectors[0]]);
                        }
                        switch (mission.MissionTypeID)
                        {
                            case 0:
                                if (mission.Objectives[0].IsCompleted && mission.Objectives[1].IsCompleted && mission.Objectives[2].IsCompleted)
                                {
                                    destines.Add(PLGlobal.Instance.Galaxy.AllSectorInfos[0]);
                                }
                                break;
                            case 25:
                            case 68:
                            case 71:
                            case 72:
                            case 780:
                            case 2437:
                            case 2580:
                            case 104851:
                                if (mission.Objectives[0].IsCompleted && mission.Objectives[1].IsCompleted)
                                {
                                    destines.Add(PLGlobal.Instance.Galaxy.AllSectorInfos[0]);
                                }
                                break;
                            case 69:
                            case 264:
                            case 683:
                                if (mission.Objectives[0].IsCompleted)
                                {
                                    destines.Add(PLGlobal.Instance.Galaxy.AllSectorInfos[0]);
                                }
                                break;
                        }
                    }
                }
                if (priorityDestines.Count > 0)
                {
                    destines.Clear();
                    destines.AddRange(priorityDestines);
                }
                if (destines.Count == 0) //Add more destines if you are not going to any mission
                {
                    foreach (PLSectorInfo plsectorInfo in PLGlobal.Instance.Galaxy.AllSectorInfos.Values)
                    {
                        if (PLEncounterManager.Instance.PlayerShip.MyStats.ThrustOutputMax >= 0.35 && !PLServer.Instance.IsFragmentCollected(10) && PLServer.Instance.CurrentCrewLevel >= 4) //Add races to possible destinations
                        {
                            if ((PLServer.Instance.RacesWonBitfield & 1) == 0 && plsectorInfo.VisualIndication == ESectorVisualIndication.RACING_SECTOR)
                            {
                                destines.Add(plsectorInfo);
                            }
                            if ((PLServer.Instance.RacesWonBitfield & 2) == 0 && plsectorInfo.VisualIndication == ESectorVisualIndication.RACING_SECTOR_2)
                            {
                                destines.Add(plsectorInfo);
                            }
                            if ((PLServer.Instance.RacesWonBitfield & 1) != 0 && (PLServer.Instance.RacesWonBitfield & 2) != 0 && (PLServer.Instance.RacesWonBitfield & 4) == 0 && plsectorInfo.VisualIndication == ESectorVisualIndication.RACING_SECTOR_3)
                            {
                                destines.Add(plsectorInfo);
                            }
                        }
                        if (!PLServer.Instance.IsFragmentCollected(1) && (PLServer.Instance.CurrentCrewCredits >= 100000 || (PLServer.Instance.CurrentCrewCredits >= 50000 && PLServer.Instance.CurrentCrewLevel >= 5)) && plsectorInfo.VisualIndication == ESectorVisualIndication.DESERT_HUB)
                        //Add burrow to possible destinations
                        {
                            destines.Add(plsectorInfo);
                        }
                        if (PLServer.Instance.HasActiveMissionWithID(104869) && !PLServer.Instance.GetMissionWithID(104869).Ended && plsectorInfo.VisualIndication == ESectorVisualIndication.GREY_HUNTSMAN_HQ) //Add bounty hunter agency to collect fragment
                        {
                            destines.Add(plsectorInfo);
                        }
                        if (PLServer.Instance.HasActiveMissionWithID(102403) && !PLServer.Instance.IsFragmentCollected(3) && plsectorInfo.VisualIndication == ESectorVisualIndication.HIGHROLLERS_STATION && PLServer.Instance.CurrentCrewCredits >= 10000) //Add high rollers
                        {
                            destines.Add(plsectorInfo);
                        }
                        if (PLServer.Instance.CurrentCrewCredits >= 80000 && plsectorInfo.VisualIndication == ESectorVisualIndication.SPACE_SCRAPYARD && !plsectorInfo.Visited) //Add not visited scrapyards
                        {
                            destines.Add(plsectorInfo);
                        }
                    }
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
            if (PLEncounterManager.Instance.PlayerShip.MyStats.HullCurrent / PLEncounterManager.Instance.PlayerShip.MyStats.HullMax < 0.6 || PLEncounterManager.Instance.PlayerShip.NumberOfFuelCapsules <= 10 || PLEncounterManager.Instance.PlayerShip.ReactorCoolantLevelPercent <= 0.25)
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
            if (nearestDestiny != null) IsRandomDestiny = false;
            if (nearestDestiny == null && PLServer.Instance.CurrentCrewLevel >= 4 && !IsRandomDestiny)
            {
                List<PLSectorInfo> random = new List<PLSectorInfo>();
                PLSectorInfo nearestPlanet = null;
                foreach (PLSectorInfo plsectorInfo in PLGlobal.Instance.Galaxy.AllSectorInfos.Values) //finds near random sectors
                {
                    if ((plsectorInfo.Position - PLServer.GetCurrentSector().Position).magnitude <= PLEncounterManager.Instance.PlayerShip.MyStats.WarpRange && !plsectorInfo.Visited && plsectorInfo.MySPI.Faction != 4 && plsectorInfo != PLServer.GetCurrentSector() && PLStarmap.ShouldShowSector(plsectorInfo))
                    {
                        random.Add(plsectorInfo);
                        PLPersistantEncounterInstance plpersistantEncounterInstance = PLEncounterManager.Instance.CreatePersistantEncounterInstanceOfID(plsectorInfo.ID, false);
                        if (plpersistantEncounterInstance != null && plpersistantEncounterInstance is PLPersistantPlanetEncounterInstance && plsectorInfo != PLGlobal.Instance.Galaxy.GetSectorOfVisualIndication(ESectorVisualIndication.TOPSEC))
                        {
                            if (nearestPlanet == null)
                            {
                                nearestPlanet = plsectorInfo;
                            }
                            else if ((plsectorInfo.Position - PLServer.GetCurrentSector().Position).magnitude < (nearestPlanet.Position - PLServer.GetCurrentSector().Position).magnitude)
                            {
                                nearestPlanet = plsectorInfo;
                            }
                        }
                    }
                }
                if (random.Count == 0) return;
                nearestDestiny = random[Random.Range(0, random.Count - 1)];
                if (nearestPlanet != null)
                {
                    nearestDestiny = nearestPlanet;
                }
                IsRandomDestiny = true;
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
            if (PLGlobal.Instance.Galaxy.GetPathToSector(PLServer.GetCurrentSector(), nearestWarpGate).Count + 1 + PLGlobal.Instance.Galaxy.GetPathToSector(nearestDestiny, nearestWarpGatetoDest).Count < PLGlobal.Instance.Galaxy.GetPathToSector(PLServer.GetCurrentSector(), nearestDestiny).Count)
            {
                PLWarpStation warpGate = null;
                if (PLEncounterManager.Instance.PlayerShip.MyFlightAI.cachedWarpStationList.Count > 0)
                {
                    warpGate = PLEncounterManager.Instance.PlayerShip.MyFlightAI.cachedWarpStationList[0];
                }
                if (nearestWarpGate != PLServer.GetCurrentSector() && (nearestWarpGate.ID != PLEncounterManager.Instance.PlayerShip.WarpTargetID || !PLEncounterManager.Instance.PlayerShip.InWarp))
                {
                    PLServer.Instance.photonView.RPC("AddCourseGoal", PhotonTargets.All, new object[]
                    {
                    nearestWarpGate.ID
                    });
                }
                else if (warpGate != null && warpGate.GetPriceForSectorID(nearestWarpGatetoDest.ID) <= PLServer.Instance.CurrentCrewCredits && !warpGate.IsAligned && Time.time - LastWarpGateUse > 30 && PLServer.GetCurrentSector() != nearestWarpGatetoDest && !PLEncounterManager.Instance.PlayerShip.InWarp)
                {
                    warpGate.photonView.RPC("SetTargetedSectorID", PhotonTargets.All, new object[]
                    {
                        nearestWarpGatetoDest.ID,
                        true
                    });
                    LastWarpGateUse = Time.time;
                }
                if (warpGate != null && warpGate.IsAligned) LastWarpGateUse = Time.time;
                if (nearestDestiny != PLServer.GetCurrentSector() && (nearestDestiny.ID != PLEncounterManager.Instance.PlayerShip.WarpTargetID || !PLEncounterManager.Instance.PlayerShip.InWarp))
                {
                    PLServer.Instance.photonView.RPC("AddCourseGoal", PhotonTargets.All, new object[]
                    {
                    nearestDestiny.ID
                    });
                }
            }
            else if (nearestDestiny != PLServer.GetCurrentSector() && (nearestDestiny.ID != PLEncounterManager.Instance.PlayerShip.WarpTargetID || !PLEncounterManager.Instance.PlayerShip.InWarp))
            {
                PLServer.Instance.photonView.RPC("AddCourseGoal", PhotonTargets.All, new object[]
                {
                    nearestDestiny.ID
                });
            }
        }
        static float LastBlindJump = 0;
        internal static void EmergencyBlindJump(PLPlayer CapBot, ref float LastAction)
        {
            if ((CapBot.StartingShip.HostileShips.Count > 1 || (CapBot.StartingShip.TargetShip != null && CapBot.StartingShip.TargetShip.GetCombatLevel() > CapBot.StartingShip.GetCombatLevel())) && CapBot.StartingShip.MyStats.HullCurrent / CapBot.StartingShip.MyStats.HullMax < 0.2f && !CapBot.StartingShip.InWarp && Time.time - LastBlindJump > 60)
            {
                //Blind jump in emergency
                CapBot.MyBot.AI_TargetPos = (CapBot.StartingShip.Spawners[4] as GameObject).transform.position;
                CapBot.MyBot.AI_TargetPos_Raw = CapBot.MyBot.AI_TargetPos;
                CapBot.MyBot.AI_TargetTLI = CapBot.StartingShip.MyTLI;
                if ((CapBot.MyBot.AI_TargetPos - CapBot.GetPawn().transform.position).sqrMagnitude > 4)
                {
                    CapBot.MyBot.EnablePathing = true;
                }
                else
                {
                    CapBot.StartingShip.BlindJumpUnlocked = true;
                    PLServer.Instance.photonView.RpcSecure("AttemptBlindJump", PhotonTargets.MasterClient, true, new object[]
                    {
                        CapBot.StartingShip.ShipID,
                        CapBot.GetPlayerID()
                    });
                    LastBlindJump = Time.time;
                }
                LastAction = Time.time;
            }
        }
    }
}
