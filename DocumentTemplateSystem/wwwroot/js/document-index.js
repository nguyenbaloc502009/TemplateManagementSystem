/**
 * Quản lý kho mẫu văn bản (Document Templates Index)
 * Chức năng: Chuyển đổi dạng xem (Grid/List), Xem trước mẫu văn bản qua Modal, Thêm mới mẫu văn bản
 */

function loadDocumentPreview(templateId) {
    $('#previewContent').html('<div class="text-center py-5"><div class="spinner-border text-primary" role="status"></div><p class="text-muted mt-2">Đang tải nội dung...</p></div>');
    $.ajax({
        url: '/api/templates/' + templateId,
        type: 'GET',
        success: function (res) {
            if (res && res.data && res.data.contentHtml) {
                $('#previewContent').html(res.data.contentHtml);
            } else {
                $('#previewContent').html('<div class="alert alert-warning">Mẫu này chưa có nội dung xem trước.</div>');
            }
        },
        error: function () {
            $('#previewContent').html(`
                <div class="alert alert-danger text-center">
                    <p>Không tải được nội dung mẫu. Vui lòng kiểm tra kết nối rồi thử lại.</p>
                    <button type="button" class="btn btn-outline-danger btn-sm retry-document-preview" data-template-id="${Number(templateId)}">Thử lại</button>
                </div>
            `);
        }
    });
}

$(document).ready(function () {
    if (new URLSearchParams(window.location.search).get('openCreateTemplate') === 'true') {
        bootstrap.Modal.getOrCreateInstance(document.getElementById('modalCreateTemplate')).show();
    }

    // Chuyển đổi qua lại giữa giao diện Lưới (Grid) và Danh sách (List)
    $('#btnViewGrid').click(function () {
        $(this).addClass('active');
        $('#btnViewList').removeClass('active');
        $('#viewContainerGrid').removeClass('d-none');
        $('#viewContainerList').addClass('d-none');
    });

    $('#btnViewList').click(function () {
        $(this).addClass('active');
        $('#btnViewGrid').removeClass('active');
        $('#viewContainerList').removeClass('d-none');
        $('#viewContainerGrid').addClass('d-none');
    });

    // Xử lý nút Xem trước mẫu văn bản (Modal Preview)
    $('.btn-preview').click(function () {
        const templateId = $(this).data('id');
        const title = $(this).data('title');
        const category = $(this).data('category');

        $('#previewTitle').text(title);
        $('#previewCategory').text(category);
        $('#formUseTemplateModal').attr('action', '/Document/UseTemplate/' + templateId);
        $('#modalPreview').modal('show');
        loadDocumentPreview(templateId);
    });

    $(document).on('click', '.retry-document-preview', function () {
        loadDocumentPreview($(this).data('template-id'));
    });

    // Xử lý thêm mẫu mới vào cơ sở dữ liệu
    $('#formCreateTemplate').submit(function (e) {
        e.preventDefault();
        const btn = $('#btnSubmitNewTemplate');
        btn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-1"></span> Đang lưu...');

        const payload = {
            code: $('#newCode').val().trim(),
            title: $('#newTitle').val().trim(),
            categoryId: parseInt($('#newCategoryId').val()),
            description: $('#newDescription').val().trim(),
            tags: $('#newTags').val().trim(),
            contentHtml: $('#newContentHtml').val().trim()
        };

        $.ajax({
            url: '/api/templates',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(payload),
            success: function (res) {
                alert('Thêm mẫu mới thành công!');
                window.location.reload();
            },
            error: function (err) {
                alert('Lỗi: ' + (err.responseJSON ? err.responseJSON.message : 'Không thể thêm mẫu'));
                btn.prop('disabled', false).html('<i class="bi bi-cloud-arrow-up-fill me-1"></i> Lưu Mẫu Mới');
            }
        });
    });
});
