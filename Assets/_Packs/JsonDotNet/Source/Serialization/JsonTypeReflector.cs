#if !UNITY_WINRT || UNITY_EDITOR || (UNITY_WP8 &&  !UNITY_WP_8_1)

#region License
// Copyright (c) 2007 James Newton-King
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
#endregion
#pragma warning disable 436
using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
#if !((UNITY_WINRT && !UNITY_EDITOR))
using System.Security.Permissions;
#endif
using Newtonsoft.Json.Utilities;

namespace Newtonsoft.Json.Serialization
{
  internal interface IMetadataTypeAttribute
  {
    Type MetadataClassType { get; }
  }

  internal static class JsonTypeReflector
  {
    public const string IdPropertyName = "$id";
    public const string RefPropertyName = "$ref";
    public const string TypePropertyName = "$type";
    public const string ValuePropertyName = "$value";
    public const string ArrayValuesPropertyName = "$values";

    public const string ShouldSerializePrefix = "ShouldSerialize";
    public const string SpecifiedPostfix = "Specified";

    private static readonly ThreadSafeStore<ICustomAttributeProvider, Type> JsonConverterTypeCache = new ThreadSafeStore<ICustomAttributeProvider, Type>(GetJsonConverterTypeFromAttribute);

#if !(UNITY_WP8 || UNITY_WP_8_1) && (!UNITY_WINRT || UNITY_EDITOR)
    private static readonly ThreadSafeStore<Type, Type> AssociatedMetadataTypesCache = new ThreadSafeStore<Type, Type>(GetAssociateMetadataTypeFromAttribute);

    private const string MetadataTypeAttributeTypeName =
      "System.ComponentModel.DataAnnotations.MetadataTypeAttribute, System.ComponentModel.DataAnnotations, Version=3.5.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
    private static Type _cachedMetadataTypeAttributeType;
#endif
    public static JsonContainerAttribute GetJsonContainerAttribute(Type type)
    {
      return CachedAttributeGetter<JsonContainerAttribute>.GetAttribute(type);
    }

    public static JsonObjectAttribute GetJsonObjectAttribute(Type type)
    {
      return GetJsonContainerAttribute(type) as JsonObjectAttribute;
    }

    public static JsonArrayAttribute GetJsonArrayAttribute(Type type)
    {
      return GetJsonContainerAttribute(type) as JsonArrayAttribute;
    }

    public static DataContractAttribute GetDataContractAttribute(Type type)
    {
      // DataContractAttribute does not have inheritance
      DataContractAttribute result = null;
      Type currentType = type;
      while (result == null && currentType != null)
      {
        result = CachedAttributeGetter<DataContractAttribute>.GetAttribute(currentType);
        currentType = currentType.BaseType;
      }

      return result;
    }

    public static DataMemberAttribute GetDataMemberAttribute(MemberInfo memberInfo)
    {
      // DataMemberAttribute does not have inheritance

      // can't override a field
      if (memberInfo.MemberType == MemberTypes.Field)
        return CachedAttributeGetter<DataMemberAttribute>.GetAttribute(memberInfo);

      // search property and then search base properties if nothing is returned and the property is virtual
      PropertyInfo propertyInfo = (PropertyInfo) memberInfo;
      DataMemberAttribute result = CachedAttributeGetter<DataMemberAttribute>.GetAttribute(propertyInfo);
      if (result == null)
      {
        if (propertyInfo.IsVirtual())
        {
          Type currentType = propertyInfo.DeclaringType;

          while (result == null && currentType != null)
          {
            PropertyInfo baseProperty = (PropertyInfo)ReflectionUtils.GetMemberInfoFromType(currentType, propertyInfo);
            if (baseProperty != null && baseProperty.IsVirtual())
              result = CachedAttributeGetter<DataMemberAttribute>.GetAttribute(baseProperty);

            currentType = currentType.BaseType;
          }
        }
      }

      return result;
    }

    public static MemberSerialization GetObjectMemberSerialization(Type objectType)
    {
      JsonObjectAttribute objectAttribute = GetJsonObjectAttribute(objectType);

      if (objectAttribute == null)
      {
        DataContractAttribute dataContractAttribute = GetDataContractAttribute(objectType);

        if (dataContractAttribute != null)
          return MemberSerialization.OptIn;

        return MemberSerialization.OptOut;
      }

      return objectAttribute.MemberSerialization;
    }

    private static Type GetJsonConverterType(ICustomAttributeProvider attributeProvider)
    {
      return JsonConverterTypeCache.Get(attributeProvider);
    }

    private static Type GetJsonConverterTypeFromAttribute(ICustomAttributeProvider attributeProvider)
    {
      JsonConverterAttribute converterAttribute = GetAttribute<JsonConverterAttribute>(attributeProvider);
      return (converterAttribute != null)
        ? converterAttribute.ConverterType
        : null;
    }

    public static JsonConverter GetJsonConverter(ICustomAttributeProvider attributeProvider, Type targetConvertedType)
    {
      Type converterType = GetJsonConverterType(attributeProvider);

      if (converterType != null)
      {
        JsonConverter memberConverter = JsonConverterAttribute.CreateJsonConverterInstance(converterType);

        if (!memberConverter.CanConvert(targetConvertedType))
          throw new JsonSerializationException("JsonConverter {0} on {1} is not compatible with member type {2}.".FormatWith(CultureInfo.InvariantCulture, memberConverter.GetType().Name, attributeProvider, targetConvertedType.Name));

        return memberConverter;
      }

      return null;
    }

#if !((UNITY_WP8 || UNITY_WP_8_1) || (UNITY_WINRT && !UNITY_EDITOR))
	public static TypeConverter GetTypeConverter(Type type)
	{
#if !((UNITY_WP8 || UNITY_WP_8_1) || (UNITY_WINRT && !UNITY_EDITOR))
		return TypeDescriptor.GetConverter(type);
#else
      Type converterType = GetTypeConverterType(type);

      if (converterType != null)
        return (TypeConverter)ReflectionUtils.CreateInstance(converterType);

      return null;
#endif
	}
#endif

#if !((UNITY_WP8 || UNITY_WP_8_1) || (UNITY_WINRT && !UNITY_EDITOR))
    private static Type GetAssociatedMetadataType(Type type)
    {
      return AssociatedMetadataTypesCache.Get(type);
    }

    private static Type GetAssociateMetadataTypeFromAttribute(Type type)
    {
      Type metadataTypeAttributeType = GetMetadataTypeAttributeType();
      if (metadataTypeAttributeType == null)
        return null;

      object attribute = type.GetCustomAttributes(metadataTypeAttributeType, true).SingleOrDefault();
      if (attribute == null)
        return null;

#if (UNITY_IOS || UNITY_IPHONE || UNITY_ANDROID)
		IMetadataTypeAttribute metadataTypeAttribute = new LateBoundMetadataTypeAttribute(attribute);

#else
      IMetadataTypeAttribute metadataTypeAttribute = (DynamicCodeGeneration)
                                                       ? DynamicWrapper.CreateWrapper<IMetadataTypeAttribute>(attribute)
                                                       : new LateBoundMetadataTypeAttribute(attribute);
#endif

      return metadataTypeAttribute.MetadataClassType;
    }

    private static Type GetMetadataTypeAttributeType()
    {
      // always attempt to get the metadata type attribute type
      // the assembly may have been loaded since last time
      if (_cachedMetadataTypeAttributeType == null)
      {
        Type metadataTypeAttributeType = Type.GetType(MetadataTypeAttributeTypeName);

        if (metadataTypeAttributeType != null)
          _cachedMetadataTypeAttributeType = metadataTypeAttributeType;
        else
          return null;
      }
      
      return _cachedMetadataTypeAttributeType;
    }
#endif

    private static T GetAttribute<T>(Type type) where T : System.Attribute
    {
      T attribute;

#if !((UNITY_WP8 || UNITY_WP_8_1) || (UNITY_WINRT && !UNITY_EDITOR))
      Type metadataType = GetAssociatedMetadataType(type);
      if (metadataType != null)
      {
        attribute = ReflectionUtils.GetAttribute<T>(metadataType, true);
        if (attribute != null)
          return attribute;
      }
#endif

      attribute = ReflectionUtils.GetAttribute<T>(type, true);
      if (attribute != null)
        return attribute;

      foreach (Type typeInterface in type.GetInterfaces())
      {
        attribute = ReflectionUtils.GetAttribute<T>(typeInterface, true);
        if (attribute != null)
          return attribute;
      }

      return null;
    }

    private static T GetAttribute<T>(MemberInfo memberInfo) where T : System.Attribute
    {
      T attribute;
#if !((UNITY_WP8 || UNITY_WP_8_1) || (UNITY_WINRT && !UNITY_EDITOR))
      Type metadataType = GetAssociatedMetadataType(memberInfo.DeclaringType);
      if (metadataType != null)
      {
        MemberInfo metadataTypeMemberInfo = ReflectionUtils.GetMemberInfoFromType(metadataType, memberInfo);

        if (metadataTypeMemberInfo != null)
        {
          attribute = ReflectionUtils.GetAttribute<T>(metadataTypeMemberInfo, true);
          if (attribute != null)
            return attribute;
        }
      }
#endif
      attribute = ReflectionUtils.GetAttribute<T>(memberInfo, true);
      if (attribute != null)
        return attribute;

      foreach (Type typeInterface in memberInfo.DeclaringType.GetInterfaces())
      {
        MemberInfo interfaceTypeMemberInfo = ReflectionUtils.GetMemberInfoFromType(typeInterface, memberInfo);

        if (interfaceTypeMemberInfo != null)
        {
          attribute = ReflectionUtils.GetAttribute<T>(interfaceTypeMemberInfo, true);
          if (attribute != null)
            return attribute;
        }
      }

      return null;
    }

    public static T GetAttribute<T>(ICustomAttributeProvider attributeProvider) where T : System.Attribute
    {
      Type type = attributeProvider as Type;
      if (type != null)
        return GetAttribute<T>(type);

      MemberInfo memberInfo = attributeProvider as MemberInfo;
      if (memberInfo != null)
        return GetAttribute<T>(memberInfo);

      return ReflectionUtils.GetAttribute<T>(attributeProvider, true);
    }

    private static bool? _dynamicCodeGeneration;

    public static bool DynamicCodeGeneration
    {
      get
      {
        if (_dynamicCodeGeneration == null)
        {
#if !(UNITY_ANDROID || UNITY_WEBPLAYER || (UNITY_IOS || UNITY_IPHONE) || (UNITY_WP8 || UNITY_WP_8_1) || (UNITY_WINRT && !UNITY_EDITOR))
          // 在新版 Unity 中，移除对已废弃或不可用的安全权限 API 的调用
          // 简化为始终设置 _dynamicCodeGeneration = false
          try
          {
            _dynamicCodeGeneration = false;
          }
          catch (Exception)
          {
            _dynamicCodeGeneration = false;
          }
#else
			_dynamicCodeGeneration = false;
#endif
        }

        return _dynamicCodeGeneration.Value;
      }
    }

    public static ReflectionDelegateFactory ReflectionDelegateFactory
    {
      get
      {
#if !((UNITY_WP8 || UNITY_WP_8_1) || (UNITY_WINRT && !UNITY_EDITOR) || UNITY_IOS || UNITY_IPHONE || UNITY_ANDROID)
        if (DynamicCodeGeneration)
          return DynamicReflectionDelegateFactory.Instance;
#endif
        return LateBoundReflectionDelegateFactory.Instance;
      }
    }
  }
}
#pragma warning restore 436
#endif