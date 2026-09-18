using Photon.Utilities;
using UnityEngine;

namespace Photon.Compression
{
	public class PackObjectSettings : SettingsScriptableObject<PackObjectSettings>
	{
		[Header("Code Generation")]
		[Tooltip("Enables the auto generation of codegen for PackObjects / PackAttributes. Disable this if you would like to suspend codegen. Existing codegen will remain, unless it produces errors.")]
		public bool autoGenerate = true;

		[Tooltip("Automatically deletes codegen if it produces any compile errors. Typically you will want to leave this enabled. Disable to see the actual errors being generated.")]
		public bool deleteBadCode = true;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		public static void Bootstrap()
		{
			PackObjectSettings single2 = SettingsScriptableObject<PackObjectSettings>.Single;
		}
	}
}
