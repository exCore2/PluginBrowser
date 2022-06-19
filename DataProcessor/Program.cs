using System.CommandLine;

var rootCommand = new RootCommand();
var generateDataCommand = new Command("build");
generateDataCommand.SetHandler(Methods.GenerateNewData);
rootCommand.AddCommand(generateDataCommand);
var postUpdatesCommand = new Command("post-updates");
var newFileArg = new Argument<FileInfo>("new-file").ExistingOnly();
var oldFileArg = new Argument<FileInfo>("old-file").ExistingOnly();
var callbackUriArg = new Argument<Uri>("callback-uri");
postUpdatesCommand.AddArgument(newFileArg);
postUpdatesCommand.AddArgument(oldFileArg);
postUpdatesCommand.AddArgument(callbackUriArg);
postUpdatesCommand.SetHandler(Methods.PostUpdateNotes, newFileArg, oldFileArg, callbackUriArg);
rootCommand.AddCommand(postUpdatesCommand);
var exitCode = await rootCommand.InvokeAsync(args);
Environment.Exit(exitCode);

record InputModel(List<PluginInfo> Plugins);

record PluginInfo(string Name, string OriginalAuthor, List<RepositoryInfo> Repositories, string Description, string? EndorsedAuthor);

record RepositoryInfo(string Author, string Name);
