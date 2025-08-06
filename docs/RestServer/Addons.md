## RestServer Plugin
In this section of you will learn how to make a `neo-cli` plugin that integrates with `RestServer`
plugin. Lets take a look at [Example Plugin](/examples/RestServerPlugin).

- No reference to `RestServer` is required.
- Requires DotNet 7.0

## Folder Structure
```bash
Project
├── Controllers
│   └── ExampleController.cs
├── ExamplePlugin.cs
├── ExamplePlugin.csproj
├── Exceptions
│   └── CustomException.cs
└── Models
    └── ErrorModel.cs
```
The only thing that is important here is the `controllers` folder. This folder is required for the `RestServer`
plugin to register the controllers in its web server. This location is where you put all your controllers.

## Controllers
The `controller` class is the same as ASP.Net Core's. Controllers must have their attribute set
as `[ApiController]` and inherent from `ControllerBase`.

## Swagger Controller
A `Swagger` controller uses special attributes that are set on your controller's class.

**Controller Class Attributes**
- `[Produces(MediaTypeNames.Application.Json)]` (_Required_)
- `[Consumes(MediaTypeNames.Application.Json)]` (_Required_)
- `[ApiExplorerSettings(GroupName = "v1")]`
  - **GroupName** - _is which version of the API you are targeting._
- `[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorModel))]` (_Required_)
  - **Type** - _Must have a base class of [error](#error-class)._

## Error Class
Needs to be the same as `RestServer` of else there will be some inconsistencies
with end users not knowing which type to use. This class can be `public` or `internal`.
Properties `Code`, `Name` and `Message` values can be whatever you desire.

**Model**
```csharp
public class ErrorModel
{
    public int Code { get; set; };
    public string Name { get; set; };
    public string Message { get; set; };
}
```

## Controller Actions
Controller actions need to have special attributes as well as code comments.

- `[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))]`

HTTP status code `200 (OK)` is required with return type defined. You can use more than one attribute. One per HTTP status code.

### Action Example
```csharp
[HttpGet("contracts/{hash:required}/sayHello", Name = "GetSayHello")]
[ProducesResponseType(StatusCodes.Status204NoContent)]
[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))]
public IActionResult GetSayHello(
    [FromRoute(Name = "hash")]
    UInt160 scripthash)
{
    if (scripthash == UInt160.Zero)
        return NoContent();
    return Ok($"Hello, {scripthash}");
}
```
Notice that the _above_ example also returns with HTTP status code of `204 No Content`.
This action `route` also extends the `contracts` API. Adding method `sayHello`. Routes
can be what you like as well. But if you want to extend on any existing controller you
must use existing routes paths.

### Path(s)
- `/api/v1/contracts/`
- `/api/v1/ledger/`
- `/api/v1/node/`
- `/api/v1/tokens`
- `/api/v1/Utils/`

### Excluded Path(s)
- `/api/v1/wallet/`

_for security reasons_.

### Code Comments for Swagger
```csharp
/// <summary>
/// 
/// </summary>
/// <param name="" example=""></param>
/// <returns></returns>
/// <response code="200">Successful</response>
/// <response code="400">An error occurred. See Response for details.</response>
```

Also note that you need to have `GenerateDocumentationFile` enabled in your
`.csproj` file. The `xml` file that is generated; in our case would be `RestServerPlugin.xml`.
This file gets put in same directory `Plugins/RestServerPlugin/` which is in the root of `neo-node`
executable folder. Where you will see `neo-cli.exe`.

File `RestServerPlugin.xml` will get added to `Swagger` automatically by the `RestServer`
plugin.
