using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace COM3D2.GizmoFix.Plugin
{
	/// <summary>
	/// プラグイン本体
	/// </summary>
	[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
	public class GizmoRenderFix : BaseUnityPlugin
	{
		/// <summary>
		/// プラグイン名
		/// </summary>
		public const string PluginName = "Gizmo Render Fix Plug-In";

		/// <summary>
		/// プラグインGuid
		/// </summary>
		public const string PluginGuid = "jp.fumble.GizmoRenderFix";

		/// <summary>
		/// プラグインカンパニー
		/// </summary>
		public const string CompanyName = "Fumble Warez";

		/// <summary>
		/// プラグインコピーライト
		/// </summary>
		public const string Copyright = "Copyright © Fumble Warez 2026";

		/// <summary>
		/// プラグインバージョン
		/// </summary>
		public const string PluginVersion = "0.1.1.0";

		/// <summary>
		/// HarmonyLib(HarmonyX)のインスタンス
		/// </summary>
		private static Harmony harmony;

		/// <summary>
		/// コンストラクタ
		/// </summary>
		static GizmoRenderFix()
		{
			// パッチする
			if (harmony == null)
				harmony = new Harmony(PluginGuid);
			harmony.PatchAll(typeof(Patch_GizmoRender_RenderGizmos));
			harmony.PatchAll(typeof(Patch_GizmoRender_Awake));
		}


		/// <summary>
		/// GizmoRender.RenderGizmosパッチ
		/// 半円描画を全円に差し替え、あたり判定も全周に拡張する
		/// </summary>
		[HarmonyPatch(typeof(GizmoRender), "RenderGizmos")]
		static class Patch_GizmoRender_RenderGizmos
		{
			static readonly FieldInfo _uForward = AccessTools.Field(typeof(GizmoRender), "uForward");
			static readonly FieldInfo _rForward = AccessTools.Field(typeof(GizmoRender), "rForward");
			static readonly FieldInfo _fForward = AccessTools.Field(typeof(GizmoRender), "fForward");

			/// <summary>
			/// DrawCircleHalf呼び出しをDrawCircleに差し替える
			/// </summary>
			[HarmonyTranspiler]
			static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
			{
				var half = AccessTools.Method(typeof(GizmoRender), "DrawCircleHalf");
				var full = AccessTools.Method(typeof(GizmoRender), "DrawCircle");
				foreach (var inst in instructions)
				{
					if (inst.opcode == OpCodes.Call && inst.operand as MethodBase == half)
						yield return new CodeInstruction(OpCodes.Call, full);
					else
						yield return inst;
				}
			}

			/// <summary>
			/// 前向きベクトルをゼロにしてDot積を常に0以上にする
			/// → OnRenderObject内の半円チェックが常にtrueになり全周でつかめる
			/// </summary>
			[HarmonyPostfix]
			static void Postfix(GizmoRender __instance)
			{
				_uForward.SetValue(__instance, Vector3.zero);
				_rForward.SetValue(__instance, Vector3.zero);
				_fForward.SetValue(__instance, Vector3.zero);
			}
		}

		/// <summary>
		/// GizmoRender.Awakeパッチ
		/// 回転ハンドルのあたり判定幅を広げる（移動ハンドルには影響しない）
		/// </summary>
		[HarmonyPatch(typeof(GizmoRender), "Awake")]
		static class Patch_GizmoRender_Awake
		{
			/// <summary>
			/// lineRSelectedThickを既定の0.1fから0.2fに拡張する
			/// </summary>
			[HarmonyPostfix]
			static void Postfix(GizmoRender __instance)
			{
				__instance.lineRSelectedThick = 0.2f;
			}
		}
	}
}
