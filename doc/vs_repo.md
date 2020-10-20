# Special instructions for consuming within the DevDiv `VS` repo

Add this import to your project:

```xml
<Import Project="$(SrcRoot)\Tests\vssdktestfx.targets" />
```

Remove any references to an App.config or App.config.tt file from your test project
since this is now generated during the build automatically.
