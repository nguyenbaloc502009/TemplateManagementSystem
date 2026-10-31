/**
 * Trình soạn thảo bộ trình chiếu (Presentation Editor)
 * Chức năng: Quản lý danh sách slide, thêm/xóa/sửa realtime trên canvas, trình chiếu full-screen, lưu vào PostgreSQL
 */

let slidesData = [];
let currentIndex = 0;
let deckId = 0;

function escapeSlideText(value) {
    return String(value || '').replace(/[&<>"']/g, function (character) {
        return ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[character];
    });
}

function renderThumbnails() {
    let html = '';
    slidesData.forEach((s, idx) => {
        const activeClass = idx === currentIndex ? 'active border-warning' : 'border-secondary border-opacity-25';
        html += `
            <div class="slide-thumb-item p-2 mb-2 rounded border ${activeClass}" data-index="${idx}" style="cursor: pointer;">
                <div class="d-flex justify-content-between align-items-center mb-1">
                    <span class="badge bg-dark small">Slide ${idx + 1}</span>
                    ${slidesData.length > 1 ? `
                        <button type="button" class="btn btn-link text-danger p-0 btn-delete-slide" data-index="${idx}" title="Xóa slide này">
                            <i class="bi bi-trash small"></i>
                        </button>
                    ` : ''}
                </div>
                <div class="slide-thumb-preview bg-black rounded p-2 text-start" style="height: 75px; overflow: hidden;">
                    <div class="small fw-bold text-truncate text-warning thumb-title">${escapeSlideText(s.SlideTitle || '(Chưa có tiêu đề)')}</div>
                    <div class="extra-small text-light text-opacity-75 text-truncate thumb-body">${escapeSlideText(s.ContentBody || '')}</div>
                </div>
            </div>
        `;
    });

    $('#slidesThumbList').html(html);
    $('#slideCountDisplay').text(slidesData.length);
    loadCurrentSlideToCanvas();
}

function loadCurrentSlideToCanvas() {
    if (currentIndex >= slidesData.length) currentIndex = slidesData.length - 1;
    if (currentIndex < 0) currentIndex = 0;

    const s = slidesData[currentIndex];
    $('#activeSlideTitle').val(s.SlideTitle || '');
    $('#activeSlideSubtitle').val(s.Subtitle || '');
    $('#activeSlideBody').val(s.ContentBody || '');
    $('#activeSlideNotes').val(s.Notes || '');
    $('#currentSlideIndicator').text(`Slide ${currentIndex + 1} / ${slidesData.length}`);
}

function syncActiveSlideFromCanvas() {
    if (slidesData[currentIndex]) {
        slidesData[currentIndex].SlideNumber = currentIndex + 1;
        slidesData[currentIndex].SlideTitle = $('#activeSlideTitle').val();
        slidesData[currentIndex].Subtitle = $('#activeSlideSubtitle').val();
        slidesData[currentIndex].ContentBody = $('#activeSlideBody').val();
        slidesData[currentIndex].Notes = $('#activeSlideNotes').val();

        // Cập nhật thumbnail tương ứng
        const thumb = $(`.slide-thumb-item[data-index="${currentIndex}"]`);
        thumb.find('.thumb-title').text(slidesData[currentIndex].SlideTitle || '(Chưa có tiêu đề)');
        thumb.find('.thumb-body').text(slidesData[currentIndex].ContentBody || '');
    }
}

$(document).ready(function () {
    // Lấy ID bản trình chiếu và danh sách slide ban đầu
    const idVal = $('#deckId').val();
    deckId = idVal ? parseInt(idVal) : 0;

    const initialDataElem = document.getElementById('slidesInitialData');
    if (initialDataElem && initialDataElem.textContent.trim()) {
        try {
            slidesData = JSON.parse(initialDataElem.textContent);
        } catch (e) {
            console.error('Lỗi parse slidesInitialData:', e);
            slidesData = [];
        }
    }

    if (!slidesData || slidesData.length === 0) {
        slidesData = [
            {
                SlideNumber: 1,
                SlideTitle: "Tiêu Đề Slide Mới",
                Subtitle: "Phụ đề slide",
                ContentBody: "• Nội dung ý chính 1\n• Nội dung ý chính 2",
                Notes: "",
                LayoutType: "TitleAndContent"
            }
        ];
    }

    // Chuyển slide khi click vào thumbnail
    $(document).on('click', '.slide-thumb-item', function (e) {
        if ($(e.target).closest('.btn-delete-slide').length > 0) return;
        syncActiveSlideFromCanvas();
        currentIndex = parseInt($(this).data('index'));
        $('.slide-thumb-item').removeClass('active border-warning').addClass('border-secondary border-opacity-25');
        $(this).addClass('active border-warning').removeClass('border-secondary border-opacity-25');
        loadCurrentSlideToCanvas();
    });

    // Thay đổi realtime trên canvas
    $('#activeSlideTitle, #activeSlideSubtitle, #activeSlideBody, #activeSlideNotes').on('input', function () {
        syncActiveSlideFromCanvas();
    });

    // Thêm slide mới
    $('#btnAddSlide').click(function () {
        syncActiveSlideFromCanvas();
        const newSlide = {
            SlideNumber: slidesData.length + 1,
            SlideTitle: "Tiêu Đề Slide " + (slidesData.length + 1),
            Subtitle: "",
            ContentBody: "• Nhập nội dung điểm 1\n• Nhập nội dung điểm 2",
            Notes: "",
            LayoutType: "TitleAndContent"
        };
        slidesData.push(newSlide);
        currentIndex = slidesData.length - 1;
        renderThumbnails();
    });

    // Xóa slide
    $(document).on('click', '.btn-delete-slide', function (e) {
        e.stopPropagation();
        const idx = parseInt($(this).data('index'));
        if (slidesData.length <= 1) {
            alert('Bản trình chiếu cần tối thiểu 1 slide!');
            return;
        }
        if (confirm('Bạn có chắc muốn xóa Slide ' + (idx + 1) + ' không?')) {
            slidesData.splice(idx, 1);
            // Đánh lại số thứ tự
            slidesData.forEach((s, i) => s.SlideNumber = i + 1);
            if (currentIndex >= slidesData.length) currentIndex = slidesData.length - 1;
            renderThumbnails();
        }
    });

    // Lưu bản trình chiếu vào PostgreSQL qua API
    $('#btnSaveDeck').click(function () {
        syncActiveSlideFromCanvas();
        const btn = $(this);
        btn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-1"></span> Đang lưu...');

        const payload = {
            id: deckId,
            deckTitle: $('#deckTitleInput').val().trim(),
            slides: slidesData,
            status: 'Saved'
        };

        $.ajax({
            url: '/api/presentations/decks/' + deckId,
            type: 'PUT',
            contentType: 'application/json',
            data: JSON.stringify(payload),
            success: function () {
                btn.prop('disabled', false).html('<i class="bi bi-floppy-fill me-1"></i> Lưu Slide');
                $('#deckStatusBadge').removeClass('bg-secondary').addClass('bg-success')
                                     .html('<i class="bi bi-check-circle me-1"></i>Đã lưu (' + new Date().toLocaleTimeString() + ')');
            },
            error: function (err) {
                btn.prop('disabled', false).html('<i class="bi bi-floppy-fill me-1"></i> Lưu Slide');
                alert('Lỗi lưu bản trình chiếu: ' + (err.responseJSON ? err.responseJSON.message : 'Kiểm tra kết nối CSDL'));
            }
        });
    });

    // Trình chiếu toàn màn hình
    $('#btnFullScreen').click(function () {
        const elem = document.getElementById("mainSlideCanvas");
        if (elem.requestFullscreen) {
            elem.requestFullscreen();
        } else if (elem.webkitRequestFullscreen) {
            elem.webkitRequestFullscreen();
        } else if (elem.msRequestFullscreen) {
            elem.msRequestFullscreen();
        }
    });

    renderThumbnails();
});
