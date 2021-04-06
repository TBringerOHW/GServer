using System;
#if UNITY_ENGINE
using UnityEngine;
#endif

namespace GServer.RPC
{
    public static class DebugLogger
    {
        public enum ELogMessageType
        {
            Default,
            Info,
            Warning,
            Error,
        }
        
        public static void LogMessage(string message, ELogMessageType messageType)
        {
#if UNITY_ENGINE
            message = $"[{DateTime.Now}] " + message;
            switch (messageType)
            {
                case ELogMessageType.Default:
                    Debug.Log(message);
                    break;
                case ELogMessageType.Info:
                    Debug.Log(message);
                    break;
                case ELogMessageType.Warning:
                    Debug.LogWarning(message);
                    break;
                case ELogMessageType.Error:
                    Debug.LogError(message);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(messageType), messageType, null);
            }
#else
            message = $"[{messageType}] [{DateTime.Now}] {message}";
            Console.WriteLine(message);
#endif
        }

        public static void LogMessage(object obj, string message, ELogMessageType messageType = ELogMessageType.Default)
        {
            LogMessage($"[{obj.GetType().Name}] {message}", messageType);
        }

        public static void LogObjectMessage(this object obj, string message, ELogMessageType messageType = ELogMessageType.Default)
        {
            LogMessage(obj, message, messageType);
        }

        public static void LogMessage(object obj, string methodName, string message, ELogMessageType messageType = ELogMessageType.Default)
        {
            LogMessage(obj, $"[{methodName}] {message}", messageType);
        }

        public static void LogObjectMessage(this object obj, string methodName, string message, ELogMessageType messageType = ELogMessageType.Default)
        {
            LogMessage(obj, methodName, message, messageType);
        }
        
    }
}