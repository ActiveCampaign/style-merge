# css-inliner
Simple CSS inlining, for email.


#### How to use this library:

```csharp
var sourceHtml = "<html><head><style>...</style></head><body>...</body></html>";
var processedHtml = CssInliner.ProcessHtml(sourceHtml);
```
