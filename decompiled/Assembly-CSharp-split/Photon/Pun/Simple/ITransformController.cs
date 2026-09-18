namespace Photon.Pun.Simple;

public interface ITransformController
{
	bool HandlesInterpolation { get; }

	bool HandlesExtrapolation { get; }
}
