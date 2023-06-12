using PulsarModLoader;
using System.Collections.Generic;
using UnityEngine;
using static PLBurrowArena;

[assembly: System.Runtime.CompilerServices.IgnoresAccessChecksTo("Assembly-CSharp")]
namespace CapBot
{
    public class Mod : PulsarMod
    {
        public override string Version => "Alpha 1.1.1";

        public override string Author => "pokegustavo";

        public override string ShortDescription => "Adds a bot as the captain";

        public override string Name => "CapBot";

        public override string HarmonyIdentifier()
        {
            return "pokegustavo.CapBot"; 
        }
    }
    public class UsefulMethods
    {
        public static Vector3 GetClosestLocation(PLPlayer CapBot, List<Vector3> Locations)
        {
            Vector3 Closest = Locations[0];
            foreach (Vector3 location in Locations)
            {
                if ((location - CapBot.GetPawn().transform.position).magnitude < (Closest - CapBot.GetPawn().transform.position).magnitude) Closest = location;
            }
            return Closest;
        }
    }
}
