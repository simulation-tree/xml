# XML

[![Test](https://github.com/simulation-tree/xml/actions/workflows/test.yml/badge.svg)](https://github.com/simulation-tree/xml/actions/workflows/test.yml)

Library for XML serialization.

### Example

XML is supported through the `XMLNode` type, which can be created from either a byte or a char array.
Each node has a name, content, a list of attributes, and a list of children:
```cs
byte[] xmlData = File.ReadAllBytes("solution.csproj");
using XMLNode project = new(xmlData);
XMLAttribute sdk = project["Sdk"];
sdk.Value = "Simulation.NET.Sdk";
project.TryGetFirst("PropertyGroup", out XMLNode propertyGroup);
project.TryGetFirst("TargetFramework", out XMLNode tfm);
tfm.Content = "net9.0";
File.WriteAllText("solution.csproj", project.ToString());
```