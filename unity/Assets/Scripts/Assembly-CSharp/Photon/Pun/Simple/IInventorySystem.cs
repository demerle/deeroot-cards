namespace Photon.Pun.Simple
{
	public interface IInventorySystem : IContactSystem
	{
	}
	public interface IInventorySystem<T> : IInventorySystem, IContactSystem
	{
	}
}
