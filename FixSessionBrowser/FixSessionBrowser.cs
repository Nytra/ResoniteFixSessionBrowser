using HarmonyLib;
using ResoniteModLoader;
using FrooxEngine;
using System.Reflection;
using System;

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
		private static ModConfigurationKey<bool> MOD_ENABLED = new ModConfigurationKey<bool>("MOD_ENABLED", "Mod enabled:", () => true);
		[AutoRegisterConfigKey]
		private static ModConfigurationKey<bool> EXTRA_DEBUG = new ModConfigurationKey<bool>("EXTRA_DEBUG", "Extra debug logging:", () => false, internalAccessOnly: true);

		public override void OnEngineInit()
		{
			Harmony harmony = new Harmony("owo.Nytra.FixSessionBrowser");
			Config = GetConfiguration();
			harmony.PatchAll();
		}

		private static MethodInfo forceUpdateMethod = AccessTools.Method(typeof(WorldItem), "ForceUpdate");
		private static FieldInfo counterRootField = AccessTools.Field(typeof(WorldThumbnailItem), "_counterRoot");

		private static void ExtraDebug(string msg)
		{
			if (Config.GetValue(EXTRA_DEBUG))
			{
				Debug(msg);
			}
		}

		private static void ScheduleForceUpdate(WorldItem item)
		{
			bool isWorldThumbnailItem = item is WorldThumbnailItem;
			string text = isWorldThumbnailItem ? "WorldThumbnailItem" : "WorldDetail";
			ExtraDebug($"Scheduling update for {text} {item.WorldOrSessionId.Value}");
			item.RunSynchronously(() =>
			{
				if (item == null)
				{
					ExtraDebug("instance became null!");
					return;
				}
				else
				{
					Debug($"Forcing update for {text} {item.WorldOrSessionId.Value}");
					forceUpdateMethod.Invoke(item, new object[] { isWorldThumbnailItem });
				}
			});
		}

		[HarmonyPatch(typeof(WorldItem), "UpdateTarget")]
		class UpdateTargetPatch
		{
			public static bool Prefix(WorldItem __instance, string ____lastId, out bool __state)
			{
				// the postfix should only run if the _lastId does not equal the WorldOrSessionId at the beginning of the method
				__state = ____lastId == __instance?.WorldOrSessionId.Value;
				return true;
			}

			public static void Postfix(WorldItem __instance, string ____lastId, Sync<bool> ____visited, bool __state) 
			{
				if (Config.GetValue(MOD_ENABLED) && __state == false && __instance != null && __instance is WorldDetail && ____lastId != null && !____lastId.StartsWith("S-", StringComparison.InvariantCultureIgnoreCase))
				{
					// this should only run for worlds that the user has visited before (has the Visited text in the thumbnail)
					// https://github.com/Yellow-Dog-Man/Resonite-Issues/issues/675
					if (____visited.Value == true)
					{
						ExtraDebug("UpdateTarget - WorldDetail");
						ScheduleForceUpdate(__instance);
					}
				}
			}
		}

		[HarmonyPatch(typeof(WorldItem), "OnWorldIdSessionsChanged")]
		class OnWorldIdSessionsChangedPatch
		{
			public static void Postfix(WorldItem __instance)
			{
				if (Config.GetValue(MOD_ENABLED) == true && __instance != null)
				{
					if (__instance is WorldThumbnailItem thumbnailItem)
					{
						// If this is a WorldThumbnailItem, only force update if it is showing a world and not a session
						// https://github.com/Yellow-Dog-Man/Resonite-Issues/issues/164
						__instance.RunSynchronously(() => 
						{
							if (thumbnailItem != null)
							{
								// Theoretically: if the counterRoot target slot is deactivated, it means it is showing a world and not a session

								// this check might need to be improved somehow, because it seems to always force an update here even if
								// the item is not a world. this makes it inefficient.
								var counterRoot = (SyncRef<FrooxEngine.UIX.RectTransform>)counterRootField.GetValue(thumbnailItem);
								if (counterRoot != null && counterRoot.Target != null)
								{
									if (counterRoot.Target.Slot.ActiveSelf == false)
									{
										ExtraDebug("OnWorldIdSessionsChanged - WorldThumbnailItem");
										ScheduleForceUpdate(__instance);
									}
									else
									{
										ExtraDebug("Counter is active!");
									}
								}
								else
								{
									ExtraDebug("Counter is null!");
								}
							}
						});
					}
					else if (__instance is WorldDetail)
					{
						// If this is a WorldDetail, always force update
						// this prevents the session from disappearing while the detail panel is open
						ExtraDebug("OnWorldIdSessionsChanged - WorldDetail");
						ScheduleForceUpdate(__instance);
					}
				}
			}
		}
	}
}