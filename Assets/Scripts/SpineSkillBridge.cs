using System.Collections.Generic;
using Spine.Unity;
using UnityEngine;

namespace Game1
{
	internal static class SpineSkillBridge
	{
		private const int SPINE_SKILL_EFFECT = 9;
		private const int SPINE_LAYER = 31;

		private static Camera overlayCamera;
		private static RenderTexture overlayTexture;
		private static int textureWidth;
		private static int textureHeight;

		private static void EnsureOverlayCamera()
		{
			if (overlayCamera != null) return;
			GameObject cameraObject = new GameObject("SpineSkillBridgeOverlayCamera");
			Object.DontDestroyOnLoad(cameraObject);
			overlayCamera = cameraObject.AddComponent<Camera>();
			overlayCamera.enabled = false;
			overlayCamera.orthographic = true;
			overlayCamera.clearFlags = CameraClearFlags.SolidColor;
			overlayCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
			overlayCamera.cullingMask = 1 << SPINE_LAYER;
			overlayCamera.nearClipPlane = -1000f;
			overlayCamera.farClipPlane = 1000f;
			overlayCamera.transform.position = new Vector3(0f, 0f, -10f);
			EnsureOverlayTexture();
		}

		private static void EnsureOverlayTexture()
		{
			int width = Mathf.Max(1, Screen.width);
			int height = Mathf.Max(1, Screen.height);
			if (overlayTexture != null && textureWidth == width && textureHeight == height) return;
			if (overlayTexture != null)
			{
				overlayTexture.Release();
				Object.Destroy(overlayTexture);
			}
			textureWidth = width;
			textureHeight = height;
			overlayTexture = new RenderTexture(width, height, 16, RenderTextureFormat.ARGB32);
			overlayTexture.filterMode = FilterMode.Point;
			overlayTexture.Create();
			overlayCamera.targetTexture = overlayTexture;
			overlayCamera.orthographicSize = height * 0.5f;
			overlayCamera.allowMSAA = false;
		}

		private static Material drawMaterial;

		public static void DrawOverlay()
		{
			if (activeEffects.Count == 0) return;
			EnsureOverlayCamera();
			EnsureOverlayTexture();
			overlayCamera.Render();
			if (Event.current.type == EventType.Repaint)
			{
				if (drawMaterial == null)
				{
					Shader shader = Shader.Find("Spine/Skeleton");
					if (shader == null) shader = Shader.Find("Sprites/Default");
					drawMaterial = new Material(shader);
				}
				Graphics.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), overlayTexture, new Rect(0, 0, 1, 1), 0, 0, 0, 0, GUI.color, drawMaterial);
			}
		}

		private sealed class ActiveEffect
		{
			public int charId;
			public GameObject go;
			public long endTime;
			public int oldHead;
			public float scaleMultiplier;
		}

		private static readonly Dictionary<int, ActiveEffect> activeEffects = new Dictionary<int, ActiveEffect>();
		private static readonly Dictionary<string, SkeletonDataAsset> skeletonCache = new Dictionary<string, SkeletonDataAsset>();

		public static void HandleMessage(Message message)
		{
			try
			{
				int subType = message.reader().readByte();
				if (subType != SPINE_SKILL_EFFECT)
				{
					return;
				}
				int charId = message.reader().readInt();
				string skeletonPath = message.reader().readUTF();
				string animation = message.reader().readUTF();
				string skin = message.reader().readUTF();
				int durationMs = message.reader().readShort();

				Char c = GetChar(charId);
				Play(charId, skeletonPath, animation, skin, durationMs);
			}
			catch (System.Exception e)
			{
				Debug.LogError("[SpineSkillBridge] " + e.Message);
			}
		}

		public static void Update()
		{
			if (activeEffects.Count == 0)
			{
				return;
			}

			long now = mSystem.currentTimeMillis();
			float zoom = mGraphics.zoomLevel;
			List<int> toRemove = new List<int>();

			foreach (KeyValuePair<int, ActiveEffect> kvp in activeEffects)
			{
				ActiveEffect effect = kvp.Value;
				Char c = GetChar(effect.charId);
				if (c == null || effect.go == null || now >= effect.endTime)
				{
					toRemove.Add(kvp.Key);
					continue;
				}

				float x = Mathf.Round((c.cx - GameScr.cmx) * zoom - Screen.width * 0.5f);
				float y = Mathf.Round(Screen.height * 0.5f - (c.cy - GameScr.cmy + GameCanvas.transY) * zoom);
				effect.go.transform.position = new Vector3(x, y, 0f);
				float scale = 8.5f * zoom * effect.scaleMultiplier;
				effect.go.transform.localScale = new Vector3(scale * c.cdir, scale, 1f);
			}

			for (int i = 0; i < toRemove.Count; i++)
			{
				Remove(toRemove[i]);
			}
		}

		private static void Play(int charId, string serverPath, string animation, string skin, int durationMs)
		{
			Remove(charId);
			SkeletonDataAsset data = LoadSkeleton(serverPath);
			if (data == null)
			{
				Debug.LogWarning("[SpineSkillBridge] Missing skeleton: " + serverPath);
				return;
			}

			GameObject go = new GameObject("SpineSkillEffect_" + charId);
			go.layer = SPINE_LAYER;
			Object.DontDestroyOnLoad(go);

			SkeletonAnimation skeletonAnimation = SkeletonAnimation.AddToGameObject(go, data);
			skeletonAnimation.Initialize(true);
			if (!string.IsNullOrEmpty(skin))
			{
				skeletonAnimation.Skeleton.SetSkin(skin);
				skeletonAnimation.Skeleton.SetSlotsToSetupPose();
			}
			string animationName = ResolveAnimationName(skeletonAnimation, animation);
			if (animationName == null)
			{
				Object.Destroy(go);
				return;
			}

			Spine.TrackEntry track = skeletonAnimation.AnimationState.SetAnimation(0, animationName, false);
			if (track != null && track.Animation != null && track.Animation.Duration > 0f && durationMs > 0)
			{
				skeletonAnimation.timeScale = track.Animation.Duration / (durationMs / 1000f);
			}

			MeshRenderer meshRenderer = go.GetComponent<MeshRenderer>();
			if (meshRenderer != null)
			{
				meshRenderer.sortingOrder = 32766;
			}

			Char c = GetChar(charId);
			activeEffects[charId] = new ActiveEffect
			{
				charId = charId,
				go = go,
				endTime = mSystem.currentTimeMillis() + durationMs,
				oldHead = c != null ? c.head : -1,
				scaleMultiplier = serverPath.Contains("Skill_3") ? 1.1f : (serverPath.Contains("Skill_1") ? 0.95f : 1.1f)
			};

			if (c != null)
			{
				SoundMn.gI().gong();
				c.isWaitBienHinh = true;
				c.lastWaitBienHinh = mSystem.currentTimeMillis();
				c.isLockMove = true;
				c.isHide = true;
			}
		}

		private static void Remove(int charId)
		{
			if (!activeEffects.TryGetValue(charId, out ActiveEffect effect))
			{
				return;
			}
			if (effect.go != null)
			{
				Object.Destroy(effect.go);
			}
			activeEffects.Remove(charId);

			Char c = GetChar(charId);
			if (c != null && c.isWaitBienHinh)
			{
				c.isWaitBienHinh = false;
				if (c.me)
				{
					c.isLockMove = false;
				}
				c.isHide = false;
			}
		}

		private static SkeletonDataAsset LoadSkeleton(string serverPath)
		{
			if (skeletonCache.TryGetValue(serverPath, out SkeletonDataAsset cached))
			{
				return cached;
			}
			SkeletonDataAsset asset = Resources.Load<SkeletonDataAsset>("Spine/" + serverPath + "_SkeletonData");
			if (asset != null)
			{
				skeletonCache[serverPath] = asset;
			}
			return asset;
		}

		private static string ResolveAnimationName(SkeletonAnimation skeletonAnimation, string animation)
		{
			if (skeletonAnimation == null || skeletonAnimation.Skeleton == null || skeletonAnimation.Skeleton.Data == null)
			{
				return null;
			}
			if (skeletonAnimation.Skeleton.Data.FindAnimation(animation) != null)
			{
				return animation;
			}
			return skeletonAnimation.Skeleton.Data.Animations.Count > 0
				? skeletonAnimation.Skeleton.Data.Animations.Items[0].Name
				: null;
		}

		private static Char GetChar(int charId)
		{
			Char c = GameScr.findCharInMap(charId);
			if (c != null)
			{
				return c;
			}
			if (Char.myCharz() != null && Char.myCharz().charID == charId)
			{
				return Char.myCharz();
			}
			return null;
		}
	}
}
