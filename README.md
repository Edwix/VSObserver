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
 * Generate project. It should be successfull.


