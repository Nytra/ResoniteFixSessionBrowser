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
		private static ModConfigurationKey<bool> FIX_WORLDDETAIL = new ModConfigurationKey<bool>("FIX_WORLDDETAIL", "Fix WorldDetail:", () => true);
		[AutoRegisterConfigKey]
		private static ModConfigurationKey<bool> FIX_WORLDTHUMBNAILITEM = new ModConfigurationKey<bool>("FIX_WORLDTHUMBNAILITEM", "Fix WorldThumbnailItem (Does nothing):", () => true);

		public override void OnEngineInit()
		{
			Harmony harmony = new Harmony("owo.Nytra.FixSessionBrowser");
			Config = GetConfiguration();
			harmony.PatchAll();
		}

		private static MethodInfo forceUpdateMethod = AccessTools.Method(typeof(WorldItem), "ForceUpdate");

		//[HarmonyPatch(typeof(WorldDetail), nameof(WorldDetail.OpenWorldDetail))]
		//class FixSessionBrowserPatch
		//{
		//	public static bool Prefix(WorldItem item)
		//	{
		//		// true: run the original method
		//		// false: skip the original method
		//		if (item == null) return true;
		//		Debug("item.WorldOrSessionId.Value: " + item.WorldOrSessionId.Value);
		//		Debug("item.CachedRecord.Name: " + item.CachedRecord?.Name ?? "null");
		//      return true;
		//	}
		//}

		//[HarmonyPatch(typeof(WorldThumbnailItem), "UpdateInfo")]
		//class UpdateInfoPatch
		//{
		//	public static void Postfix(WorldThumbnailItem __instance, SyncRef<FrooxEngine.UIX.Text> ____nameText)
		//	{
		//		if (__instance == null || __instance.WorldOrSessionId.Value == null || ____nameText.Value == null) return;
		//		Debug(____nameText.Target.Content + " " + __instance.WorldOrSessionId.Value);
		//	}
		//}

		//[HarmonyPatch(typeof(WorldListManager), "OnAwake")]
		//class OnAwakePatch
		//{
		//	public static void Postfix(WorldListManager __instance)
		//	{
		//		__instance.RunInSeconds(5, () => 
		//		{
		//			__instance.MergeSessionsByWorldId.Value = false;
		//			Debug("Set the value to false!");
		//		});
		//	}
		//}

		[HarmonyPatch(typeof(WorldItem), "UpdateTarget")]
		class UpdateTargetPatch
		{
			public static bool Prefix(WorldItem __instance, string ____lastId, out bool __state)
			{
				__state = ____lastId == __instance.WorldOrSessionId.Value;
				return true;
			}
			public static void Postfix(WorldItem __instance, string ____lastId, Sync<bool> ____visited, Record prefetchedRecord, Record ____worldRecord, bool __state) 
			{
				if (__state == false && ____lastId != null && !____lastId.StartsWith("S-", StringComparison.InvariantCultureIgnoreCase))
				{
					bool scheduleUpdate = false;
					//bool isWorldThumbnailItem = false;
					if (____visited.Value == true && Config.GetValue(FIX_WORLDDETAIL) && __instance is WorldDetail)
					{
						//Debug("WorldDetail");
						// this sometimes runs for items that don't have the (Visited) text
						Debug($"UpdateTarget: Scheduling update for WorldDetail {__instance.WorldOrSessionId.Value}");
						scheduleUpdate = true;
					}
					//else if (__instance is WorldThumbnailItem && Config.GetValue(FIX_WORLDTHUMBNAILITEM))
					//{
					//	Debug("WorldThumbnailItem");
					//	isWorldThumbnailItem = true;
					//	if (true)//prefetchedRecord == null && ____worldRecord == null)
					//	{
					//		Debug($"Scheduling update for WorldThumbnailItem {__instance.WorldOrSessionId.Value}");
					//		scheduleUpdate = true;
					//	}
					//}
					if (scheduleUpdate)
					{
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

		//[HarmonyPatch(typeof(WorldThumbnailItem), "UpdateInfo")]
		//class UpdateInfoPatch
		//{
		//	public static void Postfix(WorldThumbnailItem __instance, IReadOnlyList<SkyFrost.Base.SessionInfo> sessions)
		//	{
		//		if (__instance == null || sessions == null) 
		//		{
		//			Debug("Instance or sessions null");
		//			return;
		//		}
		//		Debug($"WorldThumbnailItem: SessionsCount: {sessions.Count} ID: {__instance.WorldOrSessionId.Value}");
		//	}
		//}

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