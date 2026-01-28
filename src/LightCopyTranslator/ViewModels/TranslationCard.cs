using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace LightCopyTranslator.ViewModels;

internal sealed class TranslationCard : INotifyPropertyChanged
{
    private string _text = "";
    private bool _isLoading;

    public TranslationCard(string sourceName)
    {
        SourceName = sourceName;
    }

    public string SourceName { get; }

    public string Text
    {
        get => _text;
        private set
        {
            if (_text == value)
            {
                return;
            }

            _text = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DisplayText));
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (_isLoading == value)
            {
                return;
            }

            _isLoading = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DisplayText));
        }
    }

    public string DisplayText => IsLoading ? "翻译中..." : Text;

    public void SetLoading()
    {
        Text = "";
        IsLoading = true;
    }

    public void SetResult(string text)
    {
        Text = text;
        IsLoading = false;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
