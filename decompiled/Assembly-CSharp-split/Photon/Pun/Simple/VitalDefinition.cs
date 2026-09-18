using System;
using System.Collections.Generic;
using Photon.Compression;
using Photon.Utilities;
using UnityEngine;

namespace Photon.Pun.Simple;

[Serializable]
public class VitalDefinition
{
	public List<IOnVitalValueChange> iOnVitalChange = new List<IOnVitalValueChange>();

	[SerializeField]
	private VitalNameType vitalName;

	[Tooltip("Values greater than this will degrade at the decay rate until this value is reached.")]
	[SerializeField]
	private double _fullValue;

	[Tooltip("The absolute greatest possible value. Values above Full Value are considered overloaded, and will decay down to Full Value.")]
	[SerializeField]
	private uint _maxValue;

	public double startValue;

	[Tooltip("Number of simulation ticks after damage until regeneration resumes.")]
	[SerializeField]
	private float regenDelay;

	[Tooltip("Amount per tick values less than Full Value will increase until Full Health is reached.")]
	[SerializeField]
	private double regenRate;

	[Tooltip("Number of simulation ticks after overload until decay resumes.")]
	[SerializeField]
	private float decayDelay;

	[Tooltip("Amount per tick overloaded values greater than Full Value will degrade until Full Health is reached.")]
	[SerializeField]
	private double decayRate;

	[Range(0f, 1f)]
	[Tooltip("How much of the damage this vital absords, the remainder is passed through to the next lower stat. 0 = None (useless), 0.5 = Half, 1 = Full. The root vital (0) likely should always be 1.")]
	[SerializeField]
	private double absorption;

	private int _decayDelayInTicks;

	private int _regenDelayInTicks;

	private double _decayPerTick;

	private double _regenPerTick;

	private int bitsForValue;

	private int bitsForDecayDelay;

	private int bitsForRegenDelay;

	public VitalNameType VitalName => vitalName;

	public double FullValue => _fullValue;

	public double MaxValue => _maxValue;

	public double Absorbtion => absorption;

	public int DecayDelayInTicks => _decayDelayInTicks;

	public int RegenDelayInTicks => _regenDelayInTicks;

	public double DecayPerTick => _decayPerTick;

	public double RegenPerTick => _regenPerTick;

	public void AddIOnVitalChange(IOnVitalValueChange cb)
	{
		iOnVitalChange.Add(cb);
	}

	public void RemoveIOnVitalChange(IOnVitalValueChange cb)
	{
		iOnVitalChange.Remove(cb);
	}

	public VitalDefinition(double fullValue, uint maxValue, double startValue, double absorbtion, float regenDelay, double regenRate, float decayDelay, double decayRate, string name)
	{
		_fullValue = fullValue;
		_maxValue = maxValue;
		this.startValue = startValue;
		absorption = absorbtion;
		this.regenDelay = regenDelay;
		this.regenRate = regenRate;
		this.decayDelay = decayDelay;
		this.decayRate = decayRate;
		vitalName = new VitalNameType(name);
	}

	public void Initialize(float tickDuration)
	{
		SetTickInterval(tickDuration);
	}

	public void SetTickInterval(float tickInterval)
	{
		_decayDelayInTicks = (int)(decayDelay / tickInterval);
		_regenDelayInTicks = (int)(regenDelay / tickInterval);
		_decayPerTick = decayRate * (double)tickInterval;
		_regenPerTick = regenRate * (double)tickInterval;
		bitsForValue = _maxValue.GetBitsForMaxValue();
		bitsForDecayDelay = _decayDelayInTicks.GetBitsForMaxValue();
		bitsForRegenDelay = _regenDelayInTicks.GetBitsForMaxValue();
	}

	public VitalData GetDefaultData()
	{
		return new VitalData(startValue, _decayDelayInTicks, _regenDelayInTicks);
	}

	public SerializationFlags Serialize(VitalData vitalData, VitalData prevVitalData, byte[] buffer, ref int bitposition, bool keyframe = true)
	{
		int ticksUntilDecay = vitalData.ticksUntilDecay;
		int ticksUntilRegen = vitalData.ticksUntilRegen;
		int num = (int)vitalData.Value;
		int num2 = (int)prevVitalData.Value;
		bool flag = num != num2;
		if (keyframe)
		{
			buffer.Write((ulong)num, ref bitposition, bitsForValue);
		}
		else
		{
			buffer.WriteBool(flag, ref bitposition);
			if (flag)
			{
				buffer.Write((ulong)num, ref bitposition, bitsForValue);
			}
		}
		if (ticksUntilDecay > 0)
		{
			buffer.WriteBool(b: true, ref bitposition);
			buffer.Write((ulong)ticksUntilDecay, ref bitposition, bitsForDecayDelay);
		}
		else
		{
			buffer.WriteBool(b: false, ref bitposition);
		}
		if (ticksUntilRegen > 0)
		{
			buffer.WriteBool(b: true, ref bitposition);
			buffer.Write((ulong)ticksUntilRegen, ref bitposition, bitsForRegenDelay);
		}
		else
		{
			buffer.WriteBool(b: false, ref bitposition);
		}
		if (!(flag || keyframe))
		{
			return SerializationFlags.None;
		}
		return SerializationFlags.HasContent;
	}

	public VitalData Deserialize(byte[] buffer, ref int bitposition, bool keyframe = true)
	{
		double value = ((keyframe || buffer.ReadBool(ref bitposition)) ? ((double)buffer.Read(ref bitposition, bitsForValue)) : double.NegativeInfinity);
		return new VitalData(value, (int)(buffer.ReadBool(ref bitposition) ? buffer.Read(ref bitposition, bitsForDecayDelay) : 0), (int)(buffer.ReadBool(ref bitposition) ? buffer.Read(ref bitposition, bitsForRegenDelay) : 0));
	}

	public VitalData Extrapolate(VitalData prev)
	{
		int num = ((prev.ticksUntilRegen > 0) ? (prev.ticksUntilRegen - 1) : 0);
		int num2 = ((prev.ticksUntilDecay > 0) ? (prev.ticksUntilDecay - 1) : 0);
		double value = prev.Value;
		return new VitalData((value > FullValue && num2 == 0) ? (value - _decayPerTick) : ((value < FullValue && num == 0) ? (value + _regenPerTick) : value), num2, num);
	}
}
