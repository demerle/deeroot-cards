using UnityEngine;

namespace ModdingUtils.MonoBehaviours
{
    public class HealthBasedEffect : ReversibleEffect
    {
        private bool active;
        private float percThreshMax;
        private float percThreshMin;
        private ColorEffect colorEffect;
        private Color color = Color.clear;

        public override void OnAwake()
        {
            SetLivesToEffect(int.MaxValue);
        }
        public override void OnStart()
        {
            applyImmediately = false;
            active = false;
        }
        public override void OnUpdate()
        {
            if (!active && HealthInRange())
            {
                ApplyColorEffect();
                ApplyModifiers();
                active = true;
            }
            else if (active && !HealthInRange())
            {
                if (colorEffect != null)
                {
                    UnityEngine.Object.Destroy(colorEffect);
                }
                ClearModifiers(false);
                active = false;
            }
        }
        private void ApplyColorEffect()
        {
            if (color != Color.clear)
            {
                colorEffect = player.gameObject.AddComponent<ColorEffect>();
                colorEffect.SetColor(color);
            }
        }
        private bool HealthInRange()
        {
            return (data.health <= data.maxHealth * percThreshMax && data.health >= data.maxHealth * percThreshMin);
        }
        public override void OnOnDisable()
        {
            // if the player is dead, clear the modifiers
            ClearModifiers(false);
            active = false;
        }
        public override void OnOnDestroy()
        {
        }
        public void SetPercThresholdMax(float perc)
        {
            percThreshMax = perc;
        }
        public void SetPercThresholdMin(float perc)
        {
            percThreshMin = perc;
        }
        public void SetColor(Color color)
        {
            this.color = color;
        }
    }

}