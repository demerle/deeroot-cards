using UnityEngine;

[ExecuteInEditMode]
public class BlurPost : MonoBehaviour
{
	[SerializeField]
	private Material postprocessMaterial;

	private void OnRenderImage(RenderTexture source, RenderTexture destination)
	{
		RenderTexture temporary = RenderTexture.GetTemporary(source.width, source.height);
		Graphics.Blit(source, temporary, postprocessMaterial, 0);
		Graphics.Blit(temporary, destination, postprocessMaterial, 1);
		RenderTexture.ReleaseTemporary(temporary);
	}
}
