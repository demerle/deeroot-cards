using UnityEngine;

public class SetRotation : MonoBehaviour
{
	public float setRot;

	public float addRot;

	public float extraSetPerLevel;

	private float rot;

	private bool inited;

	private void Init()
	{
		if (!inited)
		{
			inited = true;
			int attackLevel = GetComponentInParent<AttackLevel>().attackLevel;
			setRot += (float)attackLevel * extraSetPerLevel;
		}
	}

	public void Set()
	{
		Init();
		rot = setRot;
		base.transform.localRotation = Quaternion.Euler(new Vector3(rot, 0f, 0f));
	}

	public void Add()
	{
		Init();
		rot += addRot;
		base.transform.localRotation = Quaternion.Euler(new Vector3(rot, 0f, 0f));
	}
}
