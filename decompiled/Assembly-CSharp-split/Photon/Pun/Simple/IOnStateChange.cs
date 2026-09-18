using UnityEngine;

namespace Photon.Pun.Simple;

public interface IOnStateChange
{
	void OnStateChange(ObjState newState, ObjState previousState, Transform attachmentTransform, Mount attachTo = null, bool isReady = true);
}
