using System;
using System.Collections.Generic;
using Photon.Compression;
using UnityEngine;

namespace Photon.Pun.Simple
{
	[Serializable]
	public class Vitals : IOnVitalValueChange, IOnVitalChange, IOnVitalParamChange
	{
		public List<IOnVitalValueChange> OnVitalValueChangeCallbacks = new List<IOnVitalValueChange>();

		public List<IOnVitalParamChange> OnVitalParamChangeCallbacks = new List<IOnVitalParamChange>();

		[HideInInspector]
		public List<VitalDefinition> vitalDefs = new List<VitalDefinition>
		{
			new VitalDefinition(100.0, 125u, 125.0, 1.0, 1f, 1.0, 1f, 1.0, "Health"),
			new VitalDefinition(100.0, 125u, 50.0, 0.6669999957084656, 1f, 1.0, 1f, 0.0, "Armor"),
			new VitalDefinition(200.0, 250u, 100.0, 1.0, 1f, 20.0, 1f, 10.0, "Shield")
		};

		[NonSerialized]
		private Vital[] vitalArray;

		private Dictionary<int, Vital> vitalLookup = new Dictionary<int, Vital>();

		private int vitalCount;

		private bool initialized;

		public List<VitalDefinition> VitalDefs
		{
			get
			{
				return vitalDefs;
			}
		}

		public Vital[] VitalArray
		{
			get
			{
				if (!initialized)
				{
					Initialize();
				}
				return vitalArray;
			}
		}

		public void Initialize()
		{
			if (initialized)
			{
				return;
			}
			vitalCount = vitalDefs.Count;
			vitalArray = new Vital[vitalCount];
			for (int i = 0; i < vitalCount; i++)
			{
				VitalDefinition vitalDefinition = vitalDefs[i];
				Vital vital = new Vital(vitalDefinition);
				vitalArray[i] = vital;
				vital.Initialize(TickEngineSettings.netTickInterval);
				vital.AddIOnVitalChange(this);
				int hash = vitalDefinition.VitalName.hash;
				if (vitalDefinition.VitalName.type != 0)
				{
					if (!vitalLookup.ContainsKey(hash))
					{
						vitalLookup.Add(hash, vital);
					}
					else
					{
						Debug.LogWarning("VitalNameType hash collision! Vitals cannot have more than one of each Vital Type in its list.");
					}
				}
			}
			initialized = true;
		}

		public void ResetValues()
		{
			for (int i = 0; i < vitalCount; i++)
			{
				vitalArray[i].ResetValues();
			}
		}

		public Vital GetVital(VitalNameType vitalNameType)
		{
			if (!initialized)
			{
				Initialize();
			}
			Vital value;
			vitalLookup.TryGetValue(vitalNameType.hash, out value);
			return value;
		}

		public int GetVitalIndex(VitalNameType vitalNameType)
		{
			if (vitalDefs == null)
			{
				return -1;
			}
			int hash = vitalNameType.hash;
			int i = 0;
			for (int count = vitalDefs.Count; i < count; i++)
			{
				if (vitalDefs[i].VitalName.hash == hash)
				{
					return i;
				}
			}
			return -1;
		}

		public SerializationFlags Serialize(VitalsData vdata, VitalsData lastVData, byte[] buffer, ref int bitposition, bool keyframe)
		{
			VitalData[] datas = vdata.datas;
			VitalData[] datas2 = lastVData.datas;
			SerializationFlags serializationFlags = SerializationFlags.None;
			int i = 0;
			for (int num = vitalCount; i < num; i++)
			{
				serializationFlags |= vitalDefs[i].Serialize(datas[i], datas2[i], buffer, ref bitposition, keyframe);
			}
			return serializationFlags;
		}

		public SerializationFlags Deserialize(VitalsData vdata, byte[] buffer, ref int bitposition, bool keyframe)
		{
			VitalData[] datas = vdata.datas;
			bool flag = true;
			bool flag2 = false;
			for (int i = 0; i < vitalCount; i++)
			{
				datas[i] = vitalDefs[i].Deserialize(buffer, ref bitposition, keyframe);
				if (datas[i].Value == double.NegativeInfinity)
				{
					flag = false;
				}
				else
				{
					flag2 = flag2 || true;
				}
			}
			if (!flag)
			{
				if (!flag2)
				{
					return SerializationFlags.None;
				}
				return SerializationFlags.HasContent;
			}
			return (SerializationFlags)33;
		}

		public void Apply(VitalsData vdata)
		{
			VitalData[] datas = vdata.datas;
			for (int i = 0; i < vitalCount; i++)
			{
				vitalArray[i].Apply(datas[i]);
			}
		}

		private double ApplyCharges(int vitalIdx, double discharge, bool allowOverload, bool propagate)
		{
			double num = discharge;
			double num2 = 0.0;
			int num3 = vitalIdx;
			while (num3 >= 0)
			{
				num2 += vitalArray[num3].ApplyCharges(num, allowOverload, !propagate);
				if (propagate)
				{
					num -= num2;
					if (num == 0.0)
					{
						break;
					}
					num3--;
					continue;
				}
				return num2;
			}
			return num2;
		}

		public double ApplyCharges(double discharge, bool allowOverload, bool propagate)
		{
			return ApplyCharges(vitalCount - 1, discharge, allowOverload, propagate);
		}

		public double ApplyCharges(VitalNameType vitalNameType, double discharge, bool allowOverload, bool propagate)
		{
			int num;
			if (vitalNameType.type == VitalType.None)
			{
				num = vitalCount - 1;
			}
			else
			{
				num = GetVitalIndex(vitalNameType);
				if (num == -1)
				{
					return 0.0;
				}
			}
			double num2 = ApplyCharges(num, discharge, allowOverload, propagate);
			CheckForDisrupt(num2);
			return num2;
		}

		public void CheckForDisrupt(double consumed)
		{
			if (consumed != 0.0)
			{
				if (consumed > 0.0)
				{
					DisruptDecay();
				}
				if (consumed < 0.0)
				{
					DisruptRegen();
				}
			}
		}

		public void DisruptRegen()
		{
			int i = 0;
			for (int num = vitalCount; i < num; i++)
			{
				vitalArray[i].DisruptRegen();
			}
		}

		public void DisruptDecay()
		{
			int i = 0;
			for (int num = vitalCount; i < num; i++)
			{
				vitalArray[i].DisruptDecay();
			}
		}

		public void OnVitalValueChange(Vital vital)
		{
			int i = 0;
			for (int count = OnVitalValueChangeCallbacks.Count; i < count; i++)
			{
				OnVitalValueChangeCallbacks[i].OnVitalValueChange(vital);
			}
		}

		public void OnVitalParamChange(Vital vital)
		{
			int i = 0;
			for (int count = OnVitalParamChangeCallbacks.Count; i < count; i++)
			{
				OnVitalParamChangeCallbacks[i].OnVitalParamChange(vital);
			}
		}

		public void Simulate()
		{
			int i = 0;
			for (int num = vitalCount; i < num; i++)
			{
				vitalArray[i].Simulate();
			}
		}
	}
}
