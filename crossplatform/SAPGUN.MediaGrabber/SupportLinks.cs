using Avalonia.Interactivity;

namespace SapgunMediaGrabber;

public partial class MainWindow
{
    const string GitHubRepoUrl = "https://github.com/sapgun/SAPGUN-Media-Grabber";

    void Star_Click(object? sender, RoutedEventArgs e) => OpenTarget(GitHubRepoUrl);
}
