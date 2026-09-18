using UnityEngine;

namespace Photon.Pun.Simple;

public interface IMountable
{
	Mount CurrentMount { get; }

	bool IsDroppable { get; }

	bool IsThrowable { get; }

	Rigidbody Rb { get; }

	Rigidbody2D Rb2d { get; }

	void ImmediateUnmount();
}
