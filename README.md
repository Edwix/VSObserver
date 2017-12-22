# VSObserver

VSObserver is a software allow to check the VS variables on train network.

# Version
1.2.7

# Tech

* C# language with WPF (Windows Presentation Form) technolgies
* VS libraries
* U-test libraries
* SQLite dotnet libraries

# Brainstorming

Please put any idea, even though it is crazy.

* Put a color on path when it is a variable mapped

# Todo's

 - Mapping of variables : Removes the duplicates
 - Set timestamp in long value
 - Get the variables state (forced or not)
 - Forcing of the variables
 
# Developper instructions

## using Visual Studio Express 2012, delivered with V&V Tools BSL1.0

 * Open Visual Studio Express 2012
 * Start solution located in .\sourceCode\VSObserver\VSObserver.sln
 * Update Nuget to lastest version (Tools > extensions and updates)
   * click on updates
   * Click on Visual studio gallery
   * Update NuGet package manager
   * Restart project
 * Start package manager console (Tools > packager manager > packager manager Console)
 * Copy and paste following commands :
   > Install-Package LibGit2Sharp -Version 0.24.0
   > Install-Package System.Data.SQLite -Version 1.0.106
   > Install-Package Log4Net -Version 2.0.8
 * Generate project. It should be successfull.

## Closing Github issues using keywords

You can include keywords in your pull request titles and descriptions, as well as commit messages, to automatically close issues in GitHub.

When a pull request or commit references a keyword and issue number, it creates an association between the pull request and the issue. When the pull request is merged into your repository's default branch, the corresponding issue is automatically closed.

The following keywords, followed by an issue number, will close the issue:
 * close
 * closes
 * closed
 * fix
 * fixes
 * fixed
 * resolve
 * resolves
 * resolved

For example, to close an issue numbered 123, you could use the phrases "Closes #123" in your pull request description or commit message. Once the branch is merged into the default branch, the issue will close.
