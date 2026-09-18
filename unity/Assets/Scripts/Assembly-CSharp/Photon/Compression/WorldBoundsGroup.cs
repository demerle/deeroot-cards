using System;
using System.Collections.Generic;
using UnityEngine;
using emotitron.Compression;

namespace Photon.Compression
{
	[Serializable]
	public class WorldBoundsGroup
	{
		public const string defaultName = "Default";

		public const string newAddName = "Unnamed";

		public string name = "Default";

		[NonSerialized]
		public Action OnWorldBoundChanged;

		public ElementCrusher crusher = GetUncompressedCrusher();

		[NonSerialized]
		public readonly List<WorldBounds> activeWorldBounds = new List<WorldBounds>();

		[NonSerialized]
		public Bounds _combinedWorldBounds;

		public int ActiveBoundsObjCount
		{
			get
			{
				return activeWorldBounds.Count;
			}
		}

		public static ElementCrusher GetUncompressedCrusher()
		{
			return new ElementCrusher(TRSType.Position, false)
			{
				enableLocalSelector = false,
				hideFieldName = true,
				XCrusher = new FloatCrusher
				{
					axis = Axis.X,
					outOfBoundsHandling = OutOfBoundsHandling.Clamp,
					Resolution = 100uL,
					BitsDeterminedBy = BitsDeterminedBy.Uncompressed
				},
				YCrusher = new FloatCrusher
				{
					axis = Axis.Y,
					outOfBoundsHandling = OutOfBoundsHandling.Clamp,
					Resolution = 100uL,
					BitsDeterminedBy = BitsDeterminedBy.Uncompressed
				},
				ZCrusher = new FloatCrusher
				{
					axis = Axis.Z,
					outOfBoundsHandling = OutOfBoundsHandling.Clamp,
					Resolution = 100uL,
					BitsDeterminedBy = BitsDeterminedBy.Uncompressed
				}
			};
		}

		public void ResetActiveBounds()
		{
			activeWorldBounds.Clear();
		}

		public void RecalculateWorldCombinedBounds()
		{
			FloatCrusher xCrusher = crusher.XCrusher;
			FloatCrusher yCrusher = crusher.YCrusher;
			FloatCrusher zCrusher = crusher.ZCrusher;
			if (activeWorldBounds.Count == 0)
			{
				_combinedWorldBounds = default(Bounds);
			}
			else
			{
				if (xCrusher.BitsDeterminedBy > BitsDeterminedBy.Disabled || xCrusher.BitsDeterminedBy == BitsDeterminedBy.SetBits || xCrusher.BitsDeterminedBy == BitsDeterminedBy.Uncompressed || xCrusher.BitsDeterminedBy == BitsDeterminedBy.HalfFloat)
				{
					xCrusher.Resolution = 100uL;
					xCrusher.BitsDeterminedBy = BitsDeterminedBy.Resolution;
				}
				if (yCrusher.BitsDeterminedBy > BitsDeterminedBy.Disabled || yCrusher.BitsDeterminedBy == BitsDeterminedBy.SetBits || yCrusher.BitsDeterminedBy == BitsDeterminedBy.Uncompressed || yCrusher.BitsDeterminedBy == BitsDeterminedBy.HalfFloat)
				{
					yCrusher.Resolution = 100uL;
					yCrusher.BitsDeterminedBy = BitsDeterminedBy.Resolution;
				}
				if (zCrusher.BitsDeterminedBy > BitsDeterminedBy.Disabled || zCrusher.BitsDeterminedBy == BitsDeterminedBy.SetBits || zCrusher.BitsDeterminedBy == BitsDeterminedBy.Uncompressed || zCrusher.BitsDeterminedBy == BitsDeterminedBy.HalfFloat)
				{
					zCrusher.Resolution = 100uL;
					zCrusher.BitsDeterminedBy = BitsDeterminedBy.Resolution;
				}
				_combinedWorldBounds = activeWorldBounds[0].myBounds;
				for (int i = 1; i < activeWorldBounds.Count; i++)
				{
					_combinedWorldBounds.Encapsulate(activeWorldBounds[i].myBounds);
				}
				crusher.Bounds = _combinedWorldBounds;
			}
			if (OnWorldBoundChanged != null)
			{
				OnWorldBoundChanged();
			}
		}
	}
}
