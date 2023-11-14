using HarmonyLib;
using ResoniteModLoader;
using FrooxEngine;
using System.Reflection;
using Elements.Core;
using System;
using System.Text;
using System.Collections.Generic;

namespace FixSessionBrowser
{
	public class FixSessionBrowser : ResoniteMod
	{
		public override string Name => "FixSessionBrowser";
		public override string Author => "Nytra";
		public override string Version => "1.0.0";
		public override string Link => "https://github.com/Nytra/ResoniteFixSessionBrowser";

		public static ModConfiguration Config;

		[AutoRegisterConfigKey]
		private static ModConfigurationKey<bool> FIX_WORLDDETAIL = new ModConfigurationKey<bool>("FIX_WORLDDETAIL", "Fix WorldDetail:", () => true, internalAccessOnly: true);
		[AutoRegisterConfigKey]
		private static ModConfigurationKey<bool> FIX_WORLDTHUMBNAILITEM = new ModConfigurationKey<bool>("FIX_WORLDTHUMBNAILITEM", "Fix WorldThumbnailItem:", () => true, internalAccessOnly: true);

		public override void OnEngineInit()
		{
			Harmony harmony = new Harmony("owo.Nytra.FixSessionBrowser");
			Config = GetConfiguration();
			harmony.PatchAll();
		}

		private static MethodInfo forceUpdateMethod = AccessTools.Method(typeof(WorldItem), "ForceUpdate");

		[HarmonyPatch(typeof(WorldItem), "UpdateTarget")]
		class UpdateTargetPatch
		{
			public static bool Prefix(WorldItem __instance, string ____lastId, out bool __state)
			{
				__state = ____lastId == __instance.WorldOrSessionId.Value;
				return true;
			}
			public static void Postfix(WorldItem __instance, string ____lastId, Sync<bool> ____visited, bool __state) 
			{
				if (__state == false && ____lastId != null && !____lastId.StartsWith("S-", StringComparison.InvariantCultureIgnoreCase))
				{
					if (____visited.Value == true && Config.GetValue(FIX_WORLDDETAIL) && __instance is WorldDetail)
					{
						// this should only run for worlds that the user has visited before (has the Visited text in the thumbnail)
						Debug($"UpdateTarget: Scheduling update for WorldDetail {__instance.WorldOrSessionId.Value}");
                        __instance.RunSynchronously(() =>
                        {
                            if (__instance == null)
                            {
                                Debug("__instance became null!");
                                return;
                            }
                            else
                            {
                                Debug($"Forcing update for WorldDetail {__instance.WorldOrSessionId.Value}");
                                forceUpdateMethod.Invoke(__instance, new object[] { false });
                            }
                        });
                    }
				}
			}
		}

		[HarmonyPatch(typeof(WorldItem), "OnWorldIdSessionsChanged")]
		class OnWorldIdSessionsChangedPatch
		{
			public static void Postfix(WorldItem __instance)
			{
				if (Config.GetValue(FIX_WORLDTHUMBNAILITEM) && __instance != null && __instance is WorldThumbnailItem)
				{
					Debug($"OnWorldIdSessionsChanged: Scheduling update for WorldThumbnailItem {__instance.WorldOrSessionId.Value}");
					__instance.RunSynchronously(() =>
					{
						if (__instance == null)
						{
							Debug("__instance became null!");
							return;
						}
						else
						{
							Debug($"Forcing update for WorldThumbnailItem {__instance.WorldOrSessionId.Value}");
							forceUpdateMethod.Invoke(__instance, new object[] { true });
						}
					});
				}
			}
		}

	}
}