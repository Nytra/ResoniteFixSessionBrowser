using HarmonyLib;
using ResoniteModLoader;
using FrooxEngine;
using System.Reflection;
using System;
using SkyFrost.Base;
using System.Collections.Generic;

namespace FixSessionBrowser
{
	public class FixSessionBrowser : ResoniteMod
	{
		public override string Name => "FixSessionBrowser";
		public override string Author => "Nytra";
		public override string Version => "1.0.0-preview4";
		public override string Link => "https://github.com/Nytra/ResoniteFixSessionBrowser";

		public static ModConfiguration Config;

		[AutoRegisterConfigKey]
		private static ModConfigurationKey<bool> MOD_ENABLED = new ModConfigurationKey<bool>("MOD_ENABLED", "Mod enabled:", () => true);
		[AutoRegisterConfigKey]
		private static ModConfigurationKey<bool> RESELECT = new ModConfigurationKey<bool>("RESELECT", "Reselect the session/world in the detail view after it updates:", () => true);
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
		private static FieldInfo selectedItemField = AccessTools.Field(typeof(WorldDetail), "_selectedItem");
		private static Type sessionSelectionItemType = AccessTools.TypeByName("FrooxEngine.WorldDetail+SessionSelectionItem");
		private static FieldInfo sessionField = AccessTools.Field(sessionSelectionItemType, "session");
		private static FieldInfo worldField = AccessTools.Field(sessionSelectionItemType, "world");
		private static FieldInfo sessionSelectionListField = AccessTools.Field(typeof(WorldDetail), "_sessionSelectionList");
		private static MethodInfo updateSelectedMethod = AccessTools.Method(typeof(WorldDetail), "UpdateSelected");
		private static MethodInfo updateSessionItemsMethod = AccessTools.Method(typeof(WorldDetail), "UpdateSessionItems");

		private static HashSet<WorldItem> worldItemSet = new HashSet<WorldItem>();

		private static void ExtraDebug(string msg)
		{
			if (Config.GetValue(EXTRA_DEBUG))
			{
				Debug(msg);
			}
		}

		private static string GetSessionIdFromSessionSelectionItem(object obj)
		{
			SessionInfo session = (SessionInfo)sessionField.GetValue(obj);
			return session?.SessionId;
		}

		private static World GetWorldFromSessionSelectionItem(object obj)
		{
			return (World)worldField.GetValue(obj);
		}

		private static void ScheduleForceUpdate(WorldItem item, object selectedItem = null)
		{
			// check if this WorldItem is already being updated
			if (worldItemSet.Contains(item))
			{
				ExtraDebug($"WorldItem already in worldItemSet: {item.WorldOrSessionId.Value}");
				return;
			}

			worldItemSet.Add(item);
			ExtraDebug($"WorldItem added to worldItemSet. New size of worldItemSet: {worldItemSet.Count}");

			WorldDetail worldDetail = item as WorldDetail;
			string text = worldDetail == null ? "WorldThumbnailItem" : "WorldDetail";
			string selectedSessionId = null;
			World selectedWorld = null;
			if (selectedItem != null && selectedItem.GetType() == sessionSelectionItemType)
			{
				selectedSessionId = GetSessionIdFromSessionSelectionItem(selectedItem);
				if (selectedSessionId == null)
				{
					selectedWorld = GetWorldFromSessionSelectionItem(selectedItem);
				}
			}
			ExtraDebug($"Scheduling update for {text} {item.WorldOrSessionId.Value}");
			item.RunSynchronously(() =>
			{
				if (item == null)
				{
					ExtraDebug("instance became null!");
				}
				else
				{
					Msg($"Forcing update for {text} {item.WorldOrSessionId.Value}");
					forceUpdateMethod.Invoke(item, new object[] { worldDetail == null }); // Only notify the WorldListManager if the item is a WorldThumbnailItem
					if (Config.GetValue(RESELECT) && (selectedSessionId != null || selectedWorld != null))
					{
						// do scary reflection to reselect the session/world after the WorldDetail updates
						// this is just a quality of life improvement
						var sessionSelectionList = (System.Collections.IList)sessionSelectionListField.GetValue(worldDetail);
						if (sessionSelectionList == null) return;
						int i = 0;
						bool found = false;
						ExtraDebug("Searching for the session/world...");
						foreach(object listElement in sessionSelectionList)
						{
							if (listElement != null)
							{
								if (selectedSessionId != null)
								{
									string sessionId = GetSessionIdFromSessionSelectionItem(listElement);
									if (sessionId != null && sessionId.Equals(selectedSessionId, StringComparison.InvariantCultureIgnoreCase))
									{
										ExtraDebug($"Found the session: {sessionId}");
										found = true;
										break;
									}
								}
								else if (selectedWorld != null)
								{
									World world = GetWorldFromSessionSelectionItem(listElement);
									if (world != null && world == selectedWorld)
									{
										ExtraDebug($"Found the world: {world.Name}");
										found = true;
										break;
									}
								}
							}
							i++;
						}
						if (found)
						{
							ExtraDebug("Reselecting the session/world.");
							selectedItemField.SetValue(worldDetail, sessionSelectionList[i]);
							updateSelectedMethod.Invoke(worldDetail, new object[] { });
							updateSessionItemsMethod.Invoke(worldDetail, new object[] { });
						}
					}
				}
				if (item != null)
				{
					//ExtraDebug($"Removing WorldItem from worldItemSet: {item?.WorldOrSessionId.Value}");
					worldItemSet.Remove(item);
					ExtraDebug($"WorldItem removed from worldItemSet. New size of worldItemSet: {worldItemSet.Count}. Item: {item.WorldOrSessionId.Value}");
				}
				worldItemSet.TrimExcess();
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

						// run this at the end of the current update
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
									if (counterRoot.Target.Slot?.ActiveSelf == false)
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
					else if (__instance is WorldDetail worldDetail)
					{
						// If this is a WorldDetail, always force update
						// this prevents the session from disappearing while the detail panel is open
						// https://github.com/Yellow-Dog-Man/Resonite-Issues/issues/643
						ExtraDebug("OnWorldIdSessionsChanged - WorldDetail");
						var selectedItem = selectedItemField.GetValue(worldDetail);
						ScheduleForceUpdate(__instance, selectedItem is FrooxEngine.Record ? null : selectedItem);
					}
				}
			}
		}
	}
}