using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public sealed class GlobalErrorReporter : MonoBehaviour
{
    private const string StabilityPrefix = "[Stability]";
    private const float MessageCooldownSeconds = 1.25f;

    private static GlobalErrorReporter instance;

    private readonly Queue<string> pendingMessages = new Queue<string>();
    private readonly object queueLock = new object();

    private bool isSubscribed;
    private float lastShownMessageTime = float.NegativeInfinity;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        EnsureExists();
    }

    public static void ReportRecoverableError(string context, Exception ex, string userMessage = null)
    {
        EnsureExists();

        string safeContext = string.IsNullOrWhiteSpace(context) ? "Recoverable runtime error" : context;
        Debug.LogError($"{StabilityPrefix} {safeContext}: {ex}");
        QueueUserMessage(userMessage ?? "A runtime error occurred. The game tried to continue.");
    }

    public static void ReportRecoverableMessage(string context, string technicalMessage, string userMessage = null, LogType logType = LogType.Warning)
    {
        EnsureExists();

        string safeContext = string.IsNullOrWhiteSpace(context) ? "Recoverable runtime issue" : context;
        string safeTechnicalMessage = string.IsNullOrWhiteSpace(technicalMessage) ? "Unknown runtime issue." : technicalMessage;
        string formattedMessage = $"{StabilityPrefix} {safeContext}: {safeTechnicalMessage}";

        if (logType == LogType.Error || logType == LogType.Assert || logType == LogType.Exception)
        {
            Debug.LogError(formattedMessage);
        }
        else
        {
            Debug.LogWarning(formattedMessage);
        }

        QueueUserMessage(userMessage ?? "A runtime issue occurred. The game tried to continue.");
    }

    private static void QueueUserMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        EnsureExists();

        lock (instance.queueLock)
        {
            instance.pendingMessages.Enqueue(message);
        }
    }

    private static void EnsureExists()
    {
        if (instance != null)
        {
            return;
        }

        instance = FindFirstObjectByType<GlobalErrorReporter>();
        if (instance != null)
        {
            return;
        }

        GameObject reporterObject = new GameObject("GlobalErrorReporter");
        DontDestroyOnLoad(reporterObject);
        instance = reporterObject.AddComponent<GlobalErrorReporter>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        Subscribe();
    }

    private void OnEnable()
    {
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            Unsubscribe();
            instance = null;
        }
    }

    private void Update()
    {
        FlushPendingMessages();
    }

    private void Subscribe()
    {
        if (isSubscribed)
        {
            return;
        }

        Application.logMessageReceived += HandleLogMessage;
        AppDomain.CurrentDomain.UnhandledException += HandleUnhandledException;
        TaskScheduler.UnobservedTaskException += HandleUnobservedTaskException;
        isSubscribed = true;
    }

    private void Unsubscribe()
    {
        if (!isSubscribed)
        {
            return;
        }

        Application.logMessageReceived -= HandleLogMessage;
        AppDomain.CurrentDomain.UnhandledException -= HandleUnhandledException;
        TaskScheduler.UnobservedTaskException -= HandleUnobservedTaskException;
        isSubscribed = false;
    }

    private void HandleLogMessage(string condition, string stackTrace, LogType type)
    {
        if (string.IsNullOrWhiteSpace(condition) || condition.StartsWith(StabilityPrefix, StringComparison.Ordinal))
        {
            return;
        }

        if (type == LogType.Exception || type == LogType.Assert)
        {
            QueueUserMessage("A runtime error occurred. The game tried to continue.");
        }
    }

    private void HandleUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        QueueUserMessage("An unhandled error occurred. The game may be unstable until the current action finishes.");
    }

    private void HandleUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
    {
        if (ShouldIgnoreBackgroundTaskFailure(e.Exception))
        {
            Debug.LogWarning($"{StabilityPrefix} Ignored editor/background task failure: {e.Exception}");
            e.SetObserved();
            return;
        }

        QueueUserMessage("A background task failed. The game tried to continue.");
        e.SetObserved();
    }

    private static bool ShouldIgnoreBackgroundTaskFailure(AggregateException exception)
    {
        if (exception == null)
        {
            return false;
        }

#if UNITY_EDITOR
        string details = exception.ToString();

        if (details.IndexOf("Library/PackageCache", StringComparison.OrdinalIgnoreCase) >= 0 ||
            details.IndexOf("UnityEditor", StringComparison.OrdinalIgnoreCase) >= 0 ||
            details.IndexOf("MCPForUnity", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return true;
        }
#endif

        return false;
    }

    private void FlushPendingMessages()
    {
        if (Time.unscaledTime - lastShownMessageTime < MessageCooldownSeconds)
        {
            return;
        }

        if (GameNotificationManager.Instance == null)
        {
            return;
        }

        string nextMessage = null;
        lock (queueLock)
        {
            if (pendingMessages.Count > 0)
            {
                nextMessage = pendingMessages.Dequeue();
            }
        }

        if (string.IsNullOrWhiteSpace(nextMessage))
        {
            return;
        }

        GameNotificationManager.Instance.ShowSystemMessage(nextMessage, false);
        lastShownMessageTime = Time.unscaledTime;
    }
}
