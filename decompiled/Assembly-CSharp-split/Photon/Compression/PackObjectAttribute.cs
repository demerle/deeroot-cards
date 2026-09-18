using System;

namespace Photon.Compression;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class PackObjectAttribute : Attribute
{
	public DefaultKeyRate defaultKeyRate;

	public DefaultPackInclusion defaultInclusion;

	public SyncAs syncAs = SyncAs.State;

	public string postSnapCallback;

	public string postApplyCallback;

	public PackObjectAttribute(DefaultKeyRate defaultKeyRate = DefaultKeyRate.Every)
	{
		this.defaultKeyRate = defaultKeyRate;
	}
}
