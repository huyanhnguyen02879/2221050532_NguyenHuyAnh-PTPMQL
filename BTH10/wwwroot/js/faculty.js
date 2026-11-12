$(function () {
    window.initializeAjaxCrud({
        name: "faculty",
        loadError: "Không thể tải danh sách khoa. Vui lòng thử lại.",
        openError: "Không thể mở thông tin khoa. Vui lòng tải lại và thử lại.",
        submitError: "Không thể hoàn tất thao tác. Vui lòng thử lại."
    });
});
