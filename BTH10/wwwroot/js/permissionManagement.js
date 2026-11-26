(function (window, $) {
    $(document).on("change", ".js-permission-toggle", function () {
        const $toggle = $(this);
        const granted = $toggle.is(":checked");

        $.ajax({
            url: "/PermissionManagement/Update",
            method: "POST",
            data: {
                roleId: $toggle.data("role-id"),
                permissionId: $toggle.data("permission-id"),
                granted: granted,
                __RequestVerificationToken: $("input[name='__RequestVerificationToken']").val()
            },
            headers: { "X-Requested-With": "XMLHttpRequest" }
        }).fail(function () {
            $toggle.prop("checked", !granted);
            $("#permission-error")
                .text("Không thể cập nhật phân quyền.")
                .removeClass("d-none");
        }).done(function () {
            $("#permission-error").addClass("d-none").empty();
        });
    });
})(window, jQuery);
