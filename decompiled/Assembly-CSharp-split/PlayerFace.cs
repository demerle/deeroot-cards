using System;
using UnityEngine;

[Serializable]
public class PlayerFace
{
	public int eyeID;

	public Vector2 eyeOffset;

	public int mouthID;

	public Vector2 mouthOffset;

	public int detailID;

	public Vector2 detailOffset;

	public int detail2ID;

	public Vector2 detail2Offset;

	public void LoadFace(string key)
	{
		eyeID = PlayerPrefs.GetInt("eyeID-" + key);
		eyeOffset.x = PlayerPrefs.GetFloat("eyeOffsetX-" + key);
		eyeOffset.y = PlayerPrefs.GetFloat("eyeOffsetY-" + key);
		mouthID = PlayerPrefs.GetInt("mouthID-" + key);
		mouthOffset.x = PlayerPrefs.GetFloat("mouthOffsetX-" + key);
		mouthOffset.y = PlayerPrefs.GetFloat("mouthOffsetY-" + key);
		detailID = PlayerPrefs.GetInt("detailID-" + key);
		detailOffset.x = PlayerPrefs.GetFloat("detailOffsetX-" + key);
		detailOffset.y = PlayerPrefs.GetFloat("detailOffsetY-" + key);
		detail2ID = PlayerPrefs.GetInt("detail2ID-" + key);
		detail2Offset.x = PlayerPrefs.GetFloat("detail2OffsetX-" + key);
		detail2Offset.y = PlayerPrefs.GetFloat("detail2OffsetY-" + key);
	}

	internal static PlayerFace CopyFace(PlayerFace currentPlayerFace)
	{
		return new PlayerFace
		{
			eyeID = currentPlayerFace.eyeID,
			eyeOffset = currentPlayerFace.eyeOffset,
			mouthID = currentPlayerFace.mouthID,
			mouthOffset = currentPlayerFace.mouthOffset,
			detailID = currentPlayerFace.detailID,
			detailOffset = currentPlayerFace.detailOffset,
			detail2ID = currentPlayerFace.detail2ID,
			detail2Offset = currentPlayerFace.detail2Offset
		};
	}

	internal static PlayerFace CreateFace(int eyeID, Vector2 eyeOffset, int mouthID, Vector2 mouthOffset, int detailID, Vector2 detailOffset, int detail2ID, Vector2 detail2Offset)
	{
		return new PlayerFace
		{
			eyeID = eyeID,
			eyeOffset = eyeOffset,
			mouthID = mouthID,
			mouthOffset = mouthOffset,
			detailID = detailID,
			detailOffset = detailOffset,
			detail2ID = detail2ID,
			detail2Offset = detail2Offset
		};
	}

	public void SaveFace(string key)
	{
		PlayerPrefs.SetInt("eyeID-" + key, eyeID);
		PlayerPrefs.SetFloat("eyeOffsetX-" + key, eyeOffset.x);
		PlayerPrefs.SetFloat("eyeOffsetY-" + key, eyeOffset.y);
		PlayerPrefs.SetInt("mouthID-" + key, mouthID);
		PlayerPrefs.SetFloat("mouthOffsetX-" + key, mouthOffset.x);
		PlayerPrefs.SetFloat("mouthOffsetY-" + key, mouthOffset.y);
		PlayerPrefs.SetInt("detailID-" + key, detailID);
		PlayerPrefs.SetFloat("detailOffsetX-" + key, detailOffset.x);
		PlayerPrefs.SetFloat("detailOffsetY-" + key, detailOffset.y);
		PlayerPrefs.SetInt("detail2ID-" + key, detail2ID);
		PlayerPrefs.SetFloat("detail2OffsetX-" + key, detail2Offset.x);
		PlayerPrefs.SetFloat("detail2OffsetY-" + key, detail2Offset.y);
	}
}
