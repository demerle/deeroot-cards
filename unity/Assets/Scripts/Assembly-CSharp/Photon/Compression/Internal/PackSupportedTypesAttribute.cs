using System;

namespace Photon.Compression.Internal
{
	[AttributeUsage(AttributeTargets.Interface)]
	public class PackSupportedTypesAttribute : Attribute
	{
		public Type supportedType;

		public PackSupportedTypesAttribute(Type supportedType)
		{
			this.supportedType = supportedType;
		}
	}
}
