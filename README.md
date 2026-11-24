# Screen Profile Switcher

Ứng dụng desktop đơn giản cho Windows để lưu và chuyển nhanh các profile **Độ phân giải + Tỷ lệ Scale (DPI)** chỉ với 1 click **Set**.

## Sử dụng nhanh

1. Chạy file `ScreenProfileSwitcher.exe`.
2. Trên danh sách profile có sẵn các mẫu mặc định (`2K - Scale 100%`, `Full HD - Scale 100%`, `2K - Scale 125%`).
3. Click nút **Set** ở profile mong muốn để đổi ngay độ phân giải và scale (không cần đăng xuất).
4. Dùng phần **Thêm Profile mới** bên dưới để lưu thêm cấu hình tùy ý (tự động đọc các độ phân giải và mức scale màn hình hỗ trợ).
5. Các profile được lưu tự động vào file `profiles.json` cùng thư mục với ứng dụng.

## Build lại từ mã nguồn

Yêu cầu: Windows 10/11 (sử dụng sẵn `.NET Framework 4.x` `csc.exe`, không cần cài thêm SDK).

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1
```
