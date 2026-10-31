using DocumentTemplateSystem.Models.DTO;
using NUnit.Framework;
using PresentationTemplateSystem.Models.DTO;
using Microsoft.Extensions.Configuration;
using DocumentTemplate = DocumentTemplateSystem.Services.TemplateService;
using UserDocument = DocumentTemplateSystem.Services.UserDocumentService;
using PresentationTemplate = PresentationTemplateSystem.Services.TemplateService;
using UserDeck = PresentationTemplateSystem.Services.UserDeckService;
using System.IO;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using TemplateManagement.Core.Security;
using TemplateManagement.Core.Storage;

namespace TemplateSystem.Tests
{
    [TestFixture]
    public class DatabaseIntegrationTests
    {
        private DocumentTemplate _docTemplateService = null!;
        private UserDocument _docUserDocService = null!;
        private PresentationTemplate _presTemplateService = null!;
        private UserDeck _presUserDeckService = null!;
        private Guid _ownerUserId;
        private Guid _otherOwnerUserId;
        private InMemoryObjectStorage _objectStorage = null!;

        [SetUp]
        /// <summary>Khởi tạo service, object storage giả và hai tài khoản sở hữu độc lập cho mỗi test.</summary>
        public void Setup()
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false)
                .AddUserSecrets<DatabaseIntegrationTests>(optional: true)
                .AddEnvironmentVariables()
                .Build();
            DocumentTemplateSystem.Common.TemplateAssetStorage.Initialize(
                Path.Combine(AppContext.BaseDirectory, "wwwroot"), "uploads/thumbnails");
            PresentationTemplateSystem.Common.TemplateAssetStorage.Initialize(
                Path.Combine(AppContext.BaseDirectory, "wwwroot"), "uploads/thumbnails");
            _objectStorage = new InMemoryObjectStorage();
            _docTemplateService = new DocumentTemplate(configuration, _objectStorage);
            _docUserDocService = new UserDocument(configuration, _objectStorage);
            _presTemplateService = new PresentationTemplate(configuration, _objectStorage);
            _presUserDeckService = new UserDeck(configuration, _presTemplateService, _objectStorage);
            var accountService = new UserAccountService(configuration, new PasswordHasher<AppUser>());
            _ownerUserId = accountService.Register(
                $"test-{Guid.NewGuid():N}@example.invalid",
                "Integration Test",
                "IntegrationTest!Password2026").Id;
            _otherOwnerUserId = accountService.Register(
                $"test-{Guid.NewGuid():N}@example.invalid",
                "Other Integration Test",
                "IntegrationTest!Password2026").Id;
        }

        [Test]
        /// <summary>Kiểm tra thư viện văn bản có đủ số danh mục seed tối thiểu.</summary>
        public void Product1_GetCategories_ShouldReturnAtLeast5Categories()
        {
            var categories = _docTemplateService.GetCategories();
            Assert.That(categories, Is.Not.Null);
            Assert.That(categories.Count, Is.GreaterThanOrEqualTo(5), "Hệ thống phải có ít nhất 5 danh mục mẫu văn bản");
        }

        [Test]
        /// <summary>Kiểm tra thư viện văn bản có đủ số mẫu seed tối thiểu.</summary>
        public void Product1_GetTemplates_ShouldReturnAtLeast5InitialTemplates()
        {
            var templates = _docTemplateService.GetTemplates();
            Assert.That(templates, Is.Not.Null);
            Assert.That(templates.Count, Is.GreaterThanOrEqualTo(5), "Đề bài yêu cầu tối thiểu 5 mẫu văn bản thử nghiệm thực tế");
        }

        [Test]
        /// <summary>Kiểm tra tìm kiếm mẫu văn bản trả về kết quả phù hợp từ khóa.</summary>
        public void Product1_SearchAndFilter_ShouldFilterCorrectly()
        {
            // Tìm kiếm theo từ khóa 'Nghỉ phép'
            var results = _docTemplateService.GetTemplates(null, "Nghỉ phép");
            Assert.That(results, Is.Not.Null);
            Assert.That(results.Count, Is.GreaterThanOrEqualTo(1));
            Assert.That(results[0].Title.Contains("Nghỉ Phép", StringComparison.OrdinalIgnoreCase));
        }

        [Test]
        /// <summary>Đảm bảo tạo và sửa tài liệu người dùng không làm thay đổi mẫu DOCX gốc.</summary>
        public void Product1_TemplateIsolation_CreatingUserDocument_MustNotModifyOriginalTemplate()
        {
            // 1. Lấy thông tin mẫu gốc trước khi tạo tài liệu
            var originalBefore = _docTemplateService.GetTemplateById(1);
            Assert.That(originalBefore, Is.Not.Null, "Phải tìm thấy mẫu văn bản gốc ID: 1");
            string originalContentBefore = originalBefore.ContentHtml;
            string originalTitleBefore = originalBefore.Title;
            string templateKey = DocumentTemplateSystem.Common.TemplateAssetStorage.TemplateUrl(originalBefore.Code);
            byte[] templateHashBefore = SHA256.HashData(_objectStorage.Get(templateKey));

            // 2. Tạo tài liệu người dùng mới từ mẫu gốc
            string customDocTitle = "Đơn Xin Nghỉ Của Nhân Viên A (Thử Nghiệm)";
            var newUserDoc = _docUserDocService.CreateFromTemplate(1, _ownerUserId, customDocTitle, "Tester");

            Assert.That(newUserDoc, Is.Not.Null);
            Assert.That(newUserDoc.Id, Is.GreaterThan(0));
            Assert.That(newUserDoc.DocumentTitle, Is.EqualTo(customDocTitle));
            Assert.That(newUserDoc.FilePath, Does.StartWith("storage/user/"));
            byte[] copyHashBeforeEdit = SHA256.HashData(_objectStorage.Get(newUserDoc.FilePath!));

            // 3. Người dùng sửa đổi nội dung của tài liệu mới
            string editedContent = "<p>NỘI DUNG ĐÃ ĐƯỢC CHỈNH SỬA BỞI NGƯỜI DÙNG A</p>";
            bool saveResult = _docUserDocService.SaveUserDocument(new SaveDocumentRequest
            {
                Id = newUserDoc.Id,
                DocumentTitle = customDocTitle + " [ĐÃ CẬP NHẬT]",
                ContentHtml = editedContent,
                Status = "Saved"
            }, _ownerUserId);
            Assert.That(saveResult, Is.True);
            Assert.That(SHA256.HashData(_objectStorage.Get(newUserDoc.FilePath!)), Is.Not.EqualTo(copyHashBeforeEdit));

            // 4. KIỂM TRA MẪU GỐC: Phải giữ nguyên 100%, không bị ảnh hưởng bởi việc tạo và sửa tài liệu mới
            var originalAfter = _docTemplateService.GetTemplateById(1);
            Assert.That(originalAfter, Is.Not.Null);
            Assert.That(originalAfter.ContentHtml, Is.EqualTo(originalContentBefore), "VI PHẠM NGUYÊN TẮC: Nội dung mẫu gốc đã bị thay đổi!");
            Assert.That(originalAfter.Title, Is.EqualTo(originalTitleBefore), "VI PHẠM NGUYÊN TẮC: Tiêu đề mẫu gốc đã bị thay đổi!");
            Assert.That(SHA256.HashData(_objectStorage.Get(templateKey)), Is.EqualTo(templateHashBefore), "File DOCX template gốc không được sửa khi lưu bản sao.");

            // Dọn dẹp tài liệu test
            _docUserDocService.DeleteUserDocument(newUserDoc.Id, _ownerUserId);
        }

        [Test]
        /// <summary>Đảm bảo chỉ chủ sở hữu mới có thể đọc, sửa hoặc xóa tài liệu cá nhân.</summary>
        public void Product1_DocumentOperations_ShouldBeRestrictedToOwner()
        {
            var document = _docUserDocService.CreateFromTemplate(1, _ownerUserId, "Ownership test", "Tester");

            Assert.That(_docUserDocService.GetUserDocumentById(document.Id, _otherOwnerUserId), Is.Null);
            Assert.That(_docUserDocService.SaveUserDocument(new SaveDocumentRequest
            {
                Id = document.Id,
                DocumentTitle = "Unauthorized edit",
                ContentHtml = "<p>Not the owner</p>"
            }, _otherOwnerUserId), Is.False);
            Assert.That(_docUserDocService.DeleteUserDocument(document.Id, _otherOwnerUserId), Is.False);
            Assert.That(_docUserDocService.GetAllUserDocuments(_otherOwnerUserId)
                .Any(item => item.Id == document.Id), Is.False);

            Assert.That(_docUserDocService.DeleteUserDocument(document.Id, _ownerUserId), Is.True);
        }

        [Test]
        /// <summary>Kiểm tra thư viện trình chiếu có đủ số bộ mẫu seed tối thiểu.</summary>
        public void Product2_GetTemplates_ShouldReturnAtLeast3InitialPresentationTemplates()
        {
            var templates = _presTemplateService.GetTemplates();
            Assert.That(templates, Is.Not.Null);
            Assert.That(templates.Count, Is.GreaterThanOrEqualTo(3), "Đề bài yêu cầu tối thiểu 3 bộ template trình chiếu thử nghiệm");
            
            // Kiểm tra bộ template có đầy đủ danh sách slide xem trước
            foreach (var t in templates)
            {
                Assert.That(t.Slides, Is.Not.Null);
                Assert.That(t.Slides.Count, Is.GreaterThanOrEqualTo(1), $"Bộ template '{t.Title}' phải có ít nhất 1 slide xem trước");
                foreach (var slide in t.Slides)
                {
                    Assert.That(slide.ImageUrl, Is.Not.Null.And.Not.Empty);
                    string previewPath = Path.Combine(
                        AppContext.BaseDirectory,
                        "wwwroot",
                        slide.ImageUrl!.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                    Assert.That(File.Exists(previewPath), Is.True, $"Missing slide preview {slide.ImageUrl}.");
                }
            }
        }

        [Test]
        /// <summary>Đảm bảo tạo và sửa deck cá nhân không làm thay đổi PPTX mẫu gốc.</summary>
        public void Product2_PresentationDeckIsolation_CreatingUserDeck_MustNotModifyOriginalTemplate()
        {
            // 1. Lấy bộ template gốc trước
            var originalBefore = _presTemplateService.GetTemplateById(1);
            Assert.That(originalBefore, Is.Not.Null);
            int originalSlideCount = originalBefore.Slides.Count;
            string templateKey = PresentationTemplateSystem.Common.TemplateAssetStorage.TemplateUrl(originalBefore.Code);
            byte[] templateHashBefore = SHA256.HashData(_objectStorage.Get(templateKey));

            // 2. Nhân bản tạo bản trình chiếu độc lập
            var userDeck = _presUserDeckService.CreateFromTemplate(1, _ownerUserId, "Slide Thuyết Trình Của Tôi", "Tester");
            Assert.That(userDeck, Is.Not.Null);
            Assert.That(userDeck.Id, Is.GreaterThan(0));
            Assert.That(userDeck.FilePath, Does.StartWith("storage/user/"));
            byte[] deckCopyHashBeforeEdit = SHA256.HashData(_objectStorage.Get(userDeck.FilePath!));

            // 3. Người dùng thêm/sửa slide trong bản trình chiếu của mình
            var modifiedSlides = new List<SlideItemDto>
            {
                new SlideItemDto
                {
                    SlideNumber = 1,
                    SlideTitle = "Slide Tiêu Đề Mới Của Người Dùng",
                    ContentBody = "Nội dung riêng biệt"
                }
            };

            bool saveResult = _presUserDeckService.SaveUserDeck(new SaveDeckRequest
            {
                Id = userDeck.Id,
                DeckTitle = "Slide Thuyết Trình Đã Sửa",
                Slides = modifiedSlides,
                Status = "Saved"
            }, _ownerUserId);
            Assert.That(saveResult, Is.True);
            Assert.That(SHA256.HashData(_objectStorage.Get(userDeck.FilePath!)), Is.Not.EqualTo(deckCopyHashBeforeEdit));

            // 4. KIỂM TRA MẪU GỐC: Số lượng slide và thông tin template gốc vẫn nguyên vẹn
            var originalAfter = _presTemplateService.GetTemplateById(1);
            Assert.That(originalAfter, Is.Not.Null);
            Assert.That(originalAfter.Slides.Count, Is.EqualTo(originalSlideCount), "Template gốc phải giữ nguyên số slide ban đầu!");
            Assert.That(SHA256.HashData(_objectStorage.Get(templateKey)), Is.EqualTo(templateHashBefore), "File PPTX template gốc không được sửa khi lưu bản sao.");

            // Dọn dẹp bản trình chiếu test
            _presUserDeckService.DeleteUserDeck(userDeck.Id, _ownerUserId);
        }

        [Test]
        /// <summary>Đảm bảo chỉ chủ sở hữu mới có thể đọc, sửa hoặc xóa deck cá nhân.</summary>
        public void Product2_DeckOperations_ShouldBeRestrictedToOwner()
        {
            var deck = _presUserDeckService.CreateFromTemplate(1, _ownerUserId, "Ownership test", "Tester");

            Assert.That(_presUserDeckService.GetUserDeckById(deck.Id, _otherOwnerUserId), Is.Null);
            Assert.That(_presUserDeckService.SaveUserDeck(new SaveDeckRequest
            {
                Id = deck.Id,
                DeckTitle = "Unauthorized edit",
                Slides = new List<SlideItemDto>
                {
                    new() { SlideNumber = 1, SlideTitle = "Not the owner" }
                }
            }, _otherOwnerUserId), Is.False);
            Assert.That(_presUserDeckService.DeleteUserDeck(deck.Id, _otherOwnerUserId), Is.False);
            Assert.That(_presUserDeckService.GetAllUserDecks(_otherOwnerUserId)
                .Any(item => item.Id == deck.Id), Is.False);

            Assert.That(_presUserDeckService.DeleteUserDeck(deck.Id, _ownerUserId), Is.True);
        }

    }
}
