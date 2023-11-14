using HarmonyLib;
using ResoniteModLoader;
using FrooxEngine;
using System.Reflection;
using Elements.Core;
using System;
using System.Text;

namespace FixSessionBrowser
{
	public class FixSessionBrowser : ResoniteMod
	{
		public override string Name => "FixSessionBrowser";
		public override string Author => "Nytra";
		public override string Version => "1.0.0";
		public override string Link => "https://github.com/Nytra/ResoniteFixSessionBrowser";
		public override void OnEngineInit()
		{
			Harmony harmony = new Harmony("owo.Nytra.FixSessionBrowser");
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
			public static void Postfix(WorldItem __instance, string ____lastId, Sync<bool> ____visited, Record prefetchedRecord, bool __state) 
			{
				if (__state == false && ____lastId != null && !____lastId.StartsWith("S-", StringComparison.InvariantCultureIgnoreCase))
				{
					bool scheduleUpdate = false;
					bool isWorldThumbnailItem = false;
					if (__instance is WorldDetail)
					{
						Debug("WorldItem instance is WorldDetail");
						if (____visited.Value == true)
						{
							Debug($"Scheduling update for WorldDetail {__instance.WorldOrSessionId.Value}");
							scheduleUpdate = true;
						}
					}
					else if (__instance is WorldThumbnailItem)
					{
						Debug("WorldItem instance is WorldThumbnailItem");
						isWorldThumbnailItem = true;
						if (prefetchedRecord != null)
						{
							Debug($"Scheduling update for WorldThumbnailItem {__instance.WorldOrSessionId.Value}");
							scheduleUpdate = true;
						}
					}
					if (scheduleUpdate)
					{
						__instance.RunInUpdates(3, () =>
						{
							if (__instance == null)
							{
								Debug("__instance became null!");
								return;
							}
							else
							{
								Debug($"Forcing update for WorldItem {__instance.WorldOrSessionId.Value}");
								forceUpdateMethod.Invoke(__instance, new object[] { isWorldThumbnailItem });
							}
						});
					}
				}
			}
		}
	}
}