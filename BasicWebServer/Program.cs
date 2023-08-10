namespace BasicWebServer;

using System.Reflection;
using System.Text;
using Kevahu;
using Microsoft.AspNetCore.StaticFiles;

public static class Program
{
    // All paths the website will respond to
    private static readonly List<string> WwwrootIndex = new List<string>();

    private static readonly string WwwrootPath = "./wwwroot";

    // All members of the WWW class
    private static List<MemberInfo> _members = new List<MemberInfo>();

    public static void Main()
    {
        Logger.Info("Initializing...");

        // Inspect WWW class and add to website
        _members = typeof(Www).GetMembers().Where(m =>
        {
            dynamic member = m;
            if (member.Attributes != null && (member.Attributes.ToString() == "None" ||
                                              member.Attributes.ToString().Contains("Static") &&
                                              !member.Attributes.ToString().Contains("SpecialName")))
                return true;
            return false;
        }).ToList();

        // Get all resources
        ReloadResources();

        // Build website
        WebApplication app = WebApplication.CreateBuilder().Build();

        // Responds to all requests
        app.MapGet("{**catchAll}", MapGetPost);
        app.MapPost("{**catchAll}", MapGetPost);

        // Register signal handler for termination signal
        CancellationTokenSource cts = new CancellationTokenSource();
        Console.CancelKeyPress += (s, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
            Logger.Info("Terminating...");
        };

        // Run the website
        Logger.Info("Starting Kestrel...");
        try
        {
            app.RunAsync(cts.Token).Wait(cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Gracefully handle cancellation
            Logger.Info("Terminated.");
        }
        catch (Exception ex)
        {
            Logger.Error("Critical ERROR! Terminating...\n" + ex);
        }
    }

    private static async Task MapGetPost(HttpContext httpContext)
    {
        DateTime start = DateTime.Now;
        Logger.Info("Incoming request for: " + httpContext.Request.Path.ToString());

        // Allow all origins
        httpContext.Response.Headers.AccessControlAllowOrigin = "*";

        // Check if path is known
        if (WwwrootIndex.Any(w => w.ToLower() == httpContext.Request.Path.ToString().ToLower()))
        {
            // Allow POST only for the WWW class
            if (httpContext.Request.Method == "POST" && _members.All(m =>
                    "/" + m.Name.ToLower() != httpContext.Request.Path.ToString().ToLower()))
            {
                httpContext.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
                Logger.Warning("Method not allowed");
            }
            else
            {
                // Handle request
                await MapHandler(httpContext.Request.Path, httpContext);
            }
        }
        // If path is not known
        else
        {
            Logger.Warning("Resource is not indexed, reloading resources...");

            // Check if new files were added
            ReloadResources();

            // Check if path is known
            if (WwwrootIndex.Any(w =>
                    string.Equals(w, httpContext.Request.Path.ToString(), StringComparison.CurrentCultureIgnoreCase)))
            {
                // Handle request
                await MapHandler(httpContext.Request.Path, httpContext);
            }
            // if path is not known
            else
            {
                Logger.Fail("ERROR 404: Path not found: " + httpContext.Request.Path.ToString());

                // Respond with 404 Not Found
                httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
            }
        }

        Logger.Verbose($"Request handled in {(DateTime.Now - start).TotalMilliseconds}ms");
    }

    private static async Task MapHandler(string index, HttpContext httpContext)
    {
        // If requested path is inside the WWW class
        if (_members.Any(m => "/" + m.Name.ToLower() == index.ToLower()))
        {
            // Get member from the WWW class
            dynamic? member = _members.Find(m => "/" + m.Name.ToLower() == index.ToLower());

            try
            {
                // Try to get the value of the member. Only works when member has a value (properties, fields,...)
                object? value = member?.GetValue(null);

                // Write value to the response
                if (value != null)
                {
                    await httpContext.Response.Body.WriteAsync(
                        Encoding.UTF8.GetBytes(value.ToString() ?? string.Empty));
                    Logger.Success("Request handled successfully");
                }
                else
                {
                    Logger.Success("Request handled successfully (empty)");
                }
            }
            catch
            {
                // When you can't extract a value it's probably a method. So, get the method from the WWW class
                MethodInfo? method =
                    typeof(Www).GetMethod(_members.Find(m => "/" + m.Name.ToLower() == index.ToLower())?.Name ??
                                          string.Empty);

                // If a method is found (and actually is a method)
                if (method != null)
                {
                    // Get all parameters of the method and prepare a values list
                    ParameterInfo[] parameters = method.GetParameters();
                    List<object?> parameterValues = new List<object?>();

                    // Go over all parameters
                    parameters.ToList().ForEach(p =>
                    {
                        // If a parameter requests the HttpContext
                        if (p.ParameterType == typeof(HttpContext))
                        {
                            parameterValues.Add(httpContext);
                            return;
                        }

                        // Try to get a value out of the request query
                        string? result = httpContext.Request.Query[p.Name ?? string.Empty];

                        if (result == null) result = httpContext.Request.Form[p.Name ?? string.Empty];

                        // Parameter is found in the request query
                        if (result != null)
                            try
                            {
                                // Convert and add the value to the values list
                                parameterValues.Add(Convert.ChangeType(result, p.ParameterType));
                            }
                            catch
                            {
                                // When something goes wrong trying to convert the value, add null
                                parameterValues.Add(null);
                            }
                        // Parameter is not found in the request query
                        else if (p.HasDefaultValue)
                            // If parameter has a default value, use it
                            parameterValues.Add(p.DefaultValue);
                        else
                            // Else, add null
                            parameterValues.Add(null);
                    });
                    // Try to invoke (execute) the method with the parameters (if there are any)
                    // No try catch block so errors would be shown on console and on the website (Easier debugging)
                    object? value = method.Invoke(null, parameterValues.ToArray());

                    // If the method returns a value
                    if (value != null)
                    {
                        // Write the value to the response
                        await httpContext.Response.Body.WriteAsync(
                            Encoding.UTF8.GetBytes(value.ToString() ?? string.Empty));
                        Logger.Success("Request handled successfully");
                    }
                    else
                    {
                        Logger.Success("Request handled successfully (empty)");
                    }
                }
            }
        }
        // If requested path is /
        else if (index == "/")
        {
            try
            {
                // Set response to html
                httpContext.Response.ContentType = "text/html";

                // Write to body of response
                await using FileStream fileStream = new FileStream("./wwwroot/index.html", FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                await fileStream.CopyToAsync(httpContext.Response.Body);
                Logger.Success("Request handled successfully");
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to load resource", ex);
                ReloadResources();
                httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
                httpContext.Response.Body.Write(Encoding.UTF8.GetBytes(ex.ToString()));
            }
        }
        // If requested path does not contain . (no file extension), default to .html
        else if (!index.Contains('.'))
        {
            try
            {
                httpContext.Response.ContentType = "text/html";
                await using FileStream fileStream = new FileStream($"./wwwroot{index}.html", FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                await fileStream.CopyToAsync(httpContext.Response.Body);
                Logger.Success("Request handled successfully");
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to load resource", ex);
                ReloadResources();
                httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
                httpContext.Response.Body.Write(Encoding.UTF8.GetBytes(ex.ToString()));
            }
        }
        // Else, just respond with the requested file
        else
        {
            try
            {
                // Try to get MIME Type 
                string? contentType;
                if (new FileExtensionContentTypeProvider().TryGetContentType(index, out contentType))
                    httpContext.Response.ContentType = contentType;
                await using FileStream fileStream = new FileStream($"./wwwroot{index}", FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                await fileStream.CopyToAsync(httpContext.Response.Body);
                Logger.Success("Request handled successfully");
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to load resource", ex);
                ReloadResources();
                httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
            }
        }
    }

    private static void ReloadResources()
    {
        // Clear current list of paths
        WwwrootIndex.Clear();

        // Index all resources
        WwwrootIndex.Add("/");
        WwwrootIndex.AddRange(_members.Select(m => "/" + m.Name));
        WwwrootIndexer(WwwrootPath);
        Logger.Verbose("Resources loaded:");
        Logger.Verbose(string.Join('\n', WwwrootIndex));
    }

    private static void WwwrootIndexer(string path)
    {
        try
        {
            DirectoryInfo pathDirectory = new DirectoryInfo(path);

            // Select all the files from the current directory and extract the names, format them and add them to the WwwrootIndex
            WwwrootIndex.AddRange(pathDirectory.GetFiles().SelectMany(file =>
            {
                // Making sure HTML files get added twice (once with .html, once without)
                string name = file.FullName.Substring(file.FullName.IndexOf("wwwroot", StringComparison.Ordinal) + 7)
                    .Replace('\\', '/');
                List<string> names = new List<string>();
                names.Add(name);
                if (name.ToLower().EndsWith(".html"))
                    names.Add(name.Replace(".html", "", StringComparison.CurrentCultureIgnoreCase));
                return names;
            }));


            // Foreach directory in the root directory, run this method
            pathDirectory.GetDirectories().ToList().ForEach(dir => WwwrootIndexer(dir.FullName));
        }
        catch (Exception ex)
        {
            Logger.Error("Failed to index resource", ex);
        }
    }
}
