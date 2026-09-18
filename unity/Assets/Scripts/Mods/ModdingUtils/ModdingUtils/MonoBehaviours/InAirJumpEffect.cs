namespace ModdingUtils.MonoBehaviours
{
    public class InAirJumpEffect : ReversibleEffect
    {
        private float
            interval,
            jump_mult = 1f;
        private float jumps;
        private float costPerJump;
        private float currentjumps;
        private bool continuous_trigger;
        private bool resetOnWallGrab = true;

        private readonly float minTimeFromGround = 0.1f; // minimum amount of time off the ground before this will engage

        public override void OnStart()
        {
            SetLivesToEffect(int.MaxValue);
        }
        public override void OnUpdate()
        {
            if (data?.playerActions?.Jump is null) { return; }
            // reset if the player is on the ground
            if (data.isGrounded)
            {
                currentjumps = jumps;
                return;
            }
            // reset on wallgrab if desired
            else if (data.isWallGrab && resetOnWallGrab)
            {
                currentjumps = jumps;
                return;
            }
            // do not engage unless the player is out of normal jumps, and a bunch of other conditions are met
            else if (data.currentJumps <= 0 && currentjumps > 0f && data.sinceJump >= interval && data.sinceGrounded > minTimeFromGround && (data.playerActions.Jump.WasPressed || (continuous_trigger && data.playerActions.Jump.IsPressed)))
            {
                data.jump.Jump(true, jump_mult);
                currentjumps -= costPerJump;
            }
        }
        public override void OnOnDestroy()
        {
        }
        public void SetInterval(float interval)
        {
            this.interval = interval;
        }
        public void AddJumps(float add)
        {
            jumps += add;
        }
        public void SetJumpMult(float mult)
        {
            jump_mult = mult;
        }
        public float GetJumpMult()
        {
            return jump_mult;
        }
        public void SetContinuousTrigger(bool enabled)
        {
            continuous_trigger = enabled;
        }
        public bool GetContinuousTrigger()
        {
            return continuous_trigger;
        }
        public void SetResetOnWallGrab(bool enabled)
        {
            resetOnWallGrab = enabled;
        }
        public void SetCostPerJump(float cost)
        {
            costPerJump = cost;
        }
        public float GetCostPerJump()
        {
            return costPerJump;
        }
    }

}