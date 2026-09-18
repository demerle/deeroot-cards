using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

[ExecuteInEditMode]
public class SFRenderer : MonoBehaviour
{
	[Serializable]
	[CompilerGenerated]
	private sealed class _003C_003Ec
	{
		public static readonly _003C_003Ec _003C_003E9 = new _003C_003Ec();

		public static Func<SFLight, bool> _003C_003E9__96_0;

		public static Func<SFPolygon, bool> _003C_003E9__96_1;

		internal bool _003COnPreRender_003Eb__96_0(SFLight o)
		{
			return o.isActiveAndEnabled;
		}

		internal bool _003COnPreRender_003Eb__96_1(SFPolygon o)
		{
			return o.isActiveAndEnabled;
		}
	}

	public bool _renderInSceneView = true;

	private RenderTexture _lightMap;

	private RenderTexture _ShadowMap;

	[Tooltip("Blend the lights in linear space rather than gamma space. Nonlinear blending prevents oversaturation, but can cause draw order artifacts.")]
	public bool _linearLightBlending = true;

	public bool _shadows = true;

	[Tooltip("The global ambient light color- the ambient light is used to light your scene when no lights are affecting part of it. A darker grey, blue, or yellow is often a good place to start. Alpha unused. ")]
	public Color _ambientLight = Color.black;

	[Tooltip("Exposure is a multiplier applied to all lights in this renderer. Use to adjust all your lights at once. Particularly useful if you're using HDR lighting, otherwise it can be used to cause oversaturation.")]
	[FormerlySerializedAs("_globalDynamicRange")]
	public float _exposure = 1f;

	[Tooltip("Scale of the render texture for the colored lights. Larger numbers will give you blockier lights, but will run faster. Since lighting tends to be pretty diffuse, high numbers like 8 usually look good here. Recommended values are between 8 - 32.")]
	public float _lightMapScale = 8f;

	[Tooltip("Scale of the render texture for the colored lights. Larger numbers will give you blockier shadows, but will run faster. Blocky shadows tend to look worse than blocky lights, so this should usually be lower than the light map scale. Recommended values are between 2 - 8. Less if you have a lot of sharp shadows.")]
	public float _shadowMapScale = 4f;

	[Tooltip("How far will light penetrate into each shadow casting object. Makes it look like objects that are casting shadows are illuminated by the lights.")]
	public float _minLightPenetration = 0.2f;

	[Tooltip("Extra darkening to apply to shadows to hide precision artifacts in the seams.")]
	public float _shadowCompensation = 1.01f;

	[Tooltip("The color of the fog color. The alpha controls the fog's strength.")]
	public Color _fogColor = new Color(1f, 1f, 1f, 0f);

	[Tooltip("The scatter color is the color that the fog will glow when it is lit. Alpha is unused. Black disables illumination effects on the fog.")]
	public Color _scatterColor = new Color(0f, 0f, 0f, 0f);

	[Tooltip("What percentage of unshadowed/shadowed light should apply to the fog. At 1.0, your shadows will be fully applied to the scattered light in your fog.")]
	public float _softHardMix;

	private Rect _extents = Rect.MinMaxRect(-1f, -1f, 1f, 1f);

	private Material _shadowMaskMaterial;

	private Material _linearLightMaterial;

	private Material _softLightMaterial;

	private Material _HDRClampMaterial;

	private Material _fogMaterial;

	private Mesh _mesh;

	private bool UV_STARTS_AT_TOP;

	private static Matrix4x4 TEXTURE_FLIP_MATRIX = Matrix4x4.Scale(new Vector3(1f, -1f, 1f));

	private RenderTextureFormat lightmapFormat = RenderTextureFormat.ARGB1555;

	private RenderTextureFormat lightmapFormatHDR = RenderTextureFormat.ARGB1555;

	private List<SFPolygon> _perLightCulledPolygons = new List<SFPolygon>();

	private List<SFLight> _culledLights = new List<SFLight>();

	private List<SFPolygon> _culledPolygons = new List<SFPolygon>();

	public bool linearLightBlending
	{
		get
		{
			return _linearLightBlending;
		}
		set
		{
			_linearLightBlending = value;
		}
	}

	public bool shadows
	{
		get
		{
			return _shadows;
		}
		set
		{
			_shadows = value;
		}
	}

	public Color ambientLight
	{
		get
		{
			return _ambientLight;
		}
		set
		{
			_ambientLight = value;
		}
	}

	public float exposure
	{
		get
		{
			return _exposure;
		}
		set
		{
			_exposure = value;
		}
	}

	public float lightMapScale
	{
		get
		{
			return _lightMapScale;
		}
		set
		{
			_lightMapScale = value;
		}
	}

	public float shadowMapScale
	{
		get
		{
			return _shadowMapScale;
		}
		set
		{
			_shadowMapScale = value;
		}
	}

	public float minLightPenetration
	{
		get
		{
			return _minLightPenetration;
		}
		set
		{
			_minLightPenetration = value;
		}
	}

	public float shadowCompensation
	{
		get
		{
			return _shadowCompensation;
		}
		set
		{
			_shadowCompensation = Mathf.Clamp(value, 1f, 2f);
		}
	}

	public Color fogColor
	{
		get
		{
			return _fogColor;
		}
		set
		{
			_fogColor = value;
		}
	}

	public Color scatterColor
	{
		get
		{
			return _scatterColor;
		}
		set
		{
			_scatterColor = value;
		}
	}

	public float softHardMix
	{
		get
		{
			return _softHardMix;
		}
		set
		{
			_softHardMix = value;
		}
	}

	[Obsolete("Please use SFRenderer.exposure instead.")]
	public float globalIlluminationScale
	{
		get
		{
			return _exposure;
		}
		set
		{
			_exposure = value;
		}
	}

	[Obsolete("Please use SFRenderer.exposure instead.")]
	public float globalDynamicRange
	{
		get
		{
			return _exposure;
		}
		set
		{
			_exposure = value;
		}
	}

	public Rect extents
	{
		get
		{
			return _extents;
		}
		set
		{
			_extents = value;
		}
	}

	private Material shadowMaskMaterial
	{
		get
		{
			if (_shadowMaskMaterial == null)
			{
				_shadowMaskMaterial = new Material(Shader.Find("Hidden/SFSoftShadows/ShadowMask"));
				_shadowMaskMaterial.hideFlags = HideFlags.HideAndDontSave;
			}
			return _shadowMaskMaterial;
		}
	}

	private Material lightMaterial
	{
		get
		{
			if (_linearLightMaterial == null)
			{
				_linearLightMaterial = new Material(Shader.Find("Hidden/SFSoftShadows/LightBlendLinear"));
				_linearLightMaterial.hideFlags = HideFlags.HideAndDontSave;
				_softLightMaterial = new Material(Shader.Find("Hidden/SFSoftShadows/LightBlendSoft"));
				_softLightMaterial.hideFlags = HideFlags.HideAndDontSave;
			}
			if (!_linearLightBlending)
			{
				return _softLightMaterial;
			}
			return _linearLightMaterial;
		}
	}

	private Material HDRClampMaterial
	{
		get
		{
			if (_HDRClampMaterial == null)
			{
				_HDRClampMaterial = new Material(Shader.Find("Hidden/SFSoftShadows/HDRClamp"));
				_HDRClampMaterial.hideFlags = HideFlags.HideAndDontSave;
			}
			return _HDRClampMaterial;
		}
	}

	private Material fogMaterial
	{
		get
		{
			if (_fogMaterial == null)
			{
				_fogMaterial = new Material(Shader.Find("Hidden/SFSoftShadows/FogLayer"));
				_fogMaterial.hideFlags = HideFlags.HideAndDontSave;
			}
			return _fogMaterial;
		}
	}

	private Mesh sharedMesh
	{
		get
		{
			if (_mesh == null)
			{
				_mesh = new Mesh();
				_mesh.MarkDynamic();
				_mesh.hideFlags = HideFlags.HideAndDontSave;
			}
			return _mesh;
		}
	}

	private void ScenePreRender(Camera camera)
	{
		if (_renderInSceneView && camera.cameraType == CameraType.SceneView)
		{
			OnPreRender();
		}
	}

	private void ScenePostRender(Camera camera)
	{
		if (_renderInSceneView && camera.cameraType == CameraType.SceneView)
		{
			OnPostRender();
		}
	}

	private void OnEnable()
	{
		if (Application.isEditor)
		{
			Camera.onPreRender = (Camera.CameraCallback)Delegate.Combine(Camera.onPreRender, new Camera.CameraCallback(ScenePreRender));
			Camera.onPostRender = (Camera.CameraCallback)Delegate.Combine(Camera.onPostRender, new Camera.CameraCallback(ScenePostRender));
		}
	}

	private void OnDisable()
	{
		if (Application.isEditor)
		{
			Camera.onPreRender = (Camera.CameraCallback)Delegate.Remove(Camera.onPreRender, new Camera.CameraCallback(ScenePreRender));
			Camera.onPostRender = (Camera.CameraCallback)Delegate.Remove(Camera.onPostRender, new Camera.CameraCallback(ScenePostRender));
		}
	}

	private void Start()
	{
		GraphicsDeviceType graphicsDeviceType = SystemInfo.graphicsDeviceType;
		UV_STARTS_AT_TOP = graphicsDeviceType == GraphicsDeviceType.Direct3D9 || graphicsDeviceType == GraphicsDeviceType.Direct3D11 || graphicsDeviceType == GraphicsDeviceType.Direct3D12 || graphicsDeviceType == GraphicsDeviceType.Metal;
		if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGB32))
		{
			lightmapFormat = RenderTextureFormat.ARGB32;
		}
		else if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.BGRA32))
		{
			lightmapFormat = RenderTextureFormat.BGRA32;
		}
		if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf))
		{
			lightmapFormatHDR = RenderTextureFormat.ARGBHalf;
		}
		else if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBFloat))
		{
			lightmapFormatHDR = RenderTextureFormat.ARGBFloat;
		}
		Debug.Log(string.Concat("SFSS init: ", graphicsDeviceType, ", ", UV_STARTS_AT_TOP ? "UV_STARTS_AT_TOP, " : "", lightmapFormat, ", ", lightmapFormatHDR));
	}

	private void OnDestroy()
	{
		if ((bool)_mesh)
		{
			UnityEngine.Object.DestroyImmediate(_mesh);
		}
	}

	public static Rect _TransformRect(Matrix4x4 m, Rect r)
	{
		Vector4 vector = m.MultiplyPoint3x4(new Vector4(r.x + 0.5f * r.width, r.y + 0.5f * r.height, 0f, 1f));
		float num = 0.5f * Mathf.Max(Mathf.Abs(r.width * m[0] + r.height * m[4]), Mathf.Abs(r.width * m[0] - r.height * m[4]));
		float num2 = 0.5f * Mathf.Max(Mathf.Abs(r.width * m[1] + r.height * m[5]), Mathf.Abs(r.width * m[1] - r.height * m[5]));
		return new Rect(vector.x - num, vector.y - num2, 2f * num, 2f * num2);
	}

	private static Vector2 ClampedProjection(Matrix4x4 m, float x, float y)
	{
		float num = Math.Max(0f, x * m[3] + y * m[7] + m[15]);
		return new Vector2((x * m[0] + y * m[4] + m[12]) / num, (x * m[1] + y * m[5] + m[13]) / num);
	}

	private static Rect ScissorRect(Matrix4x4 mvp, float w, float h)
	{
		Vector2 vector = ClampedProjection(mvp, -1f, -1f);
		Vector2 vector2 = ClampedProjection(mvp, 1f, -1f);
		Vector2 vector3 = ClampedProjection(mvp, 1f, 1f);
		Vector2 vector4 = ClampedProjection(mvp, -1f, 1f);
		float num = Mathf.Min(Mathf.Min(vector.x, vector2.x), Mathf.Min(vector3.x, vector4.x));
		float num2 = Mathf.Min(Mathf.Min(vector.y, vector2.y), Mathf.Min(vector3.y, vector4.y));
		float num3 = Mathf.Max(Mathf.Max(vector.x, vector2.x), Mathf.Max(vector3.x, vector4.x));
		float num4 = Mathf.Max(Mathf.Max(vector.y, vector2.y), Mathf.Max(vector3.y, vector4.y));
		return Rect.MinMaxRect(Mathf.Max(0f, Mathf.Floor((0.5f * num + 0.5f) * w)), Mathf.Max(0f, Mathf.Floor((0.5f * num2 + 0.5f) * h)), Mathf.Min(w, Mathf.Ceil((0.5f * num3 + 0.5f) * w)), Mathf.Min(h, Mathf.Ceil((0.5f * num4 + 0.5f) * h)));
	}

	private static Matrix4x4 ClipMatrix(Rect r, float dw, float dh)
	{
		float num = r.x * dw - 1f;
		float num2 = r.y * dh - 1f;
		return Matrix4x4.Ortho(num, num + r.width * dw, num2, num2 + r.height * dh, -1f, 1f);
	}

	private static void CullPolys(List<SFPolygon> polys, Rect bounds, List<SFPolygon> culledPolygons)
	{
		for (int i = 0; i < polys.Count; i++)
		{
			SFPolygon sFPolygon = polys[i];
			if (bounds.Overlaps(sFPolygon._WorldBounds))
			{
				culledPolygons.Add(sFPolygon);
			}
		}
	}

	private Matrix4x4 TextureProjectionMatrix(Matrix4x4 m)
	{
		m.SetRow(2, new Vector4(0f, 0f, 1f, 0f));
		Matrix4x4 matrix4x = m.inverse;
		if (UV_STARTS_AT_TOP)
		{
			matrix4x = TEXTURE_FLIP_MATRIX * matrix4x * TEXTURE_FLIP_MATRIX;
		}
		return matrix4x;
	}

	private void RenderLightMap(Matrix4x4 viewMatrix, Matrix4x4 projection, Matrix4x4 vpMatrix, RenderTexture target, List<SFLight> lights, List<SFPolygon> polys, Color ambient, bool hdr)
	{
		int width = target.width;
		int height = target.height;
		Rect screenRect = new Rect(-1f, -1f, 2f, 2f);
		Rect sourceRect = new Rect(0f, 0f, 1f, 1f);
		Graphics.SetRenderTarget(target);
		GL.Clear(false, true, ambient);
		for (int i = 0; i < lights.Count; i++)
		{
			SFLight sFLight = lights[i];
			if (!sFLight.enabled)
			{
				continue;
			}
			Matrix4x4 matrix4x = sFLight._ModelMatrix(false) * sFLight._CookieMatrix();
			Rect rect = ScissorRect(vpMatrix * matrix4x, width, height);
			Matrix4x4 matrix4x2 = ClipMatrix(rect, 2f / (float)width, 2f / (float)height) * projection;
			GL.Viewport(rect);
			GL.LoadProjectionMatrix(matrix4x2);
			if (polys != null && (int)sFLight._shadowLayers != 0)
			{
				CullPolys(polys, sFLight._CalcCullBounds(vpMatrix), _perLightCulledPolygons);
				Mesh mesh = sFLight._BuildShadowMesh(sharedMesh, _perLightCulledPolygons, _minLightPenetration);
				_perLightCulledPolygons.Clear();
				if (mesh != null)
				{
					shadowMaskMaterial.SetPass(0);
					Graphics.DrawMeshNow(mesh, sFLight._ModelMatrix(true));
					mesh.Clear();
				}
				if (hdr)
				{
					Graphics.DrawTexture(screenRect, Texture2D.blackTexture, HDRClampMaterial);
				}
			}
			Texture2D texture2D = sFLight._cookieTexture;
			if (!texture2D)
			{
				texture2D = Texture2D.whiteTexture;
			}
			Material material = lightMaterial;
			if (_linearLightBlending)
			{
				material.SetFloat("_intensity", sFLight._intensity);
			}
			GL.LoadProjectionMatrix(TextureProjectionMatrix(matrix4x2 * viewMatrix * matrix4x));
			Graphics.DrawTexture(screenRect, texture2D, sourceRect, 0, 0, 0, 0, sFLight._color, material);
		}
	}

	private RenderTexture GetTexture(Camera cam, Matrix4x4 extensionInv, float downscale)
	{
		Vector4 vector = extensionInv * cam.pixelRect.size / downscale;
		RenderTextureFormat format = (cam.allowHDR ? lightmapFormatHDR : lightmapFormat);
		return RenderTexture.GetTemporary((int)vector.x, (int)vector.y, 0, format);
	}

	public static bool _FastCull(Matrix4x4 mvp, Rect bounds)
	{
		Vector2 center = bounds.center;
		Vector2 vector = 0.5f * bounds.size;
		Vector4 vector2 = mvp * new Vector4(center.x, center.y, 0f, 1f);
		float num = vector.x * Mathf.Abs(mvp[0]) + vector.y * Mathf.Abs(mvp[4]);
		float num2 = vector.x * Mathf.Abs(mvp[1]) + vector.y * Mathf.Abs(mvp[5]);
		float num3 = vector.x * Mathf.Abs(mvp[2]) + vector.y * Mathf.Abs(mvp[6]);
		float num4 = Mathf.Max(0f, vector2.w + vector.x * Mathf.Abs(mvp[3]) + vector.y * Mathf.Abs(mvp[7]));
		if (Mathf.Abs(vector2.x) - num < num4 && Mathf.Abs(vector2.y) - num2 < num4)
		{
			return Mathf.Abs(vector2.z) - num3 < num4;
		}
		return false;
	}

	private static Rect CullLights(Matrix4x4 vpMatrix, List<SFLight> lights, List<SFLight> culledLights)
	{
		float num = float.PositiveInfinity;
		float num2 = float.PositiveInfinity;
		float num3 = float.NegativeInfinity;
		float num4 = float.NegativeInfinity;
		for (int i = 0; i < lights.Count; i++)
		{
			SFLight sFLight = lights[i];
			if (_FastCull(vpMatrix * sFLight._ModelMatrix(false), sFLight._bounds))
			{
				culledLights.Add(sFLight);
				if ((int)sFLight._shadowLayers != 0)
				{
					Rect rect = sFLight._CalcCullBounds(vpMatrix);
					num = Mathf.Min(num, rect.xMin);
					num2 = Mathf.Min(num2, rect.yMin);
					num3 = Mathf.Max(num3, rect.xMax);
					num4 = Mathf.Max(num4, rect.yMax);
				}
			}
		}
		return Rect.MinMaxRect(num, num2, num3, num4);
	}

	private void OnPreRender()
	{
		Color color = _ambientLight;
		color.a = 1f;
		Matrix4x4 matrix4x = Matrix4x4.Ortho(_extents.xMin, _extents.xMax, _extents.yMin, _extents.yMax, 1f, -1f);
		Matrix4x4 inverse = matrix4x.inverse;
		RenderBuffer activeColorBuffer = Graphics.activeColorBuffer;
		RenderBuffer activeDepthBuffer = Graphics.activeDepthBuffer;
		Camera current = Camera.current;
		bool allowHDR = current.allowHDR;
		Matrix4x4 worldToCameraMatrix = current.worldToCameraMatrix;
		Matrix4x4 matrix4x2 = matrix4x * current.projectionMatrix;
		Matrix4x4 vpMatrix = matrix4x2 * worldToCameraMatrix;
		List<SFLight> lights = SFLight._lights;
		List<SFPolygon> list = SFPolygon._polygons;
		if (!Application.isPlaying)
		{
			lights = new List<SFLight>(UnityEngine.Object.FindObjectsOfType<SFLight>().Where(_003C_003Ec._003C_003E9__96_0 ?? (_003C_003Ec._003C_003E9__96_0 = _003C_003Ec._003C_003E9._003COnPreRender_003Eb__96_0)));
			list = new List<SFPolygon>(UnityEngine.Object.FindObjectsOfType<SFPolygon>().Where(_003C_003Ec._003C_003E9__96_1 ?? (_003C_003Ec._003C_003E9__96_1 = _003C_003Ec._003C_003E9._003COnPreRender_003Eb__96_1)));
			foreach (SFPolygon item in list)
			{
				item._UpdateBounds();
			}
		}
		Rect bounds = CullLights(vpMatrix, lights, _culledLights);
		GL.PushMatrix();
		_lightMap = GetTexture(current, inverse, _lightMapScale);
		RenderLightMap(worldToCameraMatrix, matrix4x2, vpMatrix, _lightMap, _culledLights, null, color, allowHDR);
		if (_shadows)
		{
			for (int i = 0; i < list.Count; i++)
			{
				list[i]._CacheWorldBounds();
			}
			CullPolys(list, bounds, _culledPolygons);
			Shader.SetGlobalFloat("_SFShadowCompensation", _shadowCompensation);
			_ShadowMap = GetTexture(current, inverse, _shadowMapScale);
			RenderLightMap(worldToCameraMatrix, matrix4x2, vpMatrix, _ShadowMap, _culledLights, _culledPolygons, color, allowHDR);
			_culledPolygons.Clear();
		}
		GL.PopMatrix();
		_culledLights.Clear();
		Graphics.SetRenderTarget(null);
		GL.Viewport(current.pixelRect);
		Shader.SetGlobalMatrix("_SFProjection", matrix4x * Camera.current.projectionMatrix);
		Shader.SetGlobalColor("_SFAmbientLight", color);
		Shader.SetGlobalFloat("_SFExposure", _exposure);
		Shader.SetGlobalTexture("_SFLightMap", _lightMap);
		Shader.SetGlobalTexture("_SFLightMapWithShadows", _shadows ? _ShadowMap : _lightMap);
		Graphics.SetRenderTarget(activeColorBuffer, activeDepthBuffer);
	}

	private void OnPostRender()
	{
		if (_fogColor.a + _scatterColor.r + _scatterColor.g + _scatterColor.b > 0f)
		{
			GL.PushMatrix();
			GL.LoadProjectionMatrix(Matrix4x4.identity);
			Color value = _scatterColor;
			value.a = _softHardMix;
			Material material = fogMaterial;
			material.SetColor("_FogColor", _fogColor);
			material.SetColor("_Scatter", value);
			material.SetPass(0);
			Graphics.DrawTexture(new Rect(-1f, -1f, 2f, 2f), Texture2D.blackTexture, material);
			GL.PopMatrix();
		}
		Shader.SetGlobalColor("_SFAmbientLight", Color.white);
		Shader.SetGlobalFloat("_SFExposure", 1f);
		Shader.SetGlobalTexture("_SFLightMap", Texture2D.whiteTexture);
		Shader.SetGlobalTexture("_SFLightMapWithShadows", Texture2D.whiteTexture);
		RenderTexture.ReleaseTemporary(_lightMap);
		_lightMap = null;
		RenderTexture.ReleaseTemporary(_ShadowMap);
		_ShadowMap = null;
	}
}
