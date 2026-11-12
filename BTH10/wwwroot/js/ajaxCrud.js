(function (window, $) {
    window.initializeAjaxCrud = function (options) {
        const name = options.name;
        const $tableContainer = $(`#${name}-table-container`);
        const $searchForm = $(`#${name}-search-form`);
        const $searchInput = $(`#${name}-search`);
        const $modal = $(`#${name}-modal`);
        const $modalContent = $(`#${name}-modal-content`);
        const $listError = $(`#${name}-list-error`);
        const modal = bootstrap.Modal.getOrCreateInstance($modal[0]);
        let searchTimer;
        let latestTableRequest = 0;

        function showListError(message) {
            $listError.text(message).removeClass("d-none");
        }

        function loadTable(page) {
            const requestId = ++latestTableRequest;
            $listError.addClass("d-none").empty();

            return $.ajax({
                url: $searchForm.attr("action"),
                method: "GET",
                data: {
                    searchTerm: $searchInput.val(),
                    page: page
                },
                headers: { "X-Requested-With": "XMLHttpRequest" }
            }).done(function (html) {
                if (requestId === latestTableRequest) {
                    $tableContainer.html(html);
                }
            }).fail(function () {
                if (requestId === latestTableRequest) {
                    showListError(options.loadError);
                }
            });
        }

        function parseModalValidation() {
            $modalContent.find("form").each(function () {
                $(this).removeData("validator").removeData("unobtrusiveValidation");
                $.validator.unobtrusive.parse(this);
            });
        }

        function showModalError() {
            $modalContent.find(".modal-body").prepend(
                $('<div class="alert alert-danger js-modal-error" role="alert"></div>')
                    .text(options.submitError)
            );
        }

        $(document).on("click", `.js-open-${name}-modal`, function () {
            $.ajax({
                url: $(this).data("url"),
                method: "GET",
                headers: { "X-Requested-With": "XMLHttpRequest" }
            }).done(function (html) {
                $modalContent.html(html);
                parseModalValidation();
                modal.show();
            }).fail(function () {
                showListError(options.openError);
            });
        });

        $searchForm.on("submit", function (event) {
            event.preventDefault();
            loadTable(1);
        });

        $searchInput.on("input", function () {
            window.clearTimeout(searchTimer);
            searchTimer = window.setTimeout(function () {
                loadTable(1);
            }, 300);
        });

        $(`#${name}-search-clear`).on("click", function () {
            $searchInput.val("");
            loadTable(1);
        });

        $tableContainer.on("click", `.js-${name}-page:not(:disabled)`, function () {
            loadTable(Number($(this).data("page")));
        });

        $(document).on("submit", `#${name}-modal-content .js-${name}-form`, function (event) {
            event.preventDefault();

            const $form = $(this);
            if (!$form.valid()) {
                return;
            }

            const operation = $form.data("operation");
            $.ajax({
                url: $form.attr("action"),
                method: "POST",
                data: $form.serialize(),
                headers: { "X-Requested-With": "XMLHttpRequest" }
            }).done(function (response, _status, xhr) {
                const contentType = xhr.getResponseHeader("content-type") || "";
                if (contentType.indexOf("application/json") !== -1 && response.success) {
                    const currentPage = Number($tableContainer.children().first().data("current-page")) || 1;
                    modal.hide();
                    loadTable(operation === "create" ? 1 : currentPage);
                    return;
                }

                $modalContent.html(response);
                parseModalValidation();
            }).fail(function (xhr) {
                const contentType = xhr.getResponseHeader("content-type") || "";
                if (contentType.indexOf("text/html") !== -1 && xhr.responseText) {
                    $modalContent.html(xhr.responseText);
                    parseModalValidation();
                    return;
                }

                showModalError();
            });
        });

        $modal.on("hidden.bs.modal", function () {
            $modalContent.empty();
        });
    };
})(window, jQuery);
