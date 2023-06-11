using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CapBot.Routines
{
    internal class Shop
    {
        static float ShopRepMultiplier()
        {
            float num = 1f;
            if (PLServer.GetCurrentSector() != null)
            {
                int faction = PLServer.GetCurrentSector().MySPI.Faction;
                if (faction != -1)
                {
                    bool flag = true;
                    if (faction == 1 && PLServer.GetCurrentSector().VisualIndication != ESectorVisualIndication.AOG_HUB)
                    {
                        flag = false;
                    }
                    if (flag)
                    {
                        num -= 0.05f * (float)PLServer.Instance.RepLevels[faction];
                    }
                }
            }
            return Mathf.Clamp(num, 0.5f, 2f);
        }
        internal static void BuyEssentials()
        {
            if (PLEncounterManager.Instance.PlayerShip.NumberOfFuelCapsules <= 15)//Buy fuel capsules if needed
            {
                int numoffuels = PLServer.Instance.CurrentCrewCredits / (int)(PLServer.Instance.GetFuelBasePrice() * ShopRepMultiplier()) / 2;
                numoffuels = Mathf.Min(numoffuels, 200 - PLEncounterManager.Instance.PlayerShip.NumberOfFuelCapsules);
                for (int i = 0; i < numoffuels; i++)
                {
                    PLServer.Instance.photonView.RPC("CaptainBuy_Fuel", PhotonTargets.All, new object[]
                    {
                         PLEncounterManager.Instance.PlayerShip.ShipID,
                         (int)(PLServer.Instance.GetFuelBasePrice()* ShopRepMultiplier())
                    });
                }
            }
            if (PLEncounterManager.Instance.PlayerShip.ReactorCoolantLevelPercent < 0.9f)//Buy coolant if needed
            {
                int numofcoolant = PLServer.Instance.CurrentCrewCredits / (int)(PLServer.Instance.GetCoolantBasePrice() * ShopRepMultiplier());
                numofcoolant = Mathf.Min(numofcoolant, (int)((1 - PLEncounterManager.Instance.PlayerShip.ReactorCoolantLevelPercent) * 8));
                for (int i = 0; i < numofcoolant; i++)
                {
                    PLServer.Instance.photonView.RPC("CaptainBuy_Coolant", PhotonTargets.All, new object[]
                    {
                         PLEncounterManager.Instance.PlayerShip.ShipID,
                         (int)(PLServer.Instance.GetCoolantBasePrice() * ShopRepMultiplier())
                    });
                }

            }
            foreach (PLShipComponent component in PLEncounterManager.Instance.PlayerShip.MyStats.AllComponents)//Buy missile refill if needed
            {
                if (component is PLTrackerMissile)
                {
                    PLTrackerMissile missile = component as PLTrackerMissile;
                    if (missile.SubTypeData < missile.AmmoCapacity && missile.IsEquipped)
                    {
                        if ((missile.AmmoCapacity - missile.SubTypeData) * missile.MissileRefillPrice * ShopRepMultiplier() < PLServer.Instance.CurrentCrewCredits)
                        {
                            PLServer.Instance.photonView.RPC("CaptainBuy_MissileRefill", PhotonTargets.All, new object[]
                            {
                                PLEncounterManager.Instance.PlayerShip.ShipID,
                                missile.NetID,
                                missile.AmmoCapacity - missile.SubTypeData,
                                (int)((missile.AmmoCapacity - missile.SubTypeData) * missile.MissileRefillPrice * ShopRepMultiplier())
                            });
                        }

                    }
                }
            }
        }
    }
}
