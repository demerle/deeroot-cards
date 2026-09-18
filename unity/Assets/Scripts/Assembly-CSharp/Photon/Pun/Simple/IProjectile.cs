using UnityEngine;

namespace Photon.Pun.Simple
{
	public interface IProjectile
	{
		IProjectileCannon Owner { get; set; }

		void Initialize(IProjectileCannon owner, int frameId, int subFrameId, Vector3 velocity, RespondTo terminateOn, RespondTo damageOn, float timeshift);
	}
}
