$(function () {
    window.initializeAjaxCrud({
        name: "lecturer",
        loadError: "Không thể tải danh sách giảng viên. Vui lòng thử lại.",
        openError: "Không thể mở thông tin giảng viên. Vui lòng tải lại và thử lại.",
        submitError: "Không thể hoàn tất thao tác. Vui lòng thử lại."
    });
});
