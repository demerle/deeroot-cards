using System;
using Photon.Utilities;
using UnityEngine;

namespace Photon.Compression;

[ExecuteInEditMode]
public class WorldBounds : MonoBehaviour
{
	[SerializeField]
	[HideInInspector]
	private Bounds manualBounds = new Bounds(new Vector3(0f, 0f, 0f), new Vector3(600f, 40f, 600f));

	[Tooltip("Selects which WorldBounds group this object should be factored into.")]
	[WorldBoundsSelectAttribute]
	[SerializeField]
	[HideInInspector]
	public int worldBoundsGrp;

	[SerializeField]
	[HideInInspector]
	private bool includeChildren = true;

	[Tooltip("Awake/Destroy will consider element into the world size as long as it exists in the scene (You may need to wake it though). Enable/Disable only factors it in if it is active.")]
	[SerializeField]
	[HideInInspector]
	private BoundsTools.BoundsType factorIn;

	[HideInInspector]
	public Bounds myBounds;

	[HideInInspector]
	public int myBoundsCount;

	public Action OnWorldBoundsChange;

	public Bounds ManualBounds
	{
		get
		{
			return manualBounds;
		}
		set
		{
			manualBounds = value;
			CollectMyBounds();
		}
	}

	public BoundsTools.BoundsType FactorIn
	{
		get
		{
			return factorIn;
		}
		set
		{
			factorIn = value;
			CollectMyBounds();
		}
	}

	private void Awake()
	{
		CollectMyBounds();
	}

	public void CollectMyBounds()
	{
		WorldBoundsSettings single = SettingsScriptableObject<WorldBoundsSettings>.Single;
		if ((bool)single)
		{
			if (SettingsScriptableObject<WorldBoundsSettings>.Single.worldBoundsGroups.Count == 0)
			{
				SettingsScriptableObject<WorldBoundsSettings>.Single.worldBoundsGroups.Add(new WorldBoundsGroup());
			}
			if (worldBoundsGrp >= SettingsScriptableObject<WorldBoundsSettings>.Single.worldBoundsGroups.Count)
			{
				worldBoundsGrp = 0;
			}
			WorldBoundsGroup worldBoundsGroup = single.worldBoundsGroups[worldBoundsGrp];
			if (factorIn == BoundsTools.BoundsType.Manual)
			{
				myBounds = manualBounds;
				myBoundsCount = 1;
			}
			else
			{
				myBounds = base.gameObject.CollectMyBounds(factorIn, out myBoundsCount, includeChildren);
			}
			WorldBoundsSettings.RemoveWorldBoundsFromAll(this);
			if (myBoundsCount > 0 && base.enabled && !worldBoundsGroup.activeWorldBounds.Contains(this))
			{
				worldBoundsGroup.activeWorldBounds.Add(this);
				worldBoundsGroup.RecalculateWorldCombinedBounds();
			}
			if (OnWorldBoundsChange != null)
			{
				OnWorldBoundsChange();
			}
		}
	}

	private void Start()
	{
	}

	private void OnEnable()
	{
		FactorInBounds(b: true);
	}

	private void OnDisable()
	{
		FactorInBounds(b: false);
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.magenta;
		Gizmos.DrawWireCube(SettingsScriptableObject<WorldBoundsSettings>.Single.worldBoundsGroups[worldBoundsGrp]._combinedWorldBounds.center, SettingsScriptableObject<WorldBoundsSettings>.Single.worldBoundsGroups[worldBoundsGrp]._combinedWorldBounds.size);
	}

	public void FactorInBounds(bool b)
	{
		if (this == null)
		{
			return;
		}
		if (worldBoundsGrp >= SettingsScriptableObject<WorldBoundsSettings>.Single.worldBoundsGroups.Count)
		{
			worldBoundsGrp = 0;
		}
		WorldBoundsGroup worldBoundsGroup = SettingsScriptableObject<WorldBoundsSettings>.Single.worldBoundsGroups[worldBoundsGrp];
		if (b)
		{
			if (!worldBoundsGroup.activeWorldBounds.Contains(this))
			{
				worldBoundsGroup.activeWorldBounds.Add(this);
			}
		}
		else
		{
			worldBoundsGroup.activeWorldBounds.Remove(this);
		}
		worldBoundsGroup.RecalculateWorldCombinedBounds();
	}
}
