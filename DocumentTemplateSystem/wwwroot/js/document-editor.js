/**
 * Trình soạn thảo tài liệu (Document Editor)
 * Quản lý định dạng văn bản A4, điền biến tự động, chèn bảng, lưu tài liệu và auto-save
 */

// Hàm thực thi lệnh định dạng văn bản cho contenteditable
function formatDoc(cmd, value = null) {
    if (value) {
        document.execCommand(cmd, false, value);
    } else {
        document.execCommand(cmd, false, null);
    }
    $('#editorContent').focus();
}

// Chèn đường phân cách ngang
function insertHorizontalRule() {
    document.execCommand('insertHorizontalRule', false, null);
}

// Chèn biến thay thế (placeholder)
function insertPlaceholder(tag) {
    document.execCommand('insertText', false, tag);
    decoratePlaceholders(document.getElementById('editorContent'));
}

const placeholderLabels = {
    HoTen: 'Họ và tên',
    NgayThang: 'Ngày tháng',
    NgayLamDon: 'Ngày làm đơn',
    NgayKy: 'Ngày ký',
    TenCongTy: 'Tên công ty',
    TenBoPhan: 'Tên bộ phận',
    ChucVu: 'Chức vụ'
};

let activePlaceholderKey = null;

function decoratePlaceholders(editor) {
    if (!editor) return;

    const textNodes = [];
    const walker = document.createTreeWalker(editor, NodeFilter.SHOW_TEXT);
    while (walker.nextNode()) {
        if (!walker.currentNode.parentElement.closest('.template-placeholder')) {
            textNodes.push(walker.currentNode);
        }
    }

    const tokenPattern = /\{\{\s*([\p{L}\p{N}_]+)\s*\}\}/gu;
    textNodes.forEach(function (textNode) {
        const text = textNode.nodeValue;
        tokenPattern.lastIndex = 0;
        let match;
        let lastIndex = 0;
        let hasPlaceholder = false;
        const fragment = document.createDocumentFragment();

        while ((match = tokenPattern.exec(text)) !== null) {
            hasPlaceholder = true;
            if (match.index > lastIndex) {
                fragment.appendChild(document.createTextNode(text.slice(lastIndex, match.index)));
            }

            const placeholder = document.createElement('span');
            placeholder.className = 'template-placeholder';
            placeholder.dataset.placeholderKey = match[1];
            placeholder.dataset.placeholderToken = match[0];
            placeholder.textContent = match[0];
            placeholder.tabIndex = 0;
            placeholder.setAttribute('role', 'button');
            placeholder.setAttribute('aria-label', 'Bấm để điền ' + getPlaceholderLabel(match[1]));
            placeholder.title = 'Bấm để điền ' + getPlaceholderLabel(match[1]);
            fragment.appendChild(placeholder);
            lastIndex = match.index + match[0].length;
        }

        if (hasPlaceholder) {
            if (lastIndex < text.length) {
                fragment.appendChild(document.createTextNode(text.slice(lastIndex)));
            }
            textNode.replaceWith(fragment);
        }
    });
}

function getPlaceholderLabel(key) {
    return placeholderLabels[key] || key.replace(/([a-z])([A-Z])/g, '$1 $2');
}

function isDatePlaceholder(key) {
    const normalizedKey = key.normalize('NFD').replace(/\p{Diacritic}/gu, '').toLowerCase();
    return /ngay|date|dob|birth|expiry|expire|issuedate/.test(normalizedKey);
}

function formatPlaceholderValue(key, value) {
    if (!isDatePlaceholder(key)) return value;

    const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(value);
    return match ? `${match[3]}/${match[2]}/${match[1]}` : value;
}

function serializeEditorContent(editor) {
    const copy = editor.cloneNode(true);
    copy.querySelectorAll('.template-placeholder').forEach(function (placeholder) {
        placeholder.replaceWith(document.createTextNode(placeholder.dataset.placeholderToken));
    });
    return copy.innerHTML;
}

function replacePlaceholder(key, value) {
    const editor = document.getElementById('editorContent');
    editor.querySelectorAll('.template-placeholder').forEach(function (placeholder) {
        if (placeholder.dataset.placeholderKey === key) {
            placeholder.replaceWith(document.createTextNode(value));
        }
    });
}

// Tự động thay thế ngày hiện tại vào các biến ngày tháng
function autoReplaceDate() {
    const today = new Date();
    const dateStr = today.getDate().toString().padStart(2, '0') + '/' + 
                    (today.getMonth() + 1).toString().padStart(2, '0') + '/' + 
                    today.getFullYear();
    
    ['NgayThang', 'NgayLamDon', 'NgayKy'].forEach(function (key) {
        replacePlaceholder(key, dateStr);
    });
    alert('Đã tự động cập nhật ngày hiện tại (' + dateStr + ') vào văn bản!');
}

// Chèn bảng biểu với số dòng/cột tùy chọn
function insertTable() {
    const rows = prompt("Nhập số dòng:", "3");
    const cols = prompt("Nhập số cột:", "3");
    if (rows && cols) {
        let tableHtml = '<table class="table table-bordered my-2" style="width: 100%; border: 1px solid #dee2e6; border-collapse: collapse;">';
        for (let r = 0; r < parseInt(rows); r++) {
            tableHtml += '<tr>';
            for (let c = 0; c < parseInt(cols); c++) {
                tableHtml += '<td style="border: 1px solid #dee2e6; padding: 8px;">Ô dữ liệu</td>';
            }
            tableHtml += '</tr>';
        }
        tableHtml += '</table><p></p>';
        document.execCommand('insertHTML', false, tableHtml);
    }
}

$(document).ready(function () {
    const docId = $('#documentId').val() || $('#editorContent').data('doc-id');
    if (!docId) return;

    const editor = document.getElementById('editorContent');
    const placeholderModal = document.getElementById('placeholderModal');
    decoratePlaceholders(editor);
    placeholderModal.addEventListener('shown.bs.modal', function () {
        document.getElementById('placeholderValue').focus();
    });

    function openPlaceholderDialog(placeholder) {
        activePlaceholderKey = placeholder.dataset.placeholderKey;
        const label = getPlaceholderLabel(activePlaceholderKey);
        const matchingCount = Array.from(editor.querySelectorAll('.template-placeholder'))
            .filter(function (item) { return item.dataset.placeholderKey === activePlaceholderKey; }).length;
        const isDate = isDatePlaceholder(activePlaceholderKey);

        $('#placeholderModalTitle').text('Điền ' + label);
        $('#placeholderFieldLabel').text(label);
        $('#placeholderValue')
            .attr('type', isDate ? 'date' : 'text')
            .val('')
            .attr('placeholder', isDate ? '' : 'Nhập ' + label.toLowerCase() + '...');
        $('#placeholderHelp').text(
            matchingCount > 1
                ? 'Giá trị sẽ thay thế cả ' + matchingCount + ' vị trí có biến {{' + activePlaceholderKey + '}} trong tài liệu.'
                : 'Giá trị chỉ thay thế biến {{' + activePlaceholderKey + '}} trong tài liệu.'
        );
        bootstrap.Modal.getOrCreateInstance(placeholderModal).show();
    }

    $('#editorContent').on('click', '.template-placeholder', function () {
        openPlaceholderDialog(this);
    });

    $('#editorContent').on('keydown', '.template-placeholder', function (event) {
        if (event.key === 'Enter' || event.key === ' ') {
            event.preventDefault();
            openPlaceholderDialog(this);
        }
    });

    $('#placeholderForm').on('submit', function (event) {
        event.preventDefault();
        const value = $('#placeholderValue').val();
        if (!activePlaceholderKey || !value || !value.trim()) return;

        replacePlaceholder(activePlaceholderKey, formatPlaceholderValue(activePlaceholderKey, value));
        activePlaceholderKey = null;
        bootstrap.Modal.getOrCreateInstance(placeholderModal).hide();
    });

    // Lưu tài liệu thủ công vào PostgreSQL qua API
    $('#btnSaveDoc').click(function () {
        const btn = $(this);
        btn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-1"></span> Đang lưu...');

        const payload = {
            id: parseInt(docId),
            documentTitle: $('#docTitleInput').val().trim(),
            contentHtml: serializeEditorContent(editor),
            status: 'Saved'
        };

        $.ajax({
            url: '/api/documents/' + docId,
            type: 'PUT',
            contentType: 'application/json',
            data: JSON.stringify(payload),
            success: function (res) {
                btn.prop('disabled', false).html('<i class="bi bi-floppy-fill me-1"></i> Lưu Tài Liệu');
                $('#statusBadge').removeClass('bg-secondary-subtle text-secondary').addClass('bg-success-subtle text-success')
                                 .html('<i class="bi bi-check-circle me-1"></i>Đã lưu (' + new Date().toLocaleTimeString() + ')');
            },
            error: function (err) {
                btn.prop('disabled', false).html('<i class="bi bi-floppy-fill me-1"></i> Lưu Tài Liệu');
                alert('Lỗi lưu tài liệu: ' + (err.responseJSON ? err.responseJSON.message : 'Vui lòng kiểm tra kết nối CSDL'));
            }
        });
    });

    // Tự động lưu bản nháp mỗi 60 giây
    setInterval(function () {
        const payload = {
            id: parseInt(docId),
            documentTitle: $('#docTitleInput').val().trim(),
            contentHtml: serializeEditorContent(editor),
            status: 'AutoSaved'
        };
        $.ajax({
            url: '/api/documents/' + docId,
            type: 'PUT',
            contentType: 'application/json',
            data: JSON.stringify(payload),
            success: function () {
                $('#statusBadge').html('<i class="bi bi-cloud-check me-1"></i>Tự động lưu');
            }
        });
    }, 60000);
});
