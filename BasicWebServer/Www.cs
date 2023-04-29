namespace BasicWebServer;

public static class Www
{
    // All public static members will be accessible as an API (GET & POST with form support)
    // FormTest => /FormTest?name=...
    // The HttpContext gets automatically filled in, but is not required
    public static string FormTest(string name, int age, DateTime appointment, HttpContext context)
    {
        context.Response.ContentType = "text/plain";
        return $"{name} (age: {age}) made an appointment for {appointment.ToShortDateString()}.";
    }
}