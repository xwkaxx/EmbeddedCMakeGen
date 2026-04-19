using EmbeddedCMakeGen.Commands;

var dispatcher = new CommandDispatcher();
var exitCode = dispatcher.Dispatch(args);

return exitCode;
