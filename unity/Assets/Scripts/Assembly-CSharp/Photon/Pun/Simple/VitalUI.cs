using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Photon.Pun.Simple
{
	public class VitalUI : VitalUIBase
	{
		protected static GameObject vitalBarDefaultPrefab;

		public bool autoOffset = true;

		[Tooltip("Found children elements are nudged (value * vitalIndex). This is to automatically stagger multiple VitalUIs")]
		public Vector3 offset = new Vector3(0f, 0.1f, 0f);

		public float widthMultiplier = 1f;

		[Tooltip("Search for UI elements in children of this GameObject.")]
		[HideInInspector]
		public bool searchChildren = true;

		[HideInInspector]
		public Text UIText;

		[HideInInspector]
		public Image UIImage;

		[HideInInspector]
		public TextMesh textMesh;

		[HideInInspector]
		public SpriteRenderer barSprite;

		[HideInInspector]
		public SpriteRenderer backSprite;

		[HideInInspector]
		public bool billboard = true;

		private const string PLACEHOLDER_CANVAS_NAME = "PLACEHOLDER_VITALS_CANVAS";

		private static List<SpriteRenderer> resuableFindSpriteRend = new List<SpriteRenderer>();

		protected override void Reset()
		{
			base.Reset();
			FindUIElements();
		}

		protected override void Awake()
		{
			base.Awake();
			FindUIElements();
			base.enabled = billboard;
		}

		public override void Recalculate()
		{
			AutoAlign();
			if ((bool)backSprite)
			{
				backSprite.size = new Vector2(widthMultiplier, backSprite.size.y);
			}
			UpdateGraphics(vital);
		}

		protected virtual void AutoAlign()
		{
			if (autoOffset && (bool)base.transform.parent)
			{
				base.transform.localPosition = offset * vitalIndex;
			}
		}

		public bool FindUIElements()
		{
			if (textMesh == null)
			{
				textMesh = (searchChildren ? GetComponentInChildren<TextMesh>() : GetComponent<TextMesh>());
			}
			if (searchChildren)
			{
				GetComponentsInChildren(resuableFindSpriteRend);
			}
			else
			{
				GetComponents(resuableFindSpriteRend);
			}
			if (resuableFindSpriteRend.Count > 0 && barSprite == null)
			{
				barSprite = resuableFindSpriteRend[0];
			}
			if (resuableFindSpriteRend.Count > 1 && backSprite == null)
			{
				backSprite = resuableFindSpriteRend[1];
			}
			if (UIText == null)
			{
				UIText = (searchChildren ? GetComponentInChildren<Text>() : GetComponent<Text>());
			}
			if (UIImage == null)
			{
				UIImage = (searchChildren ? GetComponentInChildren<Image>() : GetComponent<Image>());
			}
			if (!textMesh && !barSprite && !UIText)
			{
				return UIImage;
			}
			return true;
		}

		public override void UpdateGraphics(Vital vital)
		{
			if (vital == null)
			{
				return;
			}
			VitalDefinition vitalDef = vital.VitalDef;
			double num = ((targetField == TargetField.Value) ? vital.VitalData.Value : ((targetField == TargetField.Max) ? vitalDef.FullValue : vitalDef.MaxValue));
			if (num == double.NegativeInfinity)
			{
				return;
			}
			if ((bool)textMesh)
			{
				textMesh.text = ((int)num).ToString();
			}
			if ((bool)barSprite)
			{
				barSprite.size = new Vector2((float)(num / vitalDef.MaxValue * (double)widthMultiplier), barSprite.size.y);
			}
			if (UIText != null)
			{
				UIText.text = ((int)num).ToString();
			}
			if (UIImage != null)
			{
				double maxValue = vitalDef.MaxValue;
				if (UIImage.type == Image.Type.Filled && UIImage.sprite != null)
				{
					UIImage.fillAmount = (float)(num / maxValue * (double)widthMultiplier);
				}
				else
				{
					UIImage.rectTransform.localScale = new Vector3((float)(num / maxValue * (double)widthMultiplier), UIImage.rectTransform.localScale.y, UIImage.rectTransform.localScale.z);
				}
			}
		}

		public void LateUpdate()
		{
			Camera main = Camera.main;
			if ((bool)main)
			{
				base.transform.LookAt(main.transform, new Vector3(0f, 1f, 0f));
				Vector3 eulerAngles = base.transform.eulerAngles;
				base.transform.eulerAngles = new Vector3(0f, eulerAngles.y + 180f, 0f);
			}
		}
	}
}
