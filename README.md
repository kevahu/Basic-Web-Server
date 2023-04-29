# Basic Web Server
This is a basic web server written in C# that can be used as a starting point for building more complex web applications. The server uses the .NET Core framework and can be run on Windows, macOS, and Linux.

## Getting Started
1. Clone or download the repository.
2. Open the project in Visual Studio or Visual Studio Code.
3. Build the project to restore dependencies and compile the code.
4. Run the server by executing the BasicWebServer binary, or by pressing F5 in Visual Studio.
5. Open a web browser and navigate to http://0.0.0.0:80 to see the template website.

Everything put in the `wwwroot` directory will be publicly accessible.
The current template includes a basic HTML, CSS & JS setup.
## Configuration
By default the server URL is set to `http://0.0.0.0` and port `80`. The URL of the server can be changed ing the `appsettings.json` file:
```json
{
  "Kestrel": {
    "Endpoints": {
      "LocalUrl": {
        "Url": "http://0.0.0.0:80"
      }
    }
  }
}
```
## API
The server supports defining API endpoints as static members in the Www class. These members can be accessed via HTTP GET or POST requests, with support for form data.

For example, the following method can be called by sending a GET request to `/FormTest?name=John&age=30&appointment=2023-05-01T12:00:00Z`:
```cs
public static string FormTest(string name, int age, DateTime appointment, HttpContext context)
{
    context.Response.ContentType = "text/plain";
    return $"{name} (age: {age}) made an appointment for {appointment.ToShortDateString()}.";
}
```
Note that the HttpContext parameter is optional and will be automatically filled in by the server if present.