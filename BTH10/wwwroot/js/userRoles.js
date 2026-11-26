(function (window, $) {
    const $modal = $("#user-role-modal");
    const $modalContent = $("#user-role-modal-content");
    const modal = bootstrap.Modal.getOrCreateInstance($modal[0]);

    $(document).on("click", ".js-open-user-role-modal", function () {
        $.ajax({
            url: $(this).data("url"),
            method: "GET",
            headers: { "X-Requested-With": "XMLHttpRequest" }
        }).done(function (html) {
            $modalContent.html(html);
            modal.show();
        }).fail(function (xhr) {
            alert(xhr.status === 403
                ? "Bạn không có quyền cập nhật Role."
                : "Không thể tải danh sách Role.");
        });
    });

    $(document).on("submit", ".js-user-role-form", function (event) {
        event.preventDefault();
        const $form = $(this);

        $.ajax({
            url: $form.attr("action"),
            method: "POST",
            data: $form.serialize(),
            headers: { "X-Requested-With": "XMLHttpRequest" }
        }).done(function (response, _status, xhr) {
            const contentType = xhr.getResponseHeader("content-type") || "";
            if (contentType.indexOf("application/json") !== -1 && response.success) {
                modal.hide();
                window.location.reload();
                return;
            }

            $modalContent.html(response);
        }).fail(function (xhr) {
            alert(xhr.status === 403
                ? "Bạn không có quyền cập nhật Role."
                : "Không thể cập nhật Role.");
        });
    });

    $modal.on("hidden.bs.modal", function () {
        $modalContent.empty();
    });
})(window, jQuery);
