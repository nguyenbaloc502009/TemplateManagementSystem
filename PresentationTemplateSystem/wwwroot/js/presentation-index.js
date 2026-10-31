/**
 * Quản lý kho mẫu bài trình chiếu (Presentation Templates Index)
 * Chức năng: Xem trước bộ slide (Slide Gallery Carousel), Thêm bộ slide mẫu mới
 */

function escapeSlideHtml(value) {
    return String(value || '').replace(/[&<>"']/g, function (character) {
        return ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[character];
    });
}

function loadPresentationPreview(templateId) {
    $('#carouselInnerContent').html('<div class="text-center py-5"><div class="spinner-border text-warning" role="status"></div><p class="text-muted mt-2">Đang tải slide...</p></div>');
    $('#carouselThumbnails').empty();
    $.ajax({
        url: '/api/presentations/templates/' + templateId,
        type: 'GET',
        success: function (res) {
            if (res && res.data && res.data.slides && res.data.slides.length > 0) {
                let carouselHtml = '';
                let thumbHtml = '';

                res.data.slides.forEach(function (slide, idx) {
                    const activeClass = idx === 0 ? 'active' : '';
                    const title = escapeSlideHtml(slide.slideTitle);
                    const subtitle = escapeSlideHtml(slide.subtitle);
                    const body = escapeSlideHtml(slide.contentBody).replace(/\r?\n/g, '<br>');
                    const notes = escapeSlideHtml(slide.notes);
                    const imagePath = /^\/uploads\/slide-previews\/[A-Za-z0-9_-]+-\d+\.svg$/.test(slide.imageUrl || '')
                        ? slide.imageUrl
                        : '/uploads/thumbnails/presentation-thumbnail-fallback.svg';
                    carouselHtml += `
                        <div class="carousel-item ${activeClass}">
                            <div class="slide-preview-frame mx-auto shadow rounded-3 p-4 position-relative">
                                <span class="badge bg-dark position-absolute top-0 end-0 m-3">Trang ${Number(slide.slideNumber) || idx + 1} / ${res.data.slides.length}</span>
                                <img class="img-fluid rounded mb-3" src="${imagePath}" alt="Ảnh xem trước ${title}"
                                     onerror="this.onerror=null;this.src='/uploads/thumbnails/presentation-thumbnail-fallback.svg';" />
                                <h2 class="fw-bold text-primary mb-2">${title}</h2>
                                ${subtitle ? `<h5 class="text-muted mb-4">${subtitle}</h5>` : ''}
                                <div class="slide-body-content mt-3 fs-5" style="white-space: pre-line; line-height: 1.8;">${body}</div>
                                ${notes ? `<div class="mt-4 pt-3 border-top small text-muted"><i class="bi bi-chat-left-text me-1"></i>Ghi chú diễn giả: <em>${notes}</em></div>` : ''}
                            </div>
                        </div>
                    `;
                    thumbHtml += `
                        <button type="button" class="btn btn-outline-secondary btn-sm text-nowrap" data-bs-target="#slideCarousel" data-bs-slide-to="${idx}">
                            Trang ${Number(slide.slideNumber) || idx + 1}
                        </button>
                    `;
                });

                $('#carouselInnerContent').html(carouselHtml);
                $('#carouselThumbnails').html(thumbHtml);
            } else {
                $('#carouselInnerContent').html('<div class="alert alert-warning">Chưa có slide nào trong bộ template này.</div>');
            }
        },
        error: function () {
            $('#carouselInnerContent').html(`
                <div class="alert alert-danger text-center">
                    <p>Không tải được slide. Vui lòng kiểm tra kết nối rồi thử lại.</p>
                    <button type="button" class="btn btn-outline-danger btn-sm retry-presentation-preview" data-template-id="${Number(templateId)}">Thử lại</button>
                </div>
            `);
        }
    });
}

$(document).ready(function () {
    // Mở Modal xem trước bộ slide (Slide Gallery Carousel)
    $('.btn-preview-deck').click(function () {
        const templateId = $(this).data('id');
        const title = $(this).data('title');
        const category = $(this).data('category');

        $('#deckModalTitle').text(title);
        $('#deckModalCategory').text(category);
        $('#formUseDeckModal').attr('action', '/Presentation/UseTemplate/' + templateId);
        $('#modalSlidePreview').modal('show');
        loadPresentationPreview(templateId);
    });

    $(document).on('click', '.retry-presentation-preview', function () {
        loadPresentationPreview($(this).data('template-id'));
    });

    // Xử lý thêm bộ slide mới
    $('#formCreateDeckTemplate').submit(function (e) {
        e.preventDefault();
        const btn = $('#btnSubmitNewDeck');
        btn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-1"></span> Đang lưu...');

        const payload = {
            code: $('#newDeckCode').val().trim(),
            title: $('#newDeckTitle').val().trim(),
            categoryId: parseInt($('#newDeckCategoryId').val()),
            description: $('#newDeckDesc').val().trim(),
            tags: $('#newDeckTags').val().trim(),
            slides: [
                {
                    slideNumber: 1,
                    slideTitle: $('#newSlide1Title').val().trim(),
                    contentBody: $('#newSlide1Body').val().trim(),
                    layoutType: 'TitleSlide'
                },
                {
                    slideNumber: 2,
                    slideTitle: $('#newSlide2Title').val().trim(),
                    contentBody: $('#newSlide2Body').val().trim(),
                    layoutType: 'TitleAndContent'
                }
            ]
        };

        $.ajax({
            url: '/api/presentations/templates',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(payload),
            success: function () {
                alert('Thêm bộ template slide mới thành công!');
                window.location.reload();
            },
            error: function (err) {
                alert('Lỗi: ' + (err.responseJSON ? err.responseJSON.message : 'Không thể lưu template'));
                btn.prop('disabled', false).html('<i class="bi bi-cloud-arrow-up-fill me-1"></i> Lưu Bộ Slide Mới');
            }
        });
    });
});
