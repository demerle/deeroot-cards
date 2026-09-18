namespace Photon.Utilities
{
	public struct FastBitMask128
	{
		private ulong seg1;

		private ulong seg2;

		private int bitcount;

		private int seg1bitcount;

		private int seg2bitcount;

		private ulong alltrue1;

		private ulong alltrue2;

		public ulong Seg1
		{
			get
			{
				return seg1;
			}
		}

		public ulong Seg2
		{
			get
			{
				return seg2;
			}
		}

		public ulong AllTrue1
		{
			get
			{
				return alltrue1;
			}
		}

		public ulong AllTrue2
		{
			get
			{
				return alltrue2;
			}
		}

		public int BitCount
		{
			get
			{
				return bitcount;
			}
			set
			{
				bitcount = value;
				seg1bitcount = ((bitcount < 64) ? bitcount : 64);
				seg2bitcount = ((bitcount > 64) ? (bitcount - 64) : 0);
				alltrue1 = (ulong)((bitcount < 64) ? ((1L << bitcount) - 1) : (-1));
				alltrue2 = (ulong)((bitcount == 128) ? (-1) : ((bitcount > 64) ? ((1L << bitcount - 64) - 1) : 0));
				seg1 &= alltrue1;
				seg2 &= alltrue2;
			}
		}

		public bool this[int bit]
		{
			get
			{
				if (bit < 64)
				{
					return (seg1 & (ulong)(1L << bit)) != 0;
				}
				return (seg2 & (ulong)(1L << bit - 64)) != 0;
			}
			set
			{
				if (value)
				{
					if (bit < 64)
					{
						seg1 |= (ulong)(1L << bit);
					}
					else
					{
						seg2 |= (ulong)(1L << bit - 64);
					}
				}
				else if (bit < 64)
				{
					seg1 &= (ulong)(~(1L << bit));
				}
				else
				{
					seg2 &= (ulong)(~(1L << bit - 64));
				}
			}
		}

		public bool AllAreFalse
		{
			get
			{
				if (bitcount != 0 && seg1 == 0L)
				{
					return seg2 == 0;
				}
				return false;
			}
		}

		public bool AllAreTrue
		{
			get
			{
				if (bitcount != 0)
				{
					if (seg1 == alltrue1)
					{
						return seg2 == alltrue2;
					}
					return false;
				}
				return true;
			}
		}

		public FastBitMask128(int bitcount)
		{
			seg1 = 0uL;
			seg2 = 0uL;
			this.bitcount = bitcount;
			seg1bitcount = ((bitcount < 64) ? bitcount : 64);
			seg2bitcount = ((bitcount > 64) ? (bitcount - 64) : 0);
			alltrue1 = (ulong)((bitcount < 64) ? ((1L << bitcount) - 1) : (-1));
			alltrue2 = (ulong)((bitcount == 128) ? (-1) : ((bitcount > 64) ? ((1L << bitcount - 64) - 1) : 0));
		}

		public FastBitMask128(FastBitMask128 copyFrom)
		{
			seg1 = copyFrom.seg1;
			seg2 = copyFrom.seg2;
			bitcount = copyFrom.bitcount;
			seg1bitcount = copyFrom.seg1bitcount;
			seg2bitcount = copyFrom.seg2bitcount;
			alltrue1 = copyFrom.alltrue1;
			alltrue2 = copyFrom.alltrue2;
		}

		public bool Get(int bit)
		{
			if (bit < 64)
			{
				return (seg1 & (ulong)(1L << bit)) != 0;
			}
			return (seg2 & (ulong)(1L << bit - 64)) != 0;
		}

		public void Set(int bit, bool value)
		{
			if (value)
			{
				if (bit < 64)
				{
					seg1 |= (ulong)(1L << bit);
				}
				else
				{
					seg2 |= (ulong)(1L << bit - 64);
				}
			}
			else if (bit < 64)
			{
				seg1 &= (ulong)(~(1L << bit));
			}
			else
			{
				seg2 &= (ulong)(~(1L << bit - 64));
			}
		}

		public void SetTrue(int bit)
		{
			if (bit < 64)
			{
				seg1 |= (ulong)(1L << bit);
			}
			else
			{
				seg2 |= (ulong)(1L << bit - 64);
			}
		}

		public void SetFalse(int bit)
		{
			if (bit < 64)
			{
				seg1 &= (ulong)(~(1L << bit));
			}
			else
			{
				seg2 &= (ulong)(~(1L << bit - 64));
			}
		}

		public void SetAllTrue()
		{
			seg1 = alltrue1;
			seg2 = alltrue2;
		}

		public void SetAllFalse()
		{
			seg1 = 0uL;
			seg2 = 0uL;
		}

		public void OR(FastBitMask128 other)
		{
			seg1 |= other.seg1;
			seg2 |= other.seg2;
		}

		public void OR(FastBitMask128 other, int otherOffset)
		{
			if (otherOffset == 0)
			{
				seg1 |= other.seg1;
				seg2 |= other.seg2;
			}
			else if (otherOffset == 64)
			{
				seg2 |= other.seg1;
			}
			else if (otherOffset < 128)
			{
				if (otherOffset > 64)
				{
					seg2 |= seg1 << otherOffset - 64;
					return;
				}
				seg1 |= other.seg1 << otherOffset;
				seg2 |= other.seg1 >> 64 - otherOffset;
				seg2 |= other.seg2 << otherOffset;
			}
		}

		public void AND(FastBitMask128 other)
		{
			seg1 &= other.seg1;
			seg2 &= other.seg2;
		}

		public void XOR(FastBitMask128 other)
		{
			seg1 ^= other.seg1;
			seg2 ^= other.seg2;
		}

		public static FastBitMask128 operator |(FastBitMask128 a, FastBitMask128 b)
		{
			FastBitMask128 result = new FastBitMask128(a);
			result.seg1 = a.seg1 | b.seg1;
			result.seg2 = a.seg2 | b.seg2;
			return result;
		}

		public static FastBitMask128 operator &(FastBitMask128 a, FastBitMask128 b)
		{
			FastBitMask128 result = new FastBitMask128(a);
			result.seg1 = a.seg1 & b.seg1;
			result.seg2 = a.seg2 & b.seg2;
			return result;
		}

		public static FastBitMask128 operator ^(FastBitMask128 a, FastBitMask128 b)
		{
			FastBitMask128 result = new FastBitMask128(a);
			result.seg1 = (a.seg1 ^ b.seg1) & a.alltrue1;
			result.seg2 = (a.seg2 ^ b.seg2) & a.alltrue2;
			return result;
		}

		public static FastBitMask128 operator !(FastBitMask128 a)
		{
			FastBitMask128 result = new FastBitMask128(a);
			result.seg1 = ~a.seg1 & a.alltrue1;
			result.seg2 = ~a.seg2 & a.alltrue2;
			return result;
		}

		public FastBitMask128 NOT()
		{
			FastBitMask128 result = new FastBitMask128(this);
			result.seg1 = ~seg1 & alltrue1;
			result.seg2 = ~seg2 & alltrue2;
			return result;
		}

		public int CountTrue()
		{
			int num;
			if (seg1 == 0L)
			{
				num = 0;
			}
			else if (seg1 == alltrue1)
			{
				num = seg1bitcount;
			}
			else
			{
				num = 0;
				for (ulong num2 = seg1; num2 != 0L; num2 >>= 1)
				{
					if ((num2 & 1) == 1)
					{
						num++;
					}
				}
			}
			if (seg2 == 0L)
			{
				return num;
			}
			if (seg2 == alltrue2)
			{
				return num + seg2bitcount;
			}
			for (ulong num3 = seg2; num3 != 0L; num3 >>= 1)
			{
				if ((num3 & 1) == 1)
				{
					num++;
				}
			}
			return num;
		}

		public int CountFalse()
		{
			int num;
			if (seg1 == 0L)
			{
				num = 0;
			}
			else if (seg1 == alltrue1)
			{
				num = seg1bitcount;
			}
			else
			{
				num = 0;
				for (ulong num2 = seg1; num2 != 0L; num2 >>= 1)
				{
					if ((num2 & 1) == 1)
					{
						num++;
					}
				}
			}
			if (seg2 == 0L)
			{
				return bitcount - num;
			}
			if (seg2 == alltrue2)
			{
				return bitcount - (num + seg2bitcount);
			}
			for (ulong num3 = seg2; num3 != 0L; num3 >>= 1)
			{
				if ((num3 & 1) == 1)
				{
					num++;
				}
			}
			return bitcount - num;
		}

		public void ClearBitsBefore(int start, int count)
		{
			ulong num = (ulong)((count == 64) ? (-1) : ((1L << count) - 1));
			int num2 = start - count;
			if (bitcount > 64)
			{
				ulong num3;
				ulong num4;
				if (num2 >= 0)
				{
					num3 = num << num2;
					num4 = num >> seg2bitcount - num2;
				}
				else
				{
					ulong num5 = num << bitcount + num2;
					num3 = (num >> -num2) | num5;
					num4 = num << seg2bitcount + num2;
				}
				seg1 &= ~num3;
				seg2 &= ~num4 & alltrue2;
			}
			else
			{
				ulong num3;
				ulong num4;
				if (num2 >= 0)
				{
					num3 = num << num2;
					num4 = num >> seg1bitcount - num2;
				}
				else
				{
					num3 = num >> -num2;
					num4 = num << seg1bitcount + num2;
				}
				seg1 &= ~num3 & ~num4 & alltrue1;
			}
		}

		public int CountValidRange(int start, int lookahead)
		{
			int num = bitcount;
			for (int num2 = lookahead; num2 >= 0; num2--)
			{
				int num3 = start + num2;
				if (num3 >= num)
				{
					num3 -= num;
				}
				if (num3 < 64)
				{
					if ((seg1 & (ulong)(1L << num3)) != 0L)
					{
						return num2 + 1;
					}
				}
				else if ((seg2 & (ulong)(1L << num3 - 64)) != 0L)
				{
					return num2 + 1;
				}
			}
			return 0;
		}

		public void Copy(FastBitMask128 other)
		{
			bitcount = other.bitcount;
			seg1bitcount = other.seg1bitcount;
			seg2bitcount = other.seg2bitcount;
			seg1 = other.seg1;
			seg2 = other.seg2;
			alltrue1 = other.alltrue1;
			alltrue2 = other.alltrue2;
		}

		public bool Compare(FastBitMask128 other)
		{
			if (bitcount == other.bitcount && seg1 == other.seg1)
			{
				return seg2 == other.seg2;
			}
			return false;
		}
	}
}
