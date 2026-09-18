using System.Collections.Generic;
using ModdingUtils.Extensions;
using UnboundLib;
using UnityEngine;

namespace ModdingUtils.MonoBehaviours
{
	public class ReversibleColorEffect : ColorEffect
	{
		private int livesToEffect = 1;
		private int livesEffected;

		public void OnEnable()
		{
			if (livesEffected >= livesToEffect)
			{
				Destroy(this);
			}
		}
		public void OnDisable()
        {
			livesEffected++;

			if (livesEffected >= livesToEffect)
			{
				Destroy(this);
			}
		}
		public void SetLivesToEffect(int lives = 1)
		{
			livesToEffect = lives;
		}
	}
	public class ColorEffect : MonoBehaviour
    {
		internal ColorEffectBase colorEffectBase;
		private SetTeamColor[] teamColors;
		private Player player;
		private Color colorMinToSet;
		private Color colorMaxToSet;

		public void Awake()
        {
			player = gameObject.GetComponent<Player>();

			// create the base only if it doesn't already exist, to prevent accidentally running Awake again
			colorEffectBase = player.gameObject.GetOrAddComponent<ColorEffectBase>();
			colorEffectBase.colorEffectDrones.Add(this);

			teamColors = player.transform.root.GetComponentsInChildren<SetTeamColor>();
		}
		public void Start()
		{
			ApplyColor();
		}

		public void Update()
		{
		}
		public void OnDestroy()
		{
			// tell the base that the color effect is over
			colorEffectBase.OnDroneDestroy(this);

		}
		public void ApplyColor()
		{
			if (player.gameObject.GetComponentInChildren<PlayerSkinHandler>().simpleSkin)
			{
				SpriteMask[] sprites = player.gameObject.GetComponentInChildren<SetPlayerSpriteLayer>().transform.root.GetComponentsInChildren<SpriteMask>();
				for (int i = 0; i < sprites.Length; i++)
                {
					sprites[i].GetComponent<SpriteRenderer>().color = colorMaxToSet;
                }

				return;
			}

			PlayerSkinParticle[] componentsInChildren2 = player.gameObject.GetComponentsInChildren<PlayerSkinParticle>();
			for (int j = 0; j < componentsInChildren2.Length; j++)
			{
				ParticleSystem particleSystem2 = (ParticleSystem)componentsInChildren2[j].GetFieldValue("part");
				ParticleSystem.MainModule main2 = particleSystem2.main;
				ParticleSystem.MinMaxGradient startColor2 = particleSystem2.main.startColor;
				startColor2.colorMin = colorMinToSet;
				startColor2.colorMax = colorMaxToSet;
				main2.startColor = startColor2;
			}
			SetTeamColor[] teamColors = this.teamColors;
			for (int j = 0; j < teamColors.Length; j++)
			{
				teamColors[j].Set(new PlayerSkin
				{
					color = colorMaxToSet,
					backgroundColor = colorMaxToSet,
					winText = colorMaxToSet,
					particleEffect = colorMaxToSet
				});
			}
		}
		public void SetColor(Color color)
        {
			colorMaxToSet = color;
			colorMinToSet = color;
        }
		public void SetColorMax(Color color)
		{
			colorMaxToSet = color;
		}
		public void SetColorMin(Color color)
		{
			colorMinToSet = color;
		}
		public void Destroy()
        {
			UnityEngine.GameObject.Destroy(this);
        }
		public Color GetOriginalColorMax()
        {
			return colorEffectBase.originalColorMax;
        }
		public Color GetOriginalColorMin()
		{
			return colorEffectBase.originalColorMin;
		}
	}
	internal sealed class ColorEffectBase : MonoBehaviour
    {

		internal List<ColorEffect> colorEffectDrones = new List<ColorEffect>();
		internal Color originalColorMax;
		internal Color originalColorMin;
		private Player player;
		private SetTeamColor[] teamColors;

		public void Awake()
        {
			player = gameObject.GetComponent<Player>();
			// get original color
			originalColorMin = GetPlayerColor.GetColorMin(player);
			originalColorMax = GetPlayerColor.GetColorMax(player);

			teamColors = player.transform.root.GetComponentsInChildren<SetTeamColor>();
		}
		public void OnDroneDestroy(ColorEffect colorEffect)
        {
			int idx = colorEffectDrones.IndexOf(colorEffect);
			// if it was the only drone left, then reapply the original colors
			if (colorEffectDrones.Count == 1 && idx == 0)
            {
				ResetColor();
            }
			// if it was the last drone in the list, then reapply the previous color
			else if (idx == colorEffectDrones.Count-1)
            {
				colorEffectDrones[idx - 1].ApplyColor();
            }
			// if it was in the middle of the list, do nothing
			else
            {

            }
			// then remove it from the list
			colorEffectDrones.Remove(colorEffect);

        }
		private void ResetColor()
		{
			PlayerSkinParticle[] componentsInChildren2 = player.gameObject.GetComponentsInChildren<PlayerSkinParticle>();
			for (int j = 0; j < componentsInChildren2.Length; j++)
			{
				ParticleSystem particleSystem2 = (ParticleSystem)componentsInChildren2[j].GetFieldValue("part");
				ParticleSystem.MainModule main2 = particleSystem2.main;
				ParticleSystem.MinMaxGradient startColor2 = particleSystem2.main.startColor;
				startColor2.colorMin = originalColorMin;
				startColor2.colorMax = originalColorMax;
				main2.startColor = startColor2;
			}
			SetTeamColor[] teamColors = this.teamColors;
			for (int j = 0; j < teamColors.Length; j++)
			{
				teamColors[j].Set(new PlayerSkin
				{
					color = originalColorMax,
					backgroundColor = originalColorMax,
					winText = originalColorMax,
					particleEffect = originalColorMax
				});
			}
		}
		public void OnDestroy()
		{
			foreach (ColorEffect colorEffect in colorEffectDrones)
			{
				if (colorEffect != null) { Destroy(colorEffect); }
			}
			ResetColor();
		}
		public void Destroy()
		{
			UnityEngine.GameObject.Destroy(this);
		}

	}
	public class GunColorEffect : MonoBehaviour
	{
		private GunColorEffectBase gunColorEffectBase;
		private Player player;
		private Gun gun;
		private Color colorToSet;

		public void Awake()
		{
			player = gameObject.GetComponent<Player>();
			gun = player.GetComponent<Holding>().holdable.GetComponent<Gun>();
			// create the base only if it doesn't already exist, to prevent accidentally running Awake again
			gunColorEffectBase = player.gameObject.GetOrAddComponent<GunColorEffectBase>();
			gunColorEffectBase.gunColorEffectDrones.Add(this);
		}
		void Start()
		{
			ApplyColor();
		}

		void Update()
		{
		}
		public void OnDestroy()
		{
			// tell the base that the color effect is over
			gunColorEffectBase.OnDroneDestroy(this);

		}
		public void ApplyColor()
		{
			gun.projectileColor = colorToSet;
		}
		public void SetColor(Color color)
		{
			colorToSet = color;
		}
		public void Destroy()
		{
			UnityEngine.GameObject.Destroy(this);
		}
	}
	internal sealed class GunColorEffectBase : MonoBehaviour
	{

		internal List<GunColorEffect> gunColorEffectDrones = new List<GunColorEffect>();
		private Color originalColor;
		private Player player;
		private Gun gun;

		public void Awake()
		{
			player = gameObject.GetComponent<Player>();
			gun = player.GetComponent<Holding>().holdable.GetComponent<Gun>();
			// get original color
			originalColor = gun.projectileColor;
		}
		public void OnDroneDestroy(GunColorEffect gunColorEffect)
		{
			int idx = gunColorEffectDrones.IndexOf(gunColorEffect);
			// if it was the only drone left, then reapply the original colors
			if (gunColorEffectDrones.Count == 1 && idx == 0)
			{
				ResetColor();
			}
			// if it was the last drone in the list, then reapply the previous color
			else if (idx == gunColorEffectDrones.Count - 1)
			{
				gunColorEffectDrones[idx - 1].ApplyColor();
			}
			// if it was in the middle of the list, do nothing
			else
			{

			}
			// then remove it from the list
			gunColorEffectDrones.Remove(gunColorEffect);

		}
		private void ResetColor()
		{
			gun.projectileColor = originalColor;
		}

		public void OnDestroy()
        {
			foreach (GunColorEffect gunColorEffect in gunColorEffectDrones)
            {
				if(gunColorEffect != null) { Destroy(gunColorEffect); }
            }
			ResetColor();
        }
		public void Destroy()
		{
			UnityEngine.GameObject.Destroy(this);
		}

	}
}
