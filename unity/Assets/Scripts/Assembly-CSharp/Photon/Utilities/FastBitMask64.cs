namespace Photon.Utilities
{
	public struct FastBitMask64
	{
		public ulong bitmask;

		private int bitcount;

		private ulong alltrue;

		public int BitCount
		{
			get
			{
				return bitcount;
			}
			set
			{
				bitcount = value;
				alltrue = (ulong)((bitcount < 64) ? ((1L << bitcount) - 1) : (-1));
				bitmask &= alltrue;
			}
		}

		public bool this[int bit]
		{
			get
			{
				return (bitmask & (ulong)(1L << bit)) != 0;
			}
			set
			{
				if (value)
				{
					bitmask |= (ulong)(1L << bit);
				}
				else
				{
					bitmask &= (ulong)(~(1L << bit));
				}
			}
		}

		public bool AllAreFalse
		{
			get
			{
				if (bitcount != 0)
				{
					return bitmask == 0;
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
					return bitmask == alltrue;
				}
				return true;
			}
		}

		public FastBitMask64(int bitcount)
		{
			bitmask = 0uL;
			this.bitcount = bitcount;
			alltrue = (ulong)((bitcount < 64) ? ((1L << bitcount) - 1) : (-1));
		}

		private FastBitMask64(FastBitMask64 copyFrom)
		{
			bitmask = copyFrom.bitmask;
			bitcount = copyFrom.bitcount;
			alltrue = copyFrom.alltrue;
		}

		public bool Get(int bit)
		{
			return (bitmask & (ulong)(1L << bit)) != 0;
		}

		public void Set(int bit, bool value)
		{
			if (value)
			{
				bitmask |= (ulong)(1L << bit);
			}
			else
			{
				bitmask &= (ulong)(~(1L << bit));
			}
		}

		public void SetTrue(int bit)
		{
			bitmask |= (ulong)(1L << bit);
		}

		public void SetFalse(int bit)
		{
			bitmask &= (ulong)(~(1L << bit));
		}

		public void SetAllTrue()
		{
			bitmask = alltrue;
		}

		public void SetAllFalse()
		{
			bitmask = 0uL;
		}

		public void OR(FastBitMask64 other)
		{
			bitmask |= other.bitmask;
		}

		public void AND(FastBitMask64 other)
		{
			bitmask &= other.bitmask;
		}

		public void XOR(FastBitMask64 other)
		{
			bitmask ^= other.bitmask;
		}

		public int CountTrue()
		{
			int num;
			if (bitmask == 0L)
			{
				num = 0;
			}
			else if (bitmask == alltrue)
			{
				num = bitcount;
			}
			else
			{
				num = 0;
				for (ulong num2 = bitmask; num2 != 0L; num2 >>= 1)
				{
					if ((num2 & 1) == 1)
					{
						num++;
					}
				}
			}
			return num;
		}

		public int CountFalse()
		{
			if (bitmask == 0L)
			{
				return bitcount;
			}
			if (bitmask == alltrue)
			{
				return 0;
			}
			int num = 0;
			int i = 0;
			for (int num2 = bitcount; i < num2; i++)
			{
				if ((bitmask & (ulong)(1L << i)) == 0L)
				{
					num++;
				}
			}
			return num;
		}

		public void ClearBitsBefore(int start, int count)
		{
			ulong num = (ulong)((count == 64) ? (-1) : ((1L << count) - 1));
			int num2 = start - count;
			ulong num3;
			ulong num4;
			if (num2 >= 0)
			{
				num3 = num << num2;
				num4 = num >> bitcount - num2;
			}
			else
			{
				num3 = num >> -num2;
				num4 = num << bitcount + num2;
			}
			bitmask &= ~num3 & ~num4 & alltrue;
		}

		public int CountValidRange(int start, int lookahead)
		{
			for (int num = lookahead; num >= 0; num--)
			{
				int num2 = start + num;
				if (num2 >= bitcount)
				{
					num2 -= bitcount;
				}
				if ((bitmask & (ulong)(1L << num2)) != 0L)
				{
					return num + 1;
				}
			}
			return 0;
		}

		public void Copy(FastBitMask64 other)
		{
			bitcount = other.bitcount;
			bitmask = other.bitmask;
			alltrue = other.alltrue;
		}

		public bool Compare(FastBitMask64 other)
		{
			if (bitcount == other.bitcount)
			{
				return bitmask == other.bitmask;
			}
			return false;
		}
	}
}
