using System;

public static class RecoverableExecution
{
    public static bool Run(string context, Action action, Action cleanup = null, string userMessage = null)
    {
        try
        {
            action?.Invoke();
            return true;
        }
        catch (Exception ex)
        {
            GlobalErrorReporter.ReportRecoverableError(context, ex, userMessage);
            return false;
        }
        finally
        {
            if (cleanup != null)
            {
                try
                {
                    cleanup.Invoke();
                }
                catch (Exception cleanupEx)
                {
                    GlobalErrorReporter.ReportRecoverableError($"{context} cleanup", cleanupEx, userMessage);
                }
            }
        }
    }
}
