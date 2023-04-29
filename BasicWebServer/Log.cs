namespace Kevahu;

public static class Logger
{
    private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);
    public static bool ShowDebug { get; set; } = true;
    public static bool ShowVerbose { get; set; } = true;

    public static async Task Log(string text, ConsoleColor color = ConsoleColor.White)
    {
        await _semaphore.WaitAsync();
        foreach (string line in text.Split("\n"))
        {
            Console.BackgroundColor = ConsoleColor.White;
            Console.ForegroundColor = ConsoleColor.Black;
            Console.Write("[" + DateTime.Now.ToShortDateString() + " | " + DateTime.Now.ToShortTimeString() + "]");
            Console.ResetColor();
            Console.ForegroundColor = color;
            Console.WriteLine(" " + line);
            Console.ResetColor();
        }

        _semaphore.Release();
    }

    public static void Critical(string text)
    {
        Log($"[CRIT] {text}", ConsoleColor.DarkRed);
    }

    public static void Error(string text)
    {
        Log($"[ERRO] {text}", ConsoleColor.Red);
    }

    public static void Error(string text, Exception ex)
    {
        Log($"[ERRO] {text}" + ": " + ex, ConsoleColor.Red);
    }

    public static void Warning(string text)
    {
        Log($"[WARN] {text}", ConsoleColor.Yellow);
    }

    public static void Info(string text)
    {
        Log($"[INFO] {text}");
    }

    public static void Verbose(string text)
    {
        if (ShowVerbose)
            Log($"[VERB] {text}", ConsoleColor.Gray);
    }

    public static void Debug(string text)
    {
        if (ShowDebug)
            Log($"[DEBU] {text}", ConsoleColor.Blue);
    }

    public static void Success(string text)
    {
        Log($"[SUCC] {text}", ConsoleColor.Green);
    }

    public static void Fail(string text)
    {
        Log($"[FAIL] {text}", ConsoleColor.Magenta);
    }
}