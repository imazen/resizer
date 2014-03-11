# Git repository maintenance - please read!

**Please create pull requests for all changes you wish to preserve**. We will be deleting and re-creating this repository to remove copyrighted material that cannot be published, and to reduce its size. This should reduce the repository from ~200MB to ~16MB, and will allow us to change this from a private to a public repository. All 1,630 commits will be retained; they will just be missing certain binary files that are no longer useful.

After this occurs, you will need to delete your local copy of the repository and re-clone it. 

**You will have to copy+paste any changes you've made if you don't send us a pull request by March 13th.**


Thanks,  
Nathanael Jones  
support@imageresizing.net


## ImageResizer

### Notes for developers working on the code base

* Make sure you have NuGet 2.7 installed with package restore enabled
* If you have Visual Studio 2008/2010, don't try to open the unit tests (they require .NET 4.5)


We use Visual Studio 2012 & 2013 internally for development


This repository contains all ImageResizer core and plugin code, with history back to V1.

