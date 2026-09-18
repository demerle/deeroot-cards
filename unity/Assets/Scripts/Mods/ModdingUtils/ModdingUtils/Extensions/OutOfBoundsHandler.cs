using UnityEngine;
using UnboundLib;
namespace ModdingUtils.Extensions
{
    public static class OutOfBoundsHandlerExtensions
    {
        private static GameObject _Border = null;
        /// <summary>
        /// The OutOfBounds border gameobject
        /// </summary>
        public static GameObject Border
        {
            get
            {
                if (_Border is null)
                {
                    // look for border in vanilla position
                    _Border = UIHandler.instance?.transform?.Find("Canvas/Border")?.gameObject;
                    if (_Border is null)
                    {
                        // look for border in MapEmbiggener position if the vanilla one was not found
                        _Border = GameObject.Find("/BorderCanvas/Border");
                    }
                }
                return _Border;
            }
        }
        /// <summary>
        /// The OutOfBounds border RectTransform which singlehandedly determines the size, rotation, and position of the border
        /// </summary>
        public static RectTransform BorderRect
        {
            get
            {
                return Border.GetComponent<RectTransform>();
            }
        }

        /// <summary>
        /// The angle IN RADIANS about the Z axis of the OutOfBounds border
        /// </summary>
        public static float Angle => Mathf.PI * BorderRect.rotation.eulerAngles.z / 180f;
        public static Vector3 WorldPositionFromBoundsPoint(this OutOfBoundsHandler instance, Vector3 point)
        {
            return (Vector3)instance.InvokeMethod("GetPoint", point);
        }
        public static Vector3 BoundsPointFromWorldPosition(this OutOfBoundsHandler instance, Vector3 position)
        {
            Vector3 min = RotateFromBoundsFrame(instance.WorldPositionFromBoundsPoint(Vector3.zero));
            Vector3 max = RotateFromBoundsFrame(instance.WorldPositionFromBoundsPoint(Vector3.one));

            Vector3 rotated = RotateFromBoundsFrame(position);

            return new Vector3(Mathf.InverseLerp(min.x, max.x, rotated.x), Mathf.InverseLerp(min.y, max.y, rotated.y), 0f);
        }
        private static Vector3 RotateToBoundsFrame(Vector3 point)
        {
            float cos = Mathf.Cos(Angle);
            float sin = Mathf.Sin(Angle);
            return new Vector3(cos * point.x - sin * point.y, sin * point.x + cos * point.y, point.z);
        }
        private static Vector3 RotateFromBoundsFrame(Vector3 point)
        {
            float cos = Mathf.Cos(Angle);
            float sin = Mathf.Sin(Angle);
            return new Vector3(cos * point.x + sin * point.y, -sin * point.x + cos * point.y, point.z);
        }
    }
}
