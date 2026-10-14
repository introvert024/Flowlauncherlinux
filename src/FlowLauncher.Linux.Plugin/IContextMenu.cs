namespace FlowLauncher.Linux.Plugin;

public interface IContextMenu
{
    List<Result> LoadContextMenus(Result selectedResult);
}
