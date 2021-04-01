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
                    NetworkController.ShowException(new Exception("method's parameter " + type.FullName + " should have a parameterless constructor"));
                    return null;
                }
            }
            if (type.FullName == "System.Object[]")
            {
                NetworkController.ShowException(new Exception("invalid parameter type " + type.FullName));
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
                    NetworkController.ShowException(new Exception("method's parameter " + par.ParameterType.FullName + " should have a parameterless constructor"));
                    return null;
                }
                if (par.ParameterType.FullName == "System.Object[]")
                {
                    NetworkController.ShowException(new Exception("invalid parameter type " + par.ParameterType.FullName));
                    return null;
                }


                var obj = Activator.CreateInstance(par.ParameterType);
                var objName = par.ParameterType.FullName;

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

    }
}