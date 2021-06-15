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

		private static void LogMessage(string message, ELogMessageType messageType)
		{
		#if UNITY_ENGINE
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

		public static void LogMessage(string className, string methodName, string message, ELogMessageType messageType = ELogMessageType.Default)
		{
#if UNITY_ENGINE
            message = $"[{className}] [{methodName}] [{message}]";
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
			message = $"[{messageType}] [{DateTime.Now}] [{className}] [{methodName}] {message}";
			Console.WriteLine(message);
#endif
		}

		public static void LogMessage(object obj, string message, ELogMessageType messageType = ELogMessageType.Default)
		{
		#if UNITY_ENGINE
			var resultMessage = $"[{obj.GetType().Name}] {message}";
            switch (messageType)
            {
                case ELogMessageType.Default:
                    Debug.Log(resultMessage);
                    break;
                case ELogMessageType.Info:
                    Debug.Log(resultMessage);
                    break;
                case ELogMessageType.Warning:
                    Debug.LogWarning(resultMessage);
                    break;
                case ELogMessageType.Error:
                    Debug.LogError(resultMessage);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(messageType), messageType, null);
            }
		#else
			LogMessage($"[{obj.GetType().Name}] {message}", messageType);
		#endif
		}

		public static void LogObjectMessage(this object obj, string message, ELogMessageType messageType = ELogMessageType.Default)
		{
		#if UNITY_ENGINE
			var resultMessage = $"[{obj.GetType().Name}] {message}";
            switch (messageType)
            {
                case ELogMessageType.Default:
                    Debug.Log(resultMessage);
                    break;
                case ELogMessageType.Info:
                    Debug.Log(resultMessage);
                    break;
                case ELogMessageType.Warning:
                    Debug.LogWarning(resultMessage);
                    break;
                case ELogMessageType.Error:
                    Debug.LogError(resultMessage);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(messageType), messageType, null);
            }
		#else
			LogMessage($"[{obj.GetType().Name}] {message}", messageType);
		#endif
		}

		public static void LogMessage(object obj, string methodName, string message, ELogMessageType messageType = ELogMessageType.Default)
		{
		#if UNITY_ENGINE
			var resultMessage = $"[{obj.GetType().Name}] [{methodName}] {message}";
            switch (messageType)
            {
                case ELogMessageType.Default:
                    Debug.Log(resultMessage);
                    break;
                case ELogMessageType.Info:
                    Debug.Log(resultMessage);
                    break;
                case ELogMessageType.Warning:
                    Debug.LogWarning(resultMessage);
                    break;
                case ELogMessageType.Error:
                    Debug.LogError(resultMessage);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(messageType), messageType, null);
            }
		#else
			LogMessage($"[{obj.GetType().Name}] [{methodName}] {message}", messageType);
		#endif
		}

		public static void LogObjectMessage(this object obj, string methodName, string message, ELogMessageType messageType = ELogMessageType.Default)
		{
		#if UNITY_ENGINE
			var resultMessage = $"[{obj.GetType().Name}] [{methodName}] {message}";
            switch (messageType)
            {
                case ELogMessageType.Default:
                    Debug.Log(resultMessage);
                    break;
                case ELogMessageType.Info:
                    Debug.Log(resultMessage);
                    break;
                case ELogMessageType.Warning:
                    Debug.LogWarning(resultMessage);
                    break;
                case ELogMessageType.Error:
                    Debug.LogError(resultMessage);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(messageType), messageType, null);
            }
		#else
			LogMessage($"[{obj.GetType().Name}] [{methodName}] {message}", messageType);
		#endif
		}
	}
}