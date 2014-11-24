
## Reporting issues

The GitHub issue list is strictly for reporting bugs.  Bug reports should clearly explain the issue, describe the difference between expected and actual behavior, and provide steps to reproduce in an empty project. Including a [Gist](https://gist.github.com/) of `/resizer.debug` is also recommended.

[Follow this guide if you need support](http://imageresizing.net/support). Most embarrassing mistakes can be avoided by visiting ImageResizer’s self-diagnostics page at `/resizer.debug.ashx`. If you get a 404 for that URL, you’ve messed up Web.config somehow.

[StackOverflow has over 260 questions tagged [imageresizer]](http://stackoverflow.com/questions/tagged/imageresizer), so that’s a good place to search if the [FAQ](http://imageresizing.net/docs/faq) or [Troubleshooting guide](http://imageresizing.net/docs/troubleshoot) didn’t help.

## Discussing enhancements

All new code comes with long-term maintenance cost. We use [UserVoice](http://resizer.uservoice.com/forums/108373-image-resizer), support interactions, and surveys to get a better picture of which expensive features are most wanted. Deleting code to add functionality is always best ;)

ImageResizer’s API allows you to create your own plugins; this is typically the best way to extend functionality, and is nearly always the path we take when adding features.

If you’re interested in having (non-trivial) code added to the official repository, you should probably discuss your plan with us on [![Gitter](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/imazen/resizer?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge).

Even if they belong in a separate repository, [you should let us know about any plugins you build so we can link to them on the Community page](http://imageresizing.net/docs/community).

## Contributing code

All development [happens on the official GitHub repository](https://github.com/imazen/resizer).

We use submodules - clone with `git clone -b develop --recursive https://github.com/imazen/resizer` or run `git submodule update --init --recursive` afterwards. Many Git GUIs offer a corresponding checkbox.

All work should occur in branches forked from `develop` &mdash; not `master`.

i.e, `git checkout -b features/my-happy-feature develop`

Pull requests should be made against the ‘develop’ branch. We accept nearly all pull requests after 1-2 rounds of code review and feedback. 

Providing good xUnit tests (and reading this document) typically results in immediate acceptance of your pull request. 


### Contributing code - legally

Since we use the Apache license, you will need to release your commits under the corresponding Contributor Agreement. The easiest way is by [using clahub.com](https://www.clahub.com/agreements/imazen/resizer), which takes seconds. 

If for some reason that doesn't work for you, there are other options:

Small commits can simply include this statement: 

        I release these contributions under the terms of the Imazen Contributor Agreement V1.0 
        (http://imageresizing.net/licenses/contribute), the terms of which I accept and agree.
        - [Name] [Date]

Frequent or heavy commiters should [sign and date the actual agreement](http://imageresizing.net/licenses/contribute), which only needs to be done once. Digital signatures or scanned copies are accepted; send them to support@imageresizing.net. 

If you are introducing any new dependencies, those licenses may obligate you to create a notice.txt or license.txt file in your plugin folder.


## Folders

* Tests - Put unit/integration tests, playgrounds, and benchmark harnesses here.
* Tools - Build tools, docs generators, installers
* submodules - All external submodules
* Samples - Demo or examples for public consumption
* Core - Main assembly
* Contrib - Community-supported plugins
* Plugins - All plugins
* Plugins/Libs - Plugin dependencies we haven’t transitioned to NuGet yet.


## Design tips  

* Fail fast. Expensive failure paths are a vulnerability.
* Latency is everything. Performance is rarely intuitive. Measure with artificially slow I/O.
* Never mix I/O and bitmap work. Perform *ALL* I/O before you decompress your first image, or you could monopolize RAM for an indefinite duration.
* System.Drawing uses process-wide locks. 
* System.Drawing is not garbage collected. using(){} everything.
* Trust no data. A bitmap or GIF may also be a valid javascript file and PDF. 
* OutputBuffer can behave in a variety of ways. Protect yourself from slow clients. 
* Leverage native static file serving when possible; it can use event loops.
* Read [Image Resizing Pitfalls](http://www.nathanaeljones.com/blog/2009/20-image-resizing-pitfalls)
* Uncached database queries will kill you. Redis may not. 
* Immutable URLs can prevent much agony. Cache invalidation is optional if you control the URLs. 

## Coding style

* Keep it [DRY](http://en.wikipedia.org/wiki/Don%27t_repeat_yourself)
* [YAGNI](http://en.wikipedia.org/wiki/You_aren%27t_gonna_need_it), particularly when it comes to configuration options.
* Narrow interfaces are good. Respect [the law of demeter](http://en.wikipedia.org/wiki/Law_of_Demeter). 
* Avoid dependency on System.Drawing and System.Web types. 
* Use `var` and type-inferred arrays whenever possible.
* Write self-documenting code
* Prefer ternary operator unless conditions are nested. Avoid conditionals


### Whitespace guidelines

* 4 spaces per indentation level. Save as spaces, not tabs.
* Use spaces around operators, after commas, colons and semicolons, around { and before }. 
* Never leave trailing whitespace. 
* No spaces after (, [ or before ], ).
* No spaces after !.
* Optimize for signal-to-noise ratio. Opening brackets on same line. Single-line properties like `bool Enabled { get; set; }` are encouraged.
* [Use K&R style braces](http://en.wikipedia.org/wiki/Indent_style#K.26R_style)

### File encoding

*  UTF-8 ALL THE THINGS.
*  Git should normalize line endings for you (via .gitattributes).  VS project files, .sln, and .txt files use CRLF.  All other text files use LF.

## Branches

Always base any work on the `develop` branch. Fork to a `features/enhancement-goal` branch for enhancements, or a `bugfixes/issue-name` branch for bug-fixes.

Submit pull requests against the ‘develop’ branch. Even if you have push access, pull requests enable code review - and valuable feedback. 

If there have been conflicting changes in `develop`, we suggest rebasing your feature or bugfix branches, and either force-pushing to your existing feature branch, or resubmitting the pull request as a different branch.

`master` should roughly represent the latest stable release. We permit documentation and non-code changes on any branch, but only if the corresponding pull request has passed CI. 

`support/v3` and `support/v2` track the latest revision of the corresponding major version, 

Published DLL versions should have corresponding git tags, such as `resizer3-1-5.463`. DLLs should also have a assembly metadata attribute “Commit”, which can be viewed via `/resizer.debug`.

Unless you’re creating an emergency hotfix for a production server, pull requests should always be against `develop`.

## Commits should...

* Be single-purpose; implement one feature or change at a time.
* Be written in the present tense.
* Include a corresponding unit test - if they are an enhancement.
* Follow a corresponding failing test, if they are bug fixes.
*   ```
    Clearly explain in one line what the commit is about

    Describe the problem the commit solves. Justify why you chose
    the particular solution. Reference the issue number if not
    addressed in the title.  ```
* Compile successfully.
* Never contain binaries or images, these will grind the repository to a halt. Think carefully about adding any file > 20KiB. 


## Tagging commit messages

* Prefix your commit messages with the name of the affected area. This allows easy visual scanning for changes related to a given area of functionality.
    * Core: Add pipeline.fakeExtensions setting
    * SimpleFilters: Add black and white filter
    * Builder: Add nuget upload support
    * Docs: Add CONTRIBUTING.md draft
* Fixing a bug in a published version - always reference the GitHub issue number. Consistency is very important, and allows us to create accurate release notes.
    * Core Bugfix: Prevent NullReferenceException... Fixes #63
* Fixing a bug that was never released - use phrasing “Fix recent bug”
    * Core: Fix recent bug ...

