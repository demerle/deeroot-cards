using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class SFLight : MonoBehaviour
{
	private class VertexArray
	{
		public int capacity;

		public int size;

		public Vector3[] verts;

		public Vector4[] tangents;

		public Vector2[] uvs;

		public int[] tris;

		public VertexArray(int segments)
		{
			capacity = segments;
			size = 0;
			verts = new Vector3[segments * 4];
			tangents = new Vector4[segments * 4];
			uvs = new Vector2[segments * 4];
			tris = new int[segments * 6];
		}
	}

	[Tooltip("The radius of the light source. Larger lights cast softer shadows.")]
	public float _radius = 0.5f;

	[Tooltip("The brightness of the light. (Ignored when using non-linear light blending.) Allows for colors brighter than 1.0 in HDR lighting situations.")]
	public float _intensity = 1f;

	[Tooltip("The color of the light.")]
	public Color _color = Color.white;

	[Tooltip("The shape of the light.")]
	public Texture2D _cookieTexture;

	[Tooltip("Which layers cast shadows.")]
	public LayerMask _shadowLayers = -1;

	[Tooltip("Allows the light cookie to move off the light plane. Use gently as it can cause shadows to look weird.")]
	public bool _parallaxLight;

	private RectTransform _rt;

	public static List<SFLight> _lights = new List<SFLight>();

	private Rect _cullBounds;

	private static int GROWTH_NUM = 4;

	private static int GROWTH_DENOM = 3;

	private static VertexArray[] vertexArrays = new VertexArray[40];

	public float radius
	{
		get
		{
			return _radius;
		}
		set
		{
			_radius = value;
		}
	}

	public float intensity
	{
		get
		{
			return _intensity;
		}
		set
		{
			_intensity = value;
		}
	}

	public Color color
	{
		get
		{
			return _color;
		}
		set
		{
			_color = value;
		}
	}

	public Texture2D cookieTexture
	{
		get
		{
			return _cookieTexture;
		}
		set
		{
			_cookieTexture = value;
		}
	}

	public LayerMask shadowLayers
	{
		get
		{
			return _shadowLayers;
		}
		set
		{
			_shadowLayers = value;
		}
	}

	public bool parallaxLight
	{
		get
		{
			return _parallaxLight;
		}
		set
		{
			_parallaxLight = value;
		}
	}

	public Rect _bounds => _rt.rect;

	public Rect _CullBounds => _cullBounds;

	private void OnEnable()
	{
		_lights.Add(this);
	}

	private void OnDisable()
	{
		_lights.Remove(this);
	}

	public Matrix4x4 _ModelMatrix(bool forceProjection)
	{
		if (!_rt)
		{
			_rt = GetComponent<RectTransform>();
		}
		Matrix4x4 localToWorldMatrix = _rt.localToWorldMatrix;
		if (!_parallaxLight || forceProjection)
		{
			localToWorldMatrix.SetRow(2, new Vector4(0f, 0f, 1f, 0f));
		}
		return localToWorldMatrix;
	}

	public Matrix4x4 _CookieMatrix()
	{
		if (!_rt)
		{
			_rt = GetComponent<RectTransform>();
		}
		Vector2 vector = _rt.sizeDelta / 2f;
		Vector2 vector2 = Vector2.one - 2f * _rt.pivot;
		Matrix4x4 identity = Matrix4x4.identity;
		identity.SetRow(0, new Vector4(vector.x, 0f, 0f, vector2.x * vector.x));
		identity.SetRow(1, new Vector4(0f, vector.y, 0f, vector2.y * vector.y));
		return identity;
	}

	private static Rect Union(Rect r1, Rect r2)
	{
		return Rect.MinMaxRect(Mathf.Min(r1.xMin, r2.xMin), Mathf.Min(r1.yMin, r2.yMin), Mathf.Max(r1.xMax, r2.xMax), Mathf.Max(r1.yMax, r2.yMax));
	}

	private static Rect QuadrantCull(Rect cull, Matrix4x4 mvp, Rect r)
	{
		if (!SFRenderer._FastCull(mvp, r))
		{
			return cull;
		}
		return Union(cull, r);
	}

	public Rect _CalcCullBounds(Matrix4x4 vpMatrix)
	{
		Matrix4x4 matrix4x = _ModelMatrix(forceProjection: true);
		Matrix4x4 mvp = vpMatrix * matrix4x;
		Rect rect = new Rect(0f - _radius, 0f - _radius, 2f * _radius, 2f * _radius);
		Rect rect2 = _rt.rect;
		Rect cull = rect;
		cull = QuadrantCull(cull, mvp, Rect.MinMaxRect(rect2.xMin, rect2.yMin, rect.xMax, rect.yMax));
		cull = QuadrantCull(cull, mvp, Rect.MinMaxRect(rect.xMin, rect2.yMin, rect2.xMax, rect.yMax));
		cull = QuadrantCull(cull, mvp, Rect.MinMaxRect(rect2.xMin, rect.yMin, rect.xMax, rect2.yMax));
		cull = QuadrantCull(cull, mvp, Rect.MinMaxRect(rect.xMin, rect.yMin, rect2.xMax, rect2.yMax));
		return _cullBounds = SFRenderer._TransformRect(matrix4x, cull);
	}

	private VertexArray GetVertexArray(int segments)
	{
		int num = 0;
		int num2 = 4;
		while (segments > num2)
		{
			num2 = num2 * GROWTH_NUM / GROWTH_DENOM;
			num++;
		}
		if (num >= vertexArrays.Length)
		{
			Debug.LogError("SFSS: Maximum vertexes per light exceeded. (" + num2 + ")");
			return null;
		}
		if (vertexArrays[num] == null)
		{
			vertexArrays[num] = new VertexArray(num2);
		}
		return vertexArrays[num];
	}

	private void BatchPath(SFPolygon poly, int pathIndex, Matrix4x4 t, ref int j, bool flipped, Vector3 properties, Vector3[] verts, Vector4[] tangents, Vector2[] uvs, int[] tris)
	{
		Vector2[] path = poly.GetPath(pathIndex);
		int num;
		Vector2 vector;
		if (poly.looped)
		{
			num = 0;
			vector = Transform(t, path[^1]);
		}
		else
		{
			num = 1;
			vector = Transform(t, path[0]);
		}
		for (int i = num; i < path.Length; i++)
		{
			Vector2 vector2 = Transform(t, path[i]);
			Vector4 vector3 = (flipped ? new Vector4(vector2.x, vector2.y, vector.x, vector.y) : new Vector4(vector.x, vector.y, vector2.x, vector2.y));
			verts[j * 4] = properties;
			tangents[j * 4] = vector3;
			uvs[j * 4] = new Vector2(0f, 0f);
			verts[j * 4 + 1] = properties;
			tangents[j * 4 + 1] = vector3;
			uvs[j * 4 + 1] = new Vector2(1f, 0f);
			verts[j * 4 + 2] = properties;
			tangents[j * 4 + 2] = vector3;
			uvs[j * 4 + 2] = new Vector2(0f, 1f);
			verts[j * 4 + 3] = properties;
			tangents[j * 4 + 3] = vector3;
			uvs[j * 4 + 3] = new Vector2(1f, 1f);
			tris[j * 6] = j * 4;
			tris[j * 6 + 1] = j * 4 + 1;
			tris[j * 6 + 2] = j * 4 + 2;
			tris[j * 6 + 3] = j * 4 + 1;
			tris[j * 6 + 4] = j * 4 + 3;
			tris[j * 6 + 5] = j * 4 + 2;
			j++;
			vector = vector2;
		}
	}

	public Mesh _BuildShadowMesh(Mesh mesh, List<SFPolygon> polys, float minLightPenetration)
	{
		int num = 0;
		for (int i = 0; i < polys.Count; i++)
		{
			if (((int)polys[i].shadowLayers & (int)shadowLayers) == 0)
			{
				continue;
			}
			SFPolygon sFPolygon = polys[i];
			int activePath = sFPolygon.activePath;
			bool looped = sFPolygon.looped;
			if (activePath >= 0)
			{
				num += sFPolygon.GetPath(activePath).Length - ((!looped) ? 1 : 0);
				continue;
			}
			int pathCount = sFPolygon.pathCount;
			for (int j = 0; j < pathCount; j++)
			{
				num += sFPolygon.GetPath(j).Length - ((!looped) ? 1 : 0);
			}
		}
		VertexArray vertexArray = GetVertexArray(num);
		Vector3[] verts = vertexArray.verts;
		Vector4[] tangents = vertexArray.tangents;
		Vector2[] uvs = vertexArray.uvs;
		int[] tris = vertexArray.tris;
		if (num == 0 || vertexArray.capacity == 0)
		{
			return null;
		}
		if (!_rt)
		{
			_rt = GetComponent<RectTransform>();
		}
		Matrix4x4 worldToLocalMatrix = _rt.worldToLocalMatrix;
		int j2 = 0;
		for (int k = 0; k < polys.Count; k++)
		{
			SFPolygon sFPolygon2 = polys[k];
			if (((int)sFPolygon2.shadowLayers & (int)shadowLayers) == 0)
			{
				continue;
			}
			Matrix4x4 matrix4x = sFPolygon2._GetMatrix();
			Matrix4x4 t = worldToLocalMatrix * matrix4x;
			bool flipped = Det2x3(matrix4x) < 0f;
			float y = Mathf.Max(sFPolygon2._lightPenetration, minLightPenetration);
			Vector3 properties = new Vector3(_radius, y, sFPolygon2._opacity);
			int activePath2 = sFPolygon2.activePath;
			if (activePath2 >= 0)
			{
				BatchPath(sFPolygon2, activePath2, t, ref j2, flipped, properties, verts, tangents, uvs, tris);
				continue;
			}
			int pathCount2 = sFPolygon2.pathCount;
			for (int l = 0; l < pathCount2; l++)
			{
				BatchPath(sFPolygon2, l, t, ref j2, flipped, properties, verts, tangents, uvs, tris);
			}
		}
		for (int m = 6 * num; m < 6 * vertexArray.size; m++)
		{
			vertexArray.tris[m] = 0;
		}
		mesh.vertices = verts;
		mesh.tangents = tangents;
		mesh.uv = uvs;
		mesh.triangles = tris;
		vertexArray.size = num;
		return mesh;
	}

	private static float Det2x3(Matrix4x4 m)
	{
		return m[0] * m[5] - m[1] * m[4];
	}

	private static Vector2 Transform(Matrix4x4 m, Vector2 p)
	{
		return new Vector2(p.x * m[0] + p.y * m[4] + m[12], p.x * m[1] + p.y * m[5] + m[13]);
	}
}
