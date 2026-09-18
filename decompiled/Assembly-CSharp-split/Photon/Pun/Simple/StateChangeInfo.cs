using UnityEngine;

namespace Photon.Pun.Simple;

public struct StateChangeInfo
{
	public ObjState objState;

	public Mount mount;

	public Vector3? offsetPos;

	public Quaternion? offsetRot;

	public Vector3? velocity;

	public bool force;

	public StateChangeInfo(StateChangeInfo src)
	{
		objState = src.objState;
		mount = src.mount;
		offsetPos = src.offsetPos;
		offsetRot = src.offsetRot;
		velocity = src.velocity;
		force = src.force;
	}

	public StateChangeInfo(ObjState itemState, Mount mount, Vector3? offsetPos, Quaternion? offsetRot, Vector3? velocity, bool force)
	{
		objState = itemState;
		this.mount = mount;
		this.offsetPos = offsetPos;
		this.offsetRot = offsetRot;
		this.velocity = velocity;
		this.force = force;
	}

	public StateChangeInfo(ObjState itemState, Mount mount, Vector3? offsetPos, Vector3? velocity, bool force)
	{
		objState = itemState;
		this.mount = mount;
		this.offsetPos = offsetPos;
		offsetRot = null;
		this.velocity = velocity;
		this.force = force;
	}

	public StateChangeInfo(ObjState itemState, Mount mount, bool force)
	{
		objState = itemState;
		this.mount = mount;
		offsetPos = null;
		offsetRot = null;
		velocity = null;
		this.force = force;
	}
}
