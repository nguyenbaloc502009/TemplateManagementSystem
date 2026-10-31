/**
 * Quản lý kho tài liệu người dùng (My Documents)
 * Chức năng: Xóa tài liệu cá nhân
 */

$(document).ready(function () {
    // Xử lý xóa tài liệu cá nhân
    $('.btn-delete-doc').click(function () {
        const id = $(this).data('id');
        const title = $(this).data('title');

        if (confirm('Bạn có chắc chắn muốn xóa tài liệu "' + title + '" không?')) {
            $.ajax({
                url: '/api/documents/' + id,
                type: 'DELETE',
                success: function () {
                    alert('Đã xóa tài liệu thành công!');
                    window.location.reload();
                },
                error: function (err) {
                    alert('Lỗi xóa tài liệu: ' + (err.responseJSON ? err.responseJSON.message : 'Không thể xóa tài liệu'));
                }
            });
        }
    });
});
