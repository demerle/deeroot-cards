using System;
using System.Collections.Generic;
using UnityEngine;

public class SFPolygon : MonoBehaviour
{
	private Transform _t;

	private Rect _bounds;

	private Rect _worldBounds;

	[SerializeField]
	private Vector2[] _verts = new Vector2[3];

	[SerializeField]
	private Vector2[][] _paths;

	public int _activePath;

	public bool _looped;

	public LayerMask _shadowLayers = -1;

	public float _lightPenetration;

	public float _opacity = 1f;

	public static List<SFPolygon> _polygons = new List<SFPolygon>();

	public Rect _WorldBounds => _worldBounds;

	public int pathCount
	{
		get
		{
			if (_paths != null)
			{
				return _paths.Length + 1;
			}
			return 1;
		}
		set
		{
			int num = value - 1;
			if (value == pathCount)
			{
				return;
			}
			if (value < 1)
			{
				Debug.LogError("pathCount must be positive.");
				return;
			}
			if (value == 1)
			{
				_paths = null;
			}
			else if (_paths == null)
			{
				_paths = new Vector2[num][];
				for (int i = 0; i < _paths.Length; i++)
				{
					_paths[i] = _verts;
				}
			}
			else
			{
				Vector2[][] paths = _paths;
				_paths = new Vector2[num][];
				if (num > paths.Length)
				{
					int j;
					for (j = 0; j < paths.Length; j++)
					{
						_paths[j] = paths[j];
					}
					for (; j < num; j++)
					{
						_paths[j] = _verts;
					}
				}
				else
				{
					for (int k = 0; k < num; k++)
					{
						_paths[k] = paths[k];
					}
				}
			}
			_UpdateBounds();
		}
	}

	public Vector2[] verts
	{
		get
		{
			return _verts;
		}
		set
		{
			_verts = value;
		}
	}

	public int activePath
	{
		get
		{
			return _activePath;
		}
		set
		{
			_activePath = value;
		}
	}

	public bool looped
	{
		get
		{
			return _looped;
		}
		set
		{
			_looped = value;
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

	public float lightPenetration
	{
		get
		{
			return _lightPenetration;
		}
		set
		{
			_lightPenetration = value;
		}
	}

	public float opacity
	{
		get
		{
			return _opacity;
		}
		set
		{
			_opacity = value;
		}
	}

	public Matrix4x4 _GetMatrix()
	{
		if (!_t)
		{
			_t = base.transform;
		}
		return _t.localToWorldMatrix;
	}

	private void PathBounds(Vector2[] path, int i0, ref float l, ref float b, ref float r, ref float t)
	{
		for (int j = i0; j < path.Length; j++)
		{
			Vector2 vector = path[j];
			l = Mathf.Min(vector.x, l);
			r = Mathf.Max(vector.x, r);
			b = Mathf.Min(vector.y, b);
			t = Mathf.Max(vector.y, t);
		}
	}

	public void _UpdateBounds()
	{
		float l;
		float r;
		float b;
		float t;
		if (_activePath > 0)
		{
			Vector2 vector = GetPath(_activePath)[0];
			l = (r = vector.x);
			b = (t = vector.y);
			PathBounds(_verts, 1, ref l, ref b, ref r, ref t);
		}
		else
		{
			Vector2 vector2 = _verts[0];
			l = (r = vector2.x);
			b = (t = vector2.y);
			PathBounds(_verts, 1, ref l, ref b, ref r, ref t);
			int num = pathCount;
			for (int i = 1; i < num; i++)
			{
				PathBounds(GetPath(i), 0, ref l, ref b, ref r, ref t);
			}
		}
		_bounds = Rect.MinMaxRect(l, b, r, t);
	}

	public void _CacheWorldBounds()
	{
		if (!_t)
		{
			_t = base.transform;
		}
		_worldBounds = SFRenderer._TransformRect(_t.localToWorldMatrix, _bounds);
	}

	public Vector2[] GetPath(int index)
	{
		if (index != 0)
		{
			return _paths[index - 1];
		}
		return _verts;
	}

	public void SetPath(int index, Vector2[] path)
	{
		SetPathRaw(index, path);
		_UpdateBounds();
	}

	private void SetPathRaw(int index, Vector2[] path)
	{
		if (index == 0)
		{
			_verts = path;
		}
		else
		{
			_paths[index - 1] = path;
		}
	}

	private void OnEnable()
	{
		_polygons.Add(this);
	}

	private void OnDisable()
	{
		_polygons.Remove(this);
	}

	private void Start()
	{
		_UpdateBounds();
	}

	public void CopyFromCollider(Collider2D collider)
	{
		PolygonCollider2D polygonCollider2D = collider as PolygonCollider2D;
		BoxCollider2D boxCollider2D = collider as BoxCollider2D;
		if ((bool)polygonCollider2D)
		{
			looped = true;
			int num = (pathCount = polygonCollider2D.pathCount);
			int num3 = num;
			for (int i = 0; i < num3; i++)
			{
				Vector2[] path = polygonCollider2D.GetPath(i);
				for (int j = 0; j < path.Length; j++)
				{
					path[j] += polygonCollider2D.offset;
				}
				Array.Reverse((Array)path);
				SetPathRaw(i, path);
			}
			_UpdateBounds();
		}
		else if ((bool)boxCollider2D)
		{
			SetBoxVerts(boxCollider2D.offset - 0.5f * boxCollider2D.size, boxCollider2D.offset + 0.5f * boxCollider2D.size);
		}
		else
		{
			Debug.LogWarning("CopyFromCollider() only works with polygon and box colliders.");
		}
	}

	public void _CopyFromCollider()
	{
		Collider2D component = GetComponent<Collider2D>();
		if ((bool)component)
		{
			CopyFromCollider(component);
			return;
		}
		Debug.LogWarning("GameObject has no polygon or box collider. Adding default SFPolygon shape instead.");
		SetBoxVerts(-Vector2.one, Vector2.one);
	}

	private void SetBoxVerts(Vector2 min, Vector2 max)
	{
		looped = true;
		pathCount = 1;
		verts = new Vector2[4]
		{
			new Vector2(max.x, max.y),
			new Vector2(max.x, min.y),
			new Vector2(min.x, min.y),
			new Vector2(min.x, max.y)
		};
	}

	public void _FlipInsideOut(int index)
	{
		if (index == -1)
		{
			int num = pathCount;
			for (int i = 0; i < num; i++)
			{
				Array.Reverse((Array)GetPath(i));
			}
		}
		else
		{
			Array.Reverse((Array)GetPath(index));
		}
	}
}
