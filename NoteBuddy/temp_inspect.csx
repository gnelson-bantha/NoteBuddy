using System;
using System.Reflection;
using System.Linq;

var asm = Assembly.LoadFrom(@"C:\Users\georg\.nuget\packages\mudblazor\9.1.0\lib\net10.0\MudBlazor.dll");
var types = asm.GetTypes().Where(t => t.Name.StartsWith("MudFileUpload")).ToList();
foreach (var t in types)
{
    Console.WriteLine($"Type: {t.FullName}");
    var renderFragments = t.GetProperties()
        .Where(p => p.PropertyType.Name.Contains("RenderFragment") || p.Name.Contains("Template") || p.Name.Contains("Content") || p.Name.Contains("Activator"))
        .Select(p => $"  {p.Name} : {p.PropertyType.Name}");
    foreach (var rf in renderFragments) Console.WriteLine(rf);
}
