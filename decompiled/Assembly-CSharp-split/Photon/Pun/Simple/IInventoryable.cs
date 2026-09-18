namespace Photon.Pun.Simple;

public interface IInventoryable<T> : IContactable
{
	T Size { get; }
}
