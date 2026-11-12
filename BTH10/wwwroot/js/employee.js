$(function () {
    window.initializeAjaxCrud({
        name: "employee",
        loadError: "Không thể tải danh sách nhân viên. Vui lòng thử lại.",
        openError: "Không thể mở thông tin nhân viên. Vui lòng tải lại và thử lại.",
        submitError: "Không thể hoàn tất thao tác. Vui lòng thử lại."
    });
});
