using DocumentTemplateSystem.Common;
using NUnit.Framework;
using PresentationTemplateSystem.Common;
using System.IO;

namespace TemplateSystem.Tests
{
    [TestFixture]
    public class TemplateAssetStorageTests
    {
        [Test]
        /// <summary>Kiểm tra asset văn bản có object key đúng định dạng và thumbnail riêng.</summary>
        public void DocumentStorage_ShouldCreateObjectKeyAndUniqueThumbnail()
        {
            string root = Path.Combine(Path.GetTempPath(), $"doc-assets-{Guid.NewGuid():N}");
            try
            {
                DocumentTemplateSystem.Common.TemplateAssetStorage.Initialize(Path.Combine(root, "wwwroot"), "uploads/thumbnails");
                DocumentTemplateSystem.Common.TemplateAssetStorage.EnsureThumbnail("DOC-TEST", "Document test", "0D6EFD");

                Assert.That(File.Exists(Path.Combine(root, "wwwroot", "uploads", "thumbnails", "DOC-TEST.svg")), Is.True);
                Assert.That(DocumentTemplateSystem.Common.TemplateAssetStorage.TemplateUrl("DOC-TEST"), Is.EqualTo("storage/templates/DOC-TEST.docx"));
                Assert.That(DocumentTemplateSystem.Common.TemplateAssetStorage.IsDocumentStorageKey(
                    DocumentTemplateSystem.Common.TemplateAssetStorage.DocumentUrl(Guid.NewGuid())), Is.True);
            }
            finally
            {
                if (Directory.Exists(root))
                {
                    Directory.Delete(root, true);
                }
            }
        }

        [Test]
        /// <summary>Kiểm tra asset trình chiếu có object key đúng định dạng và thumbnail riêng.</summary>
        public void PresentationStorage_ShouldCreateObjectKeyAndUniqueThumbnail()
        {
            string root = Path.Combine(Path.GetTempPath(), $"ppt-assets-{Guid.NewGuid():N}");
            try
            {
                PresentationTemplateSystem.Common.TemplateAssetStorage.Initialize(Path.Combine(root, "wwwroot"), "uploads/thumbnails");
                PresentationTemplateSystem.Common.TemplateAssetStorage.EnsureThumbnail("PPT-TEST", "Presentation test", "7030A0");

                Assert.That(File.Exists(Path.Combine(root, "wwwroot", "uploads", "thumbnails", "PPT-TEST.svg")), Is.True);
                Assert.That(PresentationTemplateSystem.Common.TemplateAssetStorage.TemplateUrl("PPT-TEST"), Is.EqualTo("storage/templates/PPT-TEST.pptx"));
                Assert.That(PresentationTemplateSystem.Common.TemplateAssetStorage.IsDeckStorageKey(
                    PresentationTemplateSystem.Common.TemplateAssetStorage.DeckUrl(Guid.NewGuid())), Is.True);
            }
            finally
            {
                if (Directory.Exists(root))
                {
                    Directory.Delete(root, true);
                }
            }
        }
    }
}
