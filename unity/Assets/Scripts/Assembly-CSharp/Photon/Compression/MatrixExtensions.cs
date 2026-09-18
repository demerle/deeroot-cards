namespace Photon.Compression
{
	public static class MatrixExtensions
	{
		public static void CopyFrom(this Matrix target, Matrix src)
		{
			target.crusher = src.crusher;
			target.position = src.position;
			target.rotation = src.rotation;
			target.scale = src.scale;
		}
	}
}
