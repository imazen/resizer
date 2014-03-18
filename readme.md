# Git repository maintenance complete

In order to transition this from a private to public repository, we had to modify the history and delete copyrighted and/or trademarked images that were used for past regression testing. We were also able to cut 90% of the repository size by removing outdated binaries. 

If you previously had access to the private repository, you will need to delete your cloned copy and re-clone it again. Any changes you didn't submit pull requests for prior to the maintenance will need to be copied and pasted and re-commited.


Thanks,  
Nathanael Jones  
support@imageresizing.net


## ImageResizer

### Notes for developers working on the code base

* Make sure you have NuGet 2.7 installed with package restore enabled
* If you have Visual Studio 2008/2010, don't try to open the unit tests (they require .NET 4.5)

We use Visual Studio 2012 & 2013 internally for development

This repository contains all ImageResizer core and plugin code, with history back to V1.

