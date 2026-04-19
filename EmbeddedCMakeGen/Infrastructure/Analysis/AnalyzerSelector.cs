using EmbeddedCMakeGen.Domain.Interfaces;
using EmbeddedCMakeGen.Domain.Models;

namespace EmbeddedCMakeGen.Infrastructure.Analysis;

public sealed class AnalyzerSelector
{
    private const int StrongMatchThreshold = 40;
    private readonly IReadOnlyList<IProjectAnalyzer> _analyzers;

    public AnalyzerSelector(IEnumerable<IProjectAnalyzer> analyzers)
    {
        _analyzers = analyzers?.ToArray() ?? throw new ArgumentNullException(nameof(analyzers));
    }

    public IProjectAnalyzer SelectBestAnalyzer(ScanResult scanResult)
    {
        if (_analyzers.Count == 0)
        {
            throw new InvalidOperationException("No analyzers were configured.");
        }

        var ranked = _analyzers
            .Select(analyzer => new
            {
                Analyzer = analyzer,
                Match = analyzer.Match(scanResult)
            })
            .OrderByDescending(item => item.Match.Confidence)
            .ThenByDescending(item => item.Analyzer.Priority)
            .ToArray();

        var best = ranked[0];
        if (best.Match.Confidence >= StrongMatchThreshold)
        {
            return best.Analyzer;
        }

        var fallback = ranked
            .Where(item => item.Analyzer is GenericEmbeddedCAnalyzer)
            .OrderByDescending(item => item.Analyzer.Priority)
            .FirstOrDefault();

        return fallback?.Analyzer ?? best.Analyzer;
    }
}
