namespace Photon.Compression
{
	public enum SerializationFlags
	{
		None = 0,
		HasContent = 1,
		Force = 2,
		ForceReliable = 4,
		SendToSelf = 8,
		NewConnection = 0x10,
		IsComplete = 0x20
	}
}
