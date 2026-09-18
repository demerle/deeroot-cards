using System;

namespace Photon.Pun.Simple
{
	[Serializable]
	public abstract class MaskLogic
	{
		public enum Operator
		{
			EQUALS = 0,
			AND = 1,
			OR = 2
		}

		public Operator operation = Operator.AND;

		public int stateMask;

		public int notMask;

		protected int trueMask;

		protected abstract string[] EnumNames { get; }

		protected abstract int[] EnumValues { get; }

		protected abstract bool DefinesZero { get; }

		protected abstract int DefaultValue { get; }

		public MaskLogic()
		{
			stateMask = DefaultValue;
		}

		public void RecalculateMasks()
		{
			if (operation == Operator.EQUALS)
			{
				notMask = 0;
			}
			notMask &= stateMask;
			trueMask = stateMask & ~notMask;
		}

		public bool Evaluate(int state)
		{
			if (stateMask == 0)
			{
				return state == 0;
			}
			switch (operation)
			{
			case Operator.EQUALS:
				return stateMask == state;
			case Operator.OR:
				if ((trueMask & state) == 0)
				{
					return (notMask & state) != notMask;
				}
				return true;
			case Operator.AND:
				if ((trueMask & state) == trueMask)
				{
					return (notMask & state) == 0;
				}
				return false;
			default:
				operation = Operator.EQUALS;
				return stateMask == state;
			}
		}
	}
}
