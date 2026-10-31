$.ajaxPrefilter(function (options, originalOptions, jqXHR) {
    const token = $('input[name="__RequestVerificationToken"]').first().val();
    const method = (options.type || 'GET').toUpperCase();
    if (token && !['GET', 'HEAD', 'OPTIONS', 'TRACE'].includes(method)) {
        jqXHR.setRequestHeader('X-CSRF-TOKEN', token);
    }
});
