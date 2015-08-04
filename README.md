![alt Logo](https://raw.githubusercontent.com/wildbit/style-merge/master/style-merge%402x.png)
# StyleMerge
Simple CSS inlining, for email, for C# and other .net-based languages.

#### What's this for?

*StyleMerge* allows you to inline `<style>` blocks to `style=` attributes for email, this is necessary to ensure the broadest rendering support in email clients. It's the engine behind our style inlining for [Postmark Templates](http://blog.postmarkapp.com/post/125849089273/special-delivery-postmark-templates), and we're proud to provide it as Open Source to the .net community.

#### How to use this library:

```csharp
var sourceHtml = "<html><head><style>...</style></head><body>...</body></html>";
var processedHtml = StyleMerge.ProcessHtml(sourceHtml);
```

