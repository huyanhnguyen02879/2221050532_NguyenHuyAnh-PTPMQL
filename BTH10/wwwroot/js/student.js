$(function () {
    window.initializeAjaxCrud({
        name: "student",
        loadError: "Không thể tải danh sách sinh viên. Vui lòng thử lại.",
        openError: "Không thể mở thông tin sinh viên. Vui lòng tải lại và thử lại.",
        submitError: "Không thể hoàn tất thao tác. Vui lòng thử lại."
    });
});