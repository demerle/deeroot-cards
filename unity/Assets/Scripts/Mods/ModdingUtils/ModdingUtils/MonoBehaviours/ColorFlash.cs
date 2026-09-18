using UnityEngine;

namespace ModdingUtils.MonoBehaviours
{
	public class ColorFlash : MonoBehaviour
	{
		private float duration = 1f;
		private float startTime;
		private int numberOfFlashes = 1;
		private float delayBetweenFlashes = 1f;
		private Color colorMinToFlash = Color.black;
		private Color colorMaxToFlash = Color.black;
		private int flashNum;
		private bool flashing;
		private ColorEffect colorEffect;

		private Player player;

		void Awake()
		{
			player = gameObject.GetComponent<Player>();
		}

		void Start()
		{
			ResetTimer();

			Flash(colorMinToFlash, colorMaxToFlash);
		}

		void Update()
		{
			if (flashing && Time.time >= startTime + duration)
			{
				Unflash();
			}
			else if (!flashing && Time.time >= startTime + delayBetweenFlashes)
			{
				Flash(colorMinToFlash, colorMaxToFlash);
			}
			else if (flashNum >= numberOfFlashes)
			{
				Destroy();
			}
		}
		public void OnDestroy()
		{
			if (colorEffect != null)
			{
				Destroy(colorEffect);
			}
		}
		public void Flash(Color colorMin, Color colorMax)
		{
			flashing = true;
			ResetTimer();
			colorEffect = player.gameObject.AddComponent<ColorEffect>();
			colorEffect.SetColorMax(colorMax);
			colorEffect.SetColorMin(colorMin);

		}
		public void Unflash()
		{
			flashing = false;
			flashNum++;
			ResetTimer();
			if (colorEffect != null) { Destroy(colorEffect); }
		}
		public void ResetTimer()
		{
			startTime = Time.time;
		}
		public void Destroy()
		{
			UnityEngine.Object.Destroy(this);
		}
		public void SetNumberOfFlashes(int flashes)
		{
			numberOfFlashes = flashes;
		}
		public void SetDelayBetweenFlashes(float delay)
		{
			delayBetweenFlashes = delay;
		}
		public void SetColor(Color color)
        {
			colorMaxToFlash = color;
			colorMinToFlash = color;
        }
		public void SetColorMax(Color color)
		{
			colorMaxToFlash = color;
		}
		public void SetColorMin(Color color)
		{
			colorMinToFlash = color;
		}
		public void SetDuration(float duration)
		{
			this.duration = duration;
		}
		public Color GetOriginalColorMax()
		{
			return colorEffect.colorEffectBase.originalColorMax;
		}
		public Color GetOriginalColorMin()
		{
			return colorEffect.colorEffectBase.originalColorMin;
		}

	}
}