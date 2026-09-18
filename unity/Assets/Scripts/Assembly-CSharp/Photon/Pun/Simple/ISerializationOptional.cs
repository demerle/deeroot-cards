namespace Photon.Pun.Simple
{
	public interface ISerializationOptional : IOnNetSerialize
	{
		bool IncludeInSerialization { get; }
	}
}
