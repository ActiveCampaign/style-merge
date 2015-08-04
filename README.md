![alt Logo](https://raw.githubusercontent.com/wildbit/style-merge/master/style-merge%402x.png)
# StyleMerge
Simple CSS inlining, for email, for C# and other .net-based languages.

#### How to use this library:

```csharp
var sourceHtml = "<html><head><style>...</style></head><body>...</body></html>";
var processedHtml = StyleMerge.ProcessHtml(sourceHtml);
```
