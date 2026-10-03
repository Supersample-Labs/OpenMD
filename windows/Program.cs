namespace OpenMD;
internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new EditorWindow(args.FirstOrDefault()));
    }
}
