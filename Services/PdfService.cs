using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace itpayroll.Services
{
    public interface IPdfService
    {
        byte[] Generate(Action<IDocumentContainer> configure);
    }

    public class PdfService : IPdfService
    {
        public PdfService()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public byte[] Generate(Action<IDocumentContainer> configure)
        {
            return Document.Create(configure).GeneratePdf();
        }
    }
}
