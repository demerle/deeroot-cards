using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Photon.Utilities;
using UnityEngine;
using emotitron.Compression;

namespace Photon.Compression
{
	[Serializable]
	public class ElementCrusher : Crusher<ElementCrusher>, IEquatable<ElementCrusher>, ICrusherCopy<ElementCrusher>
	{
		public enum UniformAxes
		{
			NonUniform = 0,
			XY = 3,
			XZ = 5,
			YZ = 6,
			XYZ = 7
		}

		public enum StaticTRSType
		{
			Position = 0,
			Euler = 1,
			Quaternion = 2,
			Scale = 3,
			Generic = 4
		}

		public static Dictionary<int, ElementCrusher> staticElementCrushers = new Dictionary<int, ElementCrusher>();

		private static readonly CompressedElement reusableCE = new CompressedElement();

		public static ElementCrusher defaultUncompressedElementCrusher = CheckAgainstStatics(new ElementCrusher(StaticTRSType.Generic)
		{
			XCrusher = FloatCrusher.defaultUncompressedCrusher,
			YCrusher = FloatCrusher.defaultUncompressedCrusher,
			ZCrusher = FloatCrusher.defaultUncompressedCrusher,
			UCrusher = FloatCrusher.defaultUncompressedCrusher
		});

		public static ElementCrusher defaultUncompressedPosCrusher = CheckAgainstStatics(new ElementCrusher(StaticTRSType.Position)
		{
			XCrusher = FloatCrusher.defaultUncompressedCrusher,
			YCrusher = FloatCrusher.defaultUncompressedCrusher,
			ZCrusher = FloatCrusher.defaultUncompressedCrusher,
			UCrusher = FloatCrusher.defaultUncompressedCrusher
		});

		public static ElementCrusher defaultUncompressedSclCrusher = CheckAgainstStatics(new ElementCrusher(StaticTRSType.Position)
		{
			XCrusher = FloatCrusher.defaultUncompressedCrusher,
			YCrusher = FloatCrusher.defaultUncompressedCrusher,
			ZCrusher = FloatCrusher.defaultUncompressedCrusher,
			UCrusher = FloatCrusher.defaultUncompressedCrusher
		});

		public static ElementCrusher defaultHalfFloatElementCrusher = CheckAgainstStatics(new ElementCrusher(StaticTRSType.Generic)
		{
			XCrusher = FloatCrusher.defaultUncompressedCrusher,
			YCrusher = FloatCrusher.defaultUncompressedCrusher,
			ZCrusher = FloatCrusher.defaultUncompressedCrusher,
			UCrusher = FloatCrusher.defaultUncompressedCrusher
		});

		public static ElementCrusher defaultHalfFloatPosCrusher = CheckAgainstStatics(new ElementCrusher(StaticTRSType.Position)
		{
			XCrusher = FloatCrusher.defaulHalfFloatCrusher,
			YCrusher = FloatCrusher.defaulHalfFloatCrusher,
			ZCrusher = FloatCrusher.defaulHalfFloatCrusher,
			UCrusher = FloatCrusher.defaulHalfFloatCrusher
		});

		public static ElementCrusher defaultHalfFloatSclCrusher = CheckAgainstStatics(new ElementCrusher(StaticTRSType.Scale)
		{
			XCrusher = FloatCrusher.defaulHalfFloatCrusher,
			YCrusher = FloatCrusher.defaulHalfFloatCrusher,
			ZCrusher = FloatCrusher.defaulHalfFloatCrusher,
			UCrusher = FloatCrusher.defaulHalfFloatCrusher
		});

		public bool hideFieldName;

		[SerializeField]
		private TRSType _trsType;

		[SerializeField]
		public Transform defaultTransform;

		[SerializeField]
		public UniformAxes uniformAxes;

		[SerializeField]
		private FloatCrusher _xcrusher;

		[SerializeField]
		private FloatCrusher _ycrusher;

		[SerializeField]
		private FloatCrusher _zcrusher;

		[SerializeField]
		private FloatCrusher _ucrusher;

		[SerializeField]
		private QuatCrusher _qcrusher;

		[SerializeField]
		public bool local;

		[SerializeField]
		private bool useWorldBounds;

		[WorldBoundsSelectAttribute]
		[HideInInspector]
		[SerializeField]
		private int boundsGroupId;

		[SerializeField]
		public bool enableTRSTypeSelector;

		[SerializeField]
		public bool enableLocalSelector = true;

		[NonSerialized]
		private bool cached;

		[NonSerialized]
		private bool cache_xEnabled;

		[NonSerialized]
		private bool cache_yEnabled;

		[NonSerialized]
		private bool cache_zEnabled;

		[NonSerialized]
		private bool cache_uEnabled;

		[NonSerialized]
		private bool cache_qEnabled;

		[NonSerialized]
		private bool cache_isUniformScale;

		[NonSerialized]
		private readonly int[] cache_xBits = new int[4];

		[NonSerialized]
		private readonly int[] cache_yBits = new int[4];

		[NonSerialized]
		private readonly int[] cache_zBits = new int[4];

		[NonSerialized]
		private readonly int[] cache_uBits = new int[4];

		[NonSerialized]
		private readonly int[] cache_TotalBits = new int[4];

		public ReadOnlyCollection<int> Cached_TotalBits;

		[NonSerialized]
		private int cache_qBits;

		[NonSerialized]
		private bool cache_mustCorrectRotationX;

		public Bounds bounds;

		public static ulong[] reusableArray64 = new ulong[2];

		public TRSType TRSType
		{
			get
			{
				return _trsType;
			}
			set
			{
				_trsType = value;
				_xcrusher.TRSType = value;
				_ycrusher.TRSType = value;
				_zcrusher.TRSType = value;
			}
		}

		[Obsolete("Use the XCrusher property instead.")]
		public FloatCrusher xcrusher
		{
			get
			{
				return XCrusher;
			}
			set
			{
				XCrusher = value;
			}
		}

		[Obsolete("Use the YCrusher property instead.")]
		public FloatCrusher ycrusher
		{
			get
			{
				return YCrusher;
			}
			set
			{
				YCrusher = value;
			}
		}

		[Obsolete("Use the ZCrusher property instead.")]
		public FloatCrusher zcrusher
		{
			get
			{
				return ZCrusher;
			}
			set
			{
				ZCrusher = value;
			}
		}

		[Obsolete("Use the UCrusher property instead.")]
		public FloatCrusher ucrusher
		{
			get
			{
				return UCrusher;
			}
			set
			{
				UCrusher = value;
			}
		}

		[Obsolete("Use the QCrusher property instead.")]
		public QuatCrusher qcrusher
		{
			get
			{
				return QCrusher;
			}
			set
			{
				QCrusher = value;
			}
		}

		public FloatCrusher XCrusher
		{
			get
			{
				return _xcrusher;
			}
			set
			{
				if (_xcrusher != value)
				{
					if (_xcrusher != null)
					{
						FloatCrusher floatCrusher = _xcrusher;
						floatCrusher.OnRecalculated = (Action<FloatCrusher>)Delegate.Remove(floatCrusher.OnRecalculated, new Action<FloatCrusher>(OnCrusherChange));
					}
					_xcrusher = value;
					if (_xcrusher != null)
					{
						FloatCrusher floatCrusher2 = _xcrusher;
						floatCrusher2.OnRecalculated = (Action<FloatCrusher>)Delegate.Combine(floatCrusher2.OnRecalculated, new Action<FloatCrusher>(OnCrusherChange));
					}
					CacheValues();
				}
			}
		}

		public FloatCrusher YCrusher
		{
			get
			{
				return _ycrusher;
			}
			set
			{
				if (_ycrusher != value)
				{
					if (_ycrusher != null)
					{
						FloatCrusher floatCrusher = _ycrusher;
						floatCrusher.OnRecalculated = (Action<FloatCrusher>)Delegate.Remove(floatCrusher.OnRecalculated, new Action<FloatCrusher>(OnCrusherChange));
					}
					_ycrusher = value;
					if (_ycrusher != null)
					{
						FloatCrusher floatCrusher2 = _ycrusher;
						floatCrusher2.OnRecalculated = (Action<FloatCrusher>)Delegate.Combine(floatCrusher2.OnRecalculated, new Action<FloatCrusher>(OnCrusherChange));
					}
					CacheValues();
				}
			}
		}

		public FloatCrusher ZCrusher
		{
			get
			{
				return _zcrusher;
			}
			set
			{
				if (_zcrusher != value)
				{
					if (_zcrusher != null)
					{
						FloatCrusher floatCrusher = _zcrusher;
						floatCrusher.OnRecalculated = (Action<FloatCrusher>)Delegate.Remove(floatCrusher.OnRecalculated, new Action<FloatCrusher>(OnCrusherChange));
					}
					_zcrusher = value;
					if (_zcrusher != null)
					{
						FloatCrusher floatCrusher2 = _zcrusher;
						floatCrusher2.OnRecalculated = (Action<FloatCrusher>)Delegate.Combine(floatCrusher2.OnRecalculated, new Action<FloatCrusher>(OnCrusherChange));
					}
					CacheValues();
				}
			}
		}

		public FloatCrusher UCrusher
		{
			get
			{
				return _ucrusher;
			}
			set
			{
				if (_ucrusher != value)
				{
					if (_ucrusher != null)
					{
						FloatCrusher floatCrusher = _ucrusher;
						floatCrusher.OnRecalculated = (Action<FloatCrusher>)Delegate.Remove(floatCrusher.OnRecalculated, new Action<FloatCrusher>(OnCrusherChange));
					}
					_ucrusher = value;
					if (_ucrusher != null)
					{
						FloatCrusher floatCrusher2 = _ucrusher;
						floatCrusher2.OnRecalculated = (Action<FloatCrusher>)Delegate.Combine(floatCrusher2.OnRecalculated, new Action<FloatCrusher>(OnCrusherChange));
					}
					CacheValues();
				}
			}
		}

		public QuatCrusher QCrusher
		{
			get
			{
				return _qcrusher;
			}
			set
			{
				if ((object)_qcrusher != value)
				{
					if (_qcrusher != null)
					{
						QuatCrusher quatCrusher = _qcrusher;
						quatCrusher.OnRecalculated = (Action<QuatCrusher>)Delegate.Remove(quatCrusher.OnRecalculated, new Action<QuatCrusher>(OnCrusherChange));
					}
					_qcrusher = value;
					if (_qcrusher != null)
					{
						QuatCrusher quatCrusher2 = _qcrusher;
						quatCrusher2.OnRecalculated = (Action<QuatCrusher>)Delegate.Combine(quatCrusher2.OnRecalculated, new Action<QuatCrusher>(OnCrusherChange));
					}
					CacheValues();
				}
			}
		}

		[SerializeField]
		public bool UseWorldBounds
		{
			get
			{
				return useWorldBounds;
			}
			set
			{
				ApplyWorldCrusherSettings(value, boundsGroupId);
			}
		}

		public int BoundsGroupId
		{
			get
			{
				return boundsGroupId;
			}
			set
			{
				ApplyWorldCrusherSettings(useWorldBounds, value);
			}
		}

		public Bounds Bounds
		{
			get
			{
				bounds.SetMinMax(new Vector3((_xcrusher != null) ? _xcrusher.Min : 0f, (_ycrusher != null) ? _ycrusher.Min : 0f, (_zcrusher != null) ? _zcrusher.Min : 0f), new Vector3((_xcrusher != null) ? _xcrusher.Max : 0f, (_ycrusher != null) ? _ycrusher.Max : 0f, (_zcrusher != null) ? _zcrusher.Max : 0f));
				return bounds;
			}
			set
			{
				if (_xcrusher != null)
				{
					_xcrusher.SetRange(value.min.x, value.max.x);
				}
				if (_ycrusher != null)
				{
					_ycrusher.SetRange(value.min.y, value.max.y);
				}
				if (_zcrusher != null)
				{
					_zcrusher.SetRange(value.min.z, value.max.z);
				}
				CacheValues();
			}
		}

		public bool Enabled
		{
			get
			{
				if (TRSType == TRSType.Quaternion)
				{
					if (_qcrusher.Enabled)
					{
						return _qcrusher.Bits > 0;
					}
					return false;
				}
				if (TRSType == TRSType.Scale && uniformAxes != 0)
				{
					return _ucrusher.Enabled;
				}
				return _xcrusher.Enabled | _ycrusher.Enabled | _zcrusher.Enabled;
			}
			set
			{
				if (TRSType == TRSType.Quaternion)
				{
					_qcrusher.Enabled = value;
				}
				else if (TRSType == TRSType.Scale && uniformAxes != 0)
				{
					_ucrusher.Enabled = value;
				}
				_xcrusher.Enabled = value;
				_ycrusher.Enabled = value;
				_zcrusher.Enabled = value;
			}
		}

		public FloatCrusher this[int axis]
		{
			get
			{
				switch (axis)
				{
				case 0:
					return _xcrusher;
				case 1:
					return _ycrusher;
				case 2:
					return _zcrusher;
				default:
					Debug.Log("AXIS " + axis + " should not be calling happening");
					return null;
				}
			}
		}

		public static ElementCrusher GetStaticPositionCrusher(Bounds bounds, int resolution)
		{
			return CheckAgainstStatics(new ElementCrusher(StaticTRSType.Position)
			{
				XCrusher = FloatCrusher.GetStaticFloatCrusher(resolution, bounds.min.x, bounds.max.x, Axis.Generic, TRSType.Position),
				YCrusher = FloatCrusher.GetStaticFloatCrusher(resolution, bounds.min.y, bounds.max.y, Axis.Generic, TRSType.Position),
				ZCrusher = FloatCrusher.GetStaticFloatCrusher(resolution, bounds.min.z, bounds.max.z, Axis.Generic, TRSType.Position)
			});
		}

		public static ElementCrusher GetStaticQuatCrusher(int minBits)
		{
			return CheckAgainstStatics(new ElementCrusher(StaticTRSType.Quaternion)
			{
				QCrusher = new QuatCrusher(false, false)
				{
					Bits = minBits
				}
			});
		}

		public static ElementCrusher CheckAgainstStatics(ElementCrusher ec, bool CheckAgainstFloatCrushersAsWell = true)
		{
			if ((object)ec == null)
			{
				return null;
			}
			if (CheckAgainstFloatCrushersAsWell)
			{
				if (ec.cache_xEnabled)
				{
					ec.XCrusher = FloatCrusher.CheckAgainstStatics(ec._xcrusher);
				}
				if (ec.cache_yEnabled)
				{
					ec.YCrusher = FloatCrusher.CheckAgainstStatics(ec._ycrusher);
				}
				if (ec.cache_zEnabled)
				{
					ec.ZCrusher = FloatCrusher.CheckAgainstStatics(ec._zcrusher);
				}
				if (ec.cache_uEnabled)
				{
					ec.UCrusher = FloatCrusher.CheckAgainstStatics(ec._ucrusher);
				}
			}
			int hashCode = ec.GetHashCode();
			if (staticElementCrushers.ContainsKey(hashCode))
			{
				return staticElementCrushers[hashCode];
			}
			staticElementCrushers.Add(hashCode, ec);
			return ec;
		}

		public void OnCrusherChange(QuatCrusher crusher)
		{
			CacheValues();
		}

		public void OnCrusherChange(FloatCrusher crusher)
		{
			CacheValues();
		}

		private void OnWorldBoundsReady()
		{
			ApplyWorldCrusherSettings();
			CacheValues();
		}

		public void ApplyWorldCrusherSettings()
		{
			if (!useWorldBounds)
			{
				return;
			}
			if (SettingsScriptableObject<WorldBoundsSettings>.single == null)
			{
				SettingsScriptableObject<WorldBoundsSettings>.OnSingletonReady = (Action)Delegate.Remove(SettingsScriptableObject<WorldBoundsSettings>.OnSingletonReady, new Action(OnWorldBoundsReady));
				SettingsScriptableObject<WorldBoundsSettings>.OnSingletonReady = (Action)Delegate.Combine(SettingsScriptableObject<WorldBoundsSettings>.OnSingletonReady, new Action(OnWorldBoundsReady));
				return;
			}
			SettingsScriptableObject<WorldBoundsSettings>.OnSingletonReady = (Action)Delegate.Remove(SettingsScriptableObject<WorldBoundsSettings>.OnSingletonReady, new Action(OnWorldBoundsReady));
			List<WorldBoundsGroup> worldBoundsGroups = SettingsScriptableObject<WorldBoundsSettings>.single.worldBoundsGroups;
			if (boundsGroupId >= worldBoundsGroups.Count)
			{
				Debug.LogError("WorldBoundsGroup " + boundsGroupId + " no longer exists. Using Default(0).");
				boundsGroupId = 0;
			}
			WorldBoundsGroup worldBoundsGroup = worldBoundsGroups[boundsGroupId];
			worldBoundsGroup.OnWorldBoundChanged = (Action)Delegate.Remove(worldBoundsGroup.OnWorldBoundChanged, new Action(CacheValues));
			worldBoundsGroup.OnWorldBoundChanged = (Action)Delegate.Combine(worldBoundsGroup.OnWorldBoundChanged, new Action(CacheValues));
			ElementCrusher crusher = worldBoundsGroup.crusher;
			if (_xcrusher != crusher._xcrusher)
			{
				XCrusher = crusher.XCrusher;
			}
			if (_ycrusher != crusher._ycrusher)
			{
				YCrusher = crusher.YCrusher;
			}
			if (_zcrusher != crusher._zcrusher)
			{
				ZCrusher = crusher.ZCrusher;
			}
			local = crusher.local;
		}

		public void ApplyWorldCrusherSettings(bool newUseBounds, int newBndsGrpId)
		{
			if (newUseBounds != useWorldBounds)
			{
				useWorldBounds = newUseBounds;
				if (!useWorldBounds)
				{
					Defaults(TRSType.Position);
				}
			}
			if (SettingsScriptableObject<WorldBoundsSettings>.single == null)
			{
				Debug.LogWarning("Not Ready to Change the World");
				return;
			}
			List<WorldBoundsGroup> worldBoundsGroups = SettingsScriptableObject<WorldBoundsSettings>.single.worldBoundsGroups;
			if (newBndsGrpId == boundsGroupId)
			{
				return;
			}
			if (boundsGroupId < worldBoundsGroups.Count)
			{
				WorldBoundsGroup worldBoundsGroup = worldBoundsGroups[boundsGroupId];
				if (worldBoundsGroup != null)
				{
					worldBoundsGroup.OnWorldBoundChanged = (Action)Delegate.Remove(worldBoundsGroup.OnWorldBoundChanged, new Action(CacheValues));
				}
			}
			if (newBndsGrpId >= worldBoundsGroups.Count)
			{
				boundsGroupId = 0;
			}
			else
			{
				boundsGroupId = newBndsGrpId;
			}
			WorldBoundsGroup worldBoundsGroup2 = worldBoundsGroups[boundsGroupId];
			if (worldBoundsGroup2 != null && useWorldBounds)
			{
				worldBoundsGroup2.OnWorldBoundChanged = (Action)Delegate.Combine(worldBoundsGroup2.OnWorldBoundChanged, new Action(CacheValues));
				ElementCrusher crusher = worldBoundsGroup2.crusher;
				if (_xcrusher != crusher._xcrusher)
				{
					XCrusher = crusher.XCrusher;
				}
				if (_ycrusher != crusher._ycrusher)
				{
					YCrusher = crusher.YCrusher;
				}
				if (_zcrusher != crusher._zcrusher)
				{
					ZCrusher = crusher.ZCrusher;
				}
				local = crusher.local;
			}
		}

		private WorldBoundsGroup GetUsedWorldBounds()
		{
			if (_trsType == TRSType.Position && useWorldBounds)
			{
				if (boundsGroupId >= SettingsScriptableObject<WorldBoundsSettings>.Single.worldBoundsGroups.Count)
				{
					boundsGroupId = 0;
				}
				return SettingsScriptableObject<WorldBoundsSettings>.Single.worldBoundsGroups[boundsGroupId];
			}
			return null;
		}

		public override void OnBeforeSerialize()
		{
		}

		public override void OnAfterDeserialize()
		{
		}

		public void CacheValues()
		{
			ApplyWorldCrusherSettings();
			NullUnusedCrushers();
			if (_trsType == TRSType.Quaternion)
			{
				cache_qEnabled = _qcrusher != null && _qcrusher.Enabled && _qcrusher.Bits > 0;
				cache_qBits = (cache_qEnabled ? _qcrusher.Bits : 0);
				cache_TotalBits[0] = cache_qBits;
				cache_TotalBits[1] = cache_qBits;
				cache_TotalBits[2] = cache_qBits;
				cache_TotalBits[3] = cache_qBits;
				cache_isUniformScale = false;
			}
			else if (_trsType == TRSType.Scale && uniformAxes != 0)
			{
				cache_uEnabled = _ucrusher != null && _ucrusher.Enabled;
				for (int i = 0; i < 4; i++)
				{
					int num = (cache_uEnabled ? _ucrusher.GetBits((BitCullingLevel)i) : 0);
					cache_uBits[i] = num;
					cache_TotalBits[i] = num;
				}
				cache_isUniformScale = true;
			}
			else
			{
				cache_mustCorrectRotationX = _trsType == TRSType.Euler && _xcrusher.UseHalfRangeX;
				for (int j = 0; j < 4; j++)
				{
					cache_xEnabled = _xcrusher != null && _xcrusher.Enabled;
					cache_yEnabled = _ycrusher != null && _ycrusher.Enabled;
					cache_zEnabled = _zcrusher != null && _zcrusher.Enabled;
					cache_xBits[j] = (cache_xEnabled ? _xcrusher.GetBits((BitCullingLevel)j) : 0);
					cache_yBits[j] = (cache_yEnabled ? _ycrusher.GetBits((BitCullingLevel)j) : 0);
					cache_zBits[j] = (cache_zEnabled ? _zcrusher.GetBits((BitCullingLevel)j) : 0);
					cache_TotalBits[j] = cache_xBits[j] + cache_yBits[j] + cache_zBits[j];
					cache_isUniformScale = false;
				}
			}
			Cached_TotalBits = Array.AsReadOnly(cache_TotalBits);
			cached = true;
			if (OnRecalculated != null)
			{
				OnRecalculated(this);
			}
		}

		private void NullUnusedCrushers()
		{
			if (_trsType == TRSType.Quaternion)
			{
				XCrusher = null;
				YCrusher = null;
				ZCrusher = null;
				UCrusher = null;
			}
			else if (_trsType == TRSType.Scale && uniformAxes != 0)
			{
				XCrusher = null;
				YCrusher = null;
				ZCrusher = null;
				QCrusher = null;
			}
			else
			{
				QCrusher = null;
				UCrusher = null;
			}
		}

		public ElementCrusher()
		{
			Defaults(TRSType.Generic);
		}

		internal ElementCrusher(StaticTRSType staticTrsType)
		{
			_trsType = (TRSType)staticTrsType;
		}

		public ElementCrusher(bool enableTRSTypeSelector = true)
		{
			_trsType = TRSType.Generic;
			Defaults(TRSType.Generic);
			this.enableTRSTypeSelector = enableTRSTypeSelector;
		}

		public ElementCrusher(TRSType trsType, bool enableTRSTypeSelector = true)
		{
			_trsType = trsType;
			Defaults(trsType);
			this.enableTRSTypeSelector = enableTRSTypeSelector;
		}

		public void Defaults(TRSType trs)
		{
			switch (trs)
			{
			case TRSType.Euler:
			case TRSType.Quaternion:
				XCrusher = new FloatCrusher(BitPresets.Bits10, -90f, 90f, Axis.X, TRSType.Euler, true);
				YCrusher = new FloatCrusher(BitPresets.Bits12, -180f, 180f, Axis.Y, TRSType.Euler, true);
				ZCrusher = new FloatCrusher(BitPresets.Bits10, -180f, 180f, Axis.Z, TRSType.Euler, true);
				QCrusher = new QuatCrusher(true, false);
				break;
			case TRSType.Scale:
				XCrusher = new FloatCrusher(BitPresets.Bits12, 0f, 2f, Axis.X, TRSType.Scale, true);
				YCrusher = new FloatCrusher(BitPresets.Bits10, 0f, 2f, Axis.Y, TRSType.Scale, true);
				ZCrusher = new FloatCrusher(BitPresets.Bits10, 0f, 2f, Axis.Z, TRSType.Scale, true);
				UCrusher = new FloatCrusher(BitPresets.Bits10, 0f, 2f, Axis.Uniform, TRSType.Scale, true);
				break;
			default:
				XCrusher = new FloatCrusher(BitPresets.Bits12, -20f, 20f, Axis.X, trs, true);
				YCrusher = new FloatCrusher(BitPresets.Bits10, -5f, 5f, Axis.Y, trs, true);
				ZCrusher = new FloatCrusher(BitPresets.Bits10, -5f, 5f, Axis.Z, trs, true);
				break;
			}
		}

		public void Write(CompressedElement nonalloc, Transform trans, byte[] bytes, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			switch (TRSType)
			{
			case TRSType.Position:
				Write(nonalloc, local ? trans.localPosition : trans.position, bytes, ref bitposition, bcl);
				break;
			case TRSType.Euler:
				Write(nonalloc, local ? trans.localEulerAngles : trans.eulerAngles, bytes, ref bitposition, bcl);
				break;
			case TRSType.Quaternion:
				Write(nonalloc, local ? trans.localRotation : trans.rotation, bytes, ref bitposition, bcl);
				break;
			case TRSType.Scale:
				Write(nonalloc, trans.localScale, bytes, ref bitposition, bcl);
				break;
			default:
				Write(nonalloc, local ? trans.localPosition : trans.position, bytes, ref bitposition, bcl);
				break;
			}
		}

		[Obsolete]
		public CompressedElement Write(Transform trans, byte[] bytes, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			switch (TRSType)
			{
			case TRSType.Position:
				return Write(local ? trans.localPosition : trans.position, bytes, ref bitposition, bcl);
			case TRSType.Euler:
				return Write(local ? trans.localEulerAngles : trans.eulerAngles, bytes, ref bitposition, bcl);
			case TRSType.Quaternion:
				return Write(local ? trans.localRotation : trans.rotation, bytes, ref bitposition, bcl);
			case TRSType.Scale:
				return Write(trans.localScale, bytes, ref bitposition, bcl);
			default:
				return Write(local ? trans.localPosition : trans.position, bytes, ref bitposition, bcl);
			}
		}

		public void Write(CompressedElement ce, byte[] buffer, ref int bitposition, IncludedAxes ia = IncludedAxes.XYZ, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			if (!cached)
			{
				CacheValues();
			}
			if (TRSType == TRSType.Quaternion)
			{
				_qcrusher.Write(ce.cQuat, buffer, ref bitposition);
				return;
			}
			if (cache_isUniformScale)
			{
				_ucrusher.Write(ce.cUniform, buffer, ref bitposition, bcl);
				return;
			}
			if (cache_xEnabled && (ia & IncludedAxes.X) != 0)
			{
				_xcrusher.Write(ce.cx, buffer, ref bitposition, bcl);
			}
			if (cache_yEnabled && (ia & IncludedAxes.Y) != 0)
			{
				_ycrusher.Write(ce.cy, buffer, ref bitposition, bcl);
			}
			if (cache_zEnabled && (ia & IncludedAxes.Z) != 0)
			{
				_zcrusher.Write(ce.cz, buffer, ref bitposition, bcl);
			}
		}

		public void Write(CompressedElement ce, uint[] buffer, ref int bitposition, IncludedAxes ia = IncludedAxes.XYZ, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			if (!cached)
			{
				CacheValues();
			}
			if (TRSType == TRSType.Quaternion)
			{
				_qcrusher.Write(ce.cQuat, buffer, ref bitposition);
				return;
			}
			if (cache_isUniformScale)
			{
				_ucrusher.Write(ce.cUniform, buffer, ref bitposition, bcl);
				return;
			}
			if (cache_xEnabled && (ia & IncludedAxes.X) != 0)
			{
				_xcrusher.Write(ce.cx, buffer, ref bitposition, bcl);
			}
			if (cache_yEnabled && (ia & IncludedAxes.Y) != 0)
			{
				_ycrusher.Write(ce.cy, buffer, ref bitposition, bcl);
			}
			if (cache_zEnabled && (ia & IncludedAxes.Z) != 0)
			{
				_zcrusher.Write(ce.cz, buffer, ref bitposition, bcl);
			}
		}

		public void Write(CompressedElement ce, ulong[] buffer, ref int bitposition, IncludedAxes ia = IncludedAxes.XYZ, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			if (!cached)
			{
				CacheValues();
			}
			if (TRSType == TRSType.Quaternion)
			{
				_qcrusher.Write(ce.cQuat, buffer, ref bitposition);
				return;
			}
			if (cache_isUniformScale)
			{
				_ucrusher.Write(ce.cUniform, buffer, ref bitposition, bcl);
				return;
			}
			if (cache_xEnabled && (ia & IncludedAxes.X) != 0)
			{
				_xcrusher.Write(ce.cx, buffer, ref bitposition, bcl);
			}
			if (cache_yEnabled && (ia & IncludedAxes.Y) != 0)
			{
				_ycrusher.Write(ce.cy, buffer, ref bitposition, bcl);
			}
			if (cache_zEnabled && (ia & IncludedAxes.Z) != 0)
			{
				_zcrusher.Write(ce.cz, buffer, ref bitposition, bcl);
			}
		}

		public void Write(CompressedElement nonalloc, Vector3 v3, byte[] bytes, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			int bitposition = 0;
			Write(nonalloc, v3, bytes, ref bitposition);
		}

		[Obsolete]
		public CompressedElement Write(Vector3 v3, byte[] bytes, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			int bitposition = 0;
			return Write(v3, bytes, ref bitposition);
		}

		public void Write(CompressedElement nonalloc, Vector3 v3, byte[] bytes, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			if (!cached)
			{
				CacheValues();
			}
			if (cache_isUniformScale)
			{
				nonalloc.Set(this, _ucrusher.Write((uniformAxes == UniformAxes.YZ) ? v3.y : v3.x, bytes, ref bitposition, bcl).cvalue);
				return;
			}
			if (TRSType == TRSType.Quaternion)
			{
				ulong cQuat = _qcrusher.Write(Quaternion.Euler(v3), bytes, ref bitposition);
				nonalloc.Set(this, cQuat);
				return;
			}
			if (cache_mustCorrectRotationX)
			{
				v3 = v3.GetXCorrectedEuler();
			}
			nonalloc.Set(this, cache_xEnabled ? _xcrusher.Write(v3.x, bytes, ref bitposition, bcl) : default(CompressedFloat), cache_yEnabled ? _ycrusher.Write(v3.y, bytes, ref bitposition, bcl) : default(CompressedFloat), cache_zEnabled ? _zcrusher.Write(v3.z, bytes, ref bitposition, bcl) : default(CompressedFloat));
		}

		[Obsolete]
		public CompressedElement Write(Vector3 v3, byte[] bytes, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			if (!cached)
			{
				CacheValues();
			}
			if (cache_isUniformScale)
			{
				return new CompressedElement(this, (uint)((CompressedValue)_ucrusher.Write((uniformAxes == UniformAxes.YZ) ? v3.y : v3.x, bytes, ref bitposition, bcl)).cvalue);
			}
			if (TRSType == TRSType.Quaternion)
			{
				ulong cQuat = _qcrusher.Write(Quaternion.Euler(v3), bytes, ref bitposition);
				return new CompressedElement(this, cQuat);
			}
			if (cache_mustCorrectRotationX)
			{
				v3 = v3.GetXCorrectedEuler();
			}
			return new CompressedElement(this, cache_xEnabled ? ((uint)_xcrusher.Write(v3.x, bytes, ref bitposition, bcl)) : 0u, cache_yEnabled ? ((uint)_ycrusher.Write(v3.y, bytes, ref bitposition, bcl)) : 0u, cache_zEnabled ? ((uint)_zcrusher.Write(v3.z, bytes, ref bitposition, bcl)) : 0u);
		}

		public void Write(CompressedElement nonalloc, Quaternion quat, byte[] bytes, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			if (!cached)
			{
				CacheValues();
			}
			nonalloc.Set(this, _qcrusher.Write(quat, bytes, ref bitposition));
		}

		[Obsolete]
		public CompressedElement Write(Quaternion quat, byte[] bytes, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			if (!cached)
			{
				CacheValues();
			}
			return new CompressedElement(this, _qcrusher.Write(quat, bytes, ref bitposition));
		}

		public void Write(CompressedElement nonalloc, Transform trans, ulong[] buffer, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			switch (TRSType)
			{
			case TRSType.Position:
				Write(nonalloc, local ? trans.localPosition : trans.position, buffer, ref bitposition, bcl);
				break;
			case TRSType.Euler:
				Write(nonalloc, local ? trans.localEulerAngles : trans.eulerAngles, buffer, ref bitposition, bcl);
				break;
			case TRSType.Quaternion:
				Write(nonalloc, local ? trans.localRotation : trans.rotation, buffer, ref bitposition, bcl);
				break;
			case TRSType.Scale:
				Write(nonalloc, trans.localScale, buffer, ref bitposition, bcl);
				break;
			default:
				Write(nonalloc, local ? trans.localPosition : trans.position, buffer, ref bitposition, bcl);
				break;
			}
		}

		public void Write(CompressedElement nonalloc, Vector3 v3, ulong[] buffer, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			if (!cached)
			{
				CacheValues();
			}
			if (cache_isUniformScale)
			{
				nonalloc.Set(this, _ucrusher.Write((uniformAxes == UniformAxes.YZ) ? v3.y : v3.x, buffer, ref bitposition, bcl).cvalue);
				return;
			}
			if (TRSType == TRSType.Quaternion)
			{
				ulong cQuat = _qcrusher.Write(Quaternion.Euler(v3), buffer, ref bitposition);
				nonalloc.Set(this, cQuat);
				return;
			}
			if (cache_mustCorrectRotationX)
			{
				v3 = v3.GetXCorrectedEuler();
			}
			nonalloc.Set(this, cache_xEnabled ? _xcrusher.Write(v3.x, buffer, ref bitposition, bcl) : default(CompressedFloat), cache_yEnabled ? _ycrusher.Write(v3.y, buffer, ref bitposition, bcl) : default(CompressedFloat), cache_zEnabled ? _zcrusher.Write(v3.z, buffer, ref bitposition, bcl) : default(CompressedFloat));
		}

		public void Write(CompressedElement nonalloc, Quaternion quat, ulong[] bytes, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			if (!cached)
			{
				CacheValues();
			}
			nonalloc.Set(this, _qcrusher.Write(quat, bytes, ref bitposition));
		}

		public void Read(CompressedElement nonalloc, byte[] bytes, IncludedAxes ia = IncludedAxes.XYZ, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			int bitposition = 0;
			Read(nonalloc, bytes, ref bitposition, ia, bcl);
		}

		public CompressedElement Read(byte[] buffer, IncludedAxes ia = IncludedAxes.XYZ, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			int bitposition = 0;
			Read(CompressedElement.reusable, buffer, ref bitposition, ia, bcl);
			return CompressedElement.reusable;
		}

		public void Read(CompressedElement nonalloc, byte[] bytes, ref int bitposition, IncludedAxes ia = IncludedAxes.XYZ, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			if (!cached)
			{
				CacheValues();
			}
			if (TRSType == TRSType.Quaternion)
			{
				nonalloc.Set(this, bytes.Read(ref bitposition, cache_qBits));
				return;
			}
			if (cache_isUniformScale)
			{
				nonalloc.Set(this, (uint)bytes.Read(ref bitposition, cache_uBits[(int)bcl]));
				return;
			}
			CompressedFloat cx;
			if ((ia & IncludedAxes.X) != 0)
			{
				int bits = cache_xBits[(int)bcl];
				cx = (cache_xEnabled ? new CompressedFloat(_xcrusher, (uint)bytes.Read(ref bitposition, bits)) : default(CompressedFloat));
			}
			else
			{
				cx = default(CompressedFloat);
			}
			CompressedFloat cy;
			if ((ia & IncludedAxes.Y) != 0)
			{
				int bits2 = cache_yBits[(int)bcl];
				cy = (cache_yEnabled ? new CompressedFloat(_ycrusher, (uint)bytes.Read(ref bitposition, bits2)) : default(CompressedFloat));
			}
			else
			{
				cy = default(CompressedFloat);
			}
			CompressedFloat cz;
			if ((ia & IncludedAxes.Z) != 0)
			{
				int bits3 = cache_zBits[(int)bcl];
				cz = (cache_zEnabled ? new CompressedFloat(_zcrusher, (uint)bytes.Read(ref bitposition, bits3)) : default(CompressedFloat));
			}
			else
			{
				cz = default(CompressedFloat);
			}
			nonalloc.Set(this, cx, cy, cz);
		}

		public CompressedElement Read(byte[] bytes, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			Read(CompressedElement.reusable, bytes, ref bitposition);
			return CompressedElement.reusable;
		}

		public void Read(CompressedElement nonalloc, ulong[] buffer, ref int bitposition, IncludedAxes ia = IncludedAxes.XYZ, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			if (!cached)
			{
				CacheValues();
			}
			if (TRSType == TRSType.Quaternion)
			{
				nonalloc.Set(this, buffer.Read(ref bitposition, cache_qBits));
				return;
			}
			if (cache_isUniformScale)
			{
				nonalloc.Set(this, (uint)buffer.Read(ref bitposition, cache_uBits[(int)bcl]));
				return;
			}
			CompressedFloat cx;
			if ((ia & IncludedAxes.X) != 0)
			{
				int bits = cache_xBits[(int)bcl];
				cx = (cache_xEnabled ? new CompressedFloat(_xcrusher, (uint)buffer.Read(ref bitposition, bits)) : default(CompressedFloat));
			}
			else
			{
				cx = default(CompressedFloat);
			}
			CompressedFloat cy;
			if ((ia & IncludedAxes.Y) != 0)
			{
				int bits2 = cache_yBits[(int)bcl];
				cy = (cache_yEnabled ? new CompressedFloat(_ycrusher, (uint)buffer.Read(ref bitposition, bits2)) : default(CompressedFloat));
			}
			else
			{
				cy = default(CompressedFloat);
			}
			CompressedFloat cz;
			if ((ia & IncludedAxes.Z) != 0)
			{
				int bits3 = cache_zBits[(int)bcl];
				cz = (cache_zEnabled ? new CompressedFloat(_zcrusher, (uint)buffer.Read(ref bitposition, bits3)) : default(CompressedFloat));
			}
			else
			{
				cz = default(CompressedFloat);
			}
			nonalloc.Set(this, cx, cy, cz);
		}

		public void Read(CompressedElement nonalloc, uint[] buffer, ref int bitposition, IncludedAxes ia = IncludedAxes.XYZ, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			if (!cached)
			{
				CacheValues();
			}
			if (TRSType == TRSType.Quaternion)
			{
				nonalloc.Set(this, buffer.Read(ref bitposition, cache_qBits));
				return;
			}
			if (cache_isUniformScale)
			{
				nonalloc.Set(this, (uint)buffer.Read(ref bitposition, cache_uBits[(int)bcl]));
				return;
			}
			CompressedFloat cx;
			if ((ia & IncludedAxes.X) != 0)
			{
				int bits = cache_xBits[(int)bcl];
				cx = (cache_xEnabled ? new CompressedFloat(_xcrusher, (uint)buffer.Read(ref bitposition, bits)) : default(CompressedFloat));
			}
			else
			{
				cx = default(CompressedFloat);
			}
			CompressedFloat cy;
			if ((ia & IncludedAxes.Y) != 0)
			{
				int bits2 = cache_yBits[(int)bcl];
				cy = (cache_yEnabled ? new CompressedFloat(_ycrusher, (uint)buffer.Read(ref bitposition, bits2)) : default(CompressedFloat));
			}
			else
			{
				cy = default(CompressedFloat);
			}
			CompressedFloat cz;
			if ((ia & IncludedAxes.Z) != 0)
			{
				int bits3 = cache_zBits[(int)bcl];
				cz = (cache_zEnabled ? new CompressedFloat(_zcrusher, (uint)buffer.Read(ref bitposition, bits3)) : default(CompressedFloat));
			}
			else
			{
				cz = default(CompressedFloat);
			}
			nonalloc.Set(this, cx, cy, cz);
		}

		public Element ReadAndDecompress(byte[] bytes, ref int bitposition, IncludedAxes ia = IncludedAxes.XYZ, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			Read(reusableCE, bytes, ref bitposition, ia, bcl);
			return Decompress(reusableCE);
		}

		public void Write(CompressedElement nonalloc, Transform trans, ref ulong buffer, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			switch (TRSType)
			{
			case TRSType.Position:
				Write(nonalloc, local ? trans.localPosition : trans.position, ref buffer, ref bitposition, bcl);
				break;
			case TRSType.Euler:
				Write(nonalloc, local ? trans.localEulerAngles : trans.eulerAngles, ref buffer, ref bitposition, bcl);
				break;
			case TRSType.Quaternion:
				Write(nonalloc, local ? trans.localRotation : trans.rotation, ref buffer, ref bitposition);
				break;
			case TRSType.Scale:
				Write(nonalloc, trans.localScale, ref buffer, ref bitposition, bcl);
				break;
			default:
				Write(nonalloc, local ? trans.localPosition : trans.position, ref buffer, ref bitposition, bcl);
				break;
			}
		}

		public void Write(Transform trans, ref ulong buffer, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			switch (TRSType)
			{
			case TRSType.Position:
				Write(local ? trans.localPosition : trans.position, ref buffer, ref bitposition, bcl);
				break;
			case TRSType.Euler:
				Write(local ? trans.localEulerAngles : trans.eulerAngles, ref buffer, ref bitposition, bcl);
				break;
			case TRSType.Quaternion:
				Write(local ? trans.localRotation : trans.rotation, ref buffer, ref bitposition);
				break;
			case TRSType.Scale:
				Write(trans.localScale, ref buffer, ref bitposition, bcl);
				break;
			default:
				Write(local ? trans.localPosition : trans.position, ref buffer, ref bitposition, bcl);
				break;
			}
		}

		public void Write(CompressedElement nonalloc, Vector3 v3, ref ulong buffer, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			int bitposition = 0;
			Write(nonalloc, v3, ref buffer, ref bitposition, bcl);
		}

		public void Write(Vector3 v3, ref ulong buffer, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			int bitposition = 0;
			Write(v3, ref buffer, ref bitposition, bcl);
		}

		public void Write(CompressedElement nonalloc, Vector3 v3, ref ulong buffer, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			if (!cached)
			{
				CacheValues();
			}
			CompressedFloat cx = (cache_xEnabled ? _xcrusher.Write(v3.x, ref buffer, ref bitposition, bcl) : default(CompressedFloat));
			CompressedFloat cy = (cache_yEnabled ? _ycrusher.Write(v3.y, ref buffer, ref bitposition, bcl) : default(CompressedFloat));
			CompressedFloat cz = (cache_zEnabled ? _zcrusher.Write(v3.z, ref buffer, ref bitposition, bcl) : default(CompressedFloat));
			nonalloc.Set(this, cx, cy, cz);
		}

		public void Write(Vector3 v3, ref ulong buffer, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			if (!cached)
			{
				CacheValues();
			}
			if (cache_xEnabled)
			{
				_xcrusher.Write(v3.x, ref buffer, ref bitposition, bcl);
			}
			if (cache_yEnabled)
			{
				_ycrusher.Write(v3.y, ref buffer, ref bitposition, bcl);
			}
			if (cache_zEnabled)
			{
				_zcrusher.Write(v3.z, ref buffer, ref bitposition, bcl);
			}
		}

		public void Write(CompressedElement nonalloc, Quaternion quat, ref ulong buffer)
		{
			int bitposition = 0;
			Write(nonalloc, quat, ref buffer, ref bitposition);
		}

		public void Write(Quaternion quat, ref ulong buffer)
		{
			int bitposition = 0;
			Write(quat, ref buffer, ref bitposition);
		}

		public void Write(CompressedElement nonalloc, Quaternion quat, ref ulong buffer, ref int bitposition)
		{
			if (!cached)
			{
				CacheValues();
			}
			ulong cQuat = (cache_qEnabled ? _qcrusher.Write(quat, ref buffer, ref bitposition) : 0);
			nonalloc.Set(this, cQuat);
		}

		public void Write(Quaternion quat, ref ulong buffer, ref int bitposition)
		{
			if (!cached)
			{
				CacheValues();
			}
			if (cache_qEnabled)
			{
				_qcrusher.Write(quat, ref buffer, ref bitposition);
			}
		}

		public CompressedElement Write(CompressedElement ce, ref ulong buffer, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			int bitposition = 0;
			return Write(ce, ref buffer, ref bitposition, bcl);
		}

		public CompressedElement Write(CompressedElement ce, ref ulong buffer, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			if (!cached)
			{
				CacheValues();
			}
			if (cache_qEnabled)
			{
				ce.cQuat.cvalue.Inject(ref buffer, ref bitposition, cache_qBits);
			}
			else if (cache_uEnabled)
			{
				ce.cUniform.cvalue.Inject(ref buffer, ref bitposition, cache_uBits[(int)bcl]);
			}
			else
			{
				if (cache_xEnabled)
				{
					ce.cx.cvalue.Inject(ref buffer, ref bitposition, cache_xBits[(int)bcl]);
				}
				if (cache_yEnabled)
				{
					ce.cy.cvalue.Inject(ref buffer, ref bitposition, cache_yBits[(int)bcl]);
				}
				if (cache_zEnabled)
				{
					ce.cz.cvalue.Inject(ref buffer, ref bitposition, cache_zBits[(int)bcl]);
				}
			}
			return ce;
		}

		public Element Read(ulong buffer, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			if (!cached)
			{
				CacheValues();
			}
			int bitposition = 0;
			if (TRSType == TRSType.Quaternion)
			{
				ulong compressed = buffer.Read(ref bitposition, cache_qBits);
				return _qcrusher.Decompress(compressed);
			}
			if (cache_isUniformScale)
			{
				float num = _ucrusher.ReadAndDecompress(buffer, ref bitposition, bcl);
				return new Vector3(num, num, num);
			}
			return new Vector3(cache_xEnabled ? _xcrusher.ReadAndDecompress(buffer, ref bitposition, bcl) : 0f, cache_yEnabled ? _ycrusher.ReadAndDecompress(buffer, ref bitposition, bcl) : 0f, cache_zEnabled ? _zcrusher.ReadAndDecompress(buffer, ref bitposition, bcl) : 0f);
		}

		public Element Read(ulong buffer, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			if (!cached)
			{
				CacheValues();
			}
			if (TRSType == TRSType.Quaternion)
			{
				ulong compressed = buffer.Read(ref bitposition, cache_qBits);
				return _qcrusher.Decompress(compressed);
			}
			if (cache_isUniformScale)
			{
				float num = _ucrusher.ReadAndDecompress(buffer, ref bitposition, bcl);
				return new Vector3(num, num, num);
			}
			return new Vector3(cache_xEnabled ? _xcrusher.ReadAndDecompress(buffer, ref bitposition, bcl) : 0f, cache_yEnabled ? _ycrusher.ReadAndDecompress(buffer, ref bitposition, bcl) : 0f, cache_zEnabled ? _zcrusher.ReadAndDecompress(buffer, ref bitposition, bcl) : 0f);
		}

		public void Read(CompressedElement nonalloc, ulong buffer, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			int bitposition = 0;
			Read(nonalloc, buffer, ref bitposition, bcl);
		}

		public void Read(CompressedElement nonalloc, ulong buffer, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			if (!cached)
			{
				CacheValues();
			}
			if (TRSType == TRSType.Quaternion)
			{
				nonalloc.Set(this, buffer.Read(ref bitposition, cache_qBits));
				return;
			}
			if (cache_isUniformScale)
			{
				nonalloc.Set(this, (uint)buffer.Read(ref bitposition, cache_uBits[(int)bcl]));
				return;
			}
			CompressedFloat cx = (cache_xEnabled ? new CompressedFloat(_xcrusher, buffer.Read(ref bitposition, cache_xBits[(int)bcl])) : default(CompressedFloat));
			CompressedFloat cy = (cache_yEnabled ? new CompressedFloat(_ycrusher, buffer.Read(ref bitposition, cache_yBits[(int)bcl])) : default(CompressedFloat));
			CompressedFloat cz = (cache_zEnabled ? new CompressedFloat(_zcrusher, buffer.Read(ref bitposition, cache_zBits[(int)bcl])) : default(CompressedFloat));
			nonalloc.Set(this, cx, cy, cz);
		}

		public void Read(CompressedElement nonalloc, ulong frag0, ulong frag1 = 0uL, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			if (!cached)
			{
				CacheValues();
			}
			int bitposition = 0;
			reusableArray64.Write(frag0, ref bitposition, 64);
			reusableArray64.Write(frag1, ref bitposition, 64);
			bitposition = 0;
			Read(nonalloc, reusableArray64, ref bitposition, IncludedAxes.XYZ, bcl);
		}

		public CompressedElement Read(ulong frag0, ulong frag1 = 0uL, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			Read(CompressedElement.reusable, frag0, frag1, bcl);
			return CompressedElement.reusable;
		}

		public void Compress(CompressedElement nonalloc, Transform trans)
		{
			switch (TRSType)
			{
			case TRSType.Position:
				Compress(nonalloc, local ? trans.localPosition : trans.position);
				break;
			case TRSType.Euler:
				Compress(nonalloc, local ? trans.localEulerAngles : trans.eulerAngles);
				break;
			case TRSType.Quaternion:
				Compress(nonalloc, local ? trans.localRotation : trans.rotation);
				break;
			case TRSType.Scale:
				Compress(nonalloc, local ? trans.localScale : trans.lossyScale);
				break;
			default:
				Compress(nonalloc, local ? trans.localPosition : trans.position);
				break;
			}
		}

		public CompressedElement Compress(Transform trans)
		{
			Compress(CompressedElement.reusable, trans);
			return CompressedElement.reusable;
		}

		public void CompressAndWrite(Transform trans, byte[] buffer, ref int bitposition)
		{
			switch (TRSType)
			{
			case TRSType.Position:
				CompressAndWrite(local ? trans.localPosition : trans.position, buffer, ref bitposition);
				break;
			case TRSType.Euler:
				CompressAndWrite(local ? trans.localEulerAngles : trans.eulerAngles, buffer, ref bitposition);
				break;
			case TRSType.Quaternion:
				CompressAndWrite(local ? trans.localRotation : trans.rotation, buffer, ref bitposition);
				break;
			case TRSType.Scale:
				CompressAndWrite(local ? trans.localScale : trans.lossyScale, buffer, ref bitposition);
				break;
			default:
				CompressAndWrite(local ? trans.localPosition : trans.position, buffer, ref bitposition);
				break;
			}
		}

		public void Compress(CompressedElement nonalloc, Element e)
		{
			if (TRSType == TRSType.Quaternion)
			{
				Compress(nonalloc, e.quat);
			}
			else
			{
				Compress(nonalloc, e.v);
			}
		}

		public CompressedElement Compress(Element e)
		{
			if (TRSType == TRSType.Quaternion)
			{
				Compress(CompressedElement.reusable, e.quat);
			}
			else
			{
				Compress(CompressedElement.reusable, e.v);
			}
			return CompressedElement.reusable;
		}

		public void CompressAndWrite(Element e, byte[] buffer, ref int bitposition)
		{
			if (TRSType == TRSType.Quaternion)
			{
				CompressAndWrite(e.quat, buffer, ref bitposition);
			}
			else
			{
				CompressAndWrite(e.v, buffer, ref bitposition);
			}
		}

		public void Compress(CompressedElement nonalloc, Rigidbody rb, IncludedAxes ia = IncludedAxes.XYZ)
		{
			if (!cached)
			{
				CacheValues();
			}
			switch (_trsType)
			{
			case TRSType.Scale:
				Compress(nonalloc, local ? rb.transform.localScale : rb.transform.lossyScale, ia);
				break;
			case TRSType.Quaternion:
				if (cache_qEnabled)
				{
					Quaternion quat;
					if (local)
					{
						Transform parent3 = rb.transform.parent;
						quat = (parent3 ? (Quaternion.Inverse(parent3.rotation) * rb.rotation) : rb.rotation);
					}
					else
					{
						quat = rb.rotation;
					}
					nonalloc.Set(this, _qcrusher.Compress(quat));
				}
				break;
			case TRSType.Euler:
			{
				Vector3 vector2;
				if (local)
				{
					Transform parent2 = rb.transform.parent;
					vector2 = (parent2 ? (Quaternion.Inverse(parent2.rotation) * rb.rotation).eulerAngles : rb.rotation.eulerAngles);
				}
				else
				{
					vector2 = rb.rotation.eulerAngles;
				}
				CompressedFloat cx2 = ((cache_xEnabled && (ia & IncludedAxes.X) != 0) ? _xcrusher.Compress(vector2.x) : default(CompressedFloat));
				CompressedFloat cy2 = ((cache_yEnabled && (ia & IncludedAxes.Y) != 0) ? _ycrusher.Compress(vector2.y) : default(CompressedFloat));
				CompressedFloat cz2 = ((cache_zEnabled && (ia & IncludedAxes.Z) != 0) ? _zcrusher.Compress(vector2.z) : default(CompressedFloat));
				nonalloc.Set(this, cx2, cy2, cz2);
				break;
			}
			case TRSType.Position:
			{
				Vector3 vector;
				if (local)
				{
					Transform parent = rb.transform.parent;
					vector = (parent ? parent.InverseTransformPoint(rb.position) : rb.position);
				}
				else
				{
					vector = rb.position;
				}
				CompressedFloat cx = ((cache_xEnabled && (ia & IncludedAxes.X) != 0) ? _xcrusher.Compress(vector.x) : default(CompressedFloat));
				CompressedFloat cy = ((cache_yEnabled && (ia & IncludedAxes.Y) != 0) ? _ycrusher.Compress(vector.y) : default(CompressedFloat));
				CompressedFloat cz = ((cache_zEnabled && (ia & IncludedAxes.Z) != 0) ? _zcrusher.Compress(vector.z) : default(CompressedFloat));
				nonalloc.Set(this, cx, cy, cz);
				break;
			}
			default:
				nonalloc.Clear();
				break;
			}
		}

		public void Compress(CompressedElement nonalloc, Vector3 v, IncludedAxes ia = IncludedAxes.XYZ)
		{
			if (!cached)
			{
				CacheValues();
			}
			if (_trsType == TRSType.Quaternion)
			{
				Debug.LogError("We shouldn't be seeing this. Quats should not be getting compressed from Eulers!");
				if (cache_qEnabled)
				{
					nonalloc.Set(this, _qcrusher.Compress(Quaternion.Euler(v)));
				}
			}
			else if (_trsType == TRSType.Scale && uniformAxes != 0)
			{
				if (cache_uEnabled)
				{
					nonalloc.Set(this, _ucrusher.Compress((uniformAxes == UniformAxes.YZ) ? v.y : v.x));
				}
			}
			else
			{
				CompressedFloat cx = ((cache_xEnabled && (ia & IncludedAxes.X) != 0) ? _xcrusher.Compress(v.x) : default(CompressedFloat));
				CompressedFloat cy = ((cache_yEnabled && (ia & IncludedAxes.Y) != 0) ? _ycrusher.Compress(v.y) : default(CompressedFloat));
				CompressedFloat cz = ((cache_zEnabled && (ia & IncludedAxes.Z) != 0) ? _zcrusher.Compress(v.z) : default(CompressedFloat));
				nonalloc.Set(this, cx, cy, cz);
			}
		}

		public CompressedElement Compress(Vector3 v)
		{
			Compress(CompressedElement.reusable, v);
			return CompressedElement.reusable;
		}

		public void CompressAndWrite(Vector3 v, byte[] buffer, ref int bitposition, IncludedAxes ia = IncludedAxes.XYZ)
		{
			if (!cached)
			{
				CacheValues();
			}
			if (_trsType == TRSType.Scale && uniformAxes != 0)
			{
				ulong value = (cache_uEnabled ? ((ulong)_ucrusher.Compress((uniformAxes == UniformAxes.YZ) ? v.y : v.x)) : 0);
				buffer.Write(value, ref bitposition, cache_uBits[0]);
				return;
			}
			if (_trsType == TRSType.Quaternion)
			{
				Debug.Log("We shouldn't be seeing this. Quats should not be getting compressed from Eulers!");
				if (cache_qEnabled)
				{
					buffer.Write(_qcrusher.Compress(Quaternion.Euler(v)), ref bitposition, cache_qBits);
				}
				return;
			}
			if (cache_xEnabled)
			{
				buffer.Write(_xcrusher.Compress(v.x).cvalue, ref bitposition, cache_xBits[0]);
			}
			if (cache_yEnabled)
			{
				buffer.Write(_ycrusher.Compress(v.y).cvalue, ref bitposition, cache_yBits[0]);
			}
			if (cache_zEnabled)
			{
				buffer.Write(_zcrusher.Compress(v.z).cvalue, ref bitposition, cache_zBits[0]);
			}
		}

		public void Compress(CompressedElement nonalloc, Quaternion quat)
		{
			if (!cached)
			{
				CacheValues();
			}
			if (cache_qEnabled)
			{
				nonalloc.Set(this, _qcrusher.Compress(quat));
			}
		}

		public CompressedElement Compress(Quaternion quat)
		{
			Compress(CompressedElement.reusable, quat);
			return CompressedElement.reusable;
		}

		public void CompressAndWrite(Quaternion quat, byte[] buffer, ref int bitposition)
		{
			if (!cached)
			{
				CacheValues();
			}
			if (cache_qEnabled)
			{
				buffer.Write(_qcrusher.Compress(quat), ref bitposition, cache_qBits);
			}
		}

		public Element Decompress(CompressedElement compressed)
		{
			if (!cached)
			{
				CacheValues();
			}
			if (_trsType == TRSType.Scale && uniformAxes != 0)
			{
				float num = _ucrusher.Decompress(compressed.cUniform);
				return new Vector3(num, num, num);
			}
			if (_trsType == TRSType.Quaternion)
			{
				return _qcrusher.Decompress(compressed.cQuat);
			}
			return new Vector3(cache_xEnabled ? _xcrusher.Decompress(compressed.cx) : 0f, cache_yEnabled ? _ycrusher.Decompress(compressed.cy) : 0f, cache_zEnabled ? _zcrusher.Decompress(compressed.cz) : 0f);
		}

		public Element Decompress(ulong cval, IncludedAxes ia = IncludedAxes.XYZ)
		{
			if (!cached)
			{
				CacheValues();
			}
			if (_trsType == TRSType.Scale && uniformAxes != 0)
			{
				float num = _ucrusher.Decompress((uint)cval);
				return new Vector3(num, num, num);
			}
			if (_trsType == TRSType.Quaternion)
			{
				return _qcrusher.Decompress(cval);
			}
			int bitposition = 0;
			return new Vector3((cache_xEnabled && (ia & IncludedAxes.X) != 0) ? _xcrusher.ReadAndDecompress(cval, ref bitposition) : 0f, (cache_yEnabled && (ia & IncludedAxes.Y) != 0) ? _ycrusher.ReadAndDecompress(cval, ref bitposition) : 0f, (cache_zEnabled && (ia & IncludedAxes.Z) != 0) ? _zcrusher.ReadAndDecompress(cval, ref bitposition) : 0f);
		}

		[Obsolete("Apply for Rigidbody has been replaced with Move and Set, to indicate usage of MovePosition/Rotation vs rb.position/rotation.")]
		public void Apply(Rigidbody rb, CompressedElement ce, IncludedAxes ia = IncludedAxes.XYZ)
		{
			Apply(rb, Decompress(ce), ia);
		}

		[Obsolete("Apply for Rigidbody has been replaced with Move and Set, to indicate usage of MovePosition/Rotation vs rb.position/rotation.")]
		public void Apply(Rigidbody rb, Element e, IncludedAxes ia = IncludedAxes.XYZ)
		{
			if (!cached)
			{
				CacheValues();
			}
			switch (_trsType)
			{
			case TRSType.Quaternion:
				if (cache_qEnabled)
				{
					if (local && (bool)rb.transform.parent)
					{
						rb.transform.localRotation = e.quat;
					}
					else
					{
						rb.MoveRotation(e.quat);
					}
				}
				break;
			case TRSType.Position:
			{
				Vector3 vector = ((local & (bool)rb.transform.parent) ? rb.transform.TransformPoint(e.v) : e.v);
				Vector3 position = rb.position;
				rb.MovePosition(new Vector3((cache_xEnabled && (ia & IncludedAxes.X) != 0) ? vector.x : position.x, (cache_yEnabled && (ia & IncludedAxes.Y) != 0) ? vector.y : position.y, (cache_zEnabled && (ia & IncludedAxes.Z) != 0) ? vector.z : position.z));
				break;
			}
			case TRSType.Euler:
			{
				Vector3 eulerAngles = rb.rotation.eulerAngles;
				if (local && (bool)rb.transform.parent)
				{
					rb.transform.eulerAngles = new Vector3((cache_xEnabled && (ia & IncludedAxes.X) != 0) ? e.v.x : eulerAngles.x, (cache_yEnabled && (ia & IncludedAxes.Y) != 0) ? e.v.y : eulerAngles.y, (cache_zEnabled && (ia & IncludedAxes.Z) != 0) ? e.v.z : eulerAngles.z);
				}
				else
				{
					rb.MoveRotation(Quaternion.Euler((cache_xEnabled && (ia & IncludedAxes.X) != 0) ? e.v.x : eulerAngles.x, (cache_yEnabled && (ia & IncludedAxes.Y) != 0) ? e.v.y : eulerAngles.y, (cache_zEnabled && (ia & IncludedAxes.Z) != 0) ? e.v.z : eulerAngles.z));
				}
				break;
			}
			default:
				Debug.LogError("Are you trying to Apply scale to a Rigidbody?");
				break;
			}
		}

		public void Move(Rigidbody rb, CompressedElement ce, IncludedAxes ia = IncludedAxes.XYZ)
		{
			Move(rb, Decompress(ce), ia);
		}

		public void Move(Rigidbody rb, Element e, IncludedAxes ia = IncludedAxes.XYZ)
		{
			if (!cached)
			{
				CacheValues();
			}
			switch (_trsType)
			{
			case TRSType.Quaternion:
				if (cache_qEnabled)
				{
					if (local && (bool)rb.transform.parent)
					{
						rb.MoveRotation(rb.transform.parent.rotation * e.quat);
					}
					else
					{
						rb.MoveRotation(e.quat);
					}
				}
				break;
			case TRSType.Position:
			{
				Vector3 vector = ((local & (bool)rb.transform.parent) ? rb.transform.TransformPoint(e.v) : e.v);
				rb.MovePosition(new Vector3((cache_xEnabled && (ia & IncludedAxes.X) != 0) ? vector.x : rb.position.x, (cache_yEnabled && (ia & IncludedAxes.Y) != 0) ? vector.y : rb.position.y, (cache_zEnabled && (ia & IncludedAxes.Z) != 0) ? vector.z : rb.position.z));
				break;
			}
			case TRSType.Euler:
			{
				Vector3 eulerAngles = rb.rotation.eulerAngles;
				if (local && (bool)rb.transform.parent)
				{
					rb.transform.eulerAngles = new Vector3((cache_xEnabled && (ia & IncludedAxes.X) != 0) ? e.v.x : eulerAngles.x, (cache_yEnabled && (ia & IncludedAxes.Y) != 0) ? e.v.y : eulerAngles.y, (cache_zEnabled && (ia & IncludedAxes.Z) != 0) ? e.v.z : eulerAngles.z);
				}
				else
				{
					rb.MoveRotation(Quaternion.Euler((cache_xEnabled && (ia & IncludedAxes.X) != 0) ? e.v.x : eulerAngles.x, (cache_yEnabled && (ia & IncludedAxes.Y) != 0) ? e.v.y : eulerAngles.y, (cache_zEnabled && (ia & IncludedAxes.Z) != 0) ? e.v.z : eulerAngles.z));
				}
				break;
			}
			default:
				Debug.LogError("Are you trying to Apply scale to a Rigidbody?");
				break;
			}
		}

		public void Set(Rigidbody rb, CompressedElement ce, IncludedAxes ia = IncludedAxes.XYZ)
		{
			Set(rb, Decompress(ce), ia);
		}

		public void Set(Rigidbody2D rb, CompressedElement ce, IncludedAxes ia = IncludedAxes.XYZ)
		{
			Set(rb, Decompress(ce), ia);
		}

		public void Set(Rigidbody rb, Element e, IncludedAxes ia = IncludedAxes.XYZ)
		{
			if (!cached)
			{
				CacheValues();
			}
			switch (_trsType)
			{
			case TRSType.Quaternion:
				if (cache_qEnabled)
				{
					if (local && (bool)rb.transform.parent)
					{
						rb.rotation = rb.transform.parent.rotation * e.quat;
					}
					else
					{
						rb.rotation = e.quat;
					}
				}
				break;
			case TRSType.Position:
			{
				Vector3 vector = ((local & (bool)rb.transform.parent) ? rb.transform.TransformPoint(e.v) : e.v);
				Vector3 position = rb.position;
				rb.position = new Vector3((cache_xEnabled && (ia & IncludedAxes.X) != 0) ? vector.x : position.x, (cache_yEnabled && (ia & IncludedAxes.Y) != 0) ? vector.y : position.y, (cache_zEnabled && (ia & IncludedAxes.Z) != 0) ? vector.z : position.z);
				break;
			}
			case TRSType.Euler:
			{
				Vector3 eulerAngles = rb.rotation.eulerAngles;
				if (local && (bool)rb.transform.parent)
				{
					rb.transform.eulerAngles = new Vector3((cache_xEnabled && (ia & IncludedAxes.X) != 0) ? e.v.x : eulerAngles.x, (cache_yEnabled && (ia & IncludedAxes.Y) != 0) ? e.v.y : eulerAngles.y, (cache_zEnabled && (ia & IncludedAxes.Z) != 0) ? e.v.z : eulerAngles.z);
				}
				else
				{
					rb.rotation = Quaternion.Euler((cache_xEnabled && (ia & IncludedAxes.X) != 0) ? e.v.x : eulerAngles.x, (cache_yEnabled && (ia & IncludedAxes.Y) != 0) ? e.v.y : eulerAngles.y, (cache_zEnabled && (ia & IncludedAxes.Z) != 0) ? e.v.z : eulerAngles.z);
				}
				break;
			}
			default:
				Debug.LogError("Are you trying to Apply scale to a Rigidbody?");
				break;
			}
		}

		public void Set(Rigidbody2D rb2d, Element e, IncludedAxes ia = IncludedAxes.XYZ)
		{
			if (!cached)
			{
				CacheValues();
			}
			switch (_trsType)
			{
			case TRSType.Quaternion:
				if (cache_qEnabled)
				{
					if (local && (bool)rb2d.transform.parent)
					{
						rb2d.transform.localRotation = e.quat;
					}
					else
					{
						rb2d.rotation = e.quat.z;
					}
				}
				break;
			case TRSType.Position:
			{
				Vector3 vector = ((local & (bool)rb2d.transform.parent) ? rb2d.transform.TransformPoint(e.v) : e.v);
				rb2d.position = new Vector2((cache_xEnabled && (ia & IncludedAxes.X) != 0) ? vector.x : rb2d.position.x, (cache_yEnabled && (ia & IncludedAxes.Y) != 0) ? vector.y : rb2d.position.y);
				break;
			}
			case TRSType.Euler:
				if (local && (bool)rb2d.transform.parent)
				{
					rb2d.transform.localEulerAngles = new Vector3(0f, 0f, (cache_zEnabled && (ia & IncludedAxes.Z) != 0) ? e.v.z : rb2d.rotation);
				}
				else
				{
					rb2d.rotation = ((cache_zEnabled && (ia & IncludedAxes.Z) != 0) ? e.v.z : rb2d.rotation);
				}
				break;
			default:
				Debug.LogError("Are you trying to Apply scale to a Rigidbody?");
				break;
			}
		}

		public void Apply(Transform trans, CompressedElement ce, IncludedAxes ia = IncludedAxes.XYZ)
		{
			Apply(trans, Decompress(ce), ia);
		}

		public void Apply(Transform trans, Element e, IncludedAxes ia = IncludedAxes.XYZ)
		{
			if (!cached)
			{
				CacheValues();
			}
			switch (_trsType)
			{
			case TRSType.Quaternion:
				if (cache_qEnabled)
				{
					if (local)
					{
						trans.localRotation = e.quat;
					}
					else
					{
						trans.rotation = e.quat;
					}
				}
				return;
			case TRSType.Position:
				if (local)
				{
					trans.localPosition = new Vector3((cache_xEnabled && (ia & IncludedAxes.X) != 0) ? e.v.x : trans.localPosition.x, (cache_yEnabled && (ia & IncludedAxes.Y) != 0) ? e.v.y : trans.localPosition.y, (cache_zEnabled && (ia & IncludedAxes.Z) != 0) ? e.v.z : trans.localPosition.z);
				}
				else
				{
					trans.position = new Vector3((cache_xEnabled && (ia & IncludedAxes.X) != 0) ? e.v.x : trans.position.x, (cache_yEnabled && (ia & IncludedAxes.Y) != 0) ? e.v.y : trans.position.y, (cache_zEnabled && (ia & IncludedAxes.Z) != 0) ? e.v.z : trans.position.z);
				}
				return;
			case TRSType.Euler:
				if (local)
				{
					trans.localEulerAngles = new Vector3((cache_xEnabled && (ia & IncludedAxes.X) != 0) ? e.v.x : trans.localEulerAngles.x, (cache_yEnabled && (ia & IncludedAxes.Y) != 0) ? e.v.y : trans.localEulerAngles.y, (cache_zEnabled && (ia & IncludedAxes.Z) != 0) ? e.v.z : trans.localEulerAngles.z);
				}
				else
				{
					trans.eulerAngles = new Vector3((cache_xEnabled && (ia & IncludedAxes.X) != 0) ? e.v.x : trans.eulerAngles.x, (cache_yEnabled && (ia & IncludedAxes.Y) != 0) ? e.v.y : trans.eulerAngles.y, (cache_zEnabled && (ia & IncludedAxes.Z) != 0) ? e.v.z : trans.eulerAngles.z);
				}
				return;
			}
			if (local)
			{
				if (uniformAxes == UniformAxes.NonUniform)
				{
					trans.localScale = new Vector3((cache_xEnabled && (ia & IncludedAxes.X) != 0) ? e.v.x : trans.localScale.x, (cache_yEnabled && (ia & IncludedAxes.Y) != 0) ? e.v.y : trans.localScale.y, (cache_zEnabled && (ia & IncludedAxes.Z) != 0) ? e.v.z : trans.localScale.z);
					return;
				}
				float num = (((uniformAxes & (UniformAxes)1) != 0) ? e.v.x : e.v.y);
				trans.localScale = new Vector3(((uniformAxes & (UniformAxes)1) != 0) ? num : trans.localScale.x, ((uniformAxes & (UniformAxes)2) != 0) ? num : trans.localScale.y, ((uniformAxes & (UniformAxes)4) != 0) ? num : trans.localScale.z);
			}
			else if (uniformAxes == UniformAxes.NonUniform)
			{
				trans.localScale = new Vector3((cache_xEnabled && (ia & IncludedAxes.X) != 0) ? e.v.x : trans.lossyScale.x, (cache_yEnabled && (ia & IncludedAxes.Y) != 0) ? e.v.y : trans.lossyScale.y, (cache_zEnabled && (ia & IncludedAxes.Z) != 0) ? e.v.z : trans.lossyScale.z);
			}
			else
			{
				float num2 = (((uniformAxes & (UniformAxes)1) != 0) ? e.v.x : e.v.y);
				trans.localScale = new Vector3(((uniformAxes & (UniformAxes)1) != 0) ? num2 : trans.lossyScale.x, ((uniformAxes & (UniformAxes)2) != 0) ? num2 : trans.lossyScale.y, ((uniformAxes & (UniformAxes)4) != 0) ? num2 : trans.lossyScale.z);
			}
		}

		public Vector3 Clamp(Vector3 v3)
		{
			if (!cached)
			{
				CacheValues();
			}
			if (TRSType == TRSType.Quaternion)
			{
				return v3;
			}
			if (TRSType == TRSType.Scale)
			{
				if (uniformAxes == UniformAxes.NonUniform)
				{
					return new Vector3(cache_xEnabled ? _xcrusher.Clamp(v3.x) : 0f, cache_yEnabled ? _ycrusher.Clamp(v3.y) : 0f, cache_zEnabled ? _zcrusher.Clamp(v3.z) : 0f);
				}
				return new Vector3(((uniformAxes & (UniformAxes)1) != 0) ? _ucrusher.Clamp(v3.x) : 0f, ((uniformAxes & (UniformAxes)2) != 0) ? _ucrusher.Clamp(v3.x) : 0f, ((uniformAxes & (UniformAxes)4) != 0) ? _ucrusher.Clamp(v3.x) : 0f);
			}
			if (TRSType == TRSType.Euler)
			{
				return new Vector3(cache_xEnabled ? _xcrusher.ClampRotation(v3.x) : 0f, cache_yEnabled ? _ycrusher.ClampRotation(v3.y) : 0f, cache_zEnabled ? _zcrusher.ClampRotation(v3.z) : 0f);
			}
			return new Vector3(cache_xEnabled ? _xcrusher.Clamp(v3.x) : 0f, cache_yEnabled ? _ycrusher.Clamp(v3.y) : 0f, cache_zEnabled ? _zcrusher.Clamp(v3.z) : 0f);
		}

		public BitCullingLevel FindBestBitCullLevel(CompressedElement a, CompressedElement b, BitCullingLevel maxCulling)
		{
			if (TRSType == TRSType.Quaternion)
			{
				if ((ulong)a.cQuat == (ulong)b.cQuat)
				{
					return BitCullingLevel.DropAll;
				}
				return BitCullingLevel.NoCulling;
			}
			if (maxCulling == BitCullingLevel.NoCulling || !TestMatchingUpper(a, b, BitCullingLevel.DropThird))
			{
				return BitCullingLevel.NoCulling;
			}
			if (maxCulling == BitCullingLevel.DropThird || !TestMatchingUpper(a, b, BitCullingLevel.DropHalf))
			{
				return BitCullingLevel.DropThird;
			}
			if (maxCulling == BitCullingLevel.DropHalf || !TestMatchingUpper(a, b, BitCullingLevel.DropAll))
			{
				return BitCullingLevel.DropHalf;
			}
			return BitCullingLevel.DropAll;
		}

		private bool TestMatchingUpper(uint a, uint b, int lowerbits)
		{
			return a >> lowerbits << lowerbits == b >> lowerbits << lowerbits;
		}

		public bool TestMatchingUpper(CompressedElement a, CompressedElement b, BitCullingLevel bcl)
		{
			if (TestMatchingUpper(a.cx, b.cx, _xcrusher.GetBits(bcl)) && TestMatchingUpper(a.cy, b.cy, _ycrusher.GetBits(bcl)))
			{
				return TestMatchingUpper(a.cz, b.cz, _zcrusher.GetBits(bcl));
			}
			return false;
		}

		public int TallyBits(BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			if (_trsType == TRSType.Scale && uniformAxes != 0)
			{
				if (_ucrusher == null || !_ucrusher.Enabled)
				{
					return 0;
				}
				return _ucrusher.GetBits(bcl);
			}
			if (_trsType == TRSType.Quaternion)
			{
				if (!(_qcrusher != null) || !_qcrusher.Enabled)
				{
					return 0;
				}
				return _qcrusher.Bits;
			}
			if (_trsType == TRSType.Position && useWorldBounds)
			{
				return WorldBoundsSettings.TallyBits(ref boundsGroupId);
			}
			return ((_xcrusher != null && _xcrusher.Enabled) ? _xcrusher.GetBits(bcl) : 0) + ((_ycrusher != null && _ycrusher.Enabled) ? _ycrusher.GetBits(bcl) : 0) + ((_zcrusher != null && _zcrusher.Enabled) ? _zcrusher.GetBits(bcl) : 0);
		}

		public void CopyFrom(ElementCrusher src)
		{
			_trsType = src._trsType;
			uniformAxes = src.uniformAxes;
			if (_xcrusher != null && src._xcrusher != null)
			{
				_xcrusher.CopyFrom(src._xcrusher);
			}
			if (_ycrusher != null && src._ycrusher != null)
			{
				_ycrusher.CopyFrom(src._ycrusher);
			}
			if (_zcrusher != null && src._zcrusher != null)
			{
				_zcrusher.CopyFrom(src._zcrusher);
			}
			if (_ucrusher != null && src._ucrusher != null)
			{
				_ucrusher.CopyFrom(src._ucrusher);
			}
			if (_qcrusher != null && src._qcrusher != null)
			{
				_qcrusher.CopyFrom(src._qcrusher);
			}
			local = src.local;
		}

		public override string ToString()
		{
			return string.Concat("ElementCrusher [", _trsType, "] ");
		}

		public override bool Equals(object obj)
		{
			return Equals(obj as ElementCrusher);
		}

		public bool Equals(ElementCrusher other)
		{
			if (other != null && _trsType == other._trsType && EqualityComparer<Transform>.Default.Equals(defaultTransform, other.defaultTransform) && uniformAxes == other.uniformAxes && ((_xcrusher == null) ? (other._xcrusher == null) : _xcrusher.Equals(other._xcrusher)) && ((_ycrusher == null) ? (other._ycrusher == null) : _ycrusher.Equals(other._ycrusher)) && ((_zcrusher == null) ? (other._zcrusher == null) : _zcrusher.Equals(other._zcrusher)) && ((_ucrusher == null) ? (other._ucrusher == null) : _ucrusher.Equals(other._ucrusher)) && ((_qcrusher == null) ? (other._qcrusher == null) : _qcrusher.Equals(other._qcrusher)))
			{
				return local == other.local;
			}
			return false;
		}

		// public override int GetHashCode()
		// {
		// 	return (((((((-1042106911 * -1521134295 + _trsType.GetHashCode()) * -1521134295 + uniformAxes.GetHashCode()) * -1521134295 + ((_xcrusher != null) ? _xcrusher.GetHashCode() : 0)) * -1521134295 + ((_ycrusher != null) ? _ycrusher.GetHashCode() : 0)) * -1521134295 + ((_zcrusher != null) ? _zcrusher.GetHashCode() : 0)) * -1521134295 + ((_ucrusher != null) ? _ucrusher.GetHashCode() : 0)) * -1521134295 + ((!(_qcrusher == null)) ? _qcrusher.GetHashCode() : 0)) * -1521134295 + local.GetHashCode();
		// }

		public static bool operator ==(ElementCrusher crusher1, ElementCrusher crusher2)
		{
			return EqualityComparer<ElementCrusher>.Default.Equals(crusher1, crusher2);
		}

		public static bool operator !=(ElementCrusher crusher1, ElementCrusher crusher2)
		{
			return !(crusher1 == crusher2);
		}
	}
}
