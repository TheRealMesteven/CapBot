using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static EventDelegate;

namespace CapBot.Routines
{
    internal class Missions
    {
        static Vector3 targetPos = Vector3.zero;
        static List<Vector3> targets = new List<Vector3>();
        internal static void LostColony(PLPlayer CapBot, ref float LastDestiny)
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
                if (targets.Count == 0)
                {
                    targets = new List<Vector3>()
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
                    AI.AI_TargetPos = targets[Random.Range(0, targets.Count)];
                    AI.AI_TargetPos_Raw = AI.AI_TargetPos;
                    targetPos = AI.AI_TargetPos;
                }
                if ((targetPos - pawn.transform.position).magnitude <= 3f)
                {
                    targets.Remove(targetPos);
                    if (targets.Count > 0)
                    {
                        AI.AI_TargetPos = targets[Random.Range(0, targets.Count - 1)];
                        AI.AI_TargetPos_Raw = AI.AI_TargetPos;
                        targetPos = AI.AI_TargetPos;
                    }
                }
                else
                {
                    AI.AI_TargetPos = targetPos;
                    AI.AI_TargetPos_Raw = AI.AI_TargetPos;
                }
            }
            else if (!PLServer.AnyPlayerHasItemOfName("Lower Facilities Keycard")) //Step 2: Find lower facility key
            {
                if (targets.Count == 0)
                {
                    targets = new List<Vector3>()
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
                    AI.AI_TargetPos = targets[Random.Range(0, targets.Count)];
                    AI.AI_TargetPos_Raw = AI.AI_TargetPos;
                    targetPos = AI.AI_TargetPos;
                }
                if ((targetPos - pawn.transform.position).magnitude <= 3f)
                {
                    targets.Remove(targetPos);
                    if (targets.Count > 0)
                    {
                        AI.AI_TargetPos = targets[Random.Range(0, targets.Count)];
                        AI.AI_TargetPos_Raw = AI.AI_TargetPos;
                        targetPos = AI.AI_TargetPos;
                    }
                }
                else
                {
                    AI.AI_TargetPos = targetPos;
                    AI.AI_TargetPos_Raw = AI.AI_TargetPos;
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
                    PulsarModLoader.Utilities.Messaging.ChatMessage(PhotonTargets.All, $"I got the {item.GetItemName(true)}", CapBot.GetPlayerID());
                    targets.Clear();
                }
            }
        }
        internal static void WarpGuardianBattle(PLPlayer CapBot)
        {
            if (PLWarpGuardian.Instance == null) return;
            if (PLWarpGuardian.Instance.GetCurrentPhase() == 1)
            {
                if (!PLWarpGuardian.Instance.BottomArmor.Destroyed)
                {
                    PLEncounterManager.Instance.PlayerShip.TargetSpaceTarget = PLWarpGuardian.Instance.BottomArmor;
                }
                else if (!PLWarpGuardian.Instance.Core.Destroyed)
                {
                    PLEncounterManager.Instance.PlayerShip.TargetSpaceTarget = PLWarpGuardian.Instance.Core;
                }
                else if (!PLWarpGuardian.Instance.HeadBeamWeapon.Destroyed)
                {
                    PLEncounterManager.Instance.PlayerShip.TargetSpaceTarget = PLWarpGuardian.Instance.HeadBeamWeapon;
                }
                else if (!PLWarpGuardian.Instance.SideEnergyProjWeapon.Destroyed)
                {
                    PLEncounterManager.Instance.PlayerShip.TargetSpaceTarget = PLWarpGuardian.Instance.SideEnergyProjWeapon;
                }
            }
            else
            {
                if (!PLWarpGuardian.Instance.BoardingSystem.Destroyed)
                {
                    PLEncounterManager.Instance.PlayerShip.TargetSpaceTarget = PLWarpGuardian.Instance.BoardingSystem;
                }
                else if (!PLWarpGuardian.Instance.SideCannonModule.Destroyed)
                {
                    PLEncounterManager.Instance.PlayerShip.TargetSpaceTarget = PLWarpGuardian.Instance.SideCannonModule;
                }
                else if (!PLWarpGuardian.Instance.BottomArmor.Destroyed)
                {
                    PLEncounterManager.Instance.PlayerShip.TargetSpaceTarget = PLWarpGuardian.Instance.BottomArmor;
                }
                else if (!PLWarpGuardian.Instance.Core.Destroyed)
                {
                    PLEncounterManager.Instance.PlayerShip.TargetSpaceTarget = PLWarpGuardian.Instance.Core;
                }
                else if (!PLWarpGuardian.Instance.HeadBeamWeapon.Destroyed)
                {
                    PLEncounterManager.Instance.PlayerShip.TargetSpaceTarget = PLWarpGuardian.Instance.HeadBeamWeapon;
                }
                else if (!PLWarpGuardian.Instance.SideEnergyProjWeapon.Destroyed)
                {
                    PLEncounterManager.Instance.PlayerShip.TargetSpaceTarget = PLWarpGuardian.Instance.SideEnergyProjWeapon;
                }
                else if (!PLWarpGuardian.Instance.BoostModule.Destroyed)
                {
                    PLEncounterManager.Instance.PlayerShip.TargetSpaceTarget = PLWarpGuardian.Instance.BoostModule;
                }
                else if (!PLWarpGuardian.Instance.ModuleRepairModule.Destroyed)
                {
                    PLEncounterManager.Instance.PlayerShip.TargetSpaceTarget = PLWarpGuardian.Instance.ModuleRepairModule;
                }
                CapBot.ActiveMainPriority = new AIPriority(AIPriorityType.E_MAIN, 2, 1);
                //CapBot.MyBot.TickFindInvaderAction(null);
            }

        }
        internal static void WastedWing(PLPlayer CapBot, ref float LastDestiny)
        {
            PLBot AI = CapBot.MyBot;
            PLPawn pawn = CapBot.GetPawn();
            PLLockedSeamlessDoor EntranceDoor = null;
            PLQuarantineDoor FirstDoor = null;
            PLQuarantineDoor SlimeDoors = null;
            PLRobotWalkerLarge paladin = null;
            float lastChange = Time.time;
            foreach (PLTeleportationLocationInstance teleport in Object.FindObjectsOfType(typeof(PLTeleportationLocationInstance)))
            {
                if (teleport.name == "PLGamePlanet")
                {
                    CapBot.MyBot.AI_TargetTLI = teleport;
                    break;
                }
            }
            foreach (PLQuarantineDoor teleport in Object.FindObjectsOfType(typeof(PLQuarantineDoor)))
            {
                if (teleport.name == "QuarantineDoor1")
                {
                    FirstDoor = teleport;
                    break;
                }
            }
            foreach (PLQuarantineDoor teleport in Object.FindObjectsOfType(typeof(PLQuarantineDoor)))
            {
                if (teleport.name == "QuarantineDoor1 (5)")
                {
                    SlimeDoors = teleport;
                    break;
                }
            }
            foreach (PLLockedSeamlessDoor teleport in Object.FindObjectsOfType(typeof(PLLockedSeamlessDoor)))
            {
                if (teleport.name == "Automated_Doors3 (1)")
                {
                    EntranceDoor = teleport;
                    break;
                }
            }
            foreach (PLRobotWalkerLarge teleport in Object.FindObjectsOfType(typeof(PLRobotWalkerLarge)))
            {
                if (teleport.name.Contains("Clone"))
                {
                    paladin = teleport;
                    break;
                }
            }
            if (!PLServer.AnyPlayerHasItemOfName("Entrance Security Keycard")) //Step 1: Find keycard
            {
                PLRandomChildItem positions = null;
                foreach (PLRandomChildItem teleport in Object.FindObjectsOfType<PLRandomChildItem>(true))
                {
                    if (teleport.name == "KeycardRCI")
                    {
                        positions = teleport;
                        break;
                    }
                }
                if (positions != null && (Time.time - LastDestiny > 60 || targetPos == Vector3.zero))
                {
                    List<GameObject> keycards = new List<GameObject>();
                    foreach (PLPickupObject item in positions.gameObject.GetComponentsInChildren<PLPickupObject>(true))
                    {
                        keycards.Add(item.gameObject);
                    }
                    AI.AI_TargetPos = keycards[Random.Range(0, keycards.Count - 1)].transform.position;
                    AI.AI_TargetPos_Raw = AI.AI_TargetPos;
                    targetPos = AI.AI_TargetPos;
                    LastDestiny = Time.time;
                }
                else if (targetPos != Vector3.zero)
                {
                    AI.AI_TargetPos = targetPos;
                    AI.AI_TargetPos_Raw = AI.AI_TargetPos;
                }
                foreach (PLPickupObject inObj in PLGameStatic.Instance.m_AllPickupObjects)
                {
                    if ((pawn.transform.position - inObj.transform.position).magnitude < 8f && !inObj.PickedUp)
                    {
                        CapBot.photonView.RPC("AttemptToPickupObjectAtID", PhotonTargets.MasterClient, new object[]
                            {
                            inObj.PickupID
                            });
                        CapBot.GetPawn().photonView.RPC("Anim_Pickup", PhotonTargets.Others, new object[0]);
                        PLMusic.PostEvent("play_sx_player_item_pickup", CapBot.GetPawn().gameObject);
                    }
                }
            }
            else if (FirstDoor != null && !FirstDoor.IsDoorOpen && pawn.transform.position.z > -140 && pawn.transform.position.y >= -102)//Step 2: Open the first containment door and entrance door
            {
                AI.AI_TargetPos = new Vector3(58, -103, -97);
                AI.AI_TargetPos_Raw = AI.AI_TargetPos;
                if ((pawn.transform.position - EntranceDoor.transform.position).magnitude < 8f && !EntranceDoor.IsOpen())
                {
                    EntranceDoor.OpenDoor();
                }
            }
            else if (SlimeDoors != null && !SlimeDoors.IsDoorOpen)//Step 3: Kill Experiment 72 
            {
                if (pawn.transform.position.y < -120)
                {
                    if (pawn.Health / pawn.MaxHealth > 0.25f) AI.AI_TargetPos = new Vector3(59, -141, -184);
                    else AI.AI_TargetPos = new Vector3(60, -151, -186);
                    AI.AI_TargetPos_Raw = AI.AI_TargetPos;
                }
                else if (pawn.transform.position.y < -105 && pawn.transform.position.z > -232)
                {
                    AI.AI_TargetPos = new Vector3(52.2f, -121.5f, -198.2f);
                    AI.AI_TargetPos_Raw = AI.AI_TargetPos;
                }
                else if (pawn.transform.position.y < -105)
                {
                    AI.AI_TargetPos = new Vector3(58f, -119f, -231f);
                    AI.AI_TargetPos_Raw = AI.AI_TargetPos;
                }
                else
                {
                    AI.AI_TargetPos = new Vector3(47f, -111f, -248f);
                    AI.AI_TargetPos_Raw = AI.AI_TargetPos;
                }
            }
            else if (!PLServer.AnyPlayerHasItemOfName("Level 1 Admin Access Card"))//Step 4: Find keycard 1 
            {
                PLRandomChildItem positions = null;
                foreach (PLRandomChildItem teleport in Object.FindObjectsOfType(typeof(PLRandomChildItem)))
                {
                    if (teleport.name == "Keycard_ADMIN_Lvl_1_RCI")
                    {
                        positions = teleport;
                        break;
                    }
                }
                if (positions != null && Time.time - LastDestiny > 60)
                {

                    List<GameObject> keycards = new List<GameObject>();
                    foreach (PLPickupObject item in positions.gameObject.GetComponentsInChildren<PLPickupObject>(true))
                    {
                        keycards.Add(item.gameObject);
                    }
                    AI.AI_TargetPos = keycards[Random.Range(0, keycards.Count - 1)].transform.position;
                    AI.AI_TargetPos_Raw = AI.AI_TargetPos;
                    targetPos = AI.AI_TargetPos;
                    LastDestiny = Time.time;
                }
                else if (targetPos != Vector3.zero)
                {
                    AI.AI_TargetPos = targetPos;
                    AI.AI_TargetPos_Raw = AI.AI_TargetPos;
                }
                foreach (PLPickupObject inObj in PLGameStatic.Instance.m_AllPickupObjects)
                {
                    if ((pawn.transform.position - inObj.transform.position).magnitude < 8f && !inObj.PickedUp)
                    {
                        CapBot.photonView.RPC("AttemptToPickupObjectAtID", PhotonTargets.MasterClient, new object[]
                            {
                            inObj.PickupID
                            });
                        CapBot.GetPawn().photonView.RPC("Anim_Pickup", PhotonTargets.Others, new object[0]);
                        PLMusic.PostEvent("play_sx_player_item_pickup", CapBot.GetPawn().gameObject);
                    }
                }
            }
            else if (!PLServer.Instance.HasMissionWithID(55400))//Step 5: Get kill Stalker Mission 
            {

                AI.AI_TargetPos = new Vector3(-12, -151, -176);
                AI.AI_TargetPos_Raw = AI.AI_TargetPos;
                if ((pawn.transform.position - AI.AI_TargetPos).magnitude < 6f)
                {
                    PLServer.Instance.photonView.RPC("AttemptStartMissionOfTypeID", PhotonTargets.MasterClient, new object[]
                        {
                        55400,
                        true
                        });
                }
            }
            else if (!PLServer.Instance.HasCompletedMissionWithID(55400))//Step 6: Kill Stalker 
            {
                AI.AI_TargetPos = new Vector3(23, -233, -88);
                AI.AI_TargetPos_Raw = AI.AI_TargetPos;
            }
            else if (!PLServer.AnyPlayerHasItemOfName("Level 2 Admin Access Card"))//Step 7: Find keycard 2
            {
                PLRandomChildItem positions = null;
                foreach (PLRandomChildItem teleport in Object.FindObjectsOfType(typeof(PLRandomChildItem)))
                {
                    if (teleport.name == "Keycard_ADMIN_Lvl_2_RCI")
                    {
                        positions = teleport;
                        break;
                    }
                }
                if (positions != null && Time.time - LastDestiny > 60)
                {
                    List<GameObject> keycards = new List<GameObject>();
                    foreach (PLPickupObject item in positions.gameObject.GetComponentsInChildren<PLPickupObject>(true))
                    {
                        keycards.Add(item.gameObject);
                    }
                    AI.AI_TargetPos = keycards[Random.Range(0, keycards.Count - 1)].transform.position;
                    AI.AI_TargetPos_Raw = AI.AI_TargetPos;
                    targetPos = AI.AI_TargetPos;
                    LastDestiny = Time.time;
                }
                else if (targetPos != Vector3.zero)
                {
                    AI.AI_TargetPos = targetPos;
                    AI.AI_TargetPos_Raw = AI.AI_TargetPos;
                }
                foreach (PLPickupObject inObj in PLGameStatic.Instance.m_AllPickupObjects)
                {
                    if ((pawn.transform.position - inObj.transform.position).magnitude < 8f && !inObj.PickedUp)
                    {
                        CapBot.photonView.RPC("AttemptToPickupObjectAtID", PhotonTargets.MasterClient, new object[]
                            {
                            inObj.PickupID
                            });
                        CapBot.GetPawn().photonView.RPC("Anim_Pickup", PhotonTargets.Others, new object[0]);
                        PLMusic.PostEvent("play_sx_player_item_pickup", CapBot.GetPawn().gameObject);
                    }
                }
            }
            else if (paladin != null) //Step 8: Kill Elite Paladin
            {
                AI.HighPriorityTarget = paladin;
                AI.AI_TargetPos = new Vector3(21, -227, 394);
                AI.AI_TargetPos_Raw = AI.AI_TargetPos;
            }
            else if (!PLServer.AnyPlayerHasItemOfName("Level 3 Admin Access Card")) //Step 9: Find level 3 keycard 
            {
                PLRandomChildItem positions = null;
                foreach (PLRandomChildItem teleport in Object.FindObjectsOfType(typeof(PLRandomChildItem)))
                {
                    if (teleport.name == "Keycard_ADMIN_Lvl_3_RCI")
                    {
                        positions = teleport;
                        break;
                    }
                }
                if (positions != null && Time.time - LastDestiny > 60)
                {
                    List<GameObject> keycards = new List<GameObject>();
                    foreach (PLPickupObject item in positions.gameObject.GetComponentsInChildren<PLPickupObject>(true))
                    {
                        keycards.Add(item.gameObject);
                    }
                    AI.AI_TargetPos = keycards[Random.Range(0, keycards.Count - 1)].transform.position;
                    AI.AI_TargetPos_Raw = AI.AI_TargetPos;
                    targetPos = AI.AI_TargetPos;
                    LastDestiny = Time.time;
                }
                else if (targetPos != Vector3.zero)
                {
                    AI.AI_TargetPos = targetPos;
                    AI.AI_TargetPos_Raw = AI.AI_TargetPos;
                }
                foreach (PLPickupObject inObj in PLGameStatic.Instance.m_AllPickupObjects)
                {
                    if ((pawn.transform.position - inObj.transform.position).magnitude < 8f && !inObj.PickedUp)
                    {
                        CapBot.photonView.RPC("AttemptToPickupObjectAtID", PhotonTargets.MasterClient, new object[]
                            {
                            inObj.PickupID
                            });
                        CapBot.GetPawn().photonView.RPC("Anim_Pickup", PhotonTargets.Others, new object[0]);
                        PLMusic.PostEvent("play_sx_player_item_pickup", CapBot.GetPawn().gameObject);
                    }
                }
            }
            else if (!PLServer.Instance.HasMissionWithID(55401) || !PLServer.Instance.HasMissionWithID(55402)) //Step 10: Get kill Scientist and get medicine Missions 
            {
                if (!PLServer.Instance.HasActiveMissionWithID(55401))
                {
                    AI.AI_TargetPos = new Vector3(-12, -151, 463);
                }
                else
                {
                    AI.AI_TargetPos = new Vector3(-13, -151, 479);
                }
                AI.AI_TargetPos_Raw = AI.AI_TargetPos;
                if ((pawn.transform.position - new Vector3(-12, -151, 463)).magnitude < 6f && !PLServer.Instance.HasActiveMissionWithID(55401))
                {
                    PLServer.Instance.photonView.RPC("AttemptStartMissionOfTypeID", PhotonTargets.MasterClient, new object[]
                        {
                        55401,
                        true
                        });
                }
                else if ((pawn.transform.position - new Vector3(-13, -151, 479)).magnitude < 6f)
                {
                    PLServer.Instance.photonView.RPC("AttemptStartMissionOfTypeID", PhotonTargets.MasterClient, new object[]
                            {
                        55402,
                        true
                            });
                }
            }
            else if (!PLServer.Instance.HasCompletedMissionWithID(55401)) //Step 11: Kill crystal scientists
            {
                List<PLInfectedScientist> crystals = new List<PLInfectedScientist>();
                foreach (PLInfectedScientist crystal in Object.FindObjectsOfType(typeof(PLInfectedScientist)))
                {
                    if (!crystal.IsDead)
                    {
                        crystals.Add(crystal);
                    }
                }
                if (crystals.Count > 0)
                {
                    AI.AI_TargetPos = crystals[0].transform.position;
                    foreach (PLInfectedScientist crystal in crystals)
                    {
                        if ((pawn.transform.position - crystal.transform.position).magnitude < (pawn.transform.position - AI.AI_TargetPos).magnitude)
                        {
                            AI.AI_TargetPos = crystal.transform.position;
                        }
                    }
                    AI.AI_TargetPos_Raw = AI.AI_TargetPos;
                }
            }
            else if (!PLServer.Instance.HasCompletedMissionWithID(55402)) //Step 12: Finish Medicine mission
            {
                if (!PLServer.AnyPlayerHasItemOfName("Medicine Pack"))
                {
                    AI.AI_TargetPos = new Vector3(-130, -151, 459);
                    AI.AI_TargetPos_Raw = AI.AI_TargetPos;
                    foreach (PLPickupObject inObj in PLGameStatic.Instance.m_AllPickupObjects)
                    {
                        if ((pawn.transform.position - inObj.transform.position).magnitude < 8f)
                        {
                            CapBot.photonView.RPC("AttemptToPickupObjectAtID", PhotonTargets.MasterClient, new object[]
                                {
                            inObj.PickupID
                                });
                            CapBot.GetPawn().photonView.RPC("Anim_Pickup", PhotonTargets.Others, new object[0]);
                            PLMusic.PostEvent("play_sx_player_item_pickup", CapBot.GetPawn().gameObject);
                        }
                    }
                }
                else
                {
                    AI.AI_TargetPos = new Vector3(-14, -151, 479);
                    AI.AI_TargetPos_Raw = AI.AI_TargetPos;
                    if ((pawn.transform.position - new Vector3(-12, -151, 463)).magnitude < 6f)
                    {
                        PLServer.Instance.AttemptCompleteObjective("mission55402obj2");
                    }
                }
            }
            else if (!PLServer.AnyPlayerHasItemOfName("Data Pad")) //Step 13: Get Data Pad
            {
                AI.AI_TargetPos = new Vector3(-154, -151, 498);
                AI.AI_TargetPos_Raw = AI.AI_TargetPos;
                foreach (PLPickupComponent component in Object.FindObjectsOfType(typeof(PLPickupComponent)))
                {
                    if ((pawn.transform.position - component.transform.position).magnitude < 8f)
                    {
                        CapBot.photonView.RPC("AttemptToPickupComponentAtID", PhotonTargets.MasterClient, new object[]
                            {
                            component.PickupID
                            });
                        CapBot.GetPawn().photonView.RPC("Anim_Pickup", PhotonTargets.Others, new object[0]);
                        PLMusic.PostEvent("play_sx_player_item_pickup", CapBot.GetPawn().gameObject);
                    }
                }
                foreach (PLPickupObject inObj in PLGameStatic.Instance.m_AllPickupObjects)
                {
                    if ((pawn.transform.position - inObj.transform.position).magnitude < 8f && !inObj.PickedUp)
                    {
                        CapBot.photonView.RPC("AttemptToPickupObjectAtID", PhotonTargets.MasterClient, new object[]
                            {
                            inObj.PickupID
                            });
                        CapBot.GetPawn().photonView.RPC("Anim_Pickup", PhotonTargets.Others, new object[0]);
                        PLMusic.PostEvent("play_sx_player_item_pickup", CapBot.GetPawn().gameObject);
                    }
                }
                foreach (PLPickupRandomComponent component in Object.FindObjectsOfType(typeof(PLPickupRandomComponent)))
                {
                    if ((pawn.transform.position - component.transform.position).magnitude < 8f && !component.PickedUp)
                    {
                        CapBot.photonView.RPC("AttemptToPickupRandomComponentAtID", PhotonTargets.MasterClient, new object[]
                            {
                            component.PickupID
                            });
                        CapBot.GetPawn().photonView.RPC("Anim_Pickup", PhotonTargets.Others, new object[0]);
                        PLMusic.PostEvent("play_sx_player_item_pickup", CapBot.GetPawn().gameObject);
                    }
                }
            }
            else if (!PLServer.AnyPlayerHasItemOfName("Aberrant Organisms Lab Access Card")) //Step 14: Get Access card
            {
                AI.AI_TargetPos = new Vector3(-120, -151, 501);
                AI.AI_TargetPos_Raw = AI.AI_TargetPos;
                foreach (PLPickupObject inObj in PLGameStatic.Instance.m_AllPickupObjects)
                {
                    if ((pawn.transform.position - inObj.transform.position).magnitude < 8f && !inObj.PickedUp)
                    {
                        CapBot.photonView.RPC("AttemptToPickupObjectAtID", PhotonTargets.MasterClient, new object[]
                            {
                            inObj.PickupID
                            });
                        CapBot.GetPawn().photonView.RPC("Anim_Pickup", PhotonTargets.Others, new object[0]);
                        PLMusic.PostEvent("play_sx_player_item_pickup", CapBot.GetPawn().gameObject);
                    }
                }
            }
            else //step 15: Kill the crystal guy
            {
                if (pawn.Health / pawn.MaxHealth > 0.25f) AI.AI_TargetPos = new Vector3(-108, -152, 575);
                else AI.AI_TargetPos = new Vector3(-100, -152, 575);
                foreach (PLInterior interior in Object.FindObjectsOfType(typeof(PLInterior)))
                {
                    if (interior.name == "Area_05_Interior")
                    {
                        AI.AI_TargetInterior = interior;
                        break;
                    }
                }
                AI.AI_TargetPos_Raw = AI.AI_TargetPos;
                AI.HighPriorityTarget = PLInGameUI.Instance.BossUI_Target;
            }
        }
        internal static bool Burrow(PLPlayer CapBot, ref float LastAction)
        {
            if (PLServer.Instance.IsFragmentCollected(1))
            {
                LastAction = Time.time;
                return true;
            }
            if (PLServer.Instance.CurrentCrewCredits >= 100000)
            {
                CapBot.MyBot.AI_TargetPos = new Vector3(212, 64, -38);
                CapBot.MyBot.AI_TargetPos_Raw = CapBot.MyBot.AI_TargetPos;
                foreach (PLTeleportationLocationInstance teleport in Object.FindObjectsOfType(typeof(PLTeleportationLocationInstance)))
                {
                    if (teleport.name == "PLGame")
                    {
                        CapBot.MyBot.AI_TargetTLI = teleport;
                        break;
                    }
                }
                if ((CapBot.MyBot.AI_TargetPos - CapBot.GetPawn().transform.position).sqrMagnitude > 4)
                {
                    CapBot.MyBot.EnablePathing = true;
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
                CapBot.MyBot.AI_TargetPos = new Vector3(62, 18, -56);
                CapBot.MyBot.AI_TargetPos_Raw = CapBot.MyBot.AI_TargetPos;
                PLBurrowArena arena = Object.FindObjectOfType(typeof(PLBurrowArena)) as PLBurrowArena;
                if (arena != null)
                {
                    if (CapBot.GetPawn().SpawnedInArena)
                    {
                        CapBot.MyBot.AI_TargetPos = new Vector3(103, 4, -115);
                        CapBot.MyBot.AI_TargetPos_Raw = CapBot.MyBot.AI_TargetPos;
                        CapBot.MyBot.EnablePathing = true;
                    }
                    foreach (PLTeleportationLocationInstance teleport in Object.FindObjectsOfType(typeof(PLTeleportationLocationInstance)))
                    {
                        if (teleport.name == "PLGame")
                        {
                            CapBot.MyBot.AI_TargetTLI = teleport;
                            break;
                        }
                    }
                    if ((CapBot.MyBot.AI_TargetPos - CapBot.GetPawn().transform.position).sqrMagnitude > 4 && !CapBot.GetPawn().SpawnedInArena)
                    {
                        CapBot.MyBot.EnablePathing = true;
                    }
                    else if (!arena.ArenaIsActive)
                    {
                        arena.StartArena(0);
                        CapBot.GetPawn().transform.position = new Vector3(103, 4, -115);
                    }
                    else if (arena.ArenaIsActive && CapBot.GetPawn().SpawnedInArena)
                    {
                        CapBot.ActiveMainPriority = new AIPriority(AIPriorityType.E_MAIN, 2, 1);
                        CapBot.MyBot.TickFindInvaderAction(null);
                    }
                }
            }
            LastAction = Time.time;
            return false;
        }
        static float WeaponsTest = Time.time;
        internal static bool WD_Weapons_Testing(PLPlayer CapBot)
        {
            if (PLServer.GetCurrentSector() != null && PLServer.GetCurrentSector().VisualIndication == ESectorVisualIndication.WD_MISSIONCHAIN_WEAPONS_DEMO && !PLServer.Instance.HasCompletedMissionWithID(59682)) //In the W.D. Weapons testing mission 
            {
                CapBot.MyBot.AI_TargetPos = new Vector3(165, -124, -64);
                CapBot.MyBot.AI_TargetPos_Raw = CapBot.MyBot.AI_TargetPos;
                PLBurrowArena arena = Object.FindObjectOfType<PLBurrowArena>();
                foreach (PLTeleportationLocationInstance teleport in Object.FindObjectsOfType(typeof(PLTeleportationLocationInstance)))
                {
                    if (teleport.name == "PLGamePlanet")
                    {
                        CapBot.MyBot.AI_TargetTLI = teleport;
                        break;
                    }
                }
                if (arena.ArenaIsActive) WeaponsTest = Time.time;
                CapBot.MyBot.EnablePathing = true;
                if (!arena.ArenaIsActive && Time.time - WeaponsTest > 90)
                {
                    arena.StartArena_NoCredits(0);
                    PLServer.Instance.GetMissionWithID(59682).Objectives[1].AmountCompleted = 1;
                    WeaponsTest = Time.time;
                }
                if (CapBot.GetPawn().SpawnedInArena)
                {
                    CapBot.MyBot.AI_TargetPos = new Vector3(126, -139, -27);
                    CapBot.MyBot.AI_TargetPos_Raw = CapBot.MyBot.AI_TargetPos;
                    CapBot.ActiveMainPriority = new AIPriority(AIPriorityType.E_MAIN, 2, 1);
                    CapBot.MyBot.TickFindInvaderAction(null);

                }
                return true;
            }
            return false;
        }
        internal static bool Races(PLPlayer CapBot, ref float LastAction)
        {
            PLRace race = (Object.FindObjectOfType(typeof(PLRaceStartScreen)) as PLRaceStartScreen).MyRace;
            PLPickupComponent prize = Object.FindObjectOfType(typeof(PLPickupComponent)) as PLPickupComponent;
            if (PLServer.GetCurrentSector().VisualIndication == ESectorVisualIndication.RACING_SECTOR && race != null)
            {
                if (!race.ReadyToStart && (PLServer.Instance.RacesWonBitfield & 1) == 0)
                {
                    foreach (PLTeleportationLocationInstance teleport in Object.FindObjectsOfType(typeof(PLTeleportationLocationInstance)))
                    {
                        if (teleport.name == "GarageBSO")
                        {
                            CapBot.MyBot.AI_TargetTLI = teleport;
                            break;
                        }
                    }
                    if (!PLServer.Instance.HasActiveMissionWithID(43499) && !PLServer.Instance.HasActiveMissionWithID(43072) && PLServer.Instance.CurrentCrewCredits >= 1000)
                    {
                        CapBot.MyBot.AI_TargetPos = new Vector3(174, 4, -332);
                        CapBot.MyBot.AI_TargetPos_Raw = CapBot.MyBot.AI_TargetPos;
                        if ((CapBot.MyBot.AI_TargetPos - CapBot.GetPawn().transform.position).sqrMagnitude > 4)
                        {
                            CapBot.MyBot.EnablePathing = true;
                        }
                        else
                        {
                            if (PLServer.Instance.CurrentCrewCredits >= 5000 && !PLServer.Instance.HasActiveMissionWithID(43499))
                            {
                                PLServer.Instance.photonView.RPC("AttemptStartMissionOfTypeID", PhotonTargets.MasterClient, new object[]
                                {
                                    43499,
                                    false
                                });
                            }
                            else if (!PLServer.Instance.HasActiveMissionWithID(43072) && PLServer.Instance.CurrentCrewCredits >= 1000)
                            {
                                PLServer.Instance.photonView.RPC("AttemptStartMissionOfTypeID", PhotonTargets.MasterClient, new object[]
                                {
                                    43072,
                                    false
                                });
                            }
                        }
                    }
                    else
                    {
                        CapBot.MyBot.AI_TargetPos = new Vector3(158, 4, -341);
                        CapBot.MyBot.AI_TargetPos_Raw = CapBot.MyBot.AI_TargetPos;

                        if ((CapBot.MyBot.AI_TargetPos - CapBot.GetPawn().transform.position).sqrMagnitude > 4)
                        {
                            CapBot.MyBot.EnablePathing = true;
                        }
                        else
                        {
                            race.SetAsReadyToStart();
                        }
                    }
                    LastAction = Time.time;
                    return true;
                }
                else if (race.RaceEnded && (PLServer.Instance.RacesWonBitfield & 1) != 0 && ((prize != null && !prize.PickedUp) || (PLServer.Instance.HasActiveMissionWithID(43499) && !PLServer.Instance.GetMissionWithID(43499).Ended) || (PLServer.Instance.HasActiveMissionWithID(43072) && !PLServer.Instance.GetMissionWithID(43072).Ended)))
                {
                    foreach (PLTeleportationLocationInstance teleport in Object.FindObjectsOfType(typeof(PLTeleportationLocationInstance)))
                    {
                        if (teleport.name == "GarageBSO")
                        {
                            CapBot.MyBot.AI_TargetTLI = teleport;
                            break;
                        }
                    }
                    if ((PLServer.Instance.HasActiveMissionWithID(43499) && !PLServer.Instance.GetMissionWithID(43499).Ended) || (PLServer.Instance.HasActiveMissionWithID(43072) && !PLServer.Instance.GetMissionWithID(43072).Ended))
                    {
                        CapBot.MyBot.AI_TargetPos = new Vector3(174, 4, -332);
                        CapBot.MyBot.AI_TargetPos_Raw = CapBot.MyBot.AI_TargetPos;
                        if ((CapBot.MyBot.AI_TargetPos - CapBot.GetPawn().transform.position).sqrMagnitude > 4)
                        {
                            CapBot.MyBot.EnablePathing = true;
                        }
                        else
                        {
                            if (PLServer.Instance.HasActiveMissionWithID(43499))
                            {
                                PLServer.Instance.photonView.RPC("AttemptForceEndMissionOfTypeID", PhotonTargets.All, new object[]
                                {
                                    43499
                                });
                                PLServer.Instance.CurrentCrewCredits += 15000;
                            }
                            else if (PLServer.Instance.HasActiveMissionWithID(43072))
                            {
                                PLServer.Instance.photonView.RPC("AttemptForceEndMissionOfTypeID", PhotonTargets.All, new object[]
                                {
                                    43072
                                });
                                PLServer.Instance.CurrentCrewCredits += 3000;
                            }
                        }
                    }
                    else if (prize != null && !prize.PickedUp)
                    {
                        CapBot.MyBot.AI_TargetPos = new Vector3(162, 6, -335);
                        CapBot.MyBot.AI_TargetPos_Raw = CapBot.MyBot.AI_TargetPos;
                        if ((CapBot.MyBot.AI_TargetPos - CapBot.GetPawn().transform.position).sqrMagnitude > 4)
                        {
                            CapBot.MyBot.EnablePathing = true;
                        }
                        else
                        {
                            CapBot.AttemptToPickupComponentAtID(prize.PickupID);
                        }
                    }
                    LastAction = Time.time;
                    return true;
                }
            }
            else if (PLServer.GetCurrentSector().VisualIndication == ESectorVisualIndication.RACING_SECTOR_2 && race != null)
            {
                if (!race.ReadyToStart && (PLServer.Instance.RacesWonBitfield & 2) == 0)
                {
                    foreach (PLTeleportationLocationInstance teleport in Object.FindObjectsOfType(typeof(PLTeleportationLocationInstance)))
                    {
                        if (teleport.name == "GarageBSO")
                        {
                            CapBot.MyBot.AI_TargetTLI = teleport;
                            break;
                        }
                    }
                    if (!PLServer.Instance.HasActiveMissionWithID(43932) && !PLServer.Instance.HasActiveMissionWithID(43938) && PLServer.Instance.CurrentCrewCredits >= 1000)
                    {
                        CapBot.MyBot.AI_TargetPos = new Vector3(123, -15, -345);
                        CapBot.MyBot.AI_TargetPos_Raw = CapBot.MyBot.AI_TargetPos;
                        if ((CapBot.MyBot.AI_TargetPos - CapBot.GetPawn().transform.position).sqrMagnitude > 4)
                        {
                            CapBot.MyBot.EnablePathing = true;
                        }
                        else
                        {
                            if (PLServer.Instance.CurrentCrewCredits >= 5000 && !PLServer.Instance.HasActiveMissionWithID(43938))
                            {
                                PLServer.Instance.photonView.RPC("AttemptStartMissionOfTypeID", PhotonTargets.MasterClient, new object[]
                                {
                                    43938,
                                    false
                                });
                            }
                            else if (!PLServer.Instance.HasActiveMissionWithID(43932) && PLServer.Instance.CurrentCrewCredits >= 1000)
                            {
                                PLServer.Instance.photonView.RPC("AttemptStartMissionOfTypeID", PhotonTargets.MasterClient, new object[]
                                {
                                    43932,
                                    false
                                });
                            }
                        }
                    }
                    else
                    {
                        CapBot.MyBot.AI_TargetPos = new Vector3(132, -15, -278);
                        CapBot.MyBot.AI_TargetPos_Raw = CapBot.MyBot.AI_TargetPos;

                        if ((CapBot.MyBot.AI_TargetPos - CapBot.GetPawn().transform.position).sqrMagnitude > 4)
                        {
                            CapBot.MyBot.EnablePathing = true;
                        }
                        else
                        {
                            race.SetAsReadyToStart();
                        }
                    }
                    LastAction = Time.time;
                    return true;
                }
                else if (race.RaceEnded && (PLServer.Instance.RacesWonBitfield & 2) != 0 && ((prize != null && !prize.PickedUp) || (PLServer.Instance.HasActiveMissionWithID(43932) && !PLServer.Instance.GetMissionWithID(43932).Ended) || (PLServer.Instance.HasActiveMissionWithID(43938) && !PLServer.Instance.GetMissionWithID(43938).Ended)))
                {
                    foreach (PLTeleportationLocationInstance teleport in Object.FindObjectsOfType(typeof(PLTeleportationLocationInstance)))
                    {
                        if (teleport.name == "GarageBSO")
                        {
                            CapBot.MyBot.AI_TargetTLI = teleport;
                            break;
                        }
                    }
                    if ((PLServer.Instance.HasActiveMissionWithID(43938) && !PLServer.Instance.GetMissionWithID(43938).Ended) || (PLServer.Instance.HasActiveMissionWithID(43932) && !PLServer.Instance.GetMissionWithID(43932).Ended))
                    {
                        CapBot.MyBot.AI_TargetPos = new Vector3(123, -15, -345);
                        CapBot.MyBot.AI_TargetPos_Raw = CapBot.MyBot.AI_TargetPos;
                        if ((CapBot.MyBot.AI_TargetPos - CapBot.GetPawn().transform.position).sqrMagnitude > 4)
                        {
                            CapBot.MyBot.EnablePathing = true;
                        }
                        else
                        {
                            if (PLServer.Instance.HasActiveMissionWithID(43932))
                            {
                                PLServer.Instance.photonView.RPC("AttemptForceEndMissionOfTypeID", PhotonTargets.All, new object[]
                                {
                                    43932
                                });
                                PLServer.Instance.CurrentCrewCredits += 3000;
                            }
                            else if (PLServer.Instance.HasActiveMissionWithID(43938))
                            {
                                PLServer.Instance.photonView.RPC("AttemptForceEndMissionOfTypeID", PhotonTargets.All, new object[]
                                {
                                    43938
                                });
                                PLServer.Instance.CurrentCrewCredits += 15000;
                            }
                        }
                    }
                    else if (prize != null && !prize.PickedUp)
                    {
                        CapBot.MyBot.AI_TargetPos = new Vector3(129, -14, -270);
                        CapBot.MyBot.AI_TargetPos_Raw = CapBot.MyBot.AI_TargetPos;
                        if ((CapBot.MyBot.AI_TargetPos - CapBot.GetPawn().transform.position).sqrMagnitude > 4)
                        {
                            CapBot.MyBot.EnablePathing = true;
                        }
                        else
                        {
                            CapBot.AttemptToPickupComponentAtID(prize.PickupID);
                        }
                    }
                    LastAction = Time.time;
                    return true;
                }
            }
            else if (PLServer.GetCurrentSector().VisualIndication == ESectorVisualIndication.RACING_SECTOR_3 && race != null && (PLServer.Instance.RacesWonBitfield & 1) != 0 && (PLServer.Instance.RacesWonBitfield & 2) != 0)
            {
                if (!race.ReadyToStart && (PLServer.Instance.RacesWonBitfield & 4) == 0)
                {
                    foreach (PLTeleportationLocationInstance teleport in Object.FindObjectsOfType(typeof(PLTeleportationLocationInstance)))
                    {
                        if (teleport.name == "GarageBSO")
                        {
                            CapBot.MyBot.AI_TargetTLI = teleport;
                            break;
                        }
                    }
                    if (!PLServer.Instance.HasActiveMissionWithID(44085) && !PLServer.Instance.HasActiveMissionWithID(44088) && PLServer.Instance.CurrentCrewCredits >= 1000)
                    {
                        CapBot.MyBot.AI_TargetPos = new Vector3(115, -7, -233);
                        CapBot.MyBot.AI_TargetPos_Raw = CapBot.MyBot.AI_TargetPos;
                        if ((CapBot.MyBot.AI_TargetPos - CapBot.GetPawn().transform.position).sqrMagnitude > 4)
                        {
                            CapBot.MyBot.EnablePathing = true;
                        }
                        else
                        {
                            if (PLServer.Instance.CurrentCrewCredits >= 5000 && !PLServer.Instance.HasActiveMissionWithID(44088))
                            {
                                PLServer.Instance.photonView.RPC("AttemptStartMissionOfTypeID", PhotonTargets.MasterClient, new object[]
                                {
                                    44088,
                                    false
                                });
                            }
                            else if (!PLServer.Instance.HasActiveMissionWithID(44085) && PLServer.Instance.CurrentCrewCredits >= 1000)
                            {
                                PLServer.Instance.photonView.RPC("AttemptStartMissionOfTypeID", PhotonTargets.MasterClient, new object[]
                                {
                                    44085,
                                    false
                                });
                            }
                        }
                    }
                    else
                    {
                        CapBot.MyBot.AI_TargetPos = new Vector3(106, -7, -234);
                        CapBot.MyBot.AI_TargetPos_Raw = CapBot.MyBot.AI_TargetPos;

                        if ((CapBot.MyBot.AI_TargetPos - CapBot.GetPawn().transform.position).sqrMagnitude > 4)
                        {
                            CapBot.MyBot.EnablePathing = true;
                        }
                        else
                        {
                            race.SetAsReadyToStart();
                        }
                    }
                    LastAction = Time.time;
                    return true;
                }
                else if (race.RaceEnded && (PLServer.Instance.RacesWonBitfield & 4) != 0 && ((prize != null && !prize.PickedUp) || (PLServer.Instance.HasActiveMissionWithID(44085) && !PLServer.Instance.GetMissionWithID(44085).Ended) || (PLServer.Instance.HasActiveMissionWithID(44088) && !PLServer.Instance.GetMissionWithID(44088).Ended)))
                {
                    foreach (PLTeleportationLocationInstance teleport in Object.FindObjectsOfType(typeof(PLTeleportationLocationInstance)))
                    {
                        if (teleport.name == "GarageBSO")
                        {
                            CapBot.MyBot.AI_TargetTLI = teleport;
                            break;
                        }
                    }
                    if ((PLServer.Instance.HasActiveMissionWithID(44085) && !PLServer.Instance.GetMissionWithID(44085).Ended) || (PLServer.Instance.HasActiveMissionWithID(44088) && !PLServer.Instance.GetMissionWithID(44088).Ended))
                    {
                        CapBot.MyBot.AI_TargetPos = new Vector3(115, -7, -233);
                        CapBot.MyBot.AI_TargetPos_Raw = CapBot.MyBot.AI_TargetPos;
                        if ((CapBot.MyBot.AI_TargetPos - CapBot.GetPawn().transform.position).sqrMagnitude > 4)
                        {
                            CapBot.MyBot.EnablePathing = true;
                        }
                        else
                        {
                            if (PLServer.Instance.HasActiveMissionWithID(44085))
                            {
                                PLServer.Instance.photonView.RPC("AttemptForceEndMissionOfTypeID", PhotonTargets.All, new object[]
                                {
                                    44085
                                });
                                PLServer.Instance.CurrentCrewCredits += 15000;
                            }
                            else if (PLServer.Instance.HasActiveMissionWithID(44088))
                            {
                                PLServer.Instance.photonView.RPC("AttemptForceEndMissionOfTypeID", PhotonTargets.All, new object[]
                                {
                                    44088
                                });
                                PLServer.Instance.CurrentCrewCredits += 30000;
                            }
                        }
                    }
                    else if (prize != null && !prize.PickedUp)
                    {
                        CapBot.MyBot.AI_TargetPos = new Vector3(110, -6, -226);
                        CapBot.MyBot.AI_TargetPos_Raw = CapBot.MyBot.AI_TargetPos;
                        if ((CapBot.MyBot.AI_TargetPos - CapBot.GetPawn().transform.position).sqrMagnitude > 4)
                        {
                            CapBot.MyBot.EnablePathing = true;
                        }
                        else
                        {
                            CapBot.AttemptToPickupComponentAtID(prize.PickupID);
                        }
                    }
                    LastAction = Time.time;
                    return true;
                }
            }
            return false;
        }
        internal static void HighRollers(PLPlayer CapBot)
        {
            CapBot.CurrentlyInLiarsDiceGame = null;
            if (PLServer.Instance.IsFragmentCollected(3)) return;
            PLHighRollersShipInfo highRoller = Object.FindObjectOfType<PLHighRollersShipInfo>();
            if (CapBot.ActiveMainPriority == null || CapBot.ActiveMainPriority.TypeData != 65)
            {
                CapBot.ActiveMainPriority = new AIPriority(AIPriorityType.E_MAIN, 65, 1);
            }
            if (CapBot.CurrentlyInLiarsDiceGame != null && highRoller.SmallGames.Contains(CapBot.CurrentlyInLiarsDiceGame) && highRoller.CrewChips >= 3)
            {
                CapBot.CurrentlyInLiarsDiceGame = null;
            }
            if (!PLServer.Instance.GetMissionWithID(103216).Ended)
            {
                if (PLServer.Instance.CurrentCrewCredits < 10000) return;
                CapBot.MyBot.AI_TargetPos = new Vector3(64, -102, -34);
                CapBot.MyBot.AI_TargetPos_Raw = CapBot.MyBot.AI_TargetPos;
                foreach (PLTeleportationLocationInstance teleport in Object.FindObjectsOfType(typeof(PLTeleportationLocationInstance)))
                {
                    if (teleport.name == "PLGamePlanet")
                    {
                        CapBot.MyBot.AI_TargetTLI = teleport;
                        break;
                    }
                }
                if ((CapBot.MyBot.AI_TargetPos - CapBot.GetPawn().transform.position).sqrMagnitude > 4)
                {
                    CapBot.MyBot.EnablePathing = true;
                }
                else
                {
                    PLServer.Instance.GetMissionWithID(103216).Objectives[0].AmountCompleted = 1;
                }
            }
            else if (highRoller != null && highRoller.CrewChips < 3)
            {
                List<PLLiarsDiceGame> possibleGames = new List<PLLiarsDiceGame>();
                PLLiarsDiceGame neareastGame;
                foreach (PLTeleportationLocationInstance teleport in Object.FindObjectsOfType(typeof(PLTeleportationLocationInstance)))
                {
                    if (teleport.name == "PLGamePlanet")
                    {
                        CapBot.MyBot.AI_TargetTLI = teleport;
                        break;
                    }
                }
                foreach (PLLiarsDiceGame game in highRoller.SmallGames) //Finds all small games that have a slot
                {
                    if (game.LocalPlayerCanJoinRightNow())
                    {
                        possibleGames.Add(game);
                    }
                }
                if (possibleGames.Count == 0) return;
                neareastGame = possibleGames[0];
                float nearestGameDist = (neareastGame.transform.position - CapBot.GetPawn().transform.position).magnitude;
                foreach (PLLiarsDiceGame game in possibleGames)
                {
                    if ((game.transform.position - CapBot.GetPawn().transform.position).magnitude < nearestGameDist)
                    {
                        nearestGameDist = (game.transform.position - CapBot.GetPawn().transform.position).magnitude;
                        neareastGame = game;
                    }
                }
                CapBot.MyBot.AI_TargetPos = neareastGame.transform.position;
                CapBot.MyBot.AI_TargetPos_Raw = CapBot.MyBot.AI_TargetPos;
                if ((CapBot.MyBot.AI_TargetPos - CapBot.GetPawn().transform.position).sqrMagnitude > 10)
                {
                    CapBot.MyBot.EnablePathing = true;
                }
                else
                {
                    CapBot.CurrentlyInLiarsDiceGame = neareastGame;
                }
            }
            else if (highRoller.BigGame.LocalPlayerCanJoinRightNow())
            {
                CapBot.MyBot.AI_TargetPos = highRoller.BigGame.transform.position;
                CapBot.MyBot.AI_TargetPos_Raw = CapBot.MyBot.AI_TargetPos;
                foreach (PLTeleportationLocationInstance teleport in Object.FindObjectsOfType(typeof(PLTeleportationLocationInstance)))
                {
                    if (teleport.name == "PLGamePlanet")
                    {
                        CapBot.MyBot.AI_TargetTLI = teleport;
                        break;
                    }
                }
                if ((CapBot.MyBot.AI_TargetPos - CapBot.GetPawn().transform.position).sqrMagnitude > 10)
                {
                    CapBot.MyBot.EnablePathing = true;
                }
                else
                {
                    CapBot.CurrentlyInLiarsDiceGame = highRoller.BigGame;
                }
            }
        }
        internal static bool GreyHuntsmanHQ(PLPlayer __instance, ref float LastAction)
        {
            if (PLServer.Instance.HasActiveMissionWithID(104869) && !PLServer.Instance.GetMissionWithID(104869).Ended && !PLServer.Instance.IsFragmentCollected(7))
            {
                __instance.MyBot.AI_TargetPos = new Vector3(217, 111, -108);
                __instance.MyBot.AI_TargetPos_Raw = __instance.MyBot.AI_TargetPos;
                foreach (PLTeleportationLocationInstance teleport in Object.FindObjectsOfType(typeof(PLTeleportationLocationInstance)))
                {
                    if (teleport.name == "PLGamePlanet")
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
                        104869
                    });
                    PLServer.Instance.CollectFragment(7);
                }
                LastAction = Time.time;
                return true;
            }
            return false;
        }
    }
}
