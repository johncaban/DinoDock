using DinoDock.Models;

namespace DinoDock.ViewModels;

/// <summary>A launcher card: the app plus whether it was the one used last.</summary>
public sealed class AppCardViewModel : ObservableObject
{
    private bool _isLastUsed;

    public AppCardViewModel(MiniApp app) => App = app;

    public MiniApp App { get; }

    public bool IsLastUsed
    {
        get => _isLastUsed;
        set => SetProperty(ref _isLastUsed, value);
    }
}

public sealed class LauncherViewModel : ObservableObject
{
    public LauncherViewModel(IEnumerable<MiniApp> apps, Action<MiniApp> openApp)
    {
        Apps = apps.Select(a => new AppCardViewModel(a)).ToList();
        OpenCommand = new RelayCommand(p =>
        {
            if (p is AppCardViewModel card)
                openApp(card.App);
        });
    }

    public IReadOnlyList<AppCardViewModel> Apps { get; }

    public RelayCommand OpenCommand { get; }

    public void SetLastUsed(string? appId)
    {
        foreach (var card in Apps)
            card.IsLastUsed = string.Equals(card.App.Id, appId, StringComparison.OrdinalIgnoreCase);
    }
}
