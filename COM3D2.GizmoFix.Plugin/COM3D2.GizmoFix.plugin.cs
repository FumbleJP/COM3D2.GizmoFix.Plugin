using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx;
using BepInEx.Configuration;
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
		public const string PluginVersion = "0.2.2.0";

		/// <summary>
		/// HarmonyLib(HarmonyX)のインスタンス
		/// </summary>
		private static Harmony harmony;

		/// <summary>
		/// IKDragPointのサイズ
		/// </summary>
		private ConfigEntry<float> ikDragPointSize;

		/// <summary>
		/// コンストラクタ
		/// </summary>
		static GizmoRenderFix()
		{
			// パッチする
			if (harmony == null)
				harmony = new Harmony(PluginGuid);
		}

		public void Awake()
		{
			DontDestroyOnLoad(this);
			this.ikDragPointSize = this.Config.Bind("IKDragPoint Settings", "Size", 0.04f, "IKDragPointの固定サイズ（0に設定すると非固定の従来の動作）");
			harmony.PatchAll(typeof(Patch_GizmoRender_RenderGizmos));
			harmony.PatchAll(typeof(Patch_GizmoRender_Awake));
			Patch_WorldTransformAxis_ConstantScreenSize.ikDragPointSize = this.ikDragPointSize;
			harmony.PatchAll(typeof(Patch_WorldTransformAxis_ConstantScreenSize));
		}

		/// <summary>
		/// [外部インターフェース]IKDragPointのサイズ変更
		/// </summary>
		/// <param name="ikDragPointSize">サイズ</param>
		public void GRFSetIKDragPointSize(object ikDragPointSize)
		{
			try
			{
				if (ikDragPointSize is float value)
					this.ikDragPointSize.Value = value;
			}
			catch { /* ignored */}
		}

		/// <summary>
		/// GizmoRender.RenderGizmosパッチ
		/// 半円描画を全円に差し替え、あたり判定も全周に拡張する
		/// </summary>
		[HarmonyPatch(typeof(GizmoRender), "RenderGizmos")]
		public static class Patch_GizmoRender_RenderGizmos
		{
			private static readonly FieldInfo _uForward = AccessTools.Field(typeof(GizmoRender), "uForward");
			private static readonly FieldInfo _rForward = AccessTools.Field(typeof(GizmoRender), "rForward");
			private static readonly FieldInfo _fForward = AccessTools.Field(typeof(GizmoRender), "fForward");

			/// <summary>
			/// DrawCircleHalf呼び出しをDrawCircleに差し替える
			/// </summary>
			[HarmonyTranspiler]
			public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
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
			public static void Postfix(GizmoRender __instance)
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
		public static class Patch_GizmoRender_Awake
		{
			/// <summary>
			/// lineRSelectedThickを既定の0.1fから0.2fに拡張する
			/// </summary>
			[HarmonyPostfix]
			public static void Postfix(GizmoRender __instance)
			{
				__instance.lineRSelectedThick = 0.2f;
			}
		}

		[HarmonyPatch(typeof(WorldTransformAxis), "Update")]
		public static class Patch_WorldTransformAxis_ConstantScreenSize
		{
			static readonly FieldInfo _parentObj = AccessTools.Field(typeof(WorldTransformAxis), "parent_obj_");

			// 見た目のサイズ調整定数（大きくすると大きく表示）
			public static ConfigEntry<float> ikDragPointSize { get; set; }

			[HarmonyPostfix]
			public static void Postfix(WorldTransformAxis __instance)
			{
				// 設定無効化中なら抜ける
				if (ikDragPointSize.Value <= 0.0f)
					return;
				// parent_obj_ != null は子軸オブジェクト（X/Y/Z）なのでスキップ
				if (_parentObj.GetValue(__instance) != null)
					return;

				Camera camera = Camera.main;
				if (camera == null)
					return;

				var dist = Vector3.Distance(camera.transform.position, __instance.transform.position) * ikDragPointSize.Value;
				__instance.transform.localScale = new Vector3(dist, dist, dist);
			}
		}
	}
}
