#addin "Cake.FileHelpers"

///////////////////////////////////////////////////////////////////////////////
// ARGUMENTS
///////////////////////////////////////////////////////////////////////////////

var target = Argument<string>("target", "Default");
var configuration = Argument<string>("configuration", "Release");

//////////////////////////////////////////////////////////////////////
// EXTERNAL NUGET TOOLS
//////////////////////////////////////////////////////////////////////

#Tool "xunit.runner.console"

///////////////////////////////////////////////////////////////////////////////
// GLOBAL VARIABLES
///////////////////////////////////////////////////////////////////////////////

var projectName = "ServiceB";
var buildNumber = BuildSystem.IsRunningOnTeamCity ? EnvironmentVariable("BUILD_NUMBER") : "0.0.0.0";

var solutions = GetFiles("./**/*.sln");
var solutionPaths = solutions.Select(solution => solution.GetDirectory());

// Define directories.
var srcDir = Directory("./src");
var artifactsDir = Directory("./artifacts");
var testResultsDir = artifactsDir + Directory("test-results");
var nupkgDir = artifactsDir + Directory("nupkg");

var appConfig = srcDir + File("App.config.user");
var globalAssemblyFile = srcDir + File("GlobalAssemblyInfo.cs");

///////////////////////////////////////////////////////////////////////////////
// SETUP / TEARDOWN
///////////////////////////////////////////////////////////////////////////////

Setup(() =>
{
    Information("Sample A");
    Information("");
    Information("v{0}", buildNumber);
});

Teardown(() =>
{
    Information("Finished running tasks.");
});

//////////////////////////////////////////////////////////////////////
// PRIVATE TASKS
//////////////////////////////////////////////////////////////////////

Task("__Build")
    .IsDependentOn("__Clean")
    .IsDependentOn("__RestoreNugetPackages")
    .IsDependentOn("__UpdateAssemblyVersionInformation")
    .IsDependentOn("__BuildSolutions")
    .IsDependentOn("__RunTests")
    .IsDependentOn("__CreateNuGetPackages");

Task("__Clean")
    .Does(() =>
{
    CleanDirectories(new DirectoryPath[] {
        artifactsDir,
        testResultsDir,
        nupkgDir
    });

    foreach(var path in solutionPaths)
    {
        Information("Cleaning {0}", path);
        CleanDirectories(path + "/**/bin/" + configuration);
        CleanDirectories(path + "/**/obj/" + configuration);
    }
});

Task("__RestoreNugetPackages")
    .Does(() =>
{
    foreach(var solution in solutions)
    {
        Information("Restoring NuGet Packages for {0}", solution);
        NuGetRestore(solution);
    }
});

Task("__CreateNuGetPackages")
    .Does(() =>
{
});

Task("__UpdateAssemblyVersionInformation")
    .Does(() =>
{
    Information("Updating assembly version to {0}", buildNumber);

    CreateAssemblyInfo(globalAssemblyFile, new AssemblyInfoSettings {
        Version = buildNumber,
        FileVersion = buildNumber,
        Product = projectName,
        Description = projectName,
        Company = "Solutions",
        Copyright = "Copyright (c) " + DateTime.Now.Year
    });
});

Task("__BuildSolutions")
    .Does(() =>
{
    if (!FileExists(appConfig))
    {
        FileWriteText(appConfig, @"<appSettings></appSettings>");
    }

    foreach(var solution in solutions)
    {
        Information("Building {0}", solution);

        MSBuild(solution, settings =>
            settings
                .SetConfiguration(configuration)
                .WithProperty("TreatWarningsAsErrors", "true")
                .WithProperty("RunOctoPack", "true")
                .WithProperty("OctoPackPublishPackageToFileShare", MakeAbsolute(nupkgDir).ToString())
                .WithProperty("OctoPackPublishPackagesToTeamCity", "false")
                .UseToolVersion(MSBuildToolVersion.NET46)
                .SetVerbosity(Verbosity.Minimal)
                .SetNodeReuse(false));
    }
});

Task("__RunTests")
    .Does(() =>
{
    var settings = new XUnit2Settings {
        OutputDirectory = testResultsDir,
        XmlReportV1 = true
    };

    settings.ExcludeTrait("Category", new [] { "IntegrationTests" } );

    XUnit2("./src/**/bin/" + configuration + "/*.*Tests.dll", settings);
});

///////////////////////////////////////////////////////////////////////////////
// PRIMARY TARGETS
///////////////////////////////////////////////////////////////////////////////

Task("Default")
    .IsDependentOn("__Build");

///////////////////////////////////////////////////////////////////////////////
// EXECUTION
///////////////////////////////////////////////////////////////////////////////

RunTarget(target);