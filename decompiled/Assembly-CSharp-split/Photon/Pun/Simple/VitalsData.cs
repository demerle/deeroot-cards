namespace Photon.Pun.Simple;

public class VitalsData
{
	public Vitals vitals;

	public VitalData[] datas;

	public VitalsData(Vitals vitals)
	{
		this.vitals = vitals;
		datas = new VitalData[vitals.VitalArray.Length];
	}

	public void CopyFrom(VitalsData source)
	{
		VitalData[] array = source.datas;
		int i = 0;
		for (int num = array.Length; i < num; i++)
		{
			datas[i] = array[i];
		}
	}
}
