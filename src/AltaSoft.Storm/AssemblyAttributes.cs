// Used to indicate to the compiler that the .locals init flag should not be set in method headers.

using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("AltaSoft.Storm.MsSql")]
[assembly: InternalsVisibleTo("AltaSoft.Storm.TestApp")]
[assembly: InternalsVisibleTo("AltaSoft.Storm.Tests")]
[module: System.Runtime.CompilerServices.SkipLocalsInit]
