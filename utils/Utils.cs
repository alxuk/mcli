namespace mcli.utils;

public static class Utils
{
    public static readonly string HomeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    public static readonly string RootDir = Path.Combine(HomeDir, ".mcli");
}