This ImageResizer plugin requires Ghostscript to be installed on the machine.

Ghostscript.NET locates the native library via the Windows registry automatically.
As a fallback, you can manually place gsdll32.dll (32-bit) and/or gsdll64.dll (64-bit)
in the application /bin directory.

Install Ghostscript via one of:
  winget install ArtifexSoftware.GhostScript
  scoop install ghostscript
  https://ghostscript.com/releases/gsdnld.html
