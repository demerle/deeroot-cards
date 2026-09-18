using System;

namespace Photon.Pun.Simple
{
	[Serializable]
	public class ObjStateLogic : MaskLogic
	{
		protected static int[] stateValues = (int[])Enum.GetValues(typeof(ObjStateEditor));

		protected static string[] stateNames = Enum.GetNames(typeof(ObjStateEditor));

		protected override bool DefinesZero
		{
			get
			{
				return true;
			}
		}

		protected override string[] EnumNames
		{
			get
			{
				return stateNames;
			}
		}

		protected override int[] EnumValues
		{
			get
			{
				return stateValues;
			}
		}

		protected override int DefaultValue
		{
			get
			{
				return 1;
			}
		}
	}
}
