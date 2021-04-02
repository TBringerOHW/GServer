using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using GServer.Containers;
using System.Runtime.Serialization;

namespace GServer.RPC
{
    internal static class ReflectionHelper
    {
        public static object CheckNonBasicType(Type type)
        {
            if (NetworkView.IsValidBasicType(type)) return null;

            var constructor = type.GetConstructor(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null, Type.EmptyTypes, null);
            if (constructor == null)
            {
                if (FormatterServices.GetUninitializedObject(type) == null)
                {
                    NetworkController.ShowException(new Exception("method's parameter " + type.GetFullName() + " should have a parameterless constructor"));
                    return null;
                }
            }
            if (type.GetFullName() == "System.Object[]")
            {
                NetworkController.ShowException(new Exception("invalid parameter type " + type.GetFullName()));
                return null;
            }


            var obj = Activator.CreateInstance(type);
            string objName = obj.ToString();



            if (!(obj is IMarshallable))
            {
                NetworkController.ShowException(new Exception("argument " + objName + " not implement IMarshallable"));
                return null;
            }

            if (type.GetMethod("GetHashCode")?.DeclaringType != type)
            {
                NetworkController.ShowException(new Exception("argument " + objName + " not override GetHashCode"));
                return null;
            }

            return obj;
        }
        public static Dictionary<string, IMarshallable> GetMethodParamsObjects(MethodInfo methodInfo)
        {
            Dictionary<string, IMarshallable> result = new Dictionary<string, IMarshallable>();
            ParameterInfo[] args = methodInfo.GetParameters();
            foreach (ParameterInfo par in args)
            {
                if (NetworkView.IsValidBasicType(par.ParameterType))
                    continue;

                var constructor = par.ParameterType.GetConstructor(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, 
                    null, Type.EmptyTypes, null);
                if (constructor == null && !par.ParameterType.IsValueType)
                {
                    NetworkController.ShowException(new Exception("method's parameter " + par.ParameterType.GetFullName() + " should have a parameterless constructor"));
                    return null;
                }
                if (par.ParameterType.GetFullName() == "System.Object[]")
                {
                    NetworkController.ShowException(new Exception("invalid parameter type " + par.ParameterType.GetFullName()));
                    return null;
                }


                var obj = Activator.CreateInstance(par.ParameterType);
                var objName = par.ParameterType.GetFullName();

                if (!(obj is IMarshallable))
                {
                    NetworkController.ShowException(new Exception("argument " + objName + " not implement IMarshallable"));
                    return null;
                }

                if (result.ContainsKey(objName))
                    continue;

                result.Add(objName, obj as IMarshallable);
            }
            return result;
        }

        public static IEnumerable<MethodInfo> GetMethodsWithAttribute(Type classType, Type attributeType)
        {
            return classType.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public).Where(methodInfo => methodInfo.GetCustomAttributes(attributeType, true).Length > 0);
        }

        public static IEnumerable<MemberInfo> GetMembersWithAttribute(Type classType, Type attributeType)
        {
            return classType.GetMembers(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public).Where(memberInfo => memberInfo.GetCustomAttributes(attributeType, true).Length > 0);
        }
        public static IEnumerable<FieldInfo> GetFieldsWithAttribute(Type classType, Type attributeType)
        {
            return classType.GetFields().Where(fieldInfo => fieldInfo.GetCustomAttributes(attributeType, true).Length > 0);
        }
        public static IEnumerable<PropertyInfo> GetPropertiesWithAttribute(Type classType, Type attributeType)
        {
            return classType.GetProperties().Where(propertyInfo => propertyInfo.GetCustomAttributes(attributeType, true).Length > 0);
        }

        public static bool IsBasicType(Type type)
        {
            return type.IsPrimitive 
                || type.IsEnum
                || type == typeof(string)
                || type == typeof(decimal);
        }

        /// <summary>
        /// Return <see cref="Type"/> full name.
        /// </summary>
        /// <param name="type">Some <see cref="Type"/>.</param>
        /// <returns>Returning same value as <see cref="Type"/>.FullName but without square brackets and assembly info.</returns>
        public static string GetFullName(this Type type)
        {
            var fullName = type.FullName;
            fullName = fullName?.Split(',')[0];
            return fullName.Replace("[", "");
        }

        /// <summary>
        /// Returns basic type FullName. Unlike regular <see cref="Type"/> FullName perform <see cref="Enum"/> basic type check. (Ex. For <see cref="Enum"/> ESomeEnum : <see cref="byte"/> this method will return name of byte, not enum name.)
        /// </summary>
        public static string GetBasicTypeName(this Type type)
        {
            return type.IsEnum ? type.GetEnumUnderlyingType().GetFullName() : type.GetFullName();
        }

    }
}