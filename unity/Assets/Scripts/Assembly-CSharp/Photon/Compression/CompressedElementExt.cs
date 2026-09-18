using System;
using System.Text;
using emotitron.Compression;

namespace Photon.Compression
{
	public static class CompressedElementExt
	{
		public static uint[] reusableInts = new uint[3];

		public static StringBuilder AppendSB(this StringBuilder strb, CompressedElement ce)
		{
			if ((object)ce == null)
			{
				strb.Append("[Null CompElement]");
			}
			else
			{
				ElementCrusher crusher = ce.crusher;
				if (crusher == null)
				{
					strb.Append("[CE Null Crusher]");
				}
				else if (crusher.TRSType == TRSType.Quaternion)
				{
					strb.Append(crusher.TRSType).Append(" cQuat: [").Append(ce.cQuat.cvalue)
						.Append("]");
				}
				else if (crusher.TRSType == TRSType.Scale && crusher.uniformAxes != 0)
				{
					strb.Append(crusher.TRSType).Append(" cUni: [").Append(crusher.uniformAxes)
						.Append(" : ")
						.Append(ce.cUniform.cvalue)
						.Append("]");
				}
				else
				{
					strb.Append(crusher.TRSType).Append(" cXYZ: [x:").Append(ce.cx.cvalue)
						.Append(" y:")
						.Append(ce.cy.cvalue)
						.Append(" z:")
						.Append(ce.cz.cvalue)
						.Append("]");
				}
			}
			return strb;
		}

		public static void GetChangeAmount(uint[] results, CompressedElement a, CompressedElement b)
		{
			for (int i = 0; i < 3; i++)
			{
				results[i] = (uint)Math.Abs(a[i] - b[0]);
			}
		}

		[Obsolete]
		public static uint[] GetChangeAmount(CompressedElement a, CompressedElement b)
		{
			for (int i = 0; i < 3; i++)
			{
				reusableInts[i] = (uint)Math.Abs(a[i] - b[0]);
			}
			return reusableInts;
		}

		public static void GuessUpperBits(this CompressedElement newcpos, ElementCrusher ec, CompressedElement oldcpos, BitCullingLevel bcl)
		{
			newcpos.Set(ec, ec.XCrusher.GuessUpperBits(newcpos[0], oldcpos[0], bcl), ec.YCrusher.GuessUpperBits(newcpos[1], oldcpos[1], bcl), ec.ZCrusher.GuessUpperBits(newcpos[2], oldcpos[2], bcl));
		}

		[Obsolete]
		public static CompressedElement GuessUpperBits(this CompressedElement newcpos, CompressedElement oldcpos, ElementCrusher ec, BitCullingLevel bcl)
		{
			return new CompressedElement(ec, ec.XCrusher.GuessUpperBits(newcpos[0], oldcpos[0], bcl), ec.YCrusher.GuessUpperBits(newcpos[1], oldcpos[1], bcl), ec.ZCrusher.GuessUpperBits(newcpos[2], oldcpos[2], bcl));
		}

		public static void OverwriteUpperBits(this CompressedElement low, CompressedElement uppers, BitCullingLevel bcl)
		{
			ElementCrusher crusher = low.crusher;
			low.Set(crusher, crusher.XCrusher.OverwriteUpperBits(low.cx, uppers.cx, bcl), crusher.YCrusher.OverwriteUpperBits(low.cy, uppers.cy, bcl), crusher.ZCrusher.OverwriteUpperBits(low.cz, uppers.cz, bcl));
		}

		[Obsolete]
		public static CompressedElement OverwriteUpperBits(this CompressedElement low, CompressedElement up, ElementCrusher ec, BitCullingLevel bcl)
		{
			return new CompressedElement(ec, ec[0].OverwriteUpperBits(low.cx, up.cx, bcl), ec[1].OverwriteUpperBits(low.cy, up.cy, bcl), ec[2].OverwriteUpperBits(low.cz, up.cz, bcl));
		}

		public static void ZeroLowerBits(this CompressedElement fullpos, CompressedElement target, BitCullingLevel bcl)
		{
			ElementCrusher crusher = fullpos.crusher;
			target.Set(crusher, crusher.XCrusher.ZeroLowerBits(fullpos.cx, bcl), crusher.YCrusher.ZeroLowerBits(fullpos.cy, bcl), crusher.ZCrusher.ZeroLowerBits(fullpos.cz, bcl));
		}

		[Obsolete]
		public static CompressedElement ZeroLowerBits(this CompressedElement fullpos, ElementCrusher ec, BitCullingLevel bcl)
		{
			return new CompressedElement(ec, ec[0].ZeroLowerBits(fullpos.cx, bcl), ec[1].ZeroLowerBits(fullpos.cy, bcl), ec[2].ZeroLowerBits(fullpos.cz, bcl));
		}

		public static void ZeroUpperBits(this CompressedElement fullpos, CompressedElement target, BitCullingLevel bcl)
		{
			ElementCrusher crusher = fullpos.crusher;
			target.Set(crusher, crusher.XCrusher.ZeroUpperBits(fullpos.cx, bcl), crusher.YCrusher.ZeroUpperBits(fullpos.cy, bcl), crusher.ZCrusher.ZeroUpperBits(fullpos.cz, bcl));
		}

		[Obsolete]
		public static CompressedElement ZeroUpperBits(this CompressedElement fullpos, ElementCrusher ec, BitCullingLevel bcl)
		{
			return new CompressedElement(ec, ec[0].ZeroUpperBits(fullpos.cx, bcl), ec[1].ZeroUpperBits(fullpos.cy, bcl), ec[2].ZeroUpperBits(fullpos.cz, bcl));
		}
	}
}
