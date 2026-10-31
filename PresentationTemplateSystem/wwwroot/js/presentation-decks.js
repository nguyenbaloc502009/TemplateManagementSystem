/**
 * Quản lý kho slide cá nhân (My Decks)
 * Chức năng: Xóa bản trình chiếu
 */

$(document).ready(function () {
    $('.btn-delete-deck').click(function () {
        const id = $(this).data('id');
        const title = $(this).data('title');
        if (confirm('Bạn có chắc chắn muốn xóa bản trình chiếu "' + title + '" không?')) {
            $.ajax({
                url: '/api/presentations/decks/' + id,
                type: 'DELETE',
                success: function () {
                    alert('Đã xóa bản trình chiếu thành công!');
                    window.location.reload();
                },
                error: function (err) {
                    alert('Lỗi xóa: ' + (err.responseJSON ? err.responseJSON.message : 'Không thể xóa'));
                }
            });
        }
    });
});
