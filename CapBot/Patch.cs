using PulsarModLoader;
using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Linq;
using Steamworks;
using Unity.Jobs;
using CapBot.Routines;

//Fix visit planet and complete missions warnings
//Fix Boarding command and action
//Fix route control
namespace CapBot
{
    [HarmonyPatch(typeof(PLPlayer), "UpdateAIPriorities")]
    class Patch
    {
        static float LastAction = 0;
        static float LastOrder = Time.time;
        static void Postfix(PLPlayer __instance)
        {
            #region Capbot Setup
            ///<summary>
            /// Used to setup Capbot - Reset ClassAI, count bots and change race based on Paladin.
            ///</summary>
            if ((__instance.cachedAIData == null || __instance.cachedAIData.Priorities.Count == 0) && SpawnBot.capisbot && __instance.TeamID == 0 && __instance.IsBot) //Give default AI priorities
            {
                //if (__instance.cachedAIData == null) PulsarModLoader.Utilities.Messaging.Notification("Null value!");
                //if (__instance.cachedAIData != null) PulsarModLoader.Utilities.Messaging.Notification("Priorities value: " + __instance.cachedAIData.Priorities.Count);
                //PulsarModLoader.Utilities.Messaging.Notification("Name: " + __instance.cachedAIData.Priorities.Count);
                if (__instance.cachedAIData == null) __instance.cachedAIData = new AIDataIndividual();
                PLGlobal.Instance.SetupClassDefaultData(ref __instance.cachedAIData, __instance.GetClassID(), false);
            }
            if (__instance.GetPawn() == null || !__instance.IsBot || __instance.GetClassID() != 0 || __instance.TeamID != 0 || !PhotonNetwork.isMasterClient || __instance.StartingShip == null) return;
            int botcounter = 0; //Counts to check if crew is bot (for bots only games)
            foreach (PLPlayer player in PLServer.Instance.AllPlayers)
            {
                if (player.TeamID == 0 && player.IsBot)
                {
                    botcounter++;
                }
            }
            if (botcounter >= 5) SpawnBot.crewisbot = true; //Enables special stuff if everyone is bot on the crew
            else SpawnBot.crewisbot = false;
            if (__instance.StartingShip != null && __instance.StartingShip.ShipTypeID == EShipType.E_POLYTECH_SHIP && __instance.RaceID != 2) //set race to robot in paladin
            {
                __instance.RaceID = 2;
            }
            #endregion

            if (__instance.GetPawn().MyController.AI_Item_Target == __instance.GetPawn().transform) __instance.GetPawn().MyController.PreAIPriorityTick();
            if (__instance.GetPhotonPlayer() == null && PLNetworkManager.Instance.LocalPlayer != null) __instance.PhotonPlayer = PLNetworkManager.Instance.LocalPlayer.GetPhotonPlayer();
            Hostiles.CheckForHostiles(__instance, out bool HasIntruders);
            if (__instance.StartingShip != null && __instance.StartingShip.InWarp && PLServer.Instance.AllPlayersLoaded() && (__instance.StartingShip.MyShieldGenerator == null || __instance.StartingShip.MyStats.ShieldsCurrent / __instance.StartingShip.MyStats.ShieldsMax > 0.99))
                PLInGameUI.Instance.WarpSkipButtonClicked(); //Skip warp when ready
            if (__instance.MyBot.AI_TargetPos != __instance.StartingShip.CaptainsChairPivot.position && __instance.StartingShip.CaptainsChairPlayerID == __instance.GetPlayerID())
            {
                __instance.StartingShip.AttemptToSitInCaptainsChair(-1); //Leave chair
            }

            #region Sector Based Routines
            if (PLServer.GetCurrentSector() != null)
            {
                switch (PLServer.GetCurrentSector().VisualIndication)
                {
                    case ESectorVisualIndication.TOPSEC:
                        Missions.LostColony(__instance, ref LastDestiny);
                        return;
                    case ESectorVisualIndication.LCWBATTLE:
                        Missions.WarpGuardianBattle(__instance);
                        return;
                    case ESectorVisualIndication.WASTEDWING:
                        Missions.WastedWing(__instance, ref LastDestiny);
                        return;
                    case ESectorVisualIndication.COLONIAL_HUB:
                    case ESectorVisualIndication.WD_START:
                    case ESectorVisualIndication.AOG_HUB:
                    case ESectorVisualIndication.CORNELIA_HUB:
                    case ESectorVisualIndication.FLUFFY_FACTORY_01:
                        if (Captain.GetSectorMissions(__instance, ref LastOrder, ref LastDestiny)) return;
                        Shop.BuyEssentials();
                        break;
                    case ESectorVisualIndication.EXOTIC1:
                    case ESectorVisualIndication.EXOTIC2:
                    case ESectorVisualIndication.EXOTIC3:
                    case ESectorVisualIndication.EXOTIC4:
                    case ESectorVisualIndication.EXOTIC5:
                    case ESectorVisualIndication.EXOTIC6:
                    case ESectorVisualIndication.EXOTIC7:
                    case ESectorVisualIndication.GENTLEMEN_START:
                    case ESectorVisualIndication.SPACE_SCRAPYARD:
                    case ESectorVisualIndication.FLUFFY_FACTORY_02:
                    case ESectorVisualIndication.FLUFFY_FACTORY_03:
                    case ESectorVisualIndication.SPACE_CAVE_2:
                    case ESectorVisualIndication.GENERAL_STORE:
                        Shop.BuyEssentials();
                        break;
                    case ESectorVisualIndication.CYPHER_LAB:
                        if (Captain.GetSectorMissions(__instance, ref LastOrder, ref LastDestiny)) return;
                        break;
                    case ESectorVisualIndication.DESERT_HUB:
                        if (Missions.Burrow(__instance, ref LastAction)) return;
                        break;
                    case ESectorVisualIndication.RACING_SECTOR:
                    case ESectorVisualIndication.RACING_SECTOR_2:
                    case ESectorVisualIndication.RACING_SECTOR_3:
                        if (Missions.Races(__instance, ref LastAction)) return;
                        break;
                    case ESectorVisualIndication.HIGHROLLERS_STATION:
                        Missions.HighRollers(__instance);
                        break;
                    case ESectorVisualIndication.WD_MISSIONCHAIN_WEAPONS_DEMO:
                        if (Missions.WD_Weapons_Testing(__instance)) return;
                        break;
                    case ESectorVisualIndication.GREY_HUNTSMAN_HQ:
                        if (Missions.GreyHuntsmanHQ(__instance, ref LastAction)) return;
                        break;
                }
            }
            #endregion

            Hostiles.BoardShip(__instance, ref LastOrder);
            Hostiles.ClaimShip(__instance);
            if (__instance.StartingShip.CurrentRace != null) __instance.StartingShip.AutoTarget = false; //Disables ship autotarget when racing
            else __instance.StartingShip.AutoTarget = true;
            Captain.CaptainOrders(__instance, ref LastOrder, ref LastAction, HasIntruders);
            if (Time.time - __instance.StartingShip.LastTookDamageTime() < 10f && __instance.StartingShip.AlertLevel == 0) //Yellow alert if took damage recently and doesn't have a target
            {
                __instance.StartingShip.AlertLevel = 1;
            }
            Captain.AnswerComms(__instance, ref LastAction);
            Captain.UpdateWarpPath(__instance);
            Captain.IdleInChair(__instance, ref LastAction);
            Captain.EmergencyBlindJump(__instance, ref LastAction);
        }
        static float LastDestiny = Time.time;
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

    [HarmonyPatch(typeof(PLBotController), "HandleMovement")]
    class Rotation
    {
        static void Postfix(PLBotController __instance)
        {
            if (__instance.Bot_TargetXRot == 0f)
            {
                __instance.Bot_TargetXRot = __instance.TargetRot.eulerAngles.x;
                __instance.StoredXRot = Mathf.LerpAngle(__instance.StoredXRot, __instance.Bot_TargetXRot, Time.deltaTime * 5f);
            }
        }
    }

    [HarmonyPatch(typeof(PLUIClassSelectionMenu), "Update")]
    class SpawnBot
    {
        public static bool capisbot = false;
        public static float delay = 0f;
        public static bool crewisbot = false;
        static void Postfix()
        {
            if (PLEncounterManager.Instance.PlayerShip != null && PLServer.Instance.GetCachedFriendlyPlayerOfClass(0, PLEncounterManager.Instance.PlayerShip) == null && delay > 5f && PhotonNetwork.isMasterClient && !capisbot)
            {
                capisbot = true;
                PLServer.Instance.ServerAddCrewBotPlayer(0);
                PLServer.Instance.GameHasStarted = true;
                PLServer.Instance.CrewPurchaseLimitsEnabled = false;
                PLGlobal.Instance.LoadedAIData = PLGlobal.Instance.GenerateDefaultPriorities();
                PLServer.Instance.SetCustomCaptainOrderText(0, "Use the WarpGate!", false);
                PLServer.Instance.SetCustomCaptainOrderText(1, "Engage Repair Protocols!", false);
                PLServer.Instance.SetCustomCaptainOrderText(2, "Align and Jump!", false);
                PLServer.Instance.SetCustomCaptainOrderText(3, "Collect Missions!", false);
                PLServer.Instance.SetCustomCaptainOrderText(4, "Explore Planet!", false);
                PLServer.Instance.SetCustomCaptainOrderText(5, "Complete Mission!", false);
            }
            else if (PLEncounterManager.Instance.PlayerShip != null && PLServer.Instance.GetCachedFriendlyPlayerOfClass(0, PLEncounterManager.Instance.PlayerShip) == null && PhotonNetwork.isMasterClient) delay += Time.deltaTime;
            else delay = 0;
        }
    }
    [HarmonyPatch(typeof(PLPlayer), "GetAIData")]
    class CapbotReciveAI
    {
        /*
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> Instructions)
        {
            List<CodeInstruction> instructionsList = Instructions.ToList();
            instructionsList[10].opcode = OpCodes.Ldc_I4_M1;
            return instructionsList.AsEnumerable();
        }
        */
        static void Postfix(PLPlayer __instance, ref AIDataIndividual __result)
        {
            if (__instance.cachedAIData == null && SpawnBot.capisbot && __instance.TeamID == 0 && __instance.IsBot)
            {
                __instance.cachedAIData = new AIDataIndividual();
                PLGlobal.Instance.SetupClassDefaultData(ref __instance.cachedAIData, __instance.GetClassID(), false);
            }
            __result = __instance.cachedAIData;
        }
    }

    [HarmonyPatch(typeof(PLGlobal), "EnterNewGame")]
    class OnJoin
    {
        static void Postfix()
        {
            SpawnBot.delay = 0f;
            SpawnBot.capisbot = false;
            SpawnBot.crewisbot = false;
        }
    }

    #region HostAbilities
    [HarmonyPatch(typeof(PLTabMenu), "BeginDrag_SCD")]
    class DragComp
    {
        static void Postfix(PLTabMenu __instance, PLTabMenu.ShipComponentDisplay inSCD)
        {
            if (inSCD == null || inSCD.Component == null)
            {
                return;
            }
            if (PLNetworkManager.Instance.LocalPlayer != null && PhotonNetwork.isMasterClient && !inSCD.Component.Slot.Locked && SpawnBot.capisbot)
            {
                PLDraggedShipCompUI.Instance.DraggedComponent = inSCD.Component;
            }
        }
    }
    [HarmonyPatch(typeof(PLTabMenu), "LocalPlayerCanEditTalentsOfPlayer")]
    class TalentsOfBots
    {
        static void Postfix(PLPlayer inPlayer, ref bool __result)
        {
            if (PLNetworkManager.Instance != null && inPlayer != null && PLNetworkManager.Instance.LocalPlayer != null)
            {
                if (inPlayer.IsBot && inPlayer.TeamID == 0 && SpawnBot.capisbot && PhotonNetwork.isMasterClient)
                {
                    __result = true;
                }
            }
        }
    }
    [HarmonyPatch(typeof(PLOverviewPlayerInfoDisplay), "UpdateButtons")]
    class AddBots 
    {
        static void Postfix(PLOverviewPlayerInfoDisplay __instance) 
        {
            __instance.ButtonsActiveTypes.Clear();
            if (__instance.MyPlayer == null)
            {
                if (PLNetworkManager.Instance.LocalPlayer != null && ((PhotonNetwork.isMasterClient && SpawnBot.capisbot) || PLNetworkManager.Instance.LocalPlayer.GetClassID() == 0))
                {
                    if (PLServer.Instance.GetCachedFriendlyPlayerOfClass(1) == null)
                    {
                        __instance.ButtonsActiveTypes.Add(PLOverviewPlayerInfoDisplay.EPlayerButtonType.E_ADD_BOT_PILOT);
                    }
                    if (PLServer.Instance.GetCachedFriendlyPlayerOfClass(2) == null)
                    {
                        __instance.ButtonsActiveTypes.Add(PLOverviewPlayerInfoDisplay.EPlayerButtonType.E_ADD_BOT_SCI);
                    }
                    if (PLServer.Instance.GetCachedFriendlyPlayerOfClass(3) == null)
                    {
                        __instance.ButtonsActiveTypes.Add(PLOverviewPlayerInfoDisplay.EPlayerButtonType.E_ADD_BOT_WEAP);
                    }
                    if (PLServer.Instance.GetCachedFriendlyPlayerOfClass(4) == null)
                    {
                        __instance.ButtonsActiveTypes.Add(PLOverviewPlayerInfoDisplay.EPlayerButtonType.E_ADD_BOT_ENG);
                    }
                }
            }
            else
            {
                if (__instance.MyPlayer.IsBot && PLNetworkManager.Instance.LocalPlayer != null && ((PhotonNetwork.isMasterClient && SpawnBot.capisbot) || PLNetworkManager.Instance.LocalPlayer.GetClassID() == 0) && __instance.MyPlayer.GetClassID() != 0)
                {
                    __instance.ButtonsActiveTypes.Add(PLOverviewPlayerInfoDisplay.EPlayerButtonType.E_REMOVE_BOT);
                }
                if (SteamManager.Initialized && __instance.MyPlayer.SteamIDIsVisible && __instance.MyPlayer.GetPhotonPlayer() != null && __instance.MyPlayer.GetPhotonPlayer().SteamID != CSteamID.Nil)
                {
                    __instance.ButtonsActiveTypes.Add(PLOverviewPlayerInfoDisplay.EPlayerButtonType.E_ADD_FRIEND);
                }
                if (PLNetworkManager.Instance.LocalPlayer != __instance.MyPlayer && __instance.MyPlayer.TS_ValidClientID && PLVoiceChatManager.Instance.GetIsFullyStarted())
                {
                    __instance.ButtonsActiveTypes.Add(PLOverviewPlayerInfoDisplay.EPlayerButtonType.E_MUTE);
                }
                if (!__instance.MyPlayer.IsBot && PLNetworkManager.Instance.LocalPlayer != null && ((PhotonNetwork.isMasterClient && SpawnBot.capisbot) || PLNetworkManager.Instance.LocalPlayer.GetClassID() == 0) && __instance.MyPlayer.GetPhotonPlayer() != null && __instance.MyPlayer.GetClassID() != 0 && !__instance.MyPlayer.GetPhotonPlayer().isMasterClient)
                {
                    __instance.ButtonsActiveTypes.Add(PLOverviewPlayerInfoDisplay.EPlayerButtonType.E_KICK);
                }
            }
            for (int i = 0; i < 4; i++)
            {
                if (i < __instance.Buttons.Length)
                {
                    __instance.Buttons[i].MyPID = __instance;
                    if (i < __instance.ButtonsActiveTypes.Count)
                    {
                        if (__instance.Buttons[i].m_Label != null && !__instance.Buttons[i].m_Label.gameObject.activeSelf)
                        {
                            __instance.Buttons[i].m_Label.gameObject.SetActive(true);
                        }
                        __instance.Buttons[i].m_Label.text = __instance.GetStringFromButtonType(__instance.ButtonsActiveTypes[i]);
                    }
                    else if (__instance.Buttons[i].m_Label != null && __instance.Buttons[i].m_Label.gameObject.activeSelf)
                    {
                        __instance.Buttons[i].m_Label.gameObject.SetActive(false);
                    }
                }
            }
        }
    }
    #endregion
}
