using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using LightCopyTranslator.Services;

namespace LightCopyTranslator.ViewModels;

internal sealed class TranslationViewModel : INotifyPropertyChanged
{
    private readonly ITranslator[] _translators;
    private readonly Dispatcher _dispatcher;
    private CancellationTokenSource? _cts;
    private string _sourceText = "";

    public TranslationViewModel(ITranslator[] translators, Dispatcher dispatcher)
    {
        _translators = translators;
        _dispatcher = dispatcher;
        Cards = new ObservableCollection<TranslationCard>(
            translators.Select(t => new TranslationCard(t.Name)));
    }

    public ObservableCollection<TranslationCard> Cards { get; }

    public string SourceText
    {
        get => _sourceText;
        private set
        {
            if (_sourceText == value)
            {
                return;
            }

            _sourceText = value;
            OnPropertyChanged();
        }
    }

    public async Task TranslateAsync(string text)
    {
        _cts?.Cancel();
        var cts = new CancellationTokenSource();
        _cts = cts;

        await _dispatcher.InvokeAsync(() =>
        {
            SourceText = text;
            foreach (var card in Cards)
            {
                card.SetLoading();
            }
        });

        var tasks = _translators.Select((translator, index) =>
            TranslateOneAsync(translator, Cards[index], text, cts.Token));

        try
        {
            await Task.WhenAll(tasks);
        }
        catch
        {
            // Individual tasks handle their own errors.
        }
    }

    private async Task TranslateOneAsync(ITranslator translator, TranslationCard card, string text, CancellationToken token)
    {
        try
        {
            var result = await translator.TranslateAsync(text, token);
            if (token.IsCancellationRequested)
            {
                return;
            }

            await _dispatcher.InvokeAsync(() => card.SetResult(result));
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            await _dispatcher.InvokeAsync(() => card.SetResult($"翻译失败：{ex.Message}"));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
