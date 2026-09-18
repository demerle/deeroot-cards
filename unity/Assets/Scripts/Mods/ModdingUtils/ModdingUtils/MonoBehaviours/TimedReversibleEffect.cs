namespace ModdingUtils.MonoBehaviours
{
    public class TimedReversibleEffect : ReversibleEffect
    {
        public float duration;

        public override void OnUpdate()
        {
            duration -= TimeHandler.deltaTime;
            if (duration <= 0f)
            {
                this.Destroy();
            }
        }
    }
}
