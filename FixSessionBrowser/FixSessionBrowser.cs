using HarmonyLib;
using ResoniteModLoader;
using FrooxEngine;
using System.Reflection;
using Elements.Core;
using System;

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

		[HarmonyPatch(typeof(WorldDetail), nameof(WorldDetail.OpenWorldDetail))]
		class FixSessionBrowserPatch
        {
			public static bool Prefix(WorldItem item)
			{
				// true: run the original method
				// false: skip the original method
				Debug("item.WorldOrSessionId.Value: " + item.WorldOrSessionId.Value);
                Debug("item.CachedRecord.Name: " + item.CachedRecord.Name);
                return true;
			}
		}
	}
}