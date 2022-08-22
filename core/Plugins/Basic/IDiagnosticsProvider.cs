namespace ImageResizer.Plugins.Basic
{
    public interface IDiagnosticsProvider
    {
        string ProvideDiagnostics();
    }

    public interface IDiagnosticsHeaderProvider
    {
        string ProvideDiagnosticsHeader();
    }

    public interface IDiagnosticsFooterProvider
    {
        string ProvideDiagnosticsFooter();
    }

    public interface ILicenseDiagnosticsProvider
    {
        string ProvidePublicText();
    }

    public interface IDiagnosticsProviderFactory
    {
        object GetDiagnosticsProvider();
    }
}