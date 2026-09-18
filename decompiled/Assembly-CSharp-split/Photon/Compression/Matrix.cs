using System;
using Photon.Utilities;
using UnityEngine;
using emotitron.Compression;

namespace Photon.Compression;

public class Matrix
{
	public TransformCrusher crusher;

	public Vector3 position;

	public Element rotation;

	public Vector3 scale;

	public static Matrix reusable = new Matrix();

	public Matrix()
	{
	}

	public Matrix(TransformCrusher crusher)
	{
		this.crusher = crusher;
	}

	public Matrix(TransformCrusher crusher, Vector3 position, Element rotation, Vector3 scale)
	{
		this.crusher = crusher;
		this.position = position;
		this.scale = scale;
		this.rotation = rotation;
	}

	public Matrix(TransformCrusher crusher, Transform transform)
	{
		this.crusher = crusher;
		position = ((crusher == null || crusher.PosCrusher == null || crusher.PosCrusher.local) ? transform.localPosition : transform.position);
		scale = ((crusher == null || crusher.SclCrusher == null || crusher.SclCrusher.local) ? transform.localScale : transform.lossyScale);
		bool flag = crusher == null || crusher.RotCrusher == null || crusher.RotCrusher.local;
		if (crusher != null && crusher.RotCrusher != null && crusher.RotCrusher.TRSType == TRSType.Quaternion)
		{
			rotation = (flag ? transform.localRotation : transform.rotation);
		}
		else
		{
			rotation = (flag ? transform.localEulerAngles : transform.eulerAngles);
		}
	}

	public void Set(TransformCrusher crusher, Vector3 position, Element rotation, Vector3 scale)
	{
		this.crusher = crusher;
		this.position = position;
		this.scale = scale;
		this.rotation = rotation;
	}

	[Obsolete("Use Capture() instead. Set was confusing with other usage.")]
	public void Set(TransformCrusher crusher, Transform transform)
	{
		Capture(crusher, transform);
	}

	public void Capture(TransformCrusher crusher, Transform transform)
	{
		this.crusher = crusher;
		position = transform.position;
		scale = transform.localScale;
		if (crusher != null && crusher.RotCrusher != null && crusher.RotCrusher.TRSType == TRSType.Quaternion)
		{
			rotation = transform.rotation;
		}
		else
		{
			rotation = transform.eulerAngles;
		}
	}

	[Obsolete("Use Capture() instead. Set was confusing with other usage.")]
	public void Set(Transform transform)
	{
		Capture(transform);
	}

	public void Capture(Transform transform)
	{
		position = transform.position;
		scale = transform.localScale;
		if (crusher != null && crusher.RotCrusher != null && crusher.RotCrusher.TRSType == TRSType.Quaternion)
		{
			rotation = transform.rotation;
		}
		else
		{
			rotation = transform.eulerAngles;
		}
	}

	[Obsolete("Use Capture() instead. Set was confusing with other usage.")]
	public void Set(TransformCrusher crusher, Rigidbody rb)
	{
		Capture(crusher, rb);
	}

	public void Capture(TransformCrusher crusher, Rigidbody rb)
	{
		this.crusher = crusher;
		position = rb.position;
		scale = rb.transform.localScale;
		if (crusher != null && crusher.RotCrusher != null && crusher.RotCrusher.TRSType == TRSType.Quaternion)
		{
			rotation = rb.rotation;
		}
		else
		{
			rotation = rb.rotation.eulerAngles;
		}
	}

	[Obsolete("Use Capture() instead. Set was confusing with other usage.")]
	public void Set(Rigidbody rb)
	{
		Capture(rb);
	}

	public void Capture(Rigidbody rb)
	{
		position = rb.position;
		if (crusher != null && crusher.RotCrusher != null && crusher.RotCrusher.TRSType == TRSType.Quaternion)
		{
			rotation = rb.rotation;
		}
		else
		{
			rotation = rb.rotation.eulerAngles;
		}
		scale = rb.transform.localScale;
	}

	public void Clear()
	{
		crusher = null;
	}

	public void Compress(CompressedMatrix nonalloc)
	{
		crusher.Compress(nonalloc, this);
	}

	[Obsolete("Supply the transform to Apply to. Default Transform has been deprecated to allow shared TransformCrushers.")]
	public void Apply()
	{
		crusher.Apply(this);
	}

	public void Apply(Transform t)
	{
		if (crusher == null)
		{
			Debug.LogError("No crusher defined for this matrix. This matrix has not yet had a value assigned to it most likely, but you are trying to apply it to a transform.");
		}
		else
		{
			crusher.Apply(t, this);
		}
	}

	[Obsolete("Apply for Rigidbody has been replaced with Move and Set, to indicate usage of MovePosition/Rotation vs rb.position/rotation.")]
	public void Apply(Rigidbody rb)
	{
		if (crusher == null)
		{
			Debug.LogError("No crusher defined for this matrix. This matrix has not yet had a value assigned to it most likely, but you are trying to apply it to a transform.");
		}
		else
		{
			crusher.Apply(rb, this);
		}
	}

	public static Matrix Lerp(Matrix target, Matrix start, Matrix end, float t)
	{
		TransformCrusher transformCrusher = (target.crusher = end.crusher);
		target.position = Vector3.Lerp(start.position, end.position, t);
		if (transformCrusher != null && transformCrusher.RotCrusher != null)
		{
			if (transformCrusher.RotCrusher.TRSType == TRSType.Quaternion)
			{
				target.rotation = Quaternion.Slerp((Quaternion)start.rotation, (Quaternion)end.rotation, t);
			}
			else
			{
				Vector3 a = (Vector3)start.rotation;
				Vector3 vector = (Vector3)end.rotation;
				float num = a.y - vector.y;
				float num2 = a.z - vector.z;
				Vector3 b = new Vector3(vector.x, (num > 180f) ? (vector.y + 360f) : ((num < -180f) ? (vector.y - 360f) : vector.y), (num2 > 180f) ? (vector.z + 360f) : ((num2 < -180f) ? (vector.z - 360f) : vector.z));
				target.rotation = Vector3.Lerp(a, b, t);
			}
		}
		else
		{
			target.rotation = end.rotation;
		}
		target.scale = Vector3.Lerp(start.scale, end.scale, t);
		return target;
	}

	public static Matrix LerpUnclamped(Matrix target, Matrix start, Matrix end, float t)
	{
		TransformCrusher transformCrusher = (target.crusher = end.crusher);
		target.position = Vector3.LerpUnclamped(start.position, end.position, t);
		if (transformCrusher != null && transformCrusher.RotCrusher != null)
		{
			if (transformCrusher.RotCrusher.TRSType == TRSType.Quaternion)
			{
				target.rotation = Quaternion.SlerpUnclamped((Quaternion)start.rotation, (Quaternion)end.rotation, t);
			}
			else
			{
				Vector3 a = (Vector3)start.rotation;
				Vector3 vector = (Vector3)end.rotation;
				float num = a.y - vector.y;
				float num2 = a.z - vector.z;
				Vector3 b = new Vector3(vector.x, (num > 180f) ? (vector.y + 360f) : ((num < -180f) ? (vector.y - 360f) : vector.y), (num2 > 180f) ? (vector.z + 360f) : ((num2 < -180f) ? (vector.z - 360f) : vector.z));
				target.rotation = Vector3.LerpUnclamped(a, b, t);
			}
		}
		else
		{
			target.rotation = end.rotation;
		}
		target.scale = Vector3.LerpUnclamped(start.scale, end.scale, t);
		return target;
	}

	public static Matrix CatmullRomLerpUnclamped(Matrix target, Matrix pre, Matrix start, Matrix end, Matrix post, float t)
	{
		TransformCrusher transformCrusher = (target.crusher = end.crusher);
		target.position = CatmulRom.CatmullRomLerp(pre.position, start.position, end.position, post.position, t);
		if (transformCrusher != null && transformCrusher.RotCrusher != null)
		{
			if (transformCrusher.RotCrusher.TRSType == TRSType.Quaternion)
			{
				target.rotation = Quaternion.SlerpUnclamped((Quaternion)start.rotation, (Quaternion)end.rotation, t);
			}
			else
			{
				Vector3 a = (Vector3)start.rotation;
				Vector3 vector = (Vector3)end.rotation;
				float num = a.y - vector.y;
				float num2 = a.z - vector.z;
				Vector3 b = new Vector3(vector.x, (num > 180f) ? (vector.y + 360f) : ((num < -180f) ? (vector.y - 360f) : vector.y), (num2 > 180f) ? (vector.z + 360f) : ((num2 < -180f) ? (vector.z - 360f) : vector.z));
				target.rotation = Vector3.LerpUnclamped(a, b, t);
			}
		}
		else
		{
			target.rotation = end.rotation;
		}
		target.scale = CatmulRom.CatmullRomLerp(pre.scale, start.scale, end.scale, post.scale, t);
		return target;
	}

	public static Matrix CatmullRomLerpUnclamped(Matrix target, Matrix pre, Matrix start, Matrix end, float t)
	{
		TransformCrusher transformCrusher = (target.crusher = end.crusher);
		target.position = CatmulRom.CatmullRomLerp(pre.position, start.position, end.position, t);
		if (transformCrusher != null && transformCrusher.RotCrusher != null)
		{
			if (transformCrusher.RotCrusher.TRSType == TRSType.Quaternion)
			{
				target.rotation = Quaternion.SlerpUnclamped((Quaternion)start.rotation, (Quaternion)end.rotation, t);
			}
			else
			{
				Vector3 a = (Vector3)start.rotation;
				Vector3 vector = (Vector3)end.rotation;
				float num = a.y - vector.y;
				float num2 = a.z - vector.z;
				Vector3 b = new Vector3(vector.x, (num > 180f) ? (vector.y + 360f) : ((num < -180f) ? (vector.y - 360f) : vector.y), (num2 > 180f) ? (vector.z + 360f) : ((num2 < -180f) ? (vector.z - 360f) : vector.z));
				target.rotation = Vector3.LerpUnclamped(a, b, t);
			}
		}
		else
		{
			target.rotation = end.rotation;
		}
		target.scale = CatmulRom.CatmullRomLerp(pre.scale, start.scale, end.scale, t);
		return target;
	}

	public override string ToString()
	{
		return string.Concat("MATRIX pos: ", position, " rot: ", rotation, " scale: ", scale, "  rottype: ", rotation.vectorType);
	}
}
