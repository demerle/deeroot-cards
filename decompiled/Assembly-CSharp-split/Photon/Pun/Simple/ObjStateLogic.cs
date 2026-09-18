using System;

namespace Photon.Pun.Simple;

[Serializable]
public class ObjStateLogic : MaskLogic
{
	protected static int[] stateValues = (int[])Enum.GetValues(typeof(ObjStateEditor));

	protected static string[] stateNames = Enum.GetNames(typeof(ObjStateEditor));

	protected override bool DefinesZero => true;

	protected override string[] EnumNames => stateNames;

	protected override int[] EnumValues => stateValues;

	protected override int DefaultValue => 1;
}
