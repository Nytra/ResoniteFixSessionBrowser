using HarmonyLib;
using ResoniteModLoader;
using FrooxEngine;
using System.Reflection;
using Elements.Core;
using System;

namespace ModNameHere
{
	public class ModNameHere : ResoniteMod
	{
		public override string Name => "ModNameHere";
		public override string Author => "Nytra";
		public override string Version => "1.0.0";
		public override string Link => "https://github.com/Nytra/ResoniteAccessibleFullBodyCalibrator";
		public override void OnEngineInit()
		{
			Harmony harmony = new Harmony("owo.Nytra.ModNameHere");
			harmony.PatchAll();
		}

		[HarmonyPatch(typeof(FullBodyCalibratorDialog), "OnStartCalibration")]
		class AccessibleFullBodyCalibratorPatch
		{
			public static bool Prefix(FullBodyCalibratorDialog __instance)
			{
				// true: run the original method
				// false: skip the original method
				return true;
			}
		}
	}
}