using System;
using System.Reflection;
using UnityEngine;

namespace Photon.Pun.UtilityScripts;

public static class CopyComponent
{
	public static Component ComponentCopy(this Component original, GameObject destination)
	{
		Type type = original.GetType();
		Component component = destination.AddComponent(type);
		FieldInfo[] fields = type.GetFields();
		foreach (FieldInfo fieldInfo in fields)
		{
			fieldInfo.SetValue(component, fieldInfo.GetValue(original));
		}
		return component;
	}

	public static T GetCopyOf<T>(this T comp, T other) where T : Component
	{
		if (comp == null || other == null)
		{
			return comp;
		}
		Type type = comp.GetType();
		if (type != other.GetType())
		{
			return null;
		}
		BindingFlags bindingAttr = BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
		PropertyInfo[] properties = type.GetProperties(bindingAttr);
		foreach (PropertyInfo propertyInfo in properties)
		{
			if (!propertyInfo.CanWrite)
			{
				continue;
			}
			try
			{
				if (!Attribute.IsDefined(propertyInfo, typeof(ObsoleteAttribute)))
				{
					propertyInfo.SetValue(comp, propertyInfo.GetValue(other, null), null);
				}
			}
			catch
			{
			}
		}
		FieldInfo[] fields = type.GetFields(bindingAttr);
		foreach (FieldInfo fieldInfo in fields)
		{
			fieldInfo.SetValue(comp, fieldInfo.GetValue(other));
		}
		return comp;
	}

	public static T AddColliderCopy<T>(this GameObject go, T toAdd) where T : Collider
	{
		T obj = go.AddComponent(toAdd.GetType()).GetCopyOf(toAdd) as T;
		obj.sharedMaterial = toAdd.sharedMaterial;
		obj.isTrigger = toAdd.isTrigger;
		return obj;
	}
}
