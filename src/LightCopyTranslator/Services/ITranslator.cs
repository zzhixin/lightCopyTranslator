using System.Threading;
using System.Threading.Tasks;

namespace LightCopyTranslator.Services;

internal interface ITranslator
{
    string Name { get; }
    Task<string> TranslateAsync(string text, CancellationToken cancellationToken);
}
